using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrazabilidadController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet("articulos/{idArticulo:int}")]
    public async Task<IActionResult> GetPorArticulo(
        int idArticulo,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int take = 200)
    {
        var q = db.TrazabilidadEventos.AsNoTracking().Where(e => e.IdArticulo == idArticulo);
        if (desde.HasValue) q = q.Where(e => e.Fecha >= desde.Value.ToUniversalTime());
        if (hasta.HasValue) q = q.Where(e => e.Fecha <= hasta.Value.ToUniversalTime());

        var items = await q
            .OrderByDescending(e => e.Fecha)
            .Take(Math.Clamp(take, 1, 1000))
            .Select(e => new
            {
                e.Id,
                e.Fecha,
                e.IdArticulo,
                e.Cantidad,
                e.Tipo,
                e.Ubicacion,
                e.IdUsuario,
                e.IdRemito,
                e.IdComprobante,
                e.LoteNro,
                e.NroSerie,
                e.FechaVencimiento,
                e.Observaciones
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("codigo/{codigoBarras}")]
    public async Task<IActionResult> GetPorCodigoBarras(string codigoBarras, [FromQuery] int take = 200)
    {
        var artId = await db.Articulos
            .Where(a => a.CodigoBarras == codigoBarras)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync();
        if (artId is null) return NotFound("Artículo no encontrado por código de barras.");
        return await GetPorArticulo(artId.Value, null, null, take);
    }

    [HttpPost("eventos")]
    public async Task<IActionResult> CrearEvento([FromBody] CrearTrazabilidadEventoRequest req)
    {
        var artExists = await db.Articulos.AnyAsync(a => a.Id == req.IdArticulo);
        if (!artExists) return BadRequest("IdArticulo inválido.");

        var ev = new TrazabilidadEvento
        {
            Fecha = DateTime.UtcNow,
            IdArticulo = req.IdArticulo,
            Cantidad = req.Cantidad,
            Tipo = req.Tipo,
            Ubicacion = req.Ubicacion,
            IdUsuario = req.IdUsuario > 0 ? req.IdUsuario : null,
            LoteNro = req.LoteNro,
            NroSerie = req.NroSerie,
            FechaVencimiento = req.FechaVencimiento?.ToUniversalTime(),
            Observaciones = req.Observaciones
        };

        db.TrazabilidadEventos.Add(ev);
        await db.SaveChangesAsync();
        return Ok(new { ev.Id, ev.Fecha });
    }
}

public class CrearTrazabilidadEventoRequest
{
    public int IdArticulo { get; set; }
    public decimal Cantidad { get; set; }
    public TipoTrazabilidadEvento Tipo { get; set; }
    public string? Ubicacion { get; set; }
    public int IdUsuario { get; set; }
    public string? LoteNro { get; set; }
    public string? NroSerie { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? Observaciones { get; set; }
}

