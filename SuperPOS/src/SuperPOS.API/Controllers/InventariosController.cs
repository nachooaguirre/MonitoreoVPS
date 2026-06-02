using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.API.Helpers;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventariosController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await db.Inventarios
            .AsNoTracking()
            .Include(i => i.Sucursal)
            .OrderByDescending(i => i.FechaInicio)
            .Select(i => new
            {
                i.Id,
                i.Descripcion,
                i.IdSucursal,
                SucursalNombre = i.Sucursal != null ? i.Sucursal.Nombre : (string?)null,
                i.FechaInicio,
                i.FechaCierre,
                Estado = (int)i.Estado,
                i.TotalArticulos,
                i.ArticulosContados,
                i.DiferenciaValorizada
            })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var inv = await db.Inventarios
            .Include(i => i.Sucursal)
            .Include(i => i.Detalles).ThenInclude(d => d.Articulo)
            .FirstOrDefaultAsync(i => i.Id == id);
        return inv is null ? NotFound() : Ok(inv);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearInventarioRequest req)
    {
        var idSuc = req.IdSucursal ?? await StockSucursalHelper.ObtenerIdSucursalCentralAsync(db) ?? 1;

        var articulos = await db.Articulos
            .Where(a => a.Activo)
            .OrderBy(a => a.Descripcion)
            .ToListAsync();

        var inv = new Inventario
        {
            Descripcion = req.Descripcion,
            IdSucursal = idSuc,
            IdUsuario = req.IdUsuario,
            FechaInicio = DateTime.UtcNow,
            Estado = EstadoInventario.EnProceso,
            TotalArticulos = articulos.Count,
            ArticulosContados = 0,
            Detalles = new List<InventarioDetalle>()
        };

        foreach (var a in articulos)
        {
            var stockSis = await StockSucursalHelper.ObtenerCantidadAsync(db, a.Id, idSuc);
            inv.Detalles.Add(new InventarioDetalle
            {
                IdArticulo = a.Id,
                StockSistema = stockSis,
                StockContado = 0,
                FueConteado = false,
                PrecioCosto = a.PrecioCosto,
                FechaConteo = DateTime.UtcNow
            });
        }

        db.Inventarios.Add(inv);
        await db.SaveChangesAsync();
        return Ok(inv);
    }

    /// <summary>
    /// Registra el conteo de un artículo durante el inventario
    /// </summary>
    [HttpPut("{id}/contar")]
    public async Task<IActionResult> Contar(int id, [FromBody] ContarRequest req)
    {
        var det = await db.InventariosDetalle.FirstOrDefaultAsync(d => d.IdInventario == id && d.IdArticulo == req.IdArticulo);
        if (det is null) return NotFound();

        if (req.Acumulativo)
            det.StockContado += req.StockContado;
        else
            det.StockContado = req.StockContado;
        det.FueConteado = true;
        det.FechaConteo = DateTime.UtcNow;
        det.Observaciones = req.Observaciones;

        // Actualizar progreso (incluye conteo en cero)
        var inv = await db.Inventarios.FindAsync(id);
        if (inv != null)
        {
            inv.ArticulosContados = await db.InventariosDetalle
                .CountAsync(d => d.IdInventario == id && d.FueConteado);
        }

        await db.SaveChangesAsync();
        return Ok(det);
    }

    /// <summary>
    /// Cierra el inventario y opcionalmente aplica las diferencias al stock real
    /// </summary>
    [HttpPut("{id}/cerrar")]
    public async Task<IActionResult> Cerrar(int id, [FromBody] CerrarInventarioRequest req)
    {
        var inv = await db.Inventarios.Include(i => i.Detalles).FirstOrDefaultAsync(i => i.Id == id);
        if (inv is null) return NotFound();

        inv.Estado = EstadoInventario.Cerrado;
        inv.FechaCierre = DateTime.UtcNow;
        inv.DiferenciaValorizada = inv.Detalles
            .Where(d => d.FueConteado)
            .Sum(d => (d.StockContado - d.StockSistema) * d.PrecioCosto);

        if (req.AplicarAlStock)
        {
            foreach (var det in inv.Detalles)
            {
                if (!det.FueConteado) continue;
                if (det.StockContado == det.StockSistema) continue;
                await StockSucursalHelper.FijarCantidadAsync(db, det.IdArticulo, inv.IdSucursal, det.StockContado);
            }

            inv.Estado = EstadoInventario.Aplicado;
        }

        await db.SaveChangesAsync();
        return Ok(new
        {
            inv.Id,
            inv.Estado,
            inv.TotalArticulos,
            inv.ArticulosContados,
            inv.DiferenciaValorizada,
            CantDiferencias = inv.Detalles.Count(d => d.FueConteado && d.StockContado != d.StockSistema),
        });
    }

    [HttpGet("{id}/diferencias")]
    public async Task<IActionResult> GetDiferencias(int id)
    {
        var dets = await db.InventariosDetalle
            .Include(d => d.Articulo)
            .Where(d => d.IdInventario == id)
            .ToListAsync();

        var difs = dets
            .Where(d => d.FueConteado && d.StockContado != d.StockSistema)
            .Select(d => new
            {
                d.IdArticulo,
                Descripcion = d.Articulo?.Descripcion,
                d.StockSistema,
                d.StockContado,
                Diferencia = d.StockContado - d.StockSistema,
                d.PrecioCosto,
                DiferenciaValorizada = (d.StockContado - d.StockSistema) * d.PrecioCosto
            })
            .OrderBy(d => d.Descripcion)
            .ToList();

        return Ok(new
        {
            TotalDiferencias = difs.Count,
            ValorDiferencia = difs.Sum(d => d.DiferenciaValorizada),
            Detalle = difs
        });
    }
}

public class CrearInventarioRequest
{
    public string Descripcion { get; set; } = "Inventario " + DateTime.Now.ToString("dd/MM/yyyy");
    public int IdUsuario { get; set; }
    /// <summary>Sucursal inventariada. Por defecto: central.</summary>
    public int? IdSucursal { get; set; }
}

public class ContarRequest
{
    public int IdArticulo { get; set; }
    public decimal StockContado { get; set; }
    public string? Observaciones { get; set; }
    /// <summary>Si es true, suma la cantidad al conteo ya registrado (misma lectura / repaso).</summary>
    public bool Acumulativo { get; set; }
}

public class CerrarInventarioRequest
{
    public bool AplicarAlStock { get; set; }
}
