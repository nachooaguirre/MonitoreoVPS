namespace VpsMonitor.Web.Tests.Endpoints;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using VpsMonitor.Web;
using VpsMonitor.Web.Data.Entities;
using VpsMonitor.Web.Endpoints;
using VpsMonitor.Web.Infrastructure.Docker;
using VpsMonitor.Web.Infrastructure.Health;
using VpsMonitor.Web.Infrastructure.Prometheus;
using VpsMonitor.Web.Security;
using Xunit;

public class MetricsEndpointsTests
{
    private class FakeMonitorStore : IMonitorStore
    {
        public List<MonitorUser> Users { get; init; } = [];
        public List<MonitorSession> Sessions { get; } = [];
        public List<AuditEntry> Audits { get; } = [];

        public Task<MonitorUser?> FindUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.SingleOrDefault(user => user.Username == username));
        }

        public Task AddSessionAsync(MonitorSession session, CancellationToken cancellationToken = default)
        {
            Sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task<MonitorSession?> FindSessionByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Sessions.SingleOrDefault(session => session.TokenHash == tokenHash));
        }

        public Task AddAuditAsync(AuditEntry audit, CancellationToken cancellationToken = default)
        {
            Audits.Add(audit);
            return Task.CompletedTask;
        }
    }

    private class FakePrometheusQueryClient : IPrometheusQueryClient
    {
        private readonly VpsMetrics? _metrics;

        public FakePrometheusQueryClient(VpsMetrics? metrics = null)
        {
            _metrics = metrics ?? new VpsMetrics(15.5, 45.0, 30.2, 1024.0, 7200.0);
        }

        public Task<VpsMetrics?> GetVpsMetricsAsync(CancellationToken ct = default)
        {
            return Task.FromResult(_metrics);
        }

        public Task<double?> QueryScalarAsync(string query, CancellationToken ct = default)
        {
            return Task.FromResult<double?>(15.5);
        }

        public Task<List<PrometheusAlertInfo>> GetActiveAlertsAsync(CancellationToken ct = default)
        {
            return Task.FromResult(new List<PrometheusAlertInfo>());
        }
    }

    private class FakeProjectGroupingService : IProjectGroupingService
    {
        public Task<List<ProjectSummary>> GetProjectsAsync(CancellationToken ct = default)
        {
            return Task.FromResult(new List<ProjectSummary>());
        }

        public Task<ProjectSummary?> GetProjectAsync(string projectKey, CancellationToken ct = default)
        {
            return Task.FromResult<ProjectSummary?>(null);
        }

        public Task SetProjectAliasAsync(string projectKey, string alias, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task SetContainerAliasAsync(string containerIdOrName, string alias, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }

    private async Task<(WebApplication App, HttpClient Client, string OwnerCookie)> CreateTestAppAsync()
    {
        var store = new FakeMonitorStore();
        var owner = new MonitorUser { Id = Guid.NewGuid(), Username = "admin", Role = MonitorUserRole.Owner };
        store.Users.Add(owner);

        var builder = VpsMonitorApp.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IMonitorStore>(store);
        builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton(provider => new SessionService(
            provider.GetRequiredService<IMonitorStore>(),
            provider.GetRequiredService<TimeProvider>(),
            TimeSpan.FromMinutes(60)));
        builder.Services.AddSingleton<IPrometheusQueryClient>(new FakePrometheusQueryClient());
        builder.Services.AddSingleton<IProjectGroupingService>(new FakeProjectGroupingService());
        builder.Services.AddSingleton<IHealthCheckRunner, HealthCheckRunner>();

        var app = VpsMonitorApp.BuildApp(builder);
        await app.StartAsync();

        var sessions = app.Services.GetRequiredService<SessionService>();
        var token = await sessions.CreateSessionAsync(owner);
        var cookieHeader = $"{AuthEndpoints.SessionCookieName}={token}";

        var client = app.GetTestClient();
        return (app, client, cookieHeader);
    }

    [Fact]
    public async Task GetVpsMetrics_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var (app, client, _) = await CreateTestAppAsync();

        // Act
        var response = await client.GetAsync("/api/metrics/vps");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetHealthSummary_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var (app, client, _) = await CreateTestAppAsync();

        // Act
        var response = await client.GetAsync("/api/health/summary");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetVpsMetrics_WithOwnerAuth_ReturnsOkWithMetrics()
    {
        // Arrange
        var (app, client, cookie) = await CreateTestAppAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/metrics/vps");
        request.Headers.Add("Cookie", cookie);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var metrics = await response.Content.ReadFromJsonAsync<VpsMetrics>();
        Assert.NotNull(metrics);
        Assert.Equal(15.5, metrics.CpuPercent);
    }

    [Fact]
    public async Task GetHealthSummary_WithOwnerAuth_ReturnsOkWithSummary()
    {
        // Arrange
        var (app, client, cookie) = await CreateTestAppAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health/summary");
        request.Headers.Add("Cookie", cookie);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<HealthSummaryReport>();
        Assert.NotNull(summary);
        Assert.Equal("healthy", summary.Status);
    }
}
