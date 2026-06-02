using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ListasPreciosController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await db.ListasPrecios.Where(l => l.Activo).OrderBy(l => l.Nombre).ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var lista = await db.ListasPrecios.Include(l => l.PreciosEspeciales).ThenInclude(p => p.Articulo).FirstOrDefaultAsync(l => l.Id == id);
        return lista is null ? NotFound() : Ok(lista);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ListaPrecio lista)
    {
        db.ListasPrecios.Add(lista);
        await db.SaveChangesAsync();
        return Ok(lista);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ListaPrecio lista)
    {
        var ex = await db.ListasPrecios.FindAsync(id);
        if (ex is null) return NotFound();
        ex.Nombre = lista.Nombre;
        ex.Descripcion = lista.Descripcion;
        ex.Tipo = lista.Tipo;
        ex.Valor = lista.Valor;
        ex.EsDefault = lista.EsDefault;
        await db.SaveChangesAsync();
        return Ok(ex);
    }

    /// <summary>
    /// Calcula el precio de un artículo aplicando la lista de precio indicada.
    /// Tiene en cuenta bonificaciones por rango de cantidad (volumen).
    /// </summary>
    [HttpGet("precio/{idLista}/{idArticulo}")]
    public async Task<IActionResult> GetPrecio(int idLista, int idArticulo, [FromQuery] decimal cantidad = 1)
    {
        var art = await db.Articulos.FindAsync(idArticulo);
        if (art is null) return NotFound();

        var lista = await db.ListasPrecios.FindAsync(idLista);
        if (lista is null) return NotFound();

        // Precio especial individual para este artículo en esta lista
        var precioEsp = await db.ArticulosPreciosListas
            .FirstOrDefaultAsync(p => p.IdLista == idLista && p.IdArticulo == idArticulo);

        decimal precioBase;
        if (precioEsp != null)
        {
            precioBase = precioEsp.PorcentajeAjuste.HasValue
                ? art.PrecioVenta * (1 + precioEsp.PorcentajeAjuste.Value / 100)
                : precioEsp.Precio;
        }
        else
        {
            // Aplicar porcentaje de la lista
            precioBase = lista.Tipo switch
            {
                TipoListaPrecio.PorcentajeSobreVenta => art.PrecioVenta * (1 + lista.Valor / 100),
                TipoListaPrecio.PorcentajeSobreCosto => art.PrecioCosto * (1 + lista.Valor / 100) * (1 + art.AlicuotaIva / 100),
                _ => art.PrecioVenta
            };
        }

        // Bonificación por rango de cantidad
        var rango = await db.BonificacionesRango
            .Where(r => r.IdArticulo == idArticulo && r.CantidadDesde <= cantidad && r.CantidadHasta >= cantidad)
            .FirstOrDefaultAsync();

        decimal descuentoRango = rango?.PorcentajeDescuento ?? 0;
        decimal precioFinal = precioBase * (1 - descuentoRango / 100);

        // Bonificación por rango de fechas (promociones temporales)
        var promoActiva = await db.BonificacionesFecha
            .Where(b => b.IdArticulo == idArticulo && b.Aplicado && b.FechaDesde <= DateTime.UtcNow && b.FechaHasta >= DateTime.UtcNow)
            .OrderByDescending(b => b.Porcentaje)
            .FirstOrDefaultAsync();

        decimal descuentoPromoPct = promoActiva?.Porcentaje ?? 0;
        string? promoDetalle = promoActiva?.Detalle;
        precioFinal = precioFinal * (1 - descuentoPromoPct / 100);

        return Ok(new
        {
            PrecioBase = art.PrecioVenta,
            PrecioLista = Math.Round(precioBase, 2),
            DescuentoRangoPct = descuentoRango,
            DescuentoPromoPct = Math.Round(descuentoPromoPct, 2),
            PromoDetalle = promoDetalle,
            PrecioFinal = Math.Round(precioFinal, 2),
            AlicuotaIva = art.AlicuotaIva
        });
    }

    // Bonificaciones por rango de cantidad
    [HttpGet("rangos/{idArticulo}")]
    public async Task<IActionResult> GetRangos(int idArticulo)
        => Ok(await db.BonificacionesRango.Where(r => r.IdArticulo == idArticulo).OrderBy(r => r.CantidadDesde).ToListAsync());

    [HttpPost("rangos")]
    public async Task<IActionResult> CreateRango([FromBody] BonificacionRango rango)
    {
        db.BonificacionesRango.Add(rango);
        await db.SaveChangesAsync();
        return Ok(rango);
    }

    [HttpDelete("rangos/{id}")]
    public async Task<IActionResult> DeleteRango(int id)
    {
        var r = await db.BonificacionesRango.FindAsync(id);
        if (r is null) return NotFound();
        db.BonificacionesRango.Remove(r);
        await db.SaveChangesAsync();
        return Ok();
    }

    // Precios especiales por artículo en una lista
    [HttpPost("especiales")]
    public async Task<IActionResult> SetPrecioEspecial([FromBody] ArticuloPrecioLista precio)
    {
        var ex = await db.ArticulosPreciosListas.FirstOrDefaultAsync(p => p.IdLista == precio.IdLista && p.IdArticulo == precio.IdArticulo);
        if (ex != null)
        {
            ex.Precio = precio.Precio;
            ex.PorcentajeAjuste = precio.PorcentajeAjuste;
        }
        else
        {
            db.ArticulosPreciosListas.Add(precio);
        }
        await db.SaveChangesAsync();
        return Ok();
    }

    // Bonificaciones entre Fechas (Promociones Temporales)
    [HttpGet("bonificaciones-fechas/{idArticulo}")]
    public async Task<IActionResult> GetBonificacionesFechas(int idArticulo)
        => Ok(await db.BonificacionesFecha.Where(b => b.IdArticulo == idArticulo).OrderBy(b => b.FechaDesde).ToListAsync());

    [HttpPost("bonificaciones-fechas")]
    public async Task<IActionResult> CreateBonificacionFecha([FromBody] BonificacionFecha bonif)
    {
        bonif.FechaDesde = DateTime.SpecifyKind(bonif.FechaDesde, DateTimeKind.Utc);
        bonif.FechaHasta = DateTime.SpecifyKind(bonif.FechaHasta, DateTimeKind.Utc);

        db.BonificacionesFecha.Add(bonif);
        await db.SaveChangesAsync();
        return Ok(bonif);
    }

    [HttpDelete("bonificaciones-fechas/{id}")]
    public async Task<IActionResult> DeleteBonificacionFecha(int id)
    {
        var b = await db.BonificacionesFecha.FindAsync(id);
        if (b is null) return NotFound();
        db.BonificacionesFecha.Remove(b);
        await db.SaveChangesAsync();
        return Ok();
    }
}
