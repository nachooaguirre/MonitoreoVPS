using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.API.Helpers;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AuditoriaController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? idUsuario,
        [FromQuery] string? entidad,
        [FromQuery] TipoAccionAuditoria? accion,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] string? buscar,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var q = db.AuditLogs.AsQueryable();
        if (idUsuario.HasValue) q = q.Where(a => a.IdUsuario == idUsuario);
        if (!string.IsNullOrWhiteSpace(entidad)) q = q.Where(a => a.Entidad == entidad);
        if (accion.HasValue) q = q.Where(a => a.Accion == accion);
        if (desde.HasValue) q = q.Where(a => a.Fecha >= desde.Value.ToUtc());
        if (hasta.HasValue) q = q.Where(a => a.Fecha <= hasta.Value.ToUtc().AddDays(1));
        if (!string.IsNullOrWhiteSpace(buscar))
            q = q.Where(a =>
                (a.Descripcion != null && a.Descripcion.Contains(buscar)) ||
                (a.NombreUsuario != null && a.NombreUsuario.Contains(buscar)) ||
                a.EntidadId.Contains(buscar));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(a => a.Fecha)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>Lista de nombres de entidad distintos ya auditados, para poblar un filtro combo.</summary>
    [HttpGet("entidades")]
    public async Task<IActionResult> GetEntidadesDistintas() =>
        Ok(await db.AuditLogs.Select(a => a.Entidad).Distinct().OrderBy(x => x).ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var log = await db.AuditLogs.FindAsync(id);
        return log is null ? NotFound() : Ok(log);
    }
}
