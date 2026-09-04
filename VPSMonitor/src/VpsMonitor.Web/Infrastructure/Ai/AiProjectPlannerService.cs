namespace VpsMonitor.Web.Infrastructure.Ai;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using VpsMonitor.Web.Infrastructure.Docker;

public sealed record PlannedTaskResult(
    string ProjectKey,
    string ContainerName,
    string Title,
    string Description,
    string Priority,
    List<string> ActionPlanSteps
);

public interface IAiProjectPlannerService
{
    Task<PlannedTaskResult> PlanTaskFromProposalAsync(string rawInput, List<ProjectSummary> availableProjects, CancellationToken ct = default);
}

public sealed class AiProjectPlannerService : IAiProjectPlannerService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiProjectPlannerService> _logger;

    public AiProjectPlannerService(HttpClient httpClient, IConfiguration configuration, ILogger<AiProjectPlannerService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PlannedTaskResult> PlanTaskFromProposalAsync(string rawInput, List<ProjectSummary> availableProjects, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return CreateFallbackResult(rawInput, availableProjects);
        }

        var enabled = _configuration.GetValue("Ai:Enabled", true);
        if (!enabled)
        {
            return CreateFallbackResult(rawInput, availableProjects);
        }

        try
        {
            var sbList = new StringBuilder();
            foreach (var p in availableProjects)
            {
                var cList = string.Join(", ", p.Containers.Select(c => c.Name));
                sbList.AppendLine($"- Proyecto Key: '{p.ProjectKey}', Nombre: '{p.DisplayName}', Contenedores: [{cList}]");
            }

            var prompt = $@"Proyectos y contenedores activos en la infraestructura:
{sbList}

Texto de la propuesta / requerimiento del cliente o usuario:
""{rawInput}""

Responde ÚNICAMENTE con un objeto JSON válido (sin etiquetas markdown ni texto explicativo) usando esta estructura exacta:
{{
  ""ProjectKey"": ""key_del_proyecto_mas_cercano"",
  ""ContainerName"": ""nombre_del_contenedor_especifico_si_se_menciona_o_vacio"",
  ""Title"": ""Título corto de la tarea"",
  ""Description"": ""Descripción detallada del trabajo a realizar"",
  ""Priority"": ""High"",
  ""ActionPlanSteps"": [
    ""Paso 1: ..."",
    ""Paso 2: ...""
  ]
}}";

            var requestBody = new
            {
                model = _configuration["Ai:Model"] ?? "deepseek-ai/deepseek-r1",
                messages = new[]
                {
                    new { role = "system", content = "Eres un Tech Lead y Arquitecto SRE experto. Analizas propuestas de clientes y asocias las tareas al proyecto y contenedor específico correspondiente en JSON estricto." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.3,
                max_tokens = 1024
            };

            using var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AI Task Planner returned status code {StatusCode}", response.StatusCode);
                return CreateFallbackResult(rawInput, availableProjects);
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var rawContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(rawContent))
            {
                return CreateFallbackResult(rawInput, availableProjects);
            }

            return ParseAiJsonResponse(rawContent, availableProjects) ?? CreateFallbackResult(rawInput, availableProjects);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI Project Task planning response.");
            return CreateFallbackResult(rawInput, availableProjects);
        }
    }

    private static PlannedTaskResult? ParseAiJsonResponse(string rawContent, List<ProjectSummary> availableProjects)
    {
        try
        {
            var jsonText = rawContent.Trim();
            if (jsonText.StartsWith("```"))
            {
                var firstLineEnd = jsonText.IndexOf('\n');
                var lastBackticks = jsonText.LastIndexOf("```");
                if (firstLineEnd > 0 && lastBackticks > firstLineEnd)
                {
                    jsonText = jsonText.Substring(firstLineEnd + 1, lastBackticks - firstLineEnd - 1).Trim();
                }
            }

            using var jsonDoc = JsonDocument.Parse(jsonText);
            var root = jsonDoc.RootElement;

            var projectKey = root.TryGetProperty("ProjectKey", out var pk) ? pk.GetString() ?? "unassigned" : "unassigned";
            var containerName = root.TryGetProperty("ContainerName", out var cn) ? cn.GetString() ?? "" : "";
            var title = root.TryGetProperty("Title", out var t) ? t.GetString() ?? "Nueva Tarea" : "Nueva Tarea";
            var description = root.TryGetProperty("Description", out var d) ? d.GetString() ?? "" : "";
            var priority = root.TryGetProperty("Priority", out var pr) ? pr.GetString() ?? "Medium" : "Medium";

            var steps = new List<string>();
            if (root.TryGetProperty("ActionPlanSteps", out var stepsArr) && stepsArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var step in stepsArr.EnumerateArray())
                {
                    var s = step.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        steps.Add(s);
                    }
                }
            }

            var matchedProj = availableProjects.FirstOrDefault(p =>
                string.Equals(p.ProjectKey, projectKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.DisplayName, projectKey, StringComparison.OrdinalIgnoreCase) ||
                p.Containers.Any(c => string.Equals(c.Name, containerName, StringComparison.OrdinalIgnoreCase) || c.Name.Contains(containerName, StringComparison.OrdinalIgnoreCase)));

            if (matchedProj != null)
            {
                projectKey = matchedProj.ProjectKey;
                if (string.IsNullOrWhiteSpace(containerName) && matchedProj.Containers.Any())
                {
                    var matchedC = matchedProj.Containers.FirstOrDefault(c => rawContent.Contains(c.Name, StringComparison.OrdinalIgnoreCase));
                    if (matchedC != null) containerName = matchedC.Name;
                }
            }
            else if (!availableProjects.Any(p => string.Equals(p.ProjectKey, projectKey, StringComparison.OrdinalIgnoreCase)))
            {
                projectKey = availableProjects.FirstOrDefault()?.ProjectKey ?? "unassigned";
            }

            if (!steps.Any())
            {
                steps.Add("Analizar requerimientos del cliente");
                steps.Add("Diseñar e implementar cambios en el código");
                steps.Add("Probar y desplegar en producción");
            }

            return new PlannedTaskResult(projectKey, containerName, title, description, priority, steps);
        }
        catch
        {
            return null;
        }
    }

    private static PlannedTaskResult CreateFallbackResult(string rawInput, List<ProjectSummary> availableProjects)
    {
        var matchedProject = availableProjects.FirstOrDefault(p =>
            rawInput.Contains(p.ProjectKey, StringComparison.OrdinalIgnoreCase) ||
            rawInput.Contains(p.DisplayName, StringComparison.OrdinalIgnoreCase))
            ?? availableProjects.FirstOrDefault()
            ?? new ProjectSummary("unassigned", "unassigned", 0, new List<DockerContainerInfo>(), 0, "healthy", "unassigned");

        var containerName = matchedProject.Containers.FirstOrDefault(c => rawInput.Contains(c.Name, StringComparison.OrdinalIgnoreCase))?.Name ?? "";
        var title = rawInput.Length > 40 ? rawInput.Substring(0, 37) + "..." : rawInput;
        var steps = new List<string>
        {
            "Revisar propuesta del cliente",
            "Definir alcance e implementación",
            "Ejecutar y validar cambios"
        };

        return new PlannedTaskResult(matchedProject.ProjectKey, containerName, title, rawInput, "Medium", steps);
    }
}
