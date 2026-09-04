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

        var dockerProxyUrl = builder.Configuration["DockerProxy:BaseUrl"] ?? "http://docker-proxy:2375";
        builder.Services.AddHttpClient<IDockerReadOnlyClient, DockerReadOnlyClient>(client =>
        {
            client.BaseAddress = new Uri(dockerProxyUrl.EndsWith('/') ? dockerProxyUrl : $"{dockerProxyUrl}/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        builder.Services.AddScoped<IProjectGroupingService, ProjectGroupingService>();

        var prometheusUrl = builder.Configuration["Prometheus:BaseUrl"] ?? "http://prometheus:9090";
        builder.Services.AddHttpClient<IPrometheusQueryClient, PrometheusQueryClient>(client =>
        {
            client.BaseAddress = new Uri(prometheusUrl.EndsWith('/') ? prometheusUrl : $"{prometheusUrl}/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        builder.Services.AddScoped<IHealthCheckRunner, HealthCheckRunner>();
        builder.Services.AddScoped<VpsMonitor.Web.Infrastructure.Notifications.IEmailNotificationService, VpsMonitor.Web.Infrastructure.Notifications.EmailNotificationService>();

        var aiBaseUrl = builder.Configuration["Ai:BaseUrl"] ?? "http://ai-proxy:8080";
        builder.Services.AddHttpClient<VpsMonitor.Web.Infrastructure.Ai.IAiDiagnosticsClient, VpsMonitor.Web.Infrastructure.Ai.AiDiagnosticsClient>(client =>
        {
            client.BaseAddress = new Uri(aiBaseUrl.EndsWith('/') ? aiBaseUrl : $"{aiBaseUrl}/");
            client.Timeout = TimeSpan.FromSeconds(15);
            var apiKey = builder.Configuration["Ai:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }
        });

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
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            await next();
        });

        app.UseStaticFiles();

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

        if (app.Environment.IsProduction() &&
            (string.IsNullOrWhiteSpace(ownerUsername) || string.IsNullOrWhiteSpace(ownerPassword)))
        {
            throw new InvalidOperationException("MONITOR_OWNER_USERNAME and MONITOR_OWNER_PASSWORD are required in production.");
        }

        if (string.IsNullOrWhiteSpace(ownerUsername) || string.IsNullOrWhiteSpace(ownerPassword))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        await db.Database.MigrateAsync();

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
