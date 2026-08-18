using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.API.Helpers;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComprasController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
        [FromQuery] EstadoCompra? estado, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var q = db.Compras.Include(c => c.Proveedor).AsQueryable();
        if (desde.HasValue) q = q.Where(c => c.Fecha >= desde.Value.ToUtc());
        if (hasta.HasValue) q = q.Where(c => c.Fecha <= hasta.Value.ToUtc().AddDays(1));
        if (estado.HasValue) q = q.Where(c => c.Estado == estado);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(c => c.Fecha).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var compra = await db.Compras
            .Include(c => c.Proveedor)
            .Include(c => c.Detalles).ThenInclude(d => d.Articulo)
            .FirstOrDefaultAsync(c => c.Id == id);
        return compra is null ? NotFound() : Ok(compra);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] Compra compra)
    {
        compra.Fecha = DateTime.UtcNow;
        compra.Estado = EstadoCompra.Pendiente;
        foreach (var det in compra.Detalles)
        {
            det.PrecioCostoNeto = det.PrecioCosto * (1 - det.Bonificacion / 100);
            det.SubTotal = det.Cantidad * det.PrecioCostoNeto * (1 + det.AlicuotaIva / 100);
        }
        compra.SubTotal = compra.Detalles.Sum(d => d.Cantidad * d.PrecioCostoNeto);
        compra.TotalIva = compra.Detalles.Sum(d => d.Cantidad * d.PrecioCostoNeto * (d.AlicuotaIva / 100));
        compra.Total = compra.SubTotal + compra.TotalIva;

        // Estimacion provisoria del vencimiento de pago (se recalcula al recibirla).
        var proveedor = await db.Proveedores.FindAsync(compra.IdProveedor);
        compra.FechaVencimiento = compra.Fecha.AddDays(proveedor?.DiasVencimientoPago ?? 0);

        db.Compras.Add(compra);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = compra.Id }, compra);
    }

    [HttpPost("{id}/recibir")]
    public async Task<IActionResult> Recibir(long id)
    {
        var compra = await db.Compras.Include(c => c.Detalles).FirstOrDefaultAsync(c => c.Id == id);
        if (compra is null) return NotFound();
        if (compra.Estado == EstadoCompra.Recibida) return BadRequest("Ya fue recibida");

        var idDestino = await StockSucursalHelper.ObtenerIdSucursalCentralAsync(db) ?? 1;

        foreach (var det in compra.Detalles)
        {
            var art = await db.Articulos.FindAsync(det.IdArticulo);
            if (art is null) continue;
            await StockSucursalHelper.AplicarMovimientoAsync(db, det.IdArticulo, idDestino, det.Cantidad);
            if (det.ActualizaPrecio)
            {
                art.PrecioCosto = det.PrecioCostoNeto;
                // Recalcular precio de venta manteniendo el margen
                if (art.MargenGanancia > 0)
                    art.PrecioVenta = Math.Round(det.PrecioCostoNeto * (1 + art.AlicuotaIva / 100) * (1 + art.MargenGanancia / 100), 2);
            }
        }

        compra.Estado = EstadoCompra.Recibida;

        var proveedor = await db.Proveedores.FindAsync(compra.IdProveedor);
        if (proveedor != null)
            compra.FechaVencimiento = DateTime.UtcNow.AddDays(proveedor.DiasVencimientoPago);

        if (proveedor != null && compra.Total > 0)
        {
            proveedor.SaldoCtaCte += compra.Total;
            db.MovimientosCtaCteProveedor.Add(new MovimientoCtaCteProveedor
            {
                IdProveedor = compra.IdProveedor,
                Fecha = DateTime.UtcNow,
                Tipo = TipoMovimientoCteProveedor.CompraCredito,
                Concepto = $"Compra {compra.LetraFactura}{compra.NumeroFactura}".Trim(),
                IdCompra = compra.Id,
                Debe = compra.Total,
                Haber = 0,
                SaldoAcumulado = proveedor.SaldoCtaCte
            });
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Compras recibidas y pendientes de pago, ordenadas por fecha de vencimiento.</summary>
    [HttpGet("calendario-pagos")]
    public async Task<IActionResult> CalendarioPagos([FromQuery] int? idProveedor)
    {
        var q = db.Compras
            .Include(c => c.Proveedor)
            .Where(c => c.Estado == EstadoCompra.Recibida && !c.Pagada);
        if (idProveedor.HasValue) q = q.Where(c => c.IdProveedor == idProveedor.Value);

        var hoy = DateTime.UtcNow.Date;
        var items = await q.OrderBy(c => c.FechaVencimiento).Select(c => new
        {
            c.Id,
            c.NumeroFactura,
            c.LetraFactura,
            c.Fecha,
            c.FechaVencimiento,
            c.Total,
            IdProveedor = c.IdProveedor,
            Proveedor = c.Proveedor!.RazonSocial,
        }).ToListAsync();

        var resultado = items.Select(c => new
        {
            c.Id,
            c.NumeroFactura,
            c.LetraFactura,
            c.Fecha,
            c.FechaVencimiento,
            c.Total,
            c.IdProveedor,
            c.Proveedor,
            DiasParaVencer = c.FechaVencimiento.HasValue ? (c.FechaVencimiento.Value.Date - hoy).Days : (int?)null,
            Vencida = c.FechaVencimiento.HasValue && c.FechaVencimiento.Value.Date < hoy
        });

        return Ok(resultado);
    }

    [HttpPost("{id}/anular")]
    public async Task<IActionResult> Anular(long id)
    {
        var compra = await db.Compras.FindAsync(id);
        if (compra is null) return NotFound();
        if (compra.Estado == EstadoCompra.Recibida) return BadRequest("No se puede anular una compra ya recibida");
        compra.Estado = EstadoCompra.Anulada;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
