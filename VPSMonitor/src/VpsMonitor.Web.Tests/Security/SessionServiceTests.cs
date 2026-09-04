using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using VpsMonitor.Web;
using VpsMonitor.Web.Data.Entities;
using VpsMonitor.Web.Endpoints;
using VpsMonitor.Web.Security;
using Xunit;

namespace VpsMonitor.Web.Tests.Security;

public class SessionServiceTests
{
    [Fact]
    public async Task CreateSession_stores_only_token_hash_and_lookup_accepts_raw_token()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.Zero));
        var store = new FakeMonitorStore();
        var user = new MonitorUser { Id = Guid.NewGuid(), Username = "owner", Role = MonitorUserRole.Owner };
        var service = new SessionService(store, clock, TimeSpan.FromMinutes(60));

        var rawToken = await service.CreateSessionAsync(user);

        Assert.NotEmpty(rawToken);
        Assert.Single(store.Sessions);
        Assert.NotEqual(rawToken, store.Sessions[0].TokenHash);
        Assert.Same(user, await service.LookupUserAsync(rawToken));
    }

    [Fact]
    public async Task Lookup_rejects_expired_session()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.Zero));
        var store = new FakeMonitorStore();
        var user = new MonitorUser { Id = Guid.NewGuid(), Username = "viewer", Role = MonitorUserRole.Viewer };
        var service = new SessionService(store, clock, TimeSpan.FromMinutes(1));
        var rawToken = await service.CreateSessionAsync(user);

        clock.Advance(TimeSpan.FromMinutes(2));

        Assert.Null(await service.LookupUserAsync(rawToken));
    }

    [Fact]
    public async Task Revoke_prevents_future_lookup()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.Zero));
        var store = new FakeMonitorStore();
        var user = new MonitorUser { Id = Guid.NewGuid(), Username = "owner", Role = MonitorUserRole.Owner };
        var service = new SessionService(store, clock, TimeSpan.FromMinutes(60));
        var rawToken = await service.CreateSessionAsync(user);

        await service.RevokeAsync(rawToken);

        Assert.Null(await service.LookupUserAsync(rawToken));
        Assert.NotNull(store.Sessions[0].RevokedAtUtc);
    }

    [Fact]
    public async Task Login_failure_records_sanitized_audit_without_cookie()
    {
        var app = await CreateTestApp(new FakeMonitorStore
        {
            Users =
            {
                new MonitorUser
                {
                    Id = Guid.NewGuid(),
                    Username = "owner",
                    Role = MonitorUserRole.Owner,
                    PasswordHash = new PasswordHasher().Hash("secret")
                }
            }
        });
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "owner", password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.TryGetValues("Set-Cookie", out _));

        var store = (FakeMonitorStore)app.Services.GetService(typeof(IMonitorStore))!;
        var audit = Assert.Single(store.Audits);
        Assert.Equal("owner", audit.Username);
        Assert.Equal("login", audit.Action);
        Assert.Equal("auth", audit.Target);
        Assert.False(audit.Success);
        Assert.DoesNotContain("wrong", audit.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_success_sets_secure_cookie_and_records_success_audit()
    {
        var app = await CreateTestApp(new FakeMonitorStore
        {
            Users =
            {
                new MonitorUser
                {
                    Id = Guid.NewGuid(),
                    Username = "owner",
                    Role = MonitorUserRole.Owner,
                    PasswordHash = new PasswordHasher().Hash("secret")
                }
            }
        });
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "owner", password = "secret" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        var cookie = Assert.Single(cookies);
        Assert.Contains(AuthEndpoints.SessionCookieName, cookie, StringComparison.Ordinal);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);

        var store = (FakeMonitorStore)app.Services.GetService(typeof(IMonitorStore))!;
        var audit = Assert.Single(store.Audits);
        Assert.True(audit.Success);
        Assert.Equal("owner", audit.Username);
        Assert.Equal("login", audit.Action);
    }

    private static async Task<WebApplication> CreateTestApp(FakeMonitorStore store)
    {
        var builder = VpsMonitorApp.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IMonitorStore>(store);
        builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
        builder.Services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.Zero)));
        builder.Services.AddSingleton(provider => new SessionService(
            provider.GetRequiredService<IMonitorStore>(),
            provider.GetRequiredService<TimeProvider>(),
            TimeSpan.FromMinutes(60)));

        var app = VpsMonitorApp.BuildApp(builder);
        await app.StartAsync();
        return app;
    }

    private sealed class FakeMonitorStore : IMonitorStore
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

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan interval)
        {
            _now = _now.Add(interval);
        }
    }
}
