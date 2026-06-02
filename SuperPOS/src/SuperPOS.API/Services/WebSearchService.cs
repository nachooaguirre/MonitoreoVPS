using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;

namespace SuperPOS.API.Services;

/// <summary>
/// Orden de preferencia: WebSearch:TavilyApiKey → WebSearch:BingApiKey → DuckDuckGo Instant (sin clave, resultados acotados).
/// </summary>
public class WebSearchService(IConfiguration config, IHttpClientFactory httpFactory, ILogger<WebSearchService> log) : IWebSearchService
{
    public async Task<string?> BuscarResumenWebAsync(string consulta, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(consulta)) return null;
        var q = consulta.Trim();
        if (q.Length > 400) q = q[..400];

        try
        {
            var tavily = config["WebSearch:TavilyApiKey"];
            if (!string.IsNullOrWhiteSpace(tavily))
            {
                var t = await BuscarTavilyAsync(q, tavily, cancellationToken);
                if (!string.IsNullOrWhiteSpace(t)) return Truncar(t, 12_000);
            }

            var bing = config["WebSearch:BingApiKey"];
            if (!string.IsNullOrWhiteSpace(bing))
            {
                var t = await BuscarBingAsync(q, bing, cancellationToken);
                if (!string.IsNullOrWhiteSpace(t)) return Truncar(t, 12_000);
            }

            var d = await BuscarDuckDuckGoConReintentoAsync(q, cancellationToken);
            if (!string.IsNullOrWhiteSpace(d)) return Truncar(d, 8_000);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Búsqueda web falló para: {Q}", q);
        }

        return null;
    }

    private async Task<string?> BuscarDuckDuckGoConReintentoAsync(string q, CancellationToken ct)
    {
        var a = await BuscarDuckDuckGoAsync(q, ct);
        if (!string.IsNullOrWhiteSpace(a)) return a;
        if (q.Length < 8) return null;
        return await BuscarDuckDuckGoAsync(q + " precio supermercado", ct);
    }

    private async Task<string?> BuscarTavilyAsync(string query, string apiKey, CancellationToken ct)
    {
        var http = httpFactory.CreateClient("websearch");
        var body = new
        {
            api_key = apiKey,
            query,
            search_depth = "basic",
            max_results = 8,
            include_answer = true
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.tavily.com/search")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        var resp = await http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) return null;
        await using var s = await resp.Content.ReadAsStreamAsync(ct);
        var node = await JsonNode.ParseAsync(s, cancellationToken: ct) as JsonObject;
        if (node is null) return null;
        var sb = new StringBuilder();
        if (node["answer"] is JsonValue jans && jans.TryGetValue<string>(out var ans) && !string.IsNullOrEmpty(ans))
            sb.AppendLine("Resumen: ").AppendLine(ans);
        if (node["results"] is JsonArray arr)
        {
            var i = 0;
            foreach (var it in arr)
            {
                if (it is not JsonObject o) continue;
                i++;
                var title = o["title"]?.ToString() ?? "";
                var url = o["url"]?.ToString() ?? "";
                var content = o["content"]?.ToString() ?? "";
                sb.AppendLine($"[{i}] {title}");
                if (!string.IsNullOrEmpty(url)) sb.AppendLine(url);
                if (!string.IsNullOrEmpty(content)) sb.AppendLine(content);
                sb.AppendLine();
            }
        }
        return sb.Length == 0 ? null : sb.ToString();
    }

    private async Task<string?> BuscarBingAsync(string query, string bingKey, CancellationToken ct)
    {
        var http = httpFactory.CreateClient("websearch");
        var url = "https://api.bing.microsoft.com/v7.0/search?q=" + Uri.EscapeDataString(query) + "&count=8&mkt=es-AR";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("Ocp-Apim-Subscription-Key", bingKey);
        var resp = await http.SendAsync(req, HttpCompletionOption.ResponseContentRead, ct);
        if (!resp.IsSuccessStatusCode) return null;
        await using var s = await resp.Content.ReadAsStreamAsync(ct);
        var node = await JsonNode.ParseAsync(s, cancellationToken: ct) as JsonObject;
        var pages = node?["webPages"]?["value"] as JsonArray;
        if (pages is null || pages.Count == 0) return null;
        var sb = new StringBuilder();
        var i = 0;
        foreach (var p in pages)
        {
            if (p is not JsonObject w) continue;
            i++;
            var name = w["name"]?.ToString() ?? "";
            var u = w["url"]?.ToString() ?? "";
            var sn = w["snippet"]?.ToString() ?? "";
            sb.AppendLine($"[{i}] {name}").AppendLine(u).AppendLine(sn).AppendLine();
        }
        return sb.ToString();
    }

    private async Task<string?> BuscarDuckDuckGoAsync(string query, CancellationToken ct)
    {
        var http = httpFactory.CreateClient("websearch");
        var url = "https://api.duckduckgo.com/?q=" + Uri.EscapeDataString(query) + "&format=json&no_html=1&skip_disambig=1";
        var resp = await http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;
        await using var s = await resp.Content.ReadAsStreamAsync(ct);
        var node = await JsonNode.ParseAsync(s, cancellationToken: ct) as JsonObject;
        if (node is null) return null;
        var sb = new StringBuilder();
        if (node["AbstractText"] is JsonValue at && at.TryGetValue<string>(out var abs) && abs.Length > 0)
        {
            sb.AppendLine("Resumen (DuckDuckGo):").AppendLine(abs);
            if (node["AbstractURL"] is JsonValue au && au.TryGetValue<string>(out var auStr))
                sb.AppendLine(auStr);
            sb.AppendLine();
        }
        if (node["Answer"] is JsonValue an && an.TryGetValue<string>(out var ans) && ans.Length > 0)
            sb.AppendLine("Respuesta directa: ").AppendLine(ans).AppendLine();
        if (node["Infobox"] is JsonObject box)
        {
            if (box["Text"] is JsonValue tbx && tbx.TryGetValue<string>(out var inf) && !string.IsNullOrWhiteSpace(inf))
            {
                sb.AppendLine("Ficha (DuckDuckGo):");
                sb.AppendLine(inf);
                if (box["Url"] is JsonValue u0 && u0.TryGetValue<string>(out var u0s))
                    sb.AppendLine(u0s);
                sb.AppendLine();
            }
        }

        if (node["Results"] is JsonArray results)
        {
            var n = 0;
            foreach (var r in results)
            {
                if (n >= 6) break;
                if (r is not JsonObject ro) continue;
                string? t = null;
                if (ro["Text"] is JsonValue jt && jt.TryGetValue<string>(out var ts) && !string.IsNullOrWhiteSpace(ts)) t = ts;
                else if (ro["Result"] is JsonValue jr && jr.TryGetValue<string>(out var rs) && !string.IsNullOrWhiteSpace(rs)) t = rs;
                if (string.IsNullOrWhiteSpace(t)) continue;
                n++;
                if (ro["FirstURL"] is JsonValue f && f.TryGetValue<string>(out var fu) && !string.IsNullOrEmpty(fu))
                    sb.AppendLine($"[{n}] {t}").AppendLine(fu);
                else
                    sb.AppendLine($"[{n}] {t}");
                sb.AppendLine();
            }
        }

        if (node["RelatedTopics"] is JsonArray rel)
        {
            var n = 0;
            foreach (var t in rel)
            {
                if (n >= 6) break;
                if (t is JsonObject ro && ro["Text"] is JsonValue jtx && jtx.TryGetValue<string>(out var tx) && !string.IsNullOrEmpty(tx))
                {
                    n++;
                    sb.AppendLine("• " + tx);
                }
                else if (t is JsonObject withTopics && withTopics["Topics"] is JsonArray sub)
                {
                    foreach (var u in sub)
                    {
                        if (n >= 6) break;
                        if (u is JsonObject uo && uo["Text"] is JsonValue jutx && jutx.TryGetValue<string>(out var utx) && !string.IsNullOrEmpty(utx))
                        {
                            n++;
                            sb.AppendLine("  • " + utx);
                        }
                    }
                }
            }
        }
        return sb.Length == 0 ? null : sb.ToString();
    }

    private static string Truncar(string t, int max)
        => t.Length <= max ? t : t[..max] + "\n[…contenido truncado…]";
}
