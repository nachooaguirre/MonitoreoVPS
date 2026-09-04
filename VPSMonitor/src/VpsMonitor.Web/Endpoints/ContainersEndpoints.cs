namespace VpsMonitor.Web.Endpoints;

using VpsMonitor.Web.Infrastructure.Docker;

public static class ContainersEndpoints
{
    public static IEndpointRouteBuilder MapContainersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/containers").RequireMonitorOwner();

        group.MapGet("/{id}/stats", async (string id, IDockerReadOnlyClient client, CancellationToken ct) =>
        {
            var stats = await client.GetContainerStatsAsync(id, ct);
            return stats is null
                ? Results.NotFound(new { message = $"Container '{id}' stats unavailable." })
                : Results.Json(stats);
        });

        return endpoints;
    }
}
