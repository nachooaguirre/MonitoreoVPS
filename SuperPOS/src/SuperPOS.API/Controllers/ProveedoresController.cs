using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProveedoresController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? buscar, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var q = db.Proveedores.Where(p => p.Activo).AsQueryable();
        if (!string.IsNullOrWhiteSpace(buscar))
            q = q.Where(p => p.RazonSocial.ToLower().Contains(buscar.ToLower()) || p.Cuit.Contains(buscar));
        var total = await q.CountAsync();
        var items = await q.OrderBy(p => p.RazonSocial).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await db.Proveedores.FindAsync(id);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Proveedor proveedor)
    {
        proveedor.FechaAlta = DateTime.UtcNow;
        db.Proveedores.Add(proveedor);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = proveedor.Id }, proveedor);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Proveedor proveedor)
    {
        if (id != proveedor.Id) return BadRequest();
        db.Entry(proveedor).State = EntityState.Modified;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await db.Proveedores.FindAsync(id);
        if (p is null) return NotFound();
        p.Activo = false;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
