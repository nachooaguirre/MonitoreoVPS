using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SuperPOS.API.Services;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportadorController(ImportadorLegacyService importadorService) : ControllerBase
{
    [HttpPost("importar-legacy")]
    public async Task<IActionResult> ImportarLegacy([FromQuery] string? mdbPath)
    {
        string defaultPath = @"C:\Users\ignac\OneDrive\Escritorio\POS apps\supermer\LA - Supermer-20260408T013556Z-3-001\LA - Supermer\EJECUCION_PRUEBA_LOCAL\tecnolar.Mdb";
        string pathToUse = string.IsNullOrWhiteSpace(mdbPath) ? defaultPath : mdbPath;

        if (!System.IO.File.Exists(pathToUse))
        {
            return BadRequest(new { error = $"El archivo MDB no se encuentra en la ruta especificada: {pathToUse}" });
        }

        try
        {
            var result = await importadorService.ImportarDeMdbAsync(pathToUse);
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(500, new { error = result.ErrorMessage });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
