using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.API.Helpers;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CotizacionesController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? desde, 
        [FromQuery] DateTime? hasta, 
        [FromQuery] int? idProveedor,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50)
    {
        var q = db.Cotizaciones
            .Include(c => c.Proveedor)
            .AsQueryable();

        if (desde.HasValue) q = q.Where(c => c.Fecha >= desde.Value.ToUtc());
        if (hasta.HasValue) q = q.Where(c => c.Fecha <= hasta.Value.ToUtc().AddDays(1));
        if (idProveedor.HasValue) q = q.Where(c => c.IdProveedor == idProveedor.Value);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(c => c.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var cot = await db.Cotizaciones
            .Include(c => c.Proveedor)
            .Include(c => c.Detalles).ThenInclude(d => d.Articulo)
            .FirstOrDefaultAsync(c => c.Id == id);

        return cot is null ? NotFound() : Ok(cot);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Cotizacion cot)
    {
        cot.Fecha = DateTime.UtcNow;

        // Calcular número correlativo
        var ultimo = await db.Cotizaciones
            .MaxAsync(c => (long?)c.Numero) ?? 0;
        cot.Numero = ultimo + 1;

        db.Cotizaciones.Add(cot);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = cot.Id }, cot);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] Cotizacion cot)
    {
        if (id != cot.Id) return BadRequest();

        var existente = await db.Cotizaciones
            .Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (existente == null) return NotFound();

        // Actualizar propiedades básicas
        existente.IdProveedor = cot.IdProveedor;
        existente.Descripcion = cot.Descripcion;
        existente.Observacion = cot.Observacion;
        existente.PlazoEntrega = cot.PlazoEntrega;

        // Reemplazar detalles
        db.CotizacionesDetalle.RemoveRange(existente.Detalles);
        foreach (var det in cot.Detalles)
        {
            existente.Detalles.Add(new CotizacionDetalle
            {
                IdArticulo = det.IdArticulo,
                Cantidad = det.Cantidad,
                Precio = det.Precio,
                ItemNro = det.ItemNro
            });
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var cot = await db.Cotizaciones.FindAsync(id);
        if (cot == null) return NotFound();

        db.Cotizaciones.Remove(cot);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
