using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CajasController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? idSucursal, [FromQuery] bool incluirInactivas = true)
    {
        var q = db.Cajas.AsNoTracking().AsQueryable();
        if (idSucursal.HasValue) q = q.Where(c => c.IdSucursal == idSucursal.Value);
        if (!incluirInactivas) q = q.Where(c => c.Activo);

        var items = await q.OrderBy(c => c.IdSucursal).ThenBy(c => c.Id).ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Caja caja)
    {
        var existeSucursal = await db.Sucursales.AnyAsync(s => s.Id == caja.IdSucursal);
        if (!existeSucursal) return BadRequest("La sucursal indicada no existe.");

        db.Cajas.Add(caja);
        await db.SaveChangesAsync();
        return Ok(caja);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Caja actualizada)
    {
        var caja = await db.Cajas.FindAsync(id);
        if (caja is null) return NotFound();

        caja.Nombre = actualizada.Nombre;
        caja.Activo = actualizada.Activo;

        await db.SaveChangesAsync();
        return Ok(caja);
    }
}
