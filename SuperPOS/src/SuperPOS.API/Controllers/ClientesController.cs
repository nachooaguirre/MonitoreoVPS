using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? buscar, [FromQuery] bool? soloConCtaCte, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var q = db.Clientes.Where(c => c.Activo).AsQueryable();
        if (!string.IsNullOrWhiteSpace(buscar))
            q = q.Where(c => c.RazonSocial.ToLower().Contains(buscar.ToLower()) || c.Cuit.Contains(buscar));
        if (soloConCtaCte == true)
            q = q.Where(c => c.TieneCtaCte);
        var total = await q.CountAsync();
        var items = await q.OrderBy(c => c.RazonSocial).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var c = await db.Clientes.FindAsync(id);
        return c is null ? NotFound() : Ok(c);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Cliente cliente)
    {
        cliente.FechaAlta = DateTime.UtcNow;
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = cliente.Id }, cliente);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Cliente cliente)
    {
        if (id != cliente.Id) return BadRequest();
        db.Entry(cliente).State = EntityState.Modified;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await db.Clientes.FindAsync(id);
        if (c is null) return NotFound();
        c.Activo = false;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
