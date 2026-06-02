using Microsoft.AspNetCore.Mvc;
using SuperPOS.API;
using SuperPOS.API.Services;
using System.Collections.Generic;
using System.Linq;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController(IAiService ai) : ControllerBase
{
    /// <summary>
    /// Analiza artículos con stock bajo y genera sugerencias de órdenes de compra.
    /// Acepta instrucción adicional en el body para personalizar el análisis.
    /// </summary>
    [HttpPost("sugerencias-compra")]
    public async Task<IActionResult> SugerenciasCompra([FromBody] AiRequest request)
    {
        var resultado = await ai.SugerenciasCompraAsync(request.Dias, request.Instruccion, request.BuscarEnWeb, request.MaxFilasSugerencias);
        return Ok(resultado);
    }

    /// <summary>
    /// Alerta sobre lotes próximos a vencer.
    /// </summary>
    [HttpPost("alertas-vencimientos")]
    public async Task<IActionResult> AlertasVencimientos([FromBody] AiRequest request)
    {
        var resultado = await ai.AlertasVencimientosAsync(request.Dias, request.Instruccion, request.BuscarEnWeb);
        return Ok(resultado);
    }

    /// <summary>
    /// Análisis de tendencias de ventas de los últimos N días.
    /// </summary>
    [HttpPost("analisis-ventas")]
    public async Task<IActionResult> AnalisisVentas([FromBody] AiRequest request)
    {
        var resultado = await ai.AnalisisVentasAsync(request.Dias, request.Instruccion, request.BuscarEnWeb);
        return Ok(resultado);
    }

    /// <summary>
    /// Consulta libre: el usuario escribe una pregunta en lenguaje natural.
    /// </summary>
    [HttpPost("consulta")]
    public async Task<IActionResult> ConsultaLibre([FromBody] ConsultaLibreRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Pregunta))
            return BadRequest(new { error = "La pregunta no puede estar vacía." });

        var historial = request.Historial?.Select(h => new AiChatMensaje { Rol = h.Rol ?? "user", Contenido = h.Contenido ?? "" })
            .Where(h => h.Contenido.Length > 0).ToList();

        var resultado = await ai.ConsultaLibreAsync(request.Pregunta, historial, request.BuscarEnWeb);
        return Ok(resultado);
    }

    /// <summary>Recomienda cantidades a comprar según lista de precio de proveedor, bonificaciones y ventas (últ. 30 d.).</summary>
    [HttpPost("recomendar-lista-proveedor")]
    public async Task<IActionResult> RecomendarListaProveedor([FromBody] RecomendarListaRequest request, CancellationToken ct)
    {
        if (request.IdLista <= 0) return BadRequest(new { error = "Id de lista inválido." });
        var r = await ai.RecomendarCompraConBonificacionesAsync(request.IdLista, request.DiasProyeccion, request.Instruccion, ct);
        return Ok(r);
    }
}

public record AiRequest(int Dias = 30, string? Instruccion = null, bool BuscarEnWeb = false, int? MaxFilasSugerencias = null);
public record ConsultaLibreMensajeReq(string? Rol, string? Contenido);
public record ConsultaLibreRequest(string Pregunta, IReadOnlyList<ConsultaLibreMensajeReq>? Historial = null, bool BuscarEnWeb = false);
