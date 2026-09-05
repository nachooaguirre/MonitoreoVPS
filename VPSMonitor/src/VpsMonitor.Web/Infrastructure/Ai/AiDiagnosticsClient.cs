namespace VpsMonitor.Web.Infrastructure.Ai;

using System.Text;
using System.Text.Json;
using VpsMonitor.Web.Data.Entities;
using VpsMonitor.Web.Infrastructure.Docker;
using VpsMonitor.Web.Infrastructure.Health;
using VpsMonitor.Web.Infrastructure.Prometheus;

public sealed record WebChatMessage(string Role, string Content);

public interface IAiDiagnosticsClient
{
    Task<string> DiagnosticReportAsync(HealthSummaryReport report, CancellationToken ct = default);
    Task<string> ChatAsync(string userMessage, List<WebChatMessage> history, HealthSummaryReport health, VpsMetrics? metrics, List<ProjectSummary>? projects = null, List<ProjectTask>? tasks = null, CancellationToken ct = default);
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

    private string GetConfiguredModel()
    {
        var configured = _configuration["Ai:Model"];
        if (string.IsNullOrWhiteSpace(configured) || string.Equals(configured, "deepseek-ai/deepseek-r1", StringComparison.OrdinalIgnoreCase))
        {
            return "deepseek-ai/deepseek-v4-pro-0813";
        }
        return configured;
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
            var modelToUse = GetConfiguredModel();
            var requestBody = new
            {
                model = modelToUse,
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
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound && modelToUse != "deepseek-ai/deepseek-v4-pro-0813")
            {
                // Retry with active fallback model
                var fallbackBody = new
                {
                    model = "deepseek-ai/deepseek-v4-pro-0813",
                    messages = requestBody.messages,
                    temperature = 0.6,
                    max_tokens = 1024
                };
                using var fallbackResp = await _httpClient.PostAsJsonAsync(requestUrl, fallbackBody, ct);
                if (fallbackResp.IsSuccessStatusCode)
                {
                    using var fdoc = await JsonDocument.ParseAsync(await fallbackResp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
                    return fdoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? GenerateFallbackReport(report);
                }
            }

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

    public async Task<string> ChatAsync(string userMessage, List<WebChatMessage> history, HealthSummaryReport health, VpsMetrics? metrics, List<ProjectSummary>? projects = null, List<ProjectTask>? tasks = null, CancellationToken ct = default)
    {
        var enabled = _configuration.GetValue("Ai:Enabled", false);
        if (!enabled)
        {
            return "El servicio de Inteligencia Artificial está deshabilitado en la configuración.";
        }

        try
        {
            var systemContext = BuildSystemContext(health, metrics, projects, tasks);
            var messagesList = new List<object>
            {
                new { role = "system", content = systemContext }
            };

            foreach (var h in history.TakeLast(6))
            {
                messagesList.Add(new { role = h.Role, content = h.Content });
            }

            messagesList.Add(new { role = "user", content = userMessage });

            var modelToUse = GetConfiguredModel();
            var requestBody = new
            {
                model = modelToUse,
                messages = messagesList,
                temperature = 0.7,
                max_tokens = 1024
            };

            var requestUrl = "chat/completions";
            using var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound && modelToUse != "deepseek-ai/deepseek-v4-pro-0813")
            {
                // Automatic 404 self-healing retry with active model
                var fallbackBody = new
                {
                    model = "deepseek-ai/deepseek-v4-pro-0813",
                    messages = messagesList,
                    temperature = 0.7,
                    max_tokens = 1024
                };
                using var fallbackResp = await _httpClient.PostAsJsonAsync(requestUrl, fallbackBody, ct);
                if (fallbackResp.IsSuccessStatusCode)
                {
                    using var fdoc = await JsonDocument.ParseAsync(await fallbackResp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
                    return fdoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "No se recibió respuesta de la IA.";
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AI Chat request to {Uri} failed with status code {StatusCode}", response.RequestMessage?.RequestUri, response.StatusCode);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return "⚠️ **Error de Autenticación de IA**: La API Key de NVIDIA es requerida o inválida. Por favor, configura `AI_API_KEY=nvapi-...` en tu archivo `deploy/.env` y reinicia el gateway.";
                }
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return $"⚠️ **Modelo de IA no disponible**: El modelo `{modelToUse}` no está activo en NVIDIA API. Configura `AI_MODEL=deepseek-ai/deepseek-v4-pro-0813` en tu archivo `deploy/.env`.";
                }
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
            _logger.LogWarning(ex, "Error calling ChatAsync on DeepSeek AI");
            return "Ocurrió un error al procesar tu mensaje con la IA. Consulta los logs del sistema.";
        }
    }

    private static string BuildSystemContext(HealthSummaryReport health, VpsMetrics? metrics, List<ProjectSummary>? projects, List<ProjectTask>? tasks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Eres un asistente virtual experto de IA (NVIDIA DeepSeek) en DevOps, SRE y monitoreo de VPS.");
        sb.AppendLine("Puedes responder a CUALQUIER pregunta o inquietud del usuario de forma natural, fluida, completa y amable.");
        sb.AppendLine("Tienes acceso constante a todo el inventario en vivo de la infraestructura:");
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

        if (projects != null && projects.Any())
        {
            sb.AppendLine("\n- Proyectos y Contenedores Actuales:");
            foreach (var p in projects)
            {
                sb.AppendLine($"  * Proyecto: {p.DisplayName} (Key: {p.ProjectKey}, Status: {p.OverallStatus})");
                foreach (var c in p.Containers)
                {
                    var aliasStr = !string.IsNullOrWhiteSpace(c.DisplayName) && c.DisplayName != c.Name ? $" [Alias: {c.DisplayName}]" : "";
                    sb.AppendLine($"    - Contenedor: {c.Name}{aliasStr} (ID: {c.Id[..Math.Min(12, c.Id.Length)]}, Estado: {c.State})");
                }
            }
        }

        if (tasks != null && tasks.Any())
        {
            sb.AppendLine("\n- Tareas Activas Registradas:");
            foreach (var t in tasks.Take(10))
            {
                sb.AppendLine($"  * [{t.Status}] {t.Title} (Proyecto: {t.ProjectKey}, Prioridad: {t.Priority})");
            }
        }

        sb.AppendLine("\nPuedes responder a cualquier consulta sobre el servidor, los contenedores, código, arquitectura, tareas, o realizar conversaciones generales. Si el usuario solicita renombrar o asignar un alias a un contenedor/proyecto, confírmaselo amablemente.");

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
