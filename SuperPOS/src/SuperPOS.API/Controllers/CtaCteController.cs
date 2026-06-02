using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.API.Helpers;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CtaCteController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet("clientes")]
    public async Task<IActionResult> GetClientesConSaldo([FromQuery] bool? soloDeudores)
    {
        var q = db.Clientes.Where(c => c.TieneCtaCte);
        if (soloDeudores == true) q = q.Where(c => c.SaldoCtaCte > 0);
        var clientes = await q.OrderBy(c => c.RazonSocial).Select(c => new
        {
            c.Id, c.RazonSocial, c.NombreFantasia, c.Cuit, c.Telefono,
            c.SaldoCtaCte, c.LimiteCredito, c.EsMoroso
        }).ToListAsync();
        return Ok(clientes);
    }

    [HttpGet("movimientos/{idCliente}")]
    public async Task<IActionResult> GetMovimientos(int idCliente,
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var q = db.MovimientosCtaCte.Where(m => m.IdCliente == idCliente).AsQueryable();
        if (desde.HasValue) q = q.Where(m => m.Fecha >= desde.Value.ToUtc());
        if (hasta.HasValue) q = q.Where(m => m.Fecha <= hasta.Value.ToUtc().AddDays(1));
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(m => m.Fecha).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, items });
    }

    [HttpPost("pago")]
    public async Task<IActionResult> RegistrarPago([FromBody] PagoCtaCteRequest req)
    {
        var cliente = await db.Clientes.FindAsync(req.IdCliente);
        if (cliente is null) return NotFound("Cliente no encontrado");

        cliente.SaldoCtaCte -= req.Monto;
        if (cliente.SaldoCtaCte < 0) cliente.SaldoCtaCte = 0;
        cliente.EsMoroso = cliente.SaldoCtaCte > 0 && DateTime.UtcNow > (cliente.FechaVtoCtaCte ?? DateTime.MaxValue);

        var mov = new MovimientoCtaCte
        {
            IdCliente = req.IdCliente,
            Fecha = DateTime.UtcNow,
            Tipo = TipoMovimientoCte.Pago,
            Concepto = string.IsNullOrWhiteSpace(req.Concepto) ? "Pago de cuenta corriente" : req.Concepto,
            Haber = req.Monto,
            Debe = 0,
            SaldoAcumulado = cliente.SaldoCtaCte,
            IdUsuario = req.IdUsuario
        };

        db.MovimientosCtaCte.Add(mov);
        await db.SaveChangesAsync();
        return Ok(new { nuevoSaldo = cliente.SaldoCtaCte });
    }

    [HttpPost("ajuste")]
    public async Task<IActionResult> AjusteManual([FromBody] AjusteCtaCteRequest req)
    {
        var cliente = await db.Clientes.FindAsync(req.IdCliente);
        if (cliente is null) return NotFound("Cliente no encontrado");

        if (req.EsDebito)
            cliente.SaldoCtaCte += req.Monto;
        else
            cliente.SaldoCtaCte -= req.Monto;

        var mov = new MovimientoCtaCte
        {
            IdCliente = req.IdCliente,
            Fecha = DateTime.UtcNow,
            Tipo = req.EsDebito ? TipoMovimientoCte.NotaDebito : TipoMovimientoCte.NotaCredito,
            Concepto = req.Concepto ?? "Ajuste manual",
            Debe = req.EsDebito ? req.Monto : 0,
            Haber = req.EsDebito ? 0 : req.Monto,
            SaldoAcumulado = cliente.SaldoCtaCte,
            IdUsuario = req.IdUsuario
        };

        db.MovimientosCtaCte.Add(mov);
        await db.SaveChangesAsync();
        return Ok(new { nuevoSaldo = cliente.SaldoCtaCte });
    }
}

public record PagoCtaCteRequest(int IdCliente, decimal Monto, string? Concepto, int? IdUsuario);
public record AjusteCtaCteRequest(int IdCliente, decimal Monto, bool EsDebito, string? Concepto, int? IdUsuario);
