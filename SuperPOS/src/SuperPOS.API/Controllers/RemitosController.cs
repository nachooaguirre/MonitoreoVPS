using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.API.Helpers;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RemitosController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] TipoRemito? tipo,
        [FromQuery] EstadoRemito? estado,
        [FromQuery] int? idProveedor,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var q = db.Remitos.Include(r => r.Proveedor).Include(r => r.Cliente).AsQueryable();
        if (tipo.HasValue) q = q.Where(r => r.Tipo == tipo.Value);
        if (estado.HasValue) q = q.Where(r => r.Estado == estado.Value);
        if (idProveedor.HasValue) q = q.Where(r => r.IdProveedor == idProveedor.Value);
        if (desde.HasValue) q = q.Where(r => r.Fecha >= desde.Value.ToUtc());
        if (hasta.HasValue) q = q.Where(r => r.Fecha <= hasta.Value.ToUtc().AddDays(1));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(r => r.Fecha)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.NroRemito,
                r.Fecha,
                r.Tipo,
                r.Estado,
                r.NroRemitoExterno,
                r.Transportista,
                r.IdOrdenCompra,
                r.IdCompra,
                ProveedorNombre = r.Proveedor != null ? r.Proveedor.RazonSocial : null,
                ClienteNombre   = r.Cliente  != null ? r.Cliente.RazonSocial  : null,
                CantArticulos   = r.Detalles.Count
            })
            .ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var r = await db.Remitos
            .Include(x => x.Proveedor)
            .Include(x => x.Cliente)
            .Include(x => x.Detalles).ThenInclude(d => d.Articulo)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (r is null) return NotFound();

        return Ok(new
        {
            r.Id,
            r.NroRemito,
            r.Fecha,
            r.Tipo,
            r.Estado,
            r.NroRemitoExterno,
            r.Transportista,
            r.Observaciones,
            r.IdOrdenCompra,
            r.IdCompra,
            r.IdUsuario,
            ProveedorNombre = r.Proveedor?.RazonSocial,
            ClienteNombre   = r.Cliente?.RazonSocial,
            Detalles = r.Detalles.Select(d => new
            {
                d.Id,
                d.IdArticulo,
                CodigoBarras     = d.Articulo?.CodigoBarras,
                Descripcion      = d.Articulo?.Descripcion,
                d.CantidadRemitida,
                d.CantidadRecibida,
                d.PrecioCosto,
                d.LoteNro,
                d.NroSerie,
                d.FechaVencimiento,
                d.Observaciones
            }).ToList()
        });
    }

    /// <summary>
    /// Crea un remito de entrada desde una Orden de Compra (recepción de pedido de proveedor)
    /// </summary>
    [HttpPost("desde-oc/{idOC}")]
    public async Task<IActionResult> CrearDesdeOC(int idOC, [FromBody] CrearRemitoRequest req)
    {
        var oc = await db.OrdenesCompra
            .Include(o => o.Detalles).ThenInclude(d => d.Articulo)
            .FirstOrDefaultAsync(o => o.Id == idOC);
        if (oc is null) return NotFound("Orden de compra no encontrada.");

        var nro = (await db.Remitos.Where(r => r.Tipo == TipoRemito.Entrada).MaxAsync(r => (int?)r.NroRemito) ?? 0) + 1;

        var remito = new Remito
        {
            NroRemito = nro,
            Tipo = TipoRemito.Entrada,
            Fecha = DateTime.UtcNow,
            IdProveedor = oc.IdProveedor,
            IdOrdenCompra = idOC,
            IdUsuario = req.IdUsuario,
            NroRemitoExterno = req.NroRemitoExterno,
            Transportista = req.Transportista,
            Observaciones = req.Observaciones,
            Estado = EstadoRemito.Pendiente,
            Detalles = oc.Detalles.Select(d => new RemitoDetalle
            {
                IdArticulo = d.IdArticulo,
                CantidadRemitida = d.CantidadPedida,
                CantidadRecibida = 0,
                PrecioCosto = d.PrecioCosto
            }).ToList()
        };

        db.Remitos.Add(remito);
        await db.SaveChangesAsync();
        return Ok(new
        {
            remito.Id,
            remito.NroRemito,
            remito.Fecha,
            remito.Estado,
            remito.IdOrdenCompra,
            CantArticulos = remito.Detalles.Count
        });
    }

    /// <summary>
    /// Crea un remito manual (sin OC previa)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] Remito remito)
    {
        var nro = (await db.Remitos.Where(r => r.Tipo == remito.Tipo).MaxAsync(r => (int?)r.NroRemito) ?? 0) + 1;
        remito.NroRemito = nro;
        remito.Fecha = DateTime.UtcNow;
        remito.Estado = EstadoRemito.Pendiente;
        db.Remitos.Add(remito);
        await db.SaveChangesAsync();
        return Ok(new
        {
            remito.Id,
            remito.NroRemito,
            remito.Fecha,
            remito.Tipo,
            remito.Estado,
            remito.IdProveedor,
            CantArticulos = remito.Detalles.Count
        });
    }

    /// <summary>
    /// Confirma la recepción del remito: actualiza cantidades recibidas y stock de artículos
    /// </summary>
    [HttpPut("{id}/confirmar")]
    public async Task<IActionResult> Confirmar(int id, [FromBody] ConfirmarRemitoRequest req)
    {
        var remito = await db.Remitos.Include(r => r.Detalles).FirstOrDefaultAsync(r => r.Id == id);
        if (remito is null) return NotFound();
        if (remito.Estado == EstadoRemito.Confirmado) return BadRequest("El remito ya fue confirmado.");

        var idDestino = req.IdSucursalDestino
            ?? await StockSucursalHelper.ObtenerIdSucursalCentralAsync(db)
            ?? 1;

        var eventos = new List<TrazabilidadEvento>();
        foreach (var item in req.Items)
        {
            var det = remito.Detalles.FirstOrDefault(d => d.IdArticulo == item.IdArticulo);
            if (det is null) continue;
            det.CantidadRecibida = item.CantidadRecibida;
            det.LoteNro = item.LoteNro;
            det.NroSerie = item.NroSerie;
            det.FechaVencimiento = item.FechaVencimiento?.ToUniversalTime();

            if (remito.Tipo == TipoRemito.Entrada)
            {
                if (item.CantidadRecibida != 0)
                    await StockSucursalHelper.AplicarMovimientoAsync(db, item.IdArticulo, idDestino, item.CantidadRecibida);

                var art = await db.Articulos.FindAsync(item.IdArticulo);
                if (art != null && item.PrecioCosto > 0)
                {
                    art.PrecioCosto = item.PrecioCosto;
                    art.FechaModificacion = DateTime.UtcNow;
                }

                if (item.CantidadRecibida != 0)
                {
                    var sucNombre = await db.Sucursales.Where(s => s.Id == idDestino).Select(s => s.Nombre).FirstOrDefaultAsync();
                    eventos.Add(new TrazabilidadEvento
                    {
                        Fecha = DateTime.UtcNow,
                        IdArticulo = item.IdArticulo,
                        Cantidad = item.CantidadRecibida,
                        Tipo = TipoTrazabilidadEvento.RecepcionDeposito,
                        Ubicacion = sucNombre ?? $"Sucursal {idDestino}",
                        IdUsuario = req.IdUsuario > 0 ? req.IdUsuario : null,
                        IdRemito = remito.Id,
                        IdRemitoDetalle = det.Id,
                        LoteNro = det.LoteNro,
                        NroSerie = det.NroSerie,
                        FechaVencimiento = det.FechaVencimiento
                    });
                }
            }
        }

        remito.Estado = EstadoRemito.Confirmado;

        // Si tiene OC asociada, actualizar estado de la OC
        if (remito.IdOrdenCompra.HasValue)
        {
            var oc = await db.OrdenesCompra.Include(o => o.Detalles).FirstOrDefaultAsync(o => o.Id == remito.IdOrdenCompra.Value);
            if (oc != null)
            {
                foreach (var det in remito.Detalles)
                {
                    var ocDet = oc.Detalles.FirstOrDefault(d => d.IdArticulo == det.IdArticulo);
                    if (ocDet != null) ocDet.CantidadRecibida += det.CantidadRecibida;
                }
                oc.Estado = oc.Detalles.All(d => d.CantidadRecibida >= d.CantidadPedida)
                    ? EstadoOrdenCompra.Recibida : EstadoOrdenCompra.RecepcionParcial;
                oc.FechaRecepcion = DateTime.UtcNow;
            }
        }

        if (eventos.Count > 0) db.TrazabilidadEventos.AddRange(eventos);
        await db.SaveChangesAsync();
        return Ok(new { remito.Id, remito.NroRemito, remito.Estado, CantArticulos = remito.Detalles.Count });
    }

    [HttpPut("{id}/anular")]
    public async Task<IActionResult> Anular(int id)
    {
        var r = await db.Remitos.FindAsync(id);
        if (r is null) return NotFound();
        if (r.Estado == EstadoRemito.Confirmado) return BadRequest("No se puede anular un remito confirmado.");
        r.Estado = EstadoRemito.Anulado;
        await db.SaveChangesAsync();
        return Ok(r);
    }
}

public class CrearRemitoRequest
{
    public int IdUsuario { get; set; }
    public string? NroRemitoExterno { get; set; }
    public string? Transportista { get; set; }
    public string? Observaciones { get; set; }
}

public class ConfirmarRemitoRequest
{
    public int IdUsuario { get; set; }
    /// <summary>Sucursal donde ingresa la mercadería. Si no se envía, se usa la marcada como central (EsCentral).</summary>
    public int? IdSucursalDestino { get; set; }
    public List<ItemRecepcionRemito> Items { get; set; } = [];
}

public class ItemRecepcionRemito
{
    public int IdArticulo { get; set; }
    public decimal CantidadRecibida { get; set; }
    public decimal PrecioCosto { get; set; }
    public string? LoteNro { get; set; }
    public string? NroSerie { get; set; }
    public DateTime? FechaVencimiento { get; set; }
}
