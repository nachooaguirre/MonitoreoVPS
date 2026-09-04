namespace VpsMonitor.Web.Endpoints;

using VpsMonitor.Web.Infrastructure.Docker;

public static class ProjectsEndpoints
{
    public static IEndpointRouteBuilder MapProjectsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/projects").RequireMonitorOwner();

        group.MapGet("/", async (IProjectGroupingService service, CancellationToken ct) =>
        {
            var projects = await service.GetProjectsAsync(ct);
            return Results.Json(projects);
        });

        group.MapGet("/{projectKey}/containers", async (string projectKey, IProjectGroupingService service, CancellationToken ct) =>
        {
            var project = await service.GetProjectAsync(projectKey, ct);
            return project is null
                ? Results.NotFound(new { message = $"Project '{projectKey}' not found." })
                : Results.Json(project.Containers);
        });

        return endpoints;
    }
}
