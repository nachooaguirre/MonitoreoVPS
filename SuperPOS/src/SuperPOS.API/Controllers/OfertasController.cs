using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.Shared.Entities.Ventas;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OfertasController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var ofertas = await db.Ofertas
            .Include(o => o.Articulo)
            .OrderByDescending(o => o.FechaDesde)
            .ToListAsync();
        return Ok(ofertas);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var oferta = await db.Ofertas
            .Include(o => o.Articulo)
            .FirstOrDefaultAsync(o => o.Id == id);
        return oferta is null ? NotFound() : Ok(oferta);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Oferta oferta)
    {
        oferta.FechaDesde = DateTime.SpecifyKind(oferta.FechaDesde, DateTimeKind.Utc);
        oferta.FechaHasta = DateTime.SpecifyKind(oferta.FechaHasta, DateTimeKind.Utc);

        db.Ofertas.Add(oferta);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = oferta.Id }, oferta);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Oferta oferta)
    {
        if (id != oferta.Id) return BadRequest();

        var existing = await db.Ofertas.FindAsync(id);
        if (existing is null) return NotFound();

        existing.Detalle = oferta.Detalle;
        existing.FechaDesde = DateTime.SpecifyKind(oferta.FechaDesde, DateTimeKind.Utc);
        existing.FechaHasta = DateTime.SpecifyKind(oferta.FechaHasta, DateTimeKind.Utc);
        existing.PrecioOferta = oferta.PrecioOferta;
        existing.LimiteStock = oferta.LimiteStock;
        existing.CantidadVendida = oferta.CantidadVendida;
        existing.Activa = oferta.Activa;
        existing.IdArticulo = oferta.IdArticulo;

        await db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var oferta = await db.Ofertas.FindAsync(id);
        if (oferta is null) return NotFound();

        db.Ofertas.Remove(oferta);
        await db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("grafica/{id}")]
    public async Task<IActionResult> GetGraficaVentas(int id)
    {
        var oferta = await db.Ofertas.FindAsync(id);
        if (oferta is null) return NotFound();

        // Obtener detalles de comprobantes de venta para el artículo en el rango de fechas de la oferta
        var detalles = await db.ComprobantesDetalle
            .Where(d => d.IdArticulo == oferta.IdArticulo &&
                        d.Comprobante!.Fecha >= oferta.FechaDesde &&
                        d.Comprobante.Fecha <= oferta.FechaHasta &&
                        d.Comprobante.Estado != EstadoComprobante.Anulado)
            .Select(d => new { d.Cantidad, Fecha = d.Comprobante!.Fecha })
            .ToListAsync();

        // Agrupar por día (en hora local para el reporte, o UTC de la DB) y sumar cantidades
        var ventasPorDia = detalles
            .GroupBy(d => d.Fecha.Date)
            .Select(g => new
            {
                FechaLabel = g.Key.ToString("dd/MM/yyyy"),
                Fecha = g.Key,
                Cantidad = g.Sum(x => x.Cantidad)
            })
            .OrderBy(x => x.Fecha)
            .ToList();

        // Si no hay ventas, podemos rellenar con un par de días vacíos o devolver lista vacía.
        // Vamos a devolver la lista agrupada tal cual.
        return Ok(ventasPorDia);
    }
}
