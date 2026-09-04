# VPSMonitor

Independent VPS monitor foundation for the Supermer deployment. This solution is isolated from SuperPOS and starts with a small ASP.NET Core gateway, a configuration contract, and a smoke test.

## What is included

- `GET /health` returns `{"ok":true}`.
- `GET /api/version` returns the application version and build commit from environment variables.
- Configuration keys are already reserved for the monitor, Docker proxy, Prometheus, Loki, SMTP, AI, and the PostgreSQL connection string.

## Configuration

The app reads version metadata from these environment variables:

- `APP_VERSION`
- `BUILD_COMMIT`

The checked-in defaults are safe placeholders for development and internal services only. Copy `src/VpsMonitor.Web/appsettings.Development.json.example` to `src/VpsMonitor.Web/appsettings.Development.json` for local overrides.

## Run

```bash
dotnet run --project src/VpsMonitor.Web/VpsMonitor.Web.csproj
```

## Test

```bash
dotnet test src/VpsMonitor.Web.Tests/VpsMonitor.Web.Tests.csproj
```
