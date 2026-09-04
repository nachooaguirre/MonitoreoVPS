namespace VpsMonitor.Web.Tests.Infrastructure;

using VpsMonitor.Web.Infrastructure.Docker;
using Xunit;

public class ProjectGroupingServiceTests
{
    private sealed class FakeDockerReadOnlyClient : IDockerReadOnlyClient
    {
        private readonly List<DockerContainerInfo> _containers;

        public FakeDockerReadOnlyClient(List<DockerContainerInfo> containers)
        {
            _containers = containers;
        }

        public Task<List<DockerContainerInfo>> ListContainersAsync(CancellationToken ct = default)
        {
            return Task.FromResult(_containers);
        }

        public Task<DockerContainerStats?> GetContainerStatsAsync(string containerId, CancellationToken ct = default)
        {
            return Task.FromResult<DockerContainerStats?>(null);
        }

        public Task<string> GetContainerLogsAsync(string containerId, int tail = 100, CancellationToken ct = default)
        {
            return Task.FromResult("Fake logs");
        }
    }

    [Fact]
    public async Task GetProjectsAsync_GroupsByCoolifyAndComposeLabels()
    {
        // Arrange
        var containers = new List<DockerContainerInfo>
        {
            new("c1", "app1", "img1", new Dictionary<string, string> { { "coolify.projectId", "proj-a" } }, "running", "Up 2 hours", 100, 0, "proj-a"),
            new("c2", "db1", "img2", new Dictionary<string, string> { { "coolify.projectId", "proj-a" } }, "running", "Up 2 hours", 100, 1, "proj-a"),
            new("c3", "redis", "img3", new Dictionary<string, string> { { "com.docker.compose.project", "proj-b" } }, "running", "Up 1 hour", 200, 0, "proj-b"),
            new("c4", "standalone", "img4", new Dictionary<string, string>(), "running", "Up 3 hours", 300, 0, "unassigned")
        };

        var fakeDockerClient = new FakeDockerReadOnlyClient(containers);
        var service = new ProjectGroupingService(fakeDockerClient);

        // Act
        var result = await service.GetProjectsAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("proj-a", result[0].ProjectKey);
        Assert.Equal(2, result[0].ContainerCount);
        Assert.Equal(1, result[0].TotalRestarts);
        Assert.Equal("healthy", result[0].OverallStatus);
        Assert.Equal("coolify-label", result[0].AssignmentSource);

        Assert.Equal("proj-b", result[1].ProjectKey);
        Assert.Equal("compose-label", result[1].AssignmentSource);

        Assert.Equal("unassigned", result[2].ProjectKey);
        Assert.Equal("unassigned", result[2].AssignmentSource);
    }

    [Fact]
    public async Task GetProjectsAsync_MarksUnhealthyIfAnyContainerUnhealthy()
    {
        // Arrange
        var containers = new List<DockerContainerInfo>
        {
            new("c1", "web", "img1", new Dictionary<string, string> { { "coolify.projectId", "proj-c" } }, "running", "Up 1 hour", 100, 0, "proj-c"),
            new("c2", "worker", "img2", new Dictionary<string, string> { { "coolify.projectId", "proj-c" } }, "unhealthy", "Unhealthy", 100, 5, "proj-c")
        };

        var fakeDockerClient = new FakeDockerReadOnlyClient(containers);
        var service = new ProjectGroupingService(fakeDockerClient);

        // Act
        var result = await service.GetProjectsAsync();

        // Assert
        var proj = Assert.Single(result);
        Assert.Equal("unhealthy", proj.OverallStatus);
        Assert.Equal(5, proj.TotalRestarts);
    }
}
