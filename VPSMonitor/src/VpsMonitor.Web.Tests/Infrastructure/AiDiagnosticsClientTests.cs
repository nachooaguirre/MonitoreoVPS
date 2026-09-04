namespace VpsMonitor.Web.Tests.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using VpsMonitor.Web.Infrastructure.Ai;
using VpsMonitor.Web.Infrastructure.Health;
using Xunit;

public class AiDiagnosticsClientTests
{
    [Fact]
    public async Task DiagnosticReportAsync_ReturnsFallbackReportWhenAiDisabled()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Ai:Enabled", "false" }
        };

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var client = new HttpClient();
        var aiClient = new AiDiagnosticsClient(client, config, NullLogger<AiDiagnosticsClient>.Instance);

        var report = new HealthSummaryReport(
            Status: "healthy",
            TotalProjects: 1,
            HealthyProjects: 1,
            DegradedProjects: 0,
            UnhealthyProjects: 0,
            TotalContainers: 2,
            RunningContainers: 2,
            StoppedContainers: 0,
            UnhealthyContainers: 0,
            Projects: new List<ProjectHealthStatus>(),
            ActiveAlerts: new List<VpsMonitor.Web.Infrastructure.Prometheus.PrometheusAlertInfo>(),
            EvaluatedAtUtc: DateTime.UtcNow
        );

        // Act
        var result = await aiClient.DiagnosticReportAsync(report);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Diagnóstico de Sistema", result);
        Assert.Contains("HEALTHY", result);
    }
}
