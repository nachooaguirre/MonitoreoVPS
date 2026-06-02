using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.API.Helpers;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransferenciasInternasController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] EstadoTransferenciaInterna? estado)
    {
        var q = db.TransferenciasInternas
            .Include(t => t.SucursalOrigen)
            .Include(t => t.SucursalDestino)
            .AsQueryable();
        if (estado.HasValue) q = q.Where(t => t.Estado == estado.Value);
        var items = await q.OrderByDescending(t => t.Fecha).Take(200).ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var t = await db.TransferenciasInternas
            .Include(x => x.SucursalOrigen)
            .Include(x => x.SucursalDestino)
            .Include(x => x.Detalles).ThenInclude(d => d.Articulo)
            .FirstOrDefaultAsync(x => x.Id == id);
        return t is null ? NotFound() : Ok(t);
    }

    /// <summary>Crea una transferencia pendiente entre sucursales (sin mover stock hasta confirmar).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TransferenciaInterna tr)
    {
        if (tr.IdSucursalOrigen == tr.IdSucursalDestino)
            return BadRequest("Origen y destino deben ser distintos.");
        if (tr.Detalles == null || tr.Detalles.Count == 0)
            return BadRequest("Agregue al menos un artículo.");

        var nro = (await db.TransferenciasInternas.MaxAsync(t => (int?)t.NroTransferencia) ?? 0) + 1;
        tr.NroTransferencia = nro;
        tr.Fecha = DateTime.UtcNow;
        tr.Estado = EstadoTransferenciaInterna.Pendiente;
        foreach (var d in tr.Detalles)
        {
            d.Id = 0;
            d.IdTransferencia = 0;
        }

        db.TransferenciasInternas.Add(tr);
        await db.SaveChangesAsync();
        return Ok(new { tr.Id, tr.NroTransferencia, tr.Estado });
    }

    /// <summary>Descuenta stock en origen y suma en destino. Valida saldo en origen.</summary>
    [HttpPut("{id:int}/confirmar")]
    public async Task<IActionResult> Confirmar(int id)
    {
        var tr = await db.TransferenciasInternas.Include(t => t.Detalles).FirstOrDefaultAsync(t => t.Id == id);
        if (tr is null) return NotFound();
        if (tr.Estado != EstadoTransferenciaInterna.Pendiente)
            return BadRequest("Solo se pueden confirmar transferencias pendientes.");

        foreach (var d in tr.Detalles)
        {
            var disp = await StockSucursalHelper.ObtenerCantidadAsync(db, d.IdArticulo, tr.IdSucursalOrigen);
            if (disp < d.Cantidad)
                return BadRequest($"Stock insuficiente en origen para artículo {d.IdArticulo} (disponible {disp}, pedido {d.Cantidad}).");
        }

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            foreach (var d in tr.Detalles)
            {
                await StockSucursalHelper.AplicarMovimientoAsync(db, d.IdArticulo, tr.IdSucursalOrigen, -d.Cantidad);
                await StockSucursalHelper.AplicarMovimientoAsync(db, d.IdArticulo, tr.IdSucursalDestino, d.Cantidad);
            }

            tr.Estado = EstadoTransferenciaInterna.Confirmada;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new { tr.Id, tr.NroTransferencia, tr.Estado });
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync();
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:int}/anular")]
    public async Task<IActionResult> Anular(int id)
    {
        var tr = await db.TransferenciasInternas.FindAsync(id);
        if (tr is null) return NotFound();
        if (tr.Estado != EstadoTransferenciaInterna.Pendiente)
            return BadRequest("Solo se pueden anular transferencias pendientes.");
        tr.Estado = EstadoTransferenciaInterna.Anulada;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
