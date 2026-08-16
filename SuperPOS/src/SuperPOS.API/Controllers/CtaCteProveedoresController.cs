using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.API.Helpers;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/ctacte-proveedores")]
public class CtaCteProveedoresController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet("proveedores")]
    public async Task<IActionResult> GetProveedoresConSaldo([FromQuery] bool? soloConDeuda)
    {
        var q = db.Proveedores.Where(p => p.Activo);
        if (soloConDeuda == true) q = q.Where(p => p.SaldoCtaCte > 0);
        var proveedores = await q.OrderBy(p => p.RazonSocial).Select(p => new
        {
            p.Id, p.RazonSocial, p.NombreFantasia, p.Cuit, p.Telefono,
            p.SaldoCtaCte, p.DiasVencimientoPago
        }).ToListAsync();
        return Ok(proveedores);
    }

    [HttpGet("movimientos/{idProveedor}")]
    public async Task<IActionResult> GetMovimientos(int idProveedor,
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var q = db.MovimientosCtaCteProveedor.Where(m => m.IdProveedor == idProveedor).AsQueryable();
        if (desde.HasValue) q = q.Where(m => m.Fecha >= desde.Value.ToUtc());
        if (hasta.HasValue) q = q.Where(m => m.Fecha <= hasta.Value.ToUtc().AddDays(1));
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(m => m.Fecha).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, items });
    }

    [HttpPost("pago")]
    public async Task<IActionResult> RegistrarPago([FromBody] PagoCtaCteProveedorRequest req)
    {
        var proveedor = await db.Proveedores.FindAsync(req.IdProveedor);
        if (proveedor is null) return NotFound("Proveedor no encontrado");

        proveedor.SaldoCtaCte -= req.Monto;
        if (proveedor.SaldoCtaCte < 0) proveedor.SaldoCtaCte = 0;

        var mov = new MovimientoCtaCteProveedor
        {
            IdProveedor = req.IdProveedor,
            Fecha = DateTime.UtcNow,
            Tipo = TipoMovimientoCteProveedor.Pago,
            Concepto = string.IsNullOrWhiteSpace(req.Concepto) ? "Pago a proveedor" : req.Concepto,
            Haber = req.Monto,
            Debe = 0,
            SaldoAcumulado = proveedor.SaldoCtaCte,
            IdUsuario = req.IdUsuario
        };

        db.MovimientosCtaCteProveedor.Add(mov);
        await db.SaveChangesAsync();
        return Ok(new { nuevoSaldo = proveedor.SaldoCtaCte });
    }

    [HttpPost("ajuste")]
    public async Task<IActionResult> AjusteManual([FromBody] AjusteCtaCteProveedorRequest req)
    {
        var proveedor = await db.Proveedores.FindAsync(req.IdProveedor);
        if (proveedor is null) return NotFound("Proveedor no encontrado");

        if (req.EsDebito)
            proveedor.SaldoCtaCte += req.Monto;
        else
            proveedor.SaldoCtaCte -= req.Monto;
        if (proveedor.SaldoCtaCte < 0) proveedor.SaldoCtaCte = 0;

        var mov = new MovimientoCtaCteProveedor
        {
            IdProveedor = req.IdProveedor,
            Fecha = DateTime.UtcNow,
            Tipo = req.EsDebito ? TipoMovimientoCteProveedor.NotaDebito : TipoMovimientoCteProveedor.NotaCredito,
            Concepto = req.Concepto ?? "Ajuste manual",
            Debe = req.EsDebito ? req.Monto : 0,
            Haber = req.EsDebito ? 0 : req.Monto,
            SaldoAcumulado = proveedor.SaldoCtaCte,
            IdUsuario = req.IdUsuario
        };

        db.MovimientosCtaCteProveedor.Add(mov);
        await db.SaveChangesAsync();
        return Ok(new { nuevoSaldo = proveedor.SaldoCtaCte });
    }
}

public record PagoCtaCteProveedorRequest(int IdProveedor, decimal Monto, string? Concepto, int? IdUsuario);
public record AjusteCtaCteProveedorRequest(int IdProveedor, decimal Monto, bool EsDebito, string? Concepto, int? IdUsuario);
