using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediosPagoController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool incluirInactivos = false)
    {
        var q = db.MediosPago.AsNoTracking().AsQueryable();
        if (!incluirInactivos) q = q.Where(m => m.Activo);
        return Ok(await q.OrderBy(m => m.Id).ToListAsync());
    }
}
