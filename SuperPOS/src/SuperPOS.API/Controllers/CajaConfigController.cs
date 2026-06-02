using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.Shared.Entities.Ventas.Legacy;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CajaConfigController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet("config/{nroCaja}")]
    public async Task<IActionResult> GetConfig(int nroCaja)
    {
        var cfg = await db.POS_Config.FirstOrDefaultAsync(x => x.NroCaja == nroCaja);
        if (cfg == null)
        {
            // Crear una por defecto para no romper el flujo
            cfg = new POS_Config
            {
                NroCaja = nroCaja,
                PanelPrincipal = 1,
                StockOnLine = true,
                VentaCantidadDefecto = 1m
            };
        }
        return Ok(cfg);
    }

    [HttpGet("paneles")]
    public async Task<IActionResult> GetPaneles()
    {
        var paneles = await db.POS_Paneles.OrderBy(x => x.Panel).ToListAsync();
        return Ok(paneles);
    }

    [HttpGet("teclas")]
    public async Task<IActionResult> GetTeclas()
    {
        var teclas = await db.POS_Teclas.ToListAsync();
        return Ok(teclas);
    }

    [HttpGet("funciones")]
    public async Task<IActionResult> GetFunciones()
    {
        var funciones = await db.POS_Funciones.OrderBy(x => x.NroFuncion).ToListAsync();
        return Ok(funciones);
    }

    [HttpGet("funciones/panel/{panelId}")]
    public async Task<IActionResult> GetFuncionesPorPanel(int panelId)
    {
        var funciones = await db.POS_Funciones
            .Where(x => x.Panel == panelId)
            .OrderBy(x => x.NroFuncion)
            .ToListAsync();
        return Ok(funciones);
    }
}
