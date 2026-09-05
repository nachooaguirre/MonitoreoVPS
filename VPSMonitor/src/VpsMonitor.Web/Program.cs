namespace VpsMonitor.Web;

using Microsoft.EntityFrameworkCore;
using VpsMonitor.Web.Data;
using VpsMonitor.Web.Data.Entities;
using VpsMonitor.Web.Endpoints;
using VpsMonitor.Web.Infrastructure.Docker;
using VpsMonitor.Web.Infrastructure.Health;
using VpsMonitor.Web.Infrastructure.Prometheus;
using VpsMonitor.Web.Security;

public static class VpsMonitorApp
{
    public static WebApplicationBuilder CreateBuilder(string[]? args = null)
    {
        var builder = WebApplication.CreateBuilder(args ?? Array.Empty<string>());
        var sessionMinutes = builder.Configuration.GetValue("Monitor:SessionMinutes", 60);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton(new BuildInfo(
            builder.Configuration["APP_VERSION"] ?? "dev",
            builder.Configuration["BUILD_COMMIT"] ?? "unknown"));
        builder.Services.AddDbContext<MonitorDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
        builder.Services.AddScoped<IMonitorStore>(provider => provider.GetRequiredService<MonitorDbContext>());
        builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped(provider => new SessionService(
            provider.GetRequiredService<IMonitorStore>(),
            provider.GetRequiredService<TimeProvider>(),
            TimeSpan.FromMinutes(sessionMinutes)));
        builder.Services.AddScoped<UserSessionState>();

        var dockerProxyUrl = builder.Configuration["DockerProxy:BaseUrl"] ?? "http://docker-proxy:2375";
        builder.Services.AddHttpClient<IDockerReadOnlyClient, DockerReadOnlyClient>(client =>
        {
            client.BaseAddress = new Uri(dockerProxyUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        builder.Services.AddScoped<IProjectGroupingService, ProjectGroupingService>();

        var prometheusUrl = builder.Configuration["Prometheus:BaseUrl"] ?? "http://prometheus:9090";
        builder.Services.AddHttpClient<IPrometheusQueryClient, PrometheusQueryClient>(client =>
        {
            client.BaseAddress = new Uri(prometheusUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        builder.Services.AddScoped<IHealthCheckRunner, HealthCheckRunner>();
        builder.Services.AddScoped<VpsMonitor.Web.Infrastructure.Notifications.IEmailNotificationService, VpsMonitor.Web.Infrastructure.Notifications.EmailNotificationService>();

        var aiBaseUrl = builder.Configuration["Ai:BaseUrl"] ?? "https://integrate.api.nvidia.com/v1";
        var normalizedAiUri = NormalizeAiBaseUrl(aiBaseUrl);

        builder.Services.AddHttpClient<VpsMonitor.Web.Infrastructure.Ai.IAiDiagnosticsClient, VpsMonitor.Web.Infrastructure.Ai.AiDiagnosticsClient>(client =>
        {
            client.BaseAddress = normalizedAiUri;
            client.Timeout = TimeSpan.FromSeconds(25);
            var apiKey = builder.Configuration["Ai:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }
        });

        builder.Services.AddHttpClient<VpsMonitor.Web.Infrastructure.Ai.IAiProjectPlannerService, VpsMonitor.Web.Infrastructure.Ai.AiProjectPlannerService>(client =>
        {
            client.BaseAddress = normalizedAiUri;
            client.Timeout = TimeSpan.FromSeconds(30);
            var apiKey = builder.Configuration["Ai:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }
        });

        builder.Services.AddHttpClient<VpsMonitor.Web.Infrastructure.Telegram.TelegramBotService>();
        builder.Services.AddSingleton<VpsMonitor.Web.Infrastructure.Telegram.TelegramBotService>();
        builder.Services.AddSingleton<VpsMonitor.Web.Infrastructure.Telegram.ITelegramNotificationDispatcher>(sp => sp.GetRequiredService<VpsMonitor.Web.Infrastructure.Telegram.TelegramBotService>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<VpsMonitor.Web.Infrastructure.Telegram.TelegramBotService>());

        builder.Services.AddAntiforgery();
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        return builder;
    }

    public static WebApplication BuildApp(WebApplicationBuilder builder)
    {
        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            await next();
        });

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapGet("/health", () => Results.Json(new { ok = true }));
        app.MapGet("/metrics", () => Results.Text("# HELP vps_monitor_up Status of VPS Monitor Gateway\n# TYPE vps_monitor_up gauge\nvps_monitor_up 1\n", "text/plain"));
        app.MapGet("/api/version", (BuildInfo buildInfo) => Results.Json(new
        {
            version = buildInfo.ApplicationVersion,
            commit = buildInfo.BuildCommit
        }));
        app.MapAuthEndpoints();
        app.MapProjectsEndpoints();
        app.MapContainersEndpoints();
        app.MapMetricsEndpoints();

        app.MapRazorComponents<Components.App>()
            .AddInteractiveServerRenderMode();

        return app;
    }

    public static WebApplication CreateApp(string[]? args = null)
    {
        var builder = CreateBuilder(args);
        return BuildApp(builder);
    }

    public static async Task InitializeMonitorDatabaseAsync(WebApplication app)
    {
        var ownerUsername = Environment.GetEnvironmentVariable("MONITOR_OWNER_USERNAME");
        var ownerPassword = Environment.GetEnvironmentVariable("MONITOR_OWNER_PASSWORD");

        if (string.IsNullOrWhiteSpace(ownerUsername)) ownerUsername = "admin";
        if (string.IsNullOrWhiteSpace(ownerPassword)) ownerPassword = "admin_password_change_me";

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        
        var databaseCreator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>(db.Database);
        if (databaseCreator is not null)
        {
            try
            {
                if (!await databaseCreator.ExistsAsync())
                {
                    await databaseCreator.CreateAsync();
                }
                await databaseCreator.CreateTablesAsync();
            }
            catch
            {
                // Tables already exist
            }

            try
            {
                await db.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS ""ProjectAliases"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""ProjectKey"" character varying(120) NOT NULL,
                        ""Alias"" character varying(120) NOT NULL,
                        ""UpdatedAtUtc"" timestamp with time zone NOT NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ProjectAliases_ProjectKey"" ON ""ProjectAliases"" (""ProjectKey"");

                    CREATE TABLE IF NOT EXISTS ""ContainerAliases"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""ContainerIdOrName"" character varying(120) NOT NULL,
                        ""Alias"" character varying(120) NOT NULL,
                        ""UpdatedAtUtc"" timestamp with time zone NOT NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ContainerAliases_ContainerIdOrName"" ON ""ContainerAliases"" (""ContainerIdOrName"");

                    CREATE TABLE IF NOT EXISTS ""ProjectTasks"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""ProjectKey"" character varying(120) NOT NULL,
                        ""ContainerName"" character varying(120) NOT NULL DEFAULT '',
                        ""Title"" character varying(200) NOT NULL,
                        ""Description"" text NOT NULL,
                        ""Priority"" character varying(30) NOT NULL,
                        ""Status"" character varying(30) NOT NULL,
                        ""RawInput"" text NOT NULL,
                        ""ActionPlanJson"" text NOT NULL,
                        ""CreatedAtUtc"" timestamp with time zone NOT NULL,
                        ""CompletedAtUtc"" timestamp with time zone NULL
                    );
                    ALTER TABLE ""ProjectTasks"" ADD COLUMN IF NOT EXISTS ""ContainerName"" character varying(120) NOT NULL DEFAULT '';
                    CREATE INDEX IF NOT EXISTS ""IX_ProjectTasks_ProjectKey"" ON ""ProjectTasks"" (""ProjectKey"");

                    CREATE TABLE IF NOT EXISTS ""TelegramConfigs"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""BotToken"" character varying(200) NOT NULL,
                        ""ChatId"" character varying(100) NOT NULL,
                        ""IsAlertsEnabled"" boolean NOT NULL,
                        ""UpdatedAtUtc"" timestamp with time zone NOT NULL
                    );
                
                ");
            }
            catch
            {
                // Table already created
            }
        }

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var existingOwner = await db.MonitorUsers.FirstOrDefaultAsync(user => user.Role == MonitorUserRole.Owner);

        if (existingOwner is not null)
        {
            existingOwner.Username = ownerUsername.Trim();
            existingOwner.PasswordHash = passwordHasher.Hash(ownerPassword);
            await db.SaveChangesAsync();
            return;
        }

        db.MonitorUsers.Add(new MonitorUser
        {
            Id = Guid.NewGuid(),
            Username = ownerUsername.Trim(),
            PasswordHash = passwordHasher.Hash(ownerPassword),
            Role = MonitorUserRole.Owner,
            CreatedAtUtc = TimeProvider.System.GetUtcNow().UtcDateTime
        });
        await db.SaveChangesAsync();
    }

    public static Uri NormalizeAiBaseUrl(string inputUrl)
    {
        if (string.IsNullOrWhiteSpace(inputUrl))
        {
            inputUrl = "https://integrate.api.nvidia.com/v1/";
        }

        var trimmed = inputUrl.Trim();
        if (trimmed.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^"/chat/completions".Length];
        }
        else if (trimmed.EndsWith("/chat/completions/", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^"/chat/completions/".Length];
        }

        trimmed = trimmed.TrimEnd('/');

        if (!trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.Contains("/v1/", StringComparison.OrdinalIgnoreCase))
        {
            trimmed += "/v1";
        }

        return new Uri(trimmed + "/");
    }

    public sealed record BuildInfo(string ApplicationVersion, string BuildCommit);
}

public static class Program
{
    public static void Main(string[] args)
    {
        var app = VpsMonitorApp.CreateApp(args);
        VpsMonitorApp.InitializeMonitorDatabaseAsync(app).GetAwaiter().GetResult();
        app.Run();
    }
}
