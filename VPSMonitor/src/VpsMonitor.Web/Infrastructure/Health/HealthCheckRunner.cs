namespace VpsMonitor.Web.Infrastructure.Health;

using VpsMonitor.Web.Infrastructure.Docker;
using VpsMonitor.Web.Infrastructure.Prometheus;

public interface IHealthCheckRunner
{
    Task<HealthSummaryReport> GetHealthSummaryAsync(CancellationToken ct = default);
}

public sealed record HealthSummaryReport(
    string Status,
    int TotalProjects,
    int HealthyProjects,
    int DegradedProjects,
    int UnhealthyProjects,
    int TotalContainers,
    int RunningContainers,
    int StoppedContainers,
    int UnhealthyContainers,
    List<ProjectHealthStatus> Projects,
    List<PrometheusAlertInfo> ActiveAlerts,
    DateTime EvaluatedAtUtc
);

public sealed record ProjectHealthStatus(
    string ProjectKey,
    string DisplayName,
    string Status,
    int TotalContainers,
    int RunningContainers,
    int UnhealthyContainers,
    List<string> Issues
);

public sealed class HealthCheckRunner(
    IProjectGroupingService projectGroupingService,
    IPrometheusQueryClient prometheusClient,
    TimeProvider clock) : IHealthCheckRunner
{
    public async Task<HealthSummaryReport> GetHealthSummaryAsync(CancellationToken ct = default)
    {
        var projects = await projectGroupingService.GetProjectsAsync(ct);
        var activeAlerts = await prometheusClient.GetActiveAlertsAsync(ct);

        var projectStatuses = new List<ProjectHealthStatus>();
        int totalContainers = 0;
        int runningContainers = 0;
        int stoppedContainers = 0;
        int unhealthyContainers = 0;

        foreach (var proj in projects)
        {
            var issues = new List<string>();
            int projRunning = 0;
            int projUnhealthy = 0;

            foreach (var container in proj.Containers)
            {
                var stateLower = container.State.ToLowerInvariant();
                if (stateLower == "running")
                {
                    projRunning++;
                }
                else if (stateLower is "unhealthy" or "dead")
                {
                    projUnhealthy++;
                    issues.Add($"Contenedor '{container.Name}' estado: {container.State}.");
                }
                else if (stateLower is "restarting" or "exited" or "created" or "paused")
                {
                    issues.Add($"Contenedor '{container.Name}' estado: {container.State}.");
                }
                else
                {
                    issues.Add($"Contenedor '{container.Name}' estado desconocido: {container.State}.");
                }
            }

            var projAlerts = activeAlerts.Where(a =>
                a.Labels.Values.Any(val => string.Equals(val, proj.ProjectKey, StringComparison.OrdinalIgnoreCase) ||
                                           proj.Containers.Any(c => string.Equals(val, c.Name, StringComparison.OrdinalIgnoreCase)))
            ).ToList();

            foreach (var alert in projAlerts)
            {
                var desc = string.IsNullOrWhiteSpace(alert.Summary) ? alert.AlertName : alert.Summary;
                issues.Add($"Alerta [{alert.Severity}]: {desc}");
            }

            string projectStatus;
            if (projUnhealthy > 0 || projAlerts.Any(a => string.Equals(a.Severity, "critical", StringComparison.OrdinalIgnoreCase)))
            {
                projectStatus = "unhealthy";
            }
            else if (issues.Count > 0 || projRunning < proj.ContainerCount)
            {
                projectStatus = "degraded";
            }
            else
            {
                projectStatus = "healthy";
            }

            projectStatuses.Add(new ProjectHealthStatus(
                ProjectKey: proj.ProjectKey,
                DisplayName: proj.DisplayName,
                Status: projectStatus,
                TotalContainers: proj.ContainerCount,
                RunningContainers: projRunning,
                UnhealthyContainers: projUnhealthy,
                Issues: issues
            ));

            totalContainers += proj.ContainerCount;
            runningContainers += projRunning;
            unhealthyContainers += projUnhealthy;
            stoppedContainers += proj.Containers.Count(c => string.Equals(c.State, "exited", StringComparison.OrdinalIgnoreCase));
        }

        int healthyProjects = projectStatuses.Count(p => p.Status == "healthy");
        int degradedProjects = projectStatuses.Count(p => p.Status == "degraded");
        int unhealthyProjects = projectStatuses.Count(p => p.Status == "unhealthy");

        string overallStatus = "healthy";
        if (unhealthyProjects > 0 || activeAlerts.Any(a => string.Equals(a.Severity, "critical", StringComparison.OrdinalIgnoreCase)))
        {
            overallStatus = "unhealthy";
        }
        else if (degradedProjects > 0 || activeAlerts.Any(a => string.Equals(a.Severity, "warning", StringComparison.OrdinalIgnoreCase)))
        {
            overallStatus = "degraded";
        }

        return new HealthSummaryReport(
            Status: overallStatus,
            TotalProjects: projects.Count,
            HealthyProjects: healthyProjects,
            DegradedProjects: degradedProjects,
            UnhealthyProjects: unhealthyProjects,
            TotalContainers: totalContainers,
            RunningContainers: runningContainers,
            StoppedContainers: stoppedContainers,
            UnhealthyContainers: unhealthyContainers,
            Projects: projectStatuses,
            ActiveAlerts: activeAlerts,
            EvaluatedAtUtc: clock.GetUtcNow().UtcDateTime
        );
    }
}
