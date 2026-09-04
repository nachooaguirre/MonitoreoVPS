using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using VpsMonitor.Web.Data.Entities;

namespace VpsMonitor.Web.Security;

public interface IMonitorStore
{
    Task<MonitorUser?> FindUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task AddSessionAsync(MonitorSession session, CancellationToken cancellationToken = default);
    Task<MonitorSession?> FindSessionByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task AddAuditAsync(AuditEntry audit, CancellationToken cancellationToken = default);
}

public sealed class SessionService(IMonitorStore store, TimeProvider clock, TimeSpan sessionLifetime)
{
    public TimeSpan SessionLifetime { get; } = sessionLifetime;

    public async Task<string> CreateSessionAsync(MonitorUser user, CancellationToken cancellationToken = default)
    {
        var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var now = clock.GetUtcNow().UtcDateTime;

        await store.AddSessionAsync(new MonitorSession
        {
            Id = Guid.NewGuid(),
            MonitorUserId = user.Id,
            MonitorUser = user,
            TokenHash = HashToken(rawToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(SessionLifetime)
        }, cancellationToken);

        return rawToken;
    }

    public async Task<MonitorUser?> LookupUserAsync(string? rawToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var session = await store.FindSessionByTokenHashAsync(HashToken(rawToken), cancellationToken);
        if (session is null || session.RevokedAtUtc is not null || session.ExpiresAtUtc <= clock.GetUtcNow().UtcDateTime)
        {
            return null;
        }

        return session.MonitorUser;
    }

    public async Task RevokeAsync(string? rawToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return;
        }

        var session = await store.FindSessionByTokenHashAsync(HashToken(rawToken), cancellationToken);
        if (session is not null && session.RevokedAtUtc is null)
        {
            session.RevokedAtUtc = clock.GetUtcNow().UtcDateTime;
        }
    }

    public static string HashToken(string rawToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }
}
