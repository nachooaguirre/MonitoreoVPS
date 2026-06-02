using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfiguracionController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var cfg = await db.ConfiguracionEmpresa.FindAsync(1);
        return cfg is null ? NotFound() : Ok(cfg);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ConfiguracionEmpresa cfg)
    {
        cfg.Id = 1;
        var existing = await db.ConfiguracionEmpresa.FindAsync(1);
        if (existing is null)
        {
            db.ConfiguracionEmpresa.Add(cfg);
        }
        else
        {
            existing.NombreEmpresa    = cfg.NombreEmpresa;
            existing.NombreFantasia   = cfg.NombreFantasia;
            existing.Cuit             = cfg.Cuit;
            existing.IngresosBrutos   = cfg.IngresosBrutos;
            existing.Direccion        = cfg.Direccion;
            existing.Localidad        = cfg.Localidad;
            existing.Provincia        = cfg.Provincia;
            existing.Telefono         = cfg.Telefono;
            existing.Email            = cfg.Email;
            existing.SitioWeb         = cfg.SitioWeb;
            existing.PuntoVenta       = cfg.PuntoVenta;
            existing.AfipHomologacion = cfg.AfipHomologacion;
            existing.ImpresoraFiscalModelo  = cfg.ImpresoraFiscalModelo;
            existing.ImpresoraFiscalPuerto  = cfg.ImpresoraFiscalPuerto;
            existing.ImpresoraTicketNombre  = cfg.ImpresoraTicketNombre;
            existing.MensajePiePagina       = cfg.MensajePiePagina;
            existing.ControlaStock    = cfg.ControlaStock;
            existing.PrecioConIva     = cfg.PrecioConIva;
            existing.BackupRuta       = cfg.BackupRuta;
        }
        await db.SaveChangesAsync();
        return NoContent();
    }
}
