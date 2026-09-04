namespace VpsMonitor.Web.Endpoints;

using VpsMonitor.Web.Infrastructure.Health;
using VpsMonitor.Web.Infrastructure.Prometheus;

public static class MetricsEndpoints
{
    public static IEndpointRouteBuilder MapMetricsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api").RequireMonitorOwner();

        group.MapGet("/metrics/vps", async (IPrometheusQueryClient client, CancellationToken ct) =>
        {
            var metrics = await client.GetVpsMetricsAsync(ct);
            return metrics is null
                ? Results.NotFound(new { message = "VPS metrics unavailable from Prometheus." })
                : Results.Json(metrics);
        });

        group.MapGet("/health/summary", async (IHealthCheckRunner runner, CancellationToken ct) =>
        {
            var summary = await runner.GetHealthSummaryAsync(ct);
            return Results.Json(summary);
        });

        group.MapGet("/diagnostics/ai", async (IHealthCheckRunner runner, VpsMonitor.Web.Infrastructure.Ai.IAiDiagnosticsClient aiClient, CancellationToken ct) =>
        {
            var summary = await runner.GetHealthSummaryAsync(ct);
            var reportText = await aiClient.DiagnosticReportAsync(summary, ct);
            return Results.Json(new { report = reportText });
        });

        return endpoints;
    }
}
