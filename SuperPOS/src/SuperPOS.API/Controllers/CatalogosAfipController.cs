using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;

namespace SuperPOS.API.Controllers;

/// <summary>
/// Catálogos oficiales de AFIP relevados del sistema Gecom del cliente: remito electrónico
/// cárnico (RG 4256) y harinero (RG 4514), bancos (BCRA) y matriz de validación SICORE.
/// Son tablas de referencia fijas, no editables desde acá.
/// </summary>
[ApiController]
[Route("api/catalogos-afip")]
public class CatalogosAfipController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet("remito-carne-grupos")]
    public async Task<IActionResult> GetCarneGrupos() =>
        Ok(await db.AfipRemitoCarneGrupos.AsNoTracking().OrderBy(x => x.Id).ToListAsync());

    [HttpGet("remito-carne-tipos")]
    public async Task<IActionResult> GetCarneTipos([FromQuery] int? idGrupo)
    {
        var q = db.AfipRemitoCarneTipos.AsNoTracking().AsQueryable();
        if (idGrupo.HasValue) q = q.Where(x => x.IdGrupo == idGrupo.Value);
        return Ok(await q.OrderBy(x => x.Codigo).ToListAsync());
    }

    [HttpGet("remito-harina-tipos")]
    public async Task<IActionResult> GetHarinaTipos() =>
        Ok(await db.AfipRemitoHarinaTipos.AsNoTracking().OrderBy(x => x.Id).ToListAsync());

    [HttpGet("remito-harina-embalajes")]
    public async Task<IActionResult> GetHarinaEmbalajes() =>
        Ok(await db.AfipRemitoHarinaEmbalajes.AsNoTracking().OrderBy(x => x.Id).ToListAsync());

    [HttpGet("bancos")]
    public async Task<IActionResult> GetBancos() =>
        Ok(await db.BancosArgentina.AsNoTracking().OrderBy(x => x.RazonSocial).ToListAsync());
}
