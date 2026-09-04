namespace VpsMonitor.Web.Infrastructure.Docker;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VpsMonitor.Web.Data;
using VpsMonitor.Web.Data.Entities;

public interface IProjectGroupingService
{
    Task<List<ProjectSummary>> GetProjectsAsync(CancellationToken ct = default);
    Task<ProjectSummary?> GetProjectAsync(string projectKey, CancellationToken ct = default);
    Task SetProjectAliasAsync(string projectKey, string alias, CancellationToken ct = default);
}

public sealed class ProjectGroupingService : IProjectGroupingService
{
    private readonly IDockerReadOnlyClient _dockerClient;
    private readonly IServiceScopeFactory? _scopeFactory;

    public ProjectGroupingService(IDockerReadOnlyClient dockerClient, IServiceScopeFactory? scopeFactory = null)
    {
        _dockerClient = dockerClient;
        _scopeFactory = scopeFactory;
    }

    public async Task<List<ProjectSummary>> GetProjectsAsync(CancellationToken ct = default)
    {
        var containers = await _dockerClient.ListContainersAsync(ct);
        var grouped = containers.GroupBy(c => c.ProjectKey);

        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (_scopeFactory != null)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetService<MonitorDbContext>();
                if (db != null)
                {
                    aliases = await db.ProjectAliases.ToDictionaryAsync(a => a.ProjectKey, a => a.Alias, StringComparer.OrdinalIgnoreCase, ct);
                }
            }
            catch
            {
                // DB might be initializing
            }
        }

        var results = new List<ProjectSummary>();
        foreach (var group in grouped)
        {
            var projectContainers = group.ToList();
            var assignmentSource = GetAssignmentSource(projectContainers);
            var overallStatus = CalculateOverallStatus(projectContainers);
            var totalRestarts = projectContainers.Sum(c => c.RestartCount);

            string displayName = aliases.TryGetValue(group.Key, out var aliasVal) && !string.IsNullOrWhiteSpace(aliasVal)
                ? aliasVal
                : group.Key;

            results.Add(new ProjectSummary(
                ProjectKey: group.Key,
                DisplayName: displayName,
                ContainerCount: projectContainers.Count,
                Containers: projectContainers,
                TotalRestarts: totalRestarts,
                OverallStatus: overallStatus,
                AssignmentSource: assignmentSource
            ));
        }

        return results.OrderBy(p => p.DisplayName).ToList();
    }

    public async Task<ProjectSummary?> GetProjectAsync(string projectKey, CancellationToken ct = default)
    {
        var projects = await GetProjectsAsync(ct);
        return projects.FirstOrDefault(p => string.Equals(p.ProjectKey, projectKey, StringComparison.OrdinalIgnoreCase));
    }

    public async Task SetProjectAliasAsync(string projectKey, string alias, CancellationToken ct = default)
    {
        if (_scopeFactory == null) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();

        var existing = await db.ProjectAliases.FirstOrDefaultAsync(pa => pa.ProjectKey == projectKey, ct);
        if (existing == null)
        {
            db.ProjectAliases.Add(new ProjectAlias
            {
                Id = Guid.NewGuid(),
                ProjectKey = projectKey,
                Alias = alias.Trim(),
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.Alias = alias.Trim();
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
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
