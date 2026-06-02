using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SuperPOS.API;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Services;

public partial class DeepSeekAiService
{
    public async Task<AiImportListaProveedorResult> EstructurarListaProveedorAsync(
        string? textoBruto, string? imagenBase64, string? imagenMime, string? nombreProveedor, CancellationToken cancellationToken = default)
    {
        var prov = string.IsNullOrWhiteSpace(nombreProveedor) ? "Proveedor" : nombreProveedor;
        if (!string.IsNullOrEmpty(imagenBase64))
        {
            var pImg = """
                Sos un asistente de compras. Analizá la imagen (lista de precios / tarifa de proveedor) y generá un JSON estricto SIN markdown, SIN comentarios.
                Esquema: raíz con "lineas" = array. Cada ítem: codigo, descripcion, precioUnitario, ivaPorcentaje o null, bonificaciones = array de escalas: cantidadMin, porcentaje, nota.
                - El campo "codigo" (ASCII, sin tilde) es CRÍTICO: en CADA fila copiá el código de producto o el código de barras EAN tal como aparezca en la tabla (columnas tipo Cód., Art., Código, EAN, Cód. interno, etc.). Eso vincula con el depósito: sin código o EAN leíble, poner el identificador más corto que distinga la fila.
                - Los códigos alfanuméricos copiálos sin reordenar ni inventar. Si la fila solo muestra EAN, usá el EAN completo (8-13 dígitos) en "codigo".
                - bonificaciones: [] si no hay. Usá números con punto decimal.
                - Usá las claves exactas en inglés/ASCII: "codigo", "descripcion", "precioUnitario", "ivaPorcentaje", "bonificaciones" (no uses "código" con tilde en las claves JSON).
                Proveedor: 
                """ + prov + "\n";
            var raw = await LlamarDeepSeekConImagenAsync(pImg, imagenBase64, imagenMime ?? "image/jpeg", cancellationToken);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new AiImportListaProveedorResult
                {
                    Exito = false,
                    Error = "No se pudo leer la imagen. Probá PDF o Excel, o configurá DeepSeek:VisionModel (modelo con visión) en appsettings."
                };
            }
            return ParseResultadoLineasJson(raw, null);
        }

        if (string.IsNullOrWhiteSpace(textoBruto))
            return new AiImportListaProveedorResult { Exito = false, Error = "No hay contenido para analizar." };

        var trunc = textoBruto;
        if (trunc.Length > 100_000) trunc = trunc[..100_000] + "\n[…texto truncado]";

        var prompt = """
            Sos un asistente en compras B2B de supermercado. A partir del volcado, generá un JSON estricto SIN comentarios, SIN markdown.
            Esquema: "lineas" = array. Cada ítem: codigo, descripcion, precioUnitario, ivaPorcentaje, bonificaciones = array de cantidadMin, porcentaje, nota.
            - Cada "codigo" debe ser el código de proveedor, interno o EAN de la fila; si el volcado lo tiene en columnas separadas, uní la referencia al producto. Claves JSON en ASCII: "codigo", "descripcion" (no "código"/"descripción" con tildes en las claves).
            - bonificaciones: escalas por volumen; [] si no hay. Ordená por cantidadMin ascendente. Excluís totales y encabezados sueltos.
            Proveedor: 
            """ + prov + """

            Contenido:
            ---
            """ + trunc + "\n---\n";

        var raw2 = await LlamarDeepSeekAsync(prompt, 8192);
        if (string.IsNullOrWhiteSpace(raw2))
            return new AiImportListaProveedorResult { Exito = false, Error = "La IA no devolvió respuesta." };
        return ParseResultadoLineasJson(raw2, null);
    }

    private AiImportListaProveedorResult ParseResultadoLineasJson(string raw, string? aviso)
    {
        var jsonSolo = ExtraerPrimerBloqueJsonObjeto(raw);
        if (string.IsNullOrEmpty(jsonSolo))
            return new AiImportListaProveedorResult
            {
                Exito = false,
                Error = "No se pudo extraer JSON de la respuesta de la IA.",
                AvisoOrigen = raw.Length > 400 ? raw[..400] + "…" : raw
            };
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var wrapper = JsonSerializer.Deserialize<LineasWrapperIa>(jsonSolo, options);
            if (wrapper?.Lineas is null or { Count: 0 })
                return new AiImportListaProveedorResult { Exito = false, Error = "El JSON no contiene líneas." };

            return new AiImportListaProveedorResult
            {
                Exito = true,
                AvisoOrigen = aviso,
                Lineas = wrapper.Lineas.Select(x => new LineaImportProveedorDto
                {
                    CodigoProveedor = (x.CodigoNormalizado() ?? "").Trim(),
                    Descripcion = (x.DescripcionNormalizada() ?? "").Trim(),
                    PrecioUnitario = x.PrecioUnitario,
                    IvaPorcentaje = x.IvaPorcentaje,
                    Bonificaciones = (x.Bonificaciones ?? [])
                        .Select(b => new BonifEscalaImportDto
                        {
                            CantidadMin = b.CantidadMin,
                            Porcentaje = b.Porcentaje,
                            Nota = b.Nota
                        }).ToList()
                }).Where(l => l.PrecioUnitario > 0 || !string.IsNullOrWhiteSpace(l.Descripcion)).ToList()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Parse JSON lista proveedor");
            return new AiImportListaProveedorResult { Exito = false, Error = $"JSON inválido: {ex.Message}" };
        }
    }

    private sealed class LineasWrapperIa
    {
        [JsonPropertyName("lineas")]
        public List<LineaIaDeser>? Lineas { get; set; }
    }

    private sealed class LineaIaDeser
    {
        [JsonPropertyName("codigo")]
        public string? Codigo { get; set; }
        // La IA a veces devuelve "código"/"descripción" con tilde; JSON distingue claves
        [JsonPropertyName("c\u00F3digo")]
        public string? CodigoConAcento { get; set; }
        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }
        [JsonPropertyName("descripci\u00F3n")]
        public string? DescripcionConAcento { get; set; }
        [JsonPropertyName("precioUnitario")]
        public decimal PrecioUnitario { get; set; }
        [JsonPropertyName("ivaPorcentaje")]
        public decimal? IvaPorcentaje { get; set; }
        [JsonPropertyName("bonificaciones")]
        public List<BonifIaDeser>? Bonificaciones { get; set; }

        public string? CodigoNormalizado() => !string.IsNullOrWhiteSpace(Codigo) ? Codigo : CodigoConAcento;
        public string? DescripcionNormalizada() => !string.IsNullOrWhiteSpace(Descripcion) ? Descripcion : DescripcionConAcento;
    }

    private sealed class BonifIaDeser
    {
        [JsonPropertyName("cantidadMin")]
        public decimal CantidadMin { get; set; }
        [JsonPropertyName("porcentaje")]
        public decimal Porcentaje { get; set; }
        [JsonPropertyName("nota")]
        public string? Nota { get; set; }
    }

    private static string? ExtraerPrimerBloqueJsonObjeto(string raw)
    {
        var t = raw.Trim();
        if (t.StartsWith("```", StringComparison.Ordinal))
        {
            var l = t.IndexOf('\n');
            if (l > 0) t = t[(l + 1)..].Trim();
            if (t.EndsWith("```", StringComparison.Ordinal)) t = t[..^3].Trim();
        }
        var i0 = t.IndexOf('{');
        if (i0 < 0) return null;
        var depth = 0;
        for (var i = i0; i < t.Length; i++)
        {
            if (t[i] == '{') depth++;
            else if (t[i] == '}')
            {
                depth--;
                if (depth == 0) return t.Substring(i0, i - i0 + 1);
            }
        }
        return null;
    }

    public async Task<AiRespuesta> RecomendarCompraConBonificacionesAsync(
        int idListaProveedor, int diasProyeccion, string? instruccion, CancellationToken cancellationToken = default)
    {
        var lista = await db.ListasPrecioProveedor
            .AsNoTracking()
            .Include(x => x.Proveedor)
            .FirstOrDefaultAsync(x => x.Id == idListaProveedor, cancellationToken);
        if (lista is null) return new AiRespuesta { Exito = false, Texto = "", Error = "Lista no encontrada." };
        if (diasProyeccion < 1) diasProyeccion = 7;

        var lineas = await db.ListasPrecioProveedorLineas
            .AsNoTracking()
            .Where(l => l.IdLista == idListaProveedor)
            .OrderBy(l => l.Descripcion)
            .ToListAsync(cancellationToken);
        if (lineas.Count == 0)
            return new AiRespuesta { Exito = false, Texto = "", Error = "La lista no tiene líneas." };

        var idArts = lineas.Where(l => l.IdArticulo.HasValue).Select(l => l.IdArticulo!.Value).ToList();
        var desde = DateTime.UtcNow.AddDays(-30);
        var ventasQ = from d in db.ComprobantesDetalle.AsNoTracking()
            where d.Comprobante != null
                  && d.Comprobante.Fecha >= desde
                  && d.Comprobante.Estado != EstadoComprobante.Anulado
                  && idArts.Contains(d.IdArticulo)
            group d by d.IdArticulo into g
            select new { Id = g.Key, Q = g.Sum(x => x.Cantidad) };
        var ventasMap = await ventasQ.ToDictionaryAsync(x => x.Id, x => x.Q, cancellationToken);
        const int dVent = 30;
        var carga = lineas.Select(l =>
        {
            decimal? porDia = null;
            if (l.IdArticulo.HasValue && ventasMap.TryGetValue(l.IdArticulo.Value, out var v))
                porDia = v / dVent;
            return new
            {
                l.Id,
                l.CodigoProveedor,
                l.Descripcion,
                l.PrecioUnitario,
                l.IvaPorcentaje,
                l.BonificacionesJson,
                l.IdArticulo,
                ventaPromedioDiariaAprox = porDia,
                necesidadProyectada = porDia * diasProyeccion
            };
        }).ToList();

        var dataJson = JsonSerializer.Serialize(new
        {
            proveedor = lista.Proveedor?.RazonSocial,
            idProveedor = lista.IdProveedor,
            nombreLista = lista.Nombre,
            dias = diasProyeccion,
            lineas = carga
        }, _json);

        var inst = string.IsNullOrWhiteSpace(instruccion) ? "" : $"\n\nInstrucción adicional: {instruccion}\n";
        var prompt = $"""
            Sos asistente de compras. JSON con líneas de lista de precio de proveedor; BonificacionesJson tiene escalas de descuento por umbral.
            Necesidad: proyectar para ~{diasProyeccion} días. Si `ventaPromedioDiariaAprox` es null, el artículo no está vinculado: indicá vincular en Stock. Compará ofertas por escalas y sugerí cantidades.
            {inst}
            {dataJson}
            Respondé en español: 1) Resumen. 2) Recomendaciones. 3) Consejos concretos de lotes/umbrales. No repitas el JSON.
            """;
        var t = await LlamarDeepSeekAsync(prompt, 6000);
        return new AiRespuesta { Exito = t != null, Texto = t ?? "Sin respuesta de IA", Error = t == null ? "DeepSeek" : null };
    }

    private async Task<string?> LlamarDeepSeekConImagenAsync(string userText, string base64, string mime, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ApiKey)) return null;
        var visionModel = config["DeepSeek:VisionModel"] ?? Model;
        try
        {
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(120);
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ApiKey);
            var dataUrl = $"data:{mime};base64,{base64}";
            var body = new
            {
                model = visionModel,
                messages = new object[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = userText },
                            new { type = "image_url", image_url = new { url = dataUrl } }
                        }
                    }
                },
                temperature = 0.1,
                max_tokens = 8192
            };
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await http.PostAsync($"{BaseUrl}/chat/completions", content, ct);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
            return json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }
        catch
        {
            return null;
        }
    }
}
