namespace VpsMonitor.Web.Infrastructure.Docker;

public interface IProjectGroupingService
{
    Task<List<ProjectSummary>> GetProjectsAsync(CancellationToken ct = default);
    Task<ProjectSummary?> GetProjectAsync(string projectKey, CancellationToken ct = default);
}

public sealed class ProjectGroupingService : IProjectGroupingService
{
    private readonly IDockerReadOnlyClient _dockerClient;

    public ProjectGroupingService(IDockerReadOnlyClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task<List<ProjectSummary>> GetProjectsAsync(CancellationToken ct = default)
    {
        var containers = await _dockerClient.ListContainersAsync(ct);
        var grouped = containers.GroupBy(c => c.ProjectKey);

        var results = new List<ProjectSummary>();
        foreach (var group in grouped)
        {
            var projectContainers = group.ToList();
            var assignmentSource = GetAssignmentSource(projectContainers);
            var overallStatus = CalculateOverallStatus(projectContainers);
            var totalRestarts = projectContainers.Sum(c => c.RestartCount);

            results.Add(new ProjectSummary(
                ProjectKey: group.Key,
                ContainerCount: projectContainers.Count,
                Containers: projectContainers,
                TotalRestarts: totalRestarts,
                OverallStatus: overallStatus,
                AssignmentSource: assignmentSource
            ));
        }

        return results.OrderBy(p => p.ProjectKey).ToList();
    }

    public async Task<ProjectSummary?> GetProjectAsync(string projectKey, CancellationToken ct = default)
    {
        var projects = await GetProjectsAsync(ct);
        return projects.FirstOrDefault(p => string.Equals(p.ProjectKey, projectKey, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetAssignmentSource(List<DockerContainerInfo> containers)
    {
        if (containers.Any(c => c.Labels.ContainsKey("coolify.projectId"))) return "coolify-label";
        if (containers.Any(c => c.Labels.ContainsKey("com.docker.compose.project"))) return "compose-label";
        return "unassigned";
    }

    private static string CalculateOverallStatus(List<DockerContainerInfo> containers)
    {
        if (containers.Count == 0) return "empty";
        if (containers.Any(c => string.Equals(c.State, "unhealthy", StringComparison.OrdinalIgnoreCase))) return "unhealthy";
        if (containers.Any(c => string.Equals(c.State, "exited", StringComparison.OrdinalIgnoreCase) || string.Equals(c.State, "dead", StringComparison.OrdinalIgnoreCase))) return "degraded";
        if (containers.All(c => string.Equals(c.State, "running", StringComparison.OrdinalIgnoreCase))) return "healthy";
        return "mixed";
    }
}
