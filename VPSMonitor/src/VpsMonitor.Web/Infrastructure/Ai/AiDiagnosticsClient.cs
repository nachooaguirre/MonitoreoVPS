namespace VpsMonitor.Web.Infrastructure.Ai;

using System.Text;
using System.Text.Json;
using VpsMonitor.Web.Infrastructure.Health;

using VpsMonitor.Web.Infrastructure.Prometheus;

public sealed record WebChatMessage(string Role, string Content);

public interface IAiDiagnosticsClient
{
    Task<string> DiagnosticReportAsync(HealthSummaryReport report, CancellationToken ct = default);
    Task<string> ChatAsync(string userMessage, List<WebChatMessage> history, HealthSummaryReport health, VpsMetrics? metrics, CancellationToken ct = default);
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

    public async Task<string> ChatAsync(string userMessage, List<WebChatMessage> history, HealthSummaryReport health, VpsMetrics? metrics, CancellationToken ct = default)
    {
        var enabled = _configuration.GetValue("Ai:Enabled", false);
        if (!enabled)
        {
            return "El servicio de Inteligencia Artificial (NVIDIA DeepSeek-R1) está deshabilitado en la configuración.";
        }

        try
        {
            var systemContext = BuildSystemContext(health, metrics);
            var messagesList = new List<object>
            {
                new { role = "system", content = systemContext }
            };

            foreach (var h in history.TakeLast(6))
            {
                messagesList.Add(new { role = h.Role, content = h.Content });
            }

            messagesList.Add(new { role = "user", content = userMessage });

            var requestBody = new
            {
                model = _configuration["Ai:Model"] ?? "deepseek-ai/deepseek-r1",
                messages = messagesList,
                temperature = 0.7,
                max_tokens = 1024
            };

            var requestUrl = "chat/completions";
            using var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AI Chat request to {Uri} failed with status code {StatusCode}", response.RequestMessage?.RequestUri, response.StatusCode);
                return $"Error de conexión con la IA (Status {response.StatusCode}). Por favor reintenta en unos instantes.";
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content ?? "No se recibió respuesta de la IA.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calling ChatAsync on DeepSeek-R1");
            return "Ocurrió un error al procesar tu mensaje con la IA. Consulta los logs del sistema.";
        }
    }

    private static string BuildSystemContext(HealthSummaryReport health, VpsMetrics? metrics)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Eres un asistente virtual de IA (NVIDIA DeepSeek-R1) integrado en el panel de control de VPS Monitor.");
        sb.AppendLine("Responde en español de forma amable, natural, profesional y concisa.");
        sb.AppendLine("Tienes acceso al estado en vivo del servidor VPS:");
        sb.AppendLine($"- Estado global: {health.Status.ToUpper()}");
        sb.AppendLine($"- Proyectos totales: {health.TotalProjects} (Saludables: {health.HealthyProjects}, Degradados: {health.DegradedProjects}, Insaludables: {health.UnhealthyProjects})");
        sb.AppendLine($"- Contenedores: {health.RunningContainers} activos, {health.UnhealthyContainers} fallando, {health.StoppedContainers} detenidos");
        
        if (metrics != null)
        {
            sb.AppendLine($"- CPU: {metrics.CpuPercent}%");
            sb.AppendLine($"- RAM: {metrics.MemoryUsedGb} GB / {metrics.MemoryTotalGb} GB ({metrics.MemoryPercent}%)");
            sb.AppendLine($"- Disco: {metrics.DiskPercent}%");
            sb.AppendLine($"- Load Average (1m, 5m, 15m): {metrics.Load1m}, {metrics.Load5m}, {metrics.Load15m}");
            sb.AppendLine($"- Uptime: {TimeSpan.FromSeconds(metrics.UptimeSeconds).TotalDays:F1} días");
        }

        if (health.ActiveAlerts.Any())
        {
            sb.AppendLine("- Alertas Activas:");
            foreach (var a in health.ActiveAlerts)
            {
                sb.AppendLine($"  * {a.AlertName} ({a.Severity}): {a.Summary}");
            }
        }
        else
        {
            sb.AppendLine("- Alertas Activas: Ninguna (0 alertas)");
        }

        sb.AppendLine("\nSi el usuario pide expresamente crear o planificar una tarea para un proyecto o contenedor, instrúyelo o confirma amablemente.");

        return sb.ToString();
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
