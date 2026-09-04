using VpsMonitor.Web.Data.Entities;
using VpsMonitor.Web.Security;

namespace VpsMonitor.Web.Endpoints;

public static class AuthEndpoints
{
    public const string SessionCookieName = "vps_monitor_session";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/login", LoginAsync);
        endpoints.MapPost("/api/auth/logout", LogoutAsync);
        endpoints.MapGet("/api/auth/me", MeAsync);
        return endpoints;
    }

    public static TBuilder RequireMonitorOwner<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;
            var sessions = httpContext.RequestServices.GetRequiredService<SessionService>();
            var user = await sessions.LookupUserAsync(httpContext.Request.Cookies[SessionCookieName], httpContext.RequestAborted);

            if (user is null)
            {
                return Results.Unauthorized();
            }

            if (user.Role != MonitorUserRole.Owner)
            {
                return Results.Forbid();
            }

            return await next(context);
        });
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        HttpContext httpContext,
        IMonitorStore store,
        IPasswordHasher passwordHasher,
        SessionService sessions,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var username = request.Username?.Trim() ?? "";
        var user = string.IsNullOrEmpty(username)
            ? null
            : await store.FindUserByUsernameAsync(username, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password ?? "", user.PasswordHash))
        {
            await AuditAsync(store, httpContext, clock, username, "login", "auth", false, "invalid credentials", cancellationToken);
            return Results.Unauthorized();
        }

        var rawToken = await sessions.CreateSessionAsync(user, cancellationToken);
        httpContext.Response.Cookies.Append(SessionCookieName, rawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = clock.GetUtcNow().Add(sessions.SessionLifetime)
        });

        await AuditAsync(store, httpContext, clock, user.Username, "login", "auth", true, "authenticated", cancellationToken);
        return Results.Json(new AuthUserResponse(user.Username, user.Role.ToString()));
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext httpContext,
        IMonitorStore store,
        SessionService sessions,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var rawToken = httpContext.Request.Cookies[SessionCookieName];
        var user = await sessions.LookupUserAsync(rawToken, cancellationToken);
        await sessions.RevokeAsync(rawToken, cancellationToken);
        await AuditAsync(store, httpContext, clock, user?.Username ?? "", "logout", "auth", user is not null, "session revoked", cancellationToken);
        httpContext.Response.Cookies.Delete(SessionCookieName);
        return Results.NoContent();
    }

    private static async Task<IResult> MeAsync(
        HttpContext httpContext,
        SessionService sessions,
        CancellationToken cancellationToken)
    {
        var user = await sessions.LookupUserAsync(httpContext.Request.Cookies[SessionCookieName], cancellationToken);
        return user is null
            ? Results.Unauthorized()
            : Results.Json(new AuthUserResponse(user.Username, user.Role.ToString()));
    }

    private static Task AuditAsync(
        IMonitorStore store,
        HttpContext httpContext,
        TimeProvider clock,
        string username,
        string action,
        string target,
        bool success,
        string detail,
        CancellationToken cancellationToken)
    {
        return store.AddAuditAsync(new AuditEntry
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = clock.GetUtcNow().UtcDateTime,
            Username = username,
            Action = action,
            Target = target,
            RequestIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "",
            UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
            Success = success,
            Detail = detail
        }, cancellationToken);
    }

    private sealed record LoginRequest(string? Username, string? Password);
    private sealed record AuthUserResponse(string Username, string Role);
}
