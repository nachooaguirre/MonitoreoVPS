namespace VpsMonitor.Web;

using Microsoft.EntityFrameworkCore;
using VpsMonitor.Web.Data;
using VpsMonitor.Web.Data.Entities;
using VpsMonitor.Web.Endpoints;
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

        return builder;
    }

    public static WebApplication BuildApp(WebApplicationBuilder builder)
    {
        var app = builder.Build();

        app.MapGet("/health", () => Results.Json(new { ok = true }));
        app.MapGet("/api/version", (BuildInfo buildInfo) => Results.Json(new
        {
            version = buildInfo.ApplicationVersion,
            commit = buildInfo.BuildCommit
        }));
        app.MapAuthEndpoints();

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

        if (await db.MonitorUsers.AnyAsync(user => user.Role == MonitorUserRole.Owner))
        {
            return;
        }

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
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
