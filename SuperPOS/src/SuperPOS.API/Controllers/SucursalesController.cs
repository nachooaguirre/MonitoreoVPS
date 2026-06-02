using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SucursalesController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await db.Sucursales
            .Where(s => s.Activo)
            .OrderByDescending(s => s.EsCentral)
            .ThenBy(s => s.Id)
            .Select(s => new { s.Id, s.Nombre, s.EsCentral, s.Direccion })
            .ToListAsync();
        return Ok(items);
    }
}
