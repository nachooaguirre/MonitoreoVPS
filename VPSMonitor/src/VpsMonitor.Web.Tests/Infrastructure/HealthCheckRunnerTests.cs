namespace VpsMonitor.Web.Tests.Infrastructure;

using VpsMonitor.Web.Infrastructure.Docker;
using VpsMonitor.Web.Infrastructure.Health;
using VpsMonitor.Web.Infrastructure.Prometheus;
using Xunit;

public class HealthCheckRunnerTests
{
    private class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FakeTimeProvider(DateTimeOffset? now = null)
        {
            _now = now ?? new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);
        }

        public override DateTimeOffset GetUtcNow() => _now;
    }

    private class FakeProjectGroupingService : IProjectGroupingService
    {
        private readonly List<ProjectSummary> _projects;

        public FakeProjectGroupingService(List<ProjectSummary> projects)
        {
            _projects = projects;
        }

        public Task<List<ProjectSummary>> GetProjectsAsync(CancellationToken ct = default)
        {
            return Task.FromResult(_projects);
        }

        public Task<ProjectSummary?> GetProjectAsync(string projectKey, CancellationToken ct = default)
        {
            return Task.FromResult(_projects.FirstOrDefault(p => p.ProjectKey == projectKey));
        }
    }

    private class FakePrometheusQueryClient : IPrometheusQueryClient
    {
        private readonly List<PrometheusAlertInfo> _alerts;

        public FakePrometheusQueryClient(List<PrometheusAlertInfo>? alerts = null)
        {
            _alerts = alerts ?? new List<PrometheusAlertInfo>();
        }

        public Task<VpsMetrics?> GetVpsMetricsAsync(CancellationToken ct = default)
        {
            return Task.FromResult<VpsMetrics?>(new VpsMetrics(10, 20, 30, 100, 3600));
        }

        public Task<double?> QueryScalarAsync(string query, CancellationToken ct = default)
        {
            return Task.FromResult<double?>(10.0);
        }

        public Task<List<PrometheusAlertInfo>> GetActiveAlertsAsync(CancellationToken ct = default)
        {
            return Task.FromResult(_alerts);
        }
    }

    [Fact]
    public async Task GetHealthSummaryAsync_ReturnsHealthyWhenAllContainersRunningAndNoAlerts()
    {
        // Arrange
        var containers = new List<DockerContainerInfo>
        {
            new("c1", "web", "image:latest", new Dictionary<string, string>(), "running", "Up 1 hour", 0, 0, "superpos")
        };
        var projects = new List<ProjectSummary>
        {
            new("superpos", 1, containers, 0, "healthy", "coolify-label")
        };

        var clock = new FakeTimeProvider();
        var runner = new HealthCheckRunner(
            new FakeProjectGroupingService(projects),
            new FakePrometheusQueryClient(),
            clock);

        // Act
        var summary = await runner.GetHealthSummaryAsync();

        // Assert
        Assert.Equal("healthy", summary.Status);
        Assert.Equal(1, summary.TotalProjects);
        Assert.Equal(1, summary.HealthyProjects);
        Assert.Equal(0, summary.UnhealthyProjects);
        Assert.Equal(1, summary.TotalContainers);
        Assert.Equal(1, summary.RunningContainers);
    }

    [Fact]
    public async Task GetHealthSummaryAsync_ReturnsUnhealthyWhenContainerIsUnhealthy()
    {
        // Arrange
        var containers = new List<DockerContainerInfo>
        {
            new("c1", "web", "image:latest", new Dictionary<string, string>(), "unhealthy", "unhealthy", 0, 0, "superpos")
        };
        var projects = new List<ProjectSummary>
        {
            new("superpos", 1, containers, 0, "unhealthy", "coolify-label")
        };

        var clock = new FakeTimeProvider();
        var runner = new HealthCheckRunner(
            new FakeProjectGroupingService(projects),
            new FakePrometheusQueryClient(),
            clock);

        // Act
        var summary = await runner.GetHealthSummaryAsync();

        // Assert
        Assert.Equal("unhealthy", summary.Status);
        Assert.Equal(1, summary.UnhealthyProjects);
        Assert.Equal(1, summary.UnhealthyContainers);
        Assert.Single(summary.Projects[0].Issues);
    }

    [Fact]
    public async Task GetHealthSummaryAsync_ReturnsUnhealthyWhenCriticalAlertIsActive()
    {
        // Arrange
        var containers = new List<DockerContainerInfo>
        {
            new("c1", "web", "image:latest", new Dictionary<string, string>(), "running", "Up 1 hour", 0, 0, "superpos")
        };
        var projects = new List<ProjectSummary>
        {
            new("superpos", 1, containers, 0, "healthy", "coolify-label")
        };

        var alerts = new List<PrometheusAlertInfo>
        {
            new("GatewayDown", "critical", "firing", "Gateway is down", "Description", new Dictionary<string, string>())
        };

        var clock = new FakeTimeProvider();
        var runner = new HealthCheckRunner(
            new FakeProjectGroupingService(projects),
            new FakePrometheusQueryClient(alerts),
            clock);

        // Act
        var summary = await runner.GetHealthSummaryAsync();

        // Assert
        Assert.Equal("unhealthy", summary.Status);
        Assert.Single(summary.ActiveAlerts);
    }
}
