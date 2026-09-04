namespace VpsMonitor.Web.Infrastructure.Telegram;

using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VpsMonitor.Web.Data;
using VpsMonitor.Web.Data.Entities;
using VpsMonitor.Web.Infrastructure.Ai;
using VpsMonitor.Web.Infrastructure.Docker;
using VpsMonitor.Web.Infrastructure.Health;
using VpsMonitor.Web.Infrastructure.Prometheus;

public interface ITelegramNotificationDispatcher
{
    Task SendAlertAsync(string alertTitle, string alertMessage, CancellationToken ct = default);
    Task SendTextMessageAsync(string messageText, CancellationToken ct = default);
}

public sealed class TelegramBotService : BackgroundService, ITelegramNotificationDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly HttpClient _httpClient;
    private long _lastUpdateId;

    public TelegramBotService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<TelegramBotService> logger,
        HttpClient httpClient)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task SendAlertAsync(string alertTitle, string alertMessage, CancellationToken ct = default)
    {
        var formatted = $"<b>🚨 ALERTA VPS MONITOR</b>\n<b>{WebUtilityEncode(alertTitle)}</b>\n\n{WebUtilityEncode(alertMessage)}";
        await SendTextMessageAsync(formatted, ct);
    }

    public async Task SendTextMessageAsync(string messageText, CancellationToken ct = default)
    {
        var (botToken, chatId) = await GetTelegramCredentialsAsync(ct);
        if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
        {
            return;
        }

        try
        {
            var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
            var body = new
            {
                chat_id = chatId,
                text = messageText,
                parse_mode = "HTML"
            };

            using var response = await _httpClient.PostAsJsonAsync(url, body, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Telegram sendMessage failed with status {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send Telegram message.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Telegram Bot Service active and waiting for credentials...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var (botToken, chatId) = await GetTelegramCredentialsAsync(stoppingToken);
                if (!string.IsNullOrWhiteSpace(botToken))
                {
                    await PollTelegramUpdatesAsync(botToken, chatId, stoppingToken);
                }
                else
                {
                    _logger.LogDebug("Telegram Bot Token is not configured yet.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in Telegram polling loop.");
            }

            await Task.Delay(3000, stoppingToken);
        }
    }

    private async Task PollTelegramUpdatesAsync(string botToken, string configuredChatId, CancellationToken ct)
    {
        var offsetParam = _lastUpdateId > 0 ? $"offset={_lastUpdateId + 1}&" : "";
        var url = $"https://api.telegram.org/bot{botToken}/getUpdates?{offsetParam}timeout=2";
        using var response = await _httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Telegram getUpdates returned status code {StatusCode}", response.StatusCode);
            return;
        }

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root = doc.RootElement;

        if (root.TryGetProperty("ok", out var okProp) && okProp.GetBoolean() &&
            root.TryGetProperty("result", out var resultArr) && resultArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in resultArr.EnumerateArray())
            {
                var updateId = item.GetProperty("update_id").GetInt64();
                if (updateId > _lastUpdateId)
                {
                    _lastUpdateId = updateId;
                }

                if (item.TryGetProperty("message", out var messageObj))
                {
                    await ProcessTelegramMessageAsync(botToken, configuredChatId, messageObj, ct);
                }
            }
        }
    }

    private async Task ProcessTelegramMessageAsync(string botToken, string configuredChatId, JsonElement messageObj, CancellationToken ct)
    {
        if (!messageObj.TryGetProperty("text", out var textProp)) return;
        var text = textProp.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        var chatIdFromMsg = messageObj.GetProperty("chat").GetProperty("id").ToString();

        // If configuredChatId is empty, store this chatId as active!
        if (string.IsNullOrWhiteSpace(configuredChatId))
        {
            await SaveChatIdAsync(chatIdFromMsg, ct);
            configuredChatId = chatIdFromMsg;
        }

        // Handle commands and AI requests
        using var scope = _serviceProvider.CreateScope();
        var healthRunner = scope.ServiceProvider.GetRequiredService<IHealthCheckRunner>();
        var prometheusClient = scope.ServiceProvider.GetRequiredService<IPrometheusQueryClient>();
        var projectGrouping = scope.ServiceProvider.GetRequiredService<IProjectGroupingService>();
        var aiPlanner = scope.ServiceProvider.GetRequiredService<IAiProjectPlannerService>();
        var aiDiagnostics = scope.ServiceProvider.GetRequiredService<IAiDiagnosticsClient>();
        var db = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();

        var textLower = text.ToLowerInvariant();

        if (textLower is "/start" or "/help")
        {
            var helpMsg = @"<b>🤖 Bot de Asistencia VPS & IA</b>

Comandos y funciones disponibles:
• <b>/status</b> o <i>'¿cómo está todo?'</i> — Estado de la infraestructura y CPU/RAM
• <b>'proyectos'</b> — Lista de todos los proyectos activos
• <b>'tarea: [propuesta de cliente]'</b> — Asigna la tarea con DeepSeek-R1 y genera la planificación
• <i>'¿cómo está SuperPOS?'</i> — Estado detallado de un proyecto específico
• Cualquier otra consulta técnica — Diagnóstico SRE con IA";
            await ReplyTelegramAsync(botToken, chatIdFromMsg, helpMsg, ct);
            return;
        }

        if (textLower.StartsWith("/status") || textLower.Contains("estado") && (textLower.Contains("sistema") || textLower.Contains("todo") || textLower.Contains("vps")))
        {
            var metrics = await prometheusClient.GetVpsMetricsAsync(ct);
            var health = await healthRunner.GetHealthSummaryAsync(ct);

            var sb = new StringBuilder();
            sb.AppendLine($"<b>📊 ESTADO GENERAL VPS: {health.Status.ToUpper()}</b>\n");
            sb.AppendLine($"• <b>CPU</b>: {metrics?.CpuPercent:F1}%");
            sb.AppendLine($"• <b>RAM</b>: {metrics?.MemoryPercent:F1}%");
            sb.AppendLine($"• <b>Disco</b>: {metrics?.DiskPercent:F1}%");
            sb.AppendLine($"• <b>Proyectos</b>: {health.HealthyProjects}/{health.TotalProjects} saludables");
            sb.AppendLine($"• <b>Contenedores</b>: {health.RunningContainers} activos, {health.UnhealthyContainers} fallando");

            if (health.ActiveAlerts.Any())
            {
                sb.AppendLine("\n<b>🚨 Alertas Activas:</b>");
                foreach (var a in health.ActiveAlerts)
                {
                    sb.AppendLine($"• [{a.Severity}] {WebUtilityEncode(a.AlertName)}");
                }
            }

            await ReplyTelegramAsync(botToken, chatIdFromMsg, sb.ToString(), ct);
            return;
        }

        bool isTaskIntent = textLower.StartsWith("tarea:") ||
                            textLower.StartsWith("propuesta:") ||
                            textLower.StartsWith("planificar:") ||
                            textLower.Contains("asignar") ||
                            textLower.Contains("asigname") ||
                            textLower.Contains("asigna") ||
                            textLower.Contains("tarea") ||
                            textLower.Contains("propuesta") ||
                            textLower.Contains("requerimiento") ||
                            textLower.Contains("cliente");

        if (isTaskIntent)
        {
            var rawProposal = text.Contains(':') ? text.Substring(text.IndexOf(':') + 1).Trim() : text;
            var projects = await projectGrouping.GetProjectsAsync(ct);
            var planResult = await aiPlanner.PlanTaskFromProposalAsync(rawProposal, projects, ct);

            // Save Task to Database
            var newTask = new ProjectTask
            {
                Id = Guid.NewGuid(),
                ProjectKey = planResult.ProjectKey,
                ContainerName = planResult.ContainerName,
                Title = planResult.Title,
                Description = planResult.Description,
                Priority = planResult.Priority,
                Status = "Pending",
                RawInput = rawProposal,
                ActionPlanJson = JsonSerializer.Serialize(planResult.ActionPlanSteps),
                CreatedAtUtc = DateTime.UtcNow
            };

            db.ProjectTasks.Add(newTask);
            await db.SaveChangesAsync(ct);

            var sb = new StringBuilder();
            sb.AppendLine($"<b>📋 TAREA PLANIFICADA Y ASIGNADA (IA)</b>\n");
            sb.AppendLine($"<b>Proyecto:</b> <code>{WebUtilityEncode(planResult.ProjectKey)}</code>");
            if (!string.IsNullOrWhiteSpace(planResult.ContainerName))
            {
                sb.AppendLine($"<b>Contenedor:</b> <code>{WebUtilityEncode(planResult.ContainerName)}</code>");
            }
            sb.AppendLine($"<b>Título:</b> {WebUtilityEncode(planResult.Title)}");
            sb.AppendLine($"<b>Prioridad:</b> {planResult.Priority}\n");
            sb.AppendLine($"<b>Plan de Acción (Pasos):</b>");
            for (int i = 0; i < planResult.ActionPlanSteps.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {WebUtilityEncode(planResult.ActionPlanSteps[i])}");
            }

            await ReplyTelegramAsync(botToken, chatIdFromMsg, sb.ToString(), ct);
            return;
        }

        // Generic Conversational Chat handling using DeepSeek-R1
        var chatReply = await GenerateConversationalReplyAsync(text, scope, ct);
        await ReplyTelegramAsync(botToken, chatIdFromMsg, chatReply, ct);
    }

    private async Task<string> GenerateConversationalReplyAsync(string userMessage, IServiceScope scope, CancellationToken ct)
    {
        var enabled = _configuration.GetValue("Ai:Enabled", true);
        if (!enabled)
        {
            return "¡Hola! Estoy en línea monitoreando tu VPS. ¿En qué te puedo ayudar hoy con tus proyectos o contenedores?";
        }

        try
        {
            var healthRunner = scope.ServiceProvider.GetRequiredService<IHealthCheckRunner>();
            var health = await healthRunner.GetHealthSummaryAsync(ct);

            var requestBody = new
            {
                model = _configuration["Ai:Model"] ?? "deepseek-ai/deepseek-r1",
                messages = new[]
                {
                    new { role = "system", content = $"Eres un asistente virtual de DevOps y SRE conversacional, amable y muy capaz. Respondes brevemente en español. El estado actual de la infraestructura es {health.Status.ToUpper()} con {health.RunningContainers} contenedores activos." },
                    new { role = "user", content = userMessage }
                },
                temperature = 0.7,
                max_tokens = 512
            };

            using var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody, ct);
            if (response.IsSuccessStatusCode)
            {
                using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (!string.IsNullOrWhiteSpace(content))
                {
                    return WebUtilityEncode(content.Trim());
                }
            }
        }
        catch
        {
            // Fallback
        }

        return "¡Hola! Estoy activo monitoreando tu infraestructura y contenedores. Puedes pedirme '/status' o asignarme tareas como 'tarea: [propuesta]' en cualquier momento.";
    }

    private async Task ReplyTelegramAsync(string botToken, string chatId, string htmlText, CancellationToken ct)
    {
        try
        {
            var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
            var body = new
            {
                chat_id = chatId,
                text = htmlText,
                parse_mode = "HTML"
            };
            using var resp = await _httpClient.PostAsJsonAsync(url, body, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send Telegram reply.");
        }
    }

    private async Task<(string BotToken, string ChatId)> GetTelegramCredentialsAsync(CancellationToken ct)
    {
        var configToken = _configuration["Telegram:BotToken"];
        var configChatId = _configuration["Telegram:ChatId"];

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        var dbConfig = await db.TelegramConfigs.FirstOrDefaultAsync(ct);

        var token = !string.IsNullOrWhiteSpace(dbConfig?.BotToken) ? dbConfig.BotToken : configToken ?? "";
        var chatId = !string.IsNullOrWhiteSpace(dbConfig?.ChatId) ? dbConfig.ChatId : configChatId ?? "";

        return (token, chatId);
    }

    private async Task SaveChatIdAsync(string chatId, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        var dbConfig = await db.TelegramConfigs.FirstOrDefaultAsync(ct);
        if (dbConfig == null)
        {
            dbConfig = new TelegramConfig { Id = Guid.NewGuid(), ChatId = chatId, UpdatedAtUtc = DateTime.UtcNow };
            db.TelegramConfigs.Add(dbConfig);
        }
        else
        {
            dbConfig.ChatId = chatId;
            dbConfig.UpdatedAtUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }

    private static string WebUtilityEncode(string val)
    {
        return System.Net.WebUtility.HtmlEncode(val ?? "");
    }
}
