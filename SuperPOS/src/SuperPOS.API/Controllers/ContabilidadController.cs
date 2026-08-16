using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SuperPOS.API.Services;

namespace SuperPOS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContabilidadController(IContabilidadService service) : ControllerBase
    {
        [HttpGet("libro-iva-ventas-cbte")]
        public async Task<IActionResult> DownloadLibroIvaVentasCbte([FromQuery] int mes, [FromQuery] int anio)
        {
            try
            {
                var content = await service.GenerarLibroIvaVentasCbte(mes, anio);
                string fileName = $"LIBRO_IVA_DIGITAL_VENTAS_CBTE_{anio}{mes:D2}.TXT";
                return File(content, "text/plain", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("libro-iva-ventas-alic")]
        public async Task<IActionResult> DownloadLibroIvaVentasAlic([FromQuery] int mes, [FromQuery] int anio)
        {
            try
            {
                var content = await service.GenerarLibroIvaVentasAlicuotas(mes, anio);
                string fileName = $"LIBRO_IVA_DIGITAL_VENTAS_ALICUOTAS_{anio}{mes:D2}.TXT";
                return File(content, "text/plain", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("libro-iva-compras-cbte")]
        public async Task<IActionResult> DownloadLibroIvaComprasCbte([FromQuery] int mes, [FromQuery] int anio)
        {
            try
            {
                var content = await service.GenerarLibroIvaComprasCbte(mes, anio);
                string fileName = $"COMPRAS_base_{anio}{mes:D2}.txt";
                return File(content, "text/plain", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("libro-iva-compras-alic")]
        public async Task<IActionResult> DownloadLibroIvaComprasAlic([FromQuery] int mes, [FromQuery] int anio)
        {
            try
            {
                var content = await service.GenerarLibroIvaComprasAlicuotas(mes, anio);
                string fileName = $"COMPRAS_base_alícuotas_{anio}{mes:D2}.txt";
                return File(content, "text/plain", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("percepciones-iva-ventas")]
        public async Task<IActionResult> DownloadPercepcionesIvaVentas([FromQuery] int mes, [FromQuery] int anio)
        {
            try
            {
                var content = await service.GenerarPercepcionesIvaVentas(mes, anio);
                string fileName = $"AR-30702841352-{anio}{mes:D2}0-P7-LOTE2.txt";
                return File(content, "text/plain", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("percepciones-iibb-compras")]
        public async Task<IActionResult> DownloadPercepcionesIIBBCompras([FromQuery] int mes, [FromQuery] int anio)
        {
            try
            {
                var content = await service.GenerarPercepcionesIIBBCompras(mes, anio);
                string fileName = $"AR-30702841352-{anio}{mes:D2}2-6-LOTE1.txt";
                return File(content, "text/plain", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("resumen-ventas-csv")]
        public async Task<IActionResult> DownloadResumenVentasCsv([FromQuery] int mes, [FromQuery] int anio)
        {
            try
            {
                var content = await service.GenerarResumenVentasCsv(mes, anio);
                string fileName = $"resumen_ventas_{anio}{mes:D2}.csv";
                var bytes = Encoding.UTF8.GetBytes(content);
                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("resumen-compras-csv")]
        public async Task<IActionResult> DownloadResumenComprasCsv([FromQuery] int mes, [FromQuery] int anio)
        {
            try
            {
                var content = await service.GenerarResumenComprasCsv(mes, anio);
                string fileName = $"resumen_compras_{anio}{mes:D2}.csv";
                var bytes = Encoding.UTF8.GetBytes(content);
                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
