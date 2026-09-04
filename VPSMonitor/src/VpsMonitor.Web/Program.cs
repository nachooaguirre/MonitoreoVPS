namespace VpsMonitor.Web;

public static class VpsMonitorApp
{
    public static WebApplicationBuilder CreateBuilder(string[]? args = null)
    {
        var builder = WebApplication.CreateBuilder(args ?? Array.Empty<string>());

        builder.Services.AddSingleton(new BuildInfo(
            builder.Configuration["APP_VERSION"] ?? "dev",
            builder.Configuration["BUILD_COMMIT"] ?? "unknown"));

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

        return app;
    }

    public static WebApplication CreateApp(string[]? args = null)
    {
        var builder = CreateBuilder(args);
        return BuildApp(builder);
    }

    public sealed record BuildInfo(string ApplicationVersion, string BuildCommit);
}

public static class Program
{
    public static void Main(string[] args)
    {
        VpsMonitorApp.CreateApp(args).Run();
    }
}
