using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EtiquetasController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet("cola")]
    public async Task<IActionResult> GetCola()
    {
        var items = await db.EtiquetasCola
            .Include(e => e.Articulo)
            .OrderByDescending(e => e.FechaCreado)
            .Select(e => new
            {
                e.Id,
                e.IdArticulo,
                e.Cantidad,
                e.FechaCreado,
                Articulo = e.Articulo != null ? new
                {
                    e.Articulo.Id,
                    e.Articulo.CodigoBarras,
                    e.Articulo.CodigoInterno,
                    e.Articulo.Descripcion,
                    e.Articulo.PrecioVenta,
                    e.Articulo.PrecioCosto,
                    e.Articulo.StockActual,
                    e.Articulo.AlicuotaIva,
                    e.Articulo.AplicaIva,
                    e.Articulo.ContenidoValor,
                    e.Articulo.ContenidoUnidad
                } : null
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("encolar")]
    public async Task<IActionResult> Encolar([FromBody] EncolarEtiquetaRequest req)
    {
        int? artId = null;

        if (req.IdArticulo > 0)
        {
            var exists = await db.Articulos.AnyAsync(a => a.Id == req.IdArticulo);
            if (exists) artId = req.IdArticulo;
        }
        else if (!string.IsNullOrWhiteSpace(req.CodigoBarras))
        {
            var codigo = req.CodigoBarras.Trim();
            // Buscar por código de barra principal
            artId = await db.Articulos
                .Where(a => a.Activo && a.CodigoBarras == codigo)
                .Select(a => (int?)a.Id)
                .FirstOrDefaultAsync();

            // Si no se encuentra, buscar por códigos de barra adicionales
            if (artId == null)
            {
                artId = await db.ArticulosCodigoBarras
                    .Where(cb => cb.CodigoBarras == codigo)
                    .Select(cb => (int?)cb.IdArticulo)
                    .FirstOrDefaultAsync();
            }
        }

        if (artId == null)
        {
            return BadRequest("Artículo no encontrado.");
        }

        // Verificar si ya está en la cola para acumular cantidad
        var existing = await db.EtiquetasCola.FirstOrDefaultAsync(e => e.IdArticulo == artId.Value);
        if (existing != null)
        {
            existing.Cantidad += req.Cantidad > 0 ? req.Cantidad : 1;
            existing.FechaCreado = DateTime.UtcNow;
        }
        else
        {
            var item = new EtiquetaCola
            {
                IdArticulo = artId.Value,
                Cantidad = req.Cantidad > 0 ? req.Cantidad : 1,
                FechaCreado = DateTime.UtcNow
            };
            db.EtiquetasCola.Add(item);
        }

        await db.SaveChangesAsync();
        return Ok(new { success = true, idArticulo = artId.Value });
    }

    [HttpDelete("cola/{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var item = await db.EtiquetasCola.FindAsync(id);
        if (item is null) return NotFound();

        db.EtiquetasCola.Remove(item);
        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpDelete("cola/limpiar")]
    public async Task<IActionResult> LimpiarCola()
    {
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"EtiquetasCola\"");
        return Ok(new { success = true });
    }

    [HttpPost("cola/imprimir-marcar")]
    public async Task<IActionResult> MarcarComoImpresas([FromBody] MarcarImpresasRequest req)
    {
        if (req.Ids == null || req.Ids.Count == 0) return BadRequest("Debe especificar IDs a eliminar.");

        var items = await db.EtiquetasCola.Where(e => req.Ids.Contains(e.Id)).ToListAsync();
        db.EtiquetasCola.RemoveRange(items);
        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}

public class EncolarEtiquetaRequest
{
    public int IdArticulo { get; set; }
    public string? CodigoBarras { get; set; }
    public int Cantidad { get; set; } = 1;
}

public class MarcarImpresasRequest
{
    public List<int> Ids { get; set; } = [];
}
