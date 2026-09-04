namespace VpsMonitor.Web.Infrastructure.Ai;

using System.Text;
using System.Text.Json;
using VpsMonitor.Web.Infrastructure.Health;

public interface IAiDiagnosticsClient
{
    Task<string> DiagnosticReportAsync(HealthSummaryReport report, CancellationToken ct = default);
}

public sealed class AiDiagnosticsClient : IAiDiagnosticsClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiDiagnosticsClient> _logger;

    public AiDiagnosticsClient(HttpClient httpClient, IConfiguration configuration, ILogger<AiDiagnosticsClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> DiagnosticReportAsync(HealthSummaryReport report, CancellationToken ct = default)
    {
        var enabled = _configuration.GetValue("Ai:Enabled", false);
        if (!enabled)
        {
            return GenerateFallbackReport(report);
        }

        try
        {
            var prompt = BuildDiagnosticPrompt(report);
            var requestBody = new
            {
                model = _configuration["Ai:Model"] ?? "deepseek-ai/deepseek-r1",
                messages = new[]
                {
                    new { role = "system", content = "Eres un ingeniero experto en SRE y DevOps. Analiza la infraestructura del VPS, salud de contenedores y métricas, y provee un diagnóstico conciso con 3 recomendaciones prácticas." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.6,
                max_tokens = 1024
            };

            var requestUrl = "chat/completions";
            using var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AI Diagnostics request returned status code {StatusCode}", response.StatusCode);
                return GenerateFallbackReport(report);
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content ?? GenerateFallbackReport(report);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to call AI Diagnostics API. Using fallback report.");
            return GenerateFallbackReport(report);
        }
    }

    private static string BuildDiagnosticPrompt(HealthSummaryReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Estado General VPS: {report.Status.ToUpper()}");
        sb.AppendLine($"Proyectos Totales: {report.TotalProjects} (Saludables: {report.HealthyProjects}, Degradados: {report.DegradedProjects}, Insaludables: {report.UnhealthyProjects})");
        sb.AppendLine($"Contenedores: {report.RunningContainers} ejecutándose, {report.UnhealthyContainers} con error, {report.StoppedContainers} detenidos");
        
        if (report.ActiveAlerts.Any())
        {
            sb.AppendLine("Alertas Activas:");
            foreach (var a in report.ActiveAlerts)
            {
                sb.AppendLine($"- [{a.Severity}] {a.AlertName}: {a.Summary}");
            }
        }

        return sb.ToString();
    }

    private static string GenerateFallbackReport(HealthSummaryReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"### Diagnóstico de Sistema — Estado: {report.Status.ToUpper()}");
        sb.AppendLine($"- **Proyectos**: {report.HealthyProjects}/{report.TotalProjects} en estado óptimo.");
        sb.AppendLine($"- **Contenedores**: {report.RunningContainers} activos, {report.UnhealthyContainers} fallando, {report.StoppedContainers} detenidos.");
        
        if (report.ActiveAlerts.Any())
        {
            sb.AppendLine("\n**Alertas Críticas / Advertencias detectadas:**");
            foreach (var a in report.ActiveAlerts)
            {
                sb.AppendLine($"  - **{a.AlertName}** (`{a.Severity}`): {a.Summary}");
            }
            sb.AppendLine("\n*Recomendación*: Revisar logs de contenedores afectados y evaluar recursos de CPU/RAM del VPS.");
        }
        else
        {
            sb.AppendLine("\n✅ *Todos los servicios y contenedores operan dentro de los parámetros normales.*");
        }

        return sb.ToString();
    }
}
