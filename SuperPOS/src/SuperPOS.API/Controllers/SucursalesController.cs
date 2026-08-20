using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SucursalesController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool incluirInactivas = false)
    {
        var q = db.Sucursales.AsNoTracking().AsQueryable();
        if (!incluirInactivas) q = q.Where(s => s.Activo);

        var items = await q
            .OrderByDescending(s => s.EsCentral)
            .ThenBy(s => s.Id)
            .Select(s => new
            {
                s.Id,
                s.Nombre,
                s.EsCentral,
                s.Direccion,
                s.Activo,
                CajasActivas = db.Cajas.Count(c => c.IdSucursal == s.Id && c.Activo),
                CajasInactivas = db.Cajas.Count(c => c.IdSucursal == s.Id && !c.Activo)
            })
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Sucursal sucursal)
    {
        db.Sucursales.Add(sucursal);
        await db.SaveChangesAsync();
        return Ok(sucursal);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Sucursal actualizada)
    {
        var s = await db.Sucursales.FindAsync(id);
        if (s is null) return NotFound();

        s.Nombre = actualizada.Nombre;
        s.Direccion = actualizada.Direccion;
        s.EsCentral = actualizada.EsCentral;
        s.Activo = actualizada.Activo;

        await db.SaveChangesAsync();
        return Ok(s);
    }
}
