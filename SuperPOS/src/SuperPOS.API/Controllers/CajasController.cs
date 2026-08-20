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

    /// <summary>Cajas activas de sucursales activas, para el selector de "qué punto de venta abro" al loguearse.</summary>
    [HttpGet("disponibles")]
    public async Task<IActionResult> GetDisponibles()
    {
        var items = await db.Cajas.AsNoTracking()
            .Include(c => c.Sucursal)
            .Where(c => c.Activo && c.Sucursal!.Activo)
            .OrderBy(c => c.IdSucursal).ThenBy(c => c.Id)
            .Select(c => new { c.Id, c.Nombre, c.IdSucursal, SucursalNombre = c.Sucursal!.Nombre })
            .ToListAsync();
        return Ok(items);
    }

    /// <summary>Estado de cada terminal: última venta registrada y si tuvo actividad reciente ("en línea").</summary>
    [HttpGet("estado")]
    public async Task<IActionResult> GetEstado()
    {
        var cajas = await db.Cajas.AsNoTracking().Include(c => c.Sucursal)
            .OrderBy(c => c.IdSucursal).ThenBy(c => c.Id).ToListAsync();

        var ultimaPorCaja = await db.Comprobantes.AsNoTracking()
            .Where(c => c.Estado != EstadoComprobante.Anulado)
            .GroupBy(c => c.IdCaja)
            .Select(g => new { IdCaja = g.Key, Ultima = g.Max(c => c.Fecha) })
            .ToListAsync();

        var ahora = DateTime.UtcNow;
        var items = cajas.Select(c =>
        {
            var ultima = ultimaPorCaja.FirstOrDefault(u => u.IdCaja == c.Id)?.Ultima;
            return new
            {
                c.Id,
                c.Nombre,
                c.Activo,
                c.IdSucursal,
                SucursalNombre = c.Sucursal?.Nombre,
                UltimaVenta = ultima,
                // ponytail: "en línea" aproximado por venta reciente (sin sesión persistida por terminal); ajustar la ventana si hace falta más precisión.
                EnLinea = ultima.HasValue && ahora - ultima.Value < TimeSpan.FromMinutes(15)
            };
        });
        return Ok(items);
    }
}
