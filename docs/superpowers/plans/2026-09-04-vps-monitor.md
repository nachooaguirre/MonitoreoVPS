# VPS Monitor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an independent, Coolify-deployable monitoring project that exposes one authenticated gateway port and reports VPS, container/project, service-health, log, alert, audit, and read-only AI diagnostics.

**Architecture:** A .NET 10 ASP.NET Core web application provides the authenticated gateway, configuration, project inventory, audit records, health checks, and dashboard API/UI. Prometheus, Grafana, Alertmanager, cAdvisor, Node Exporter, Alloy/Loki, and a private PostgreSQL instance run as internal services in the monitor's own Docker Compose stack. A read-only Docker socket proxy supplies container metadata and stats; no management endpoints or shell access are exposed.

**Tech Stack:** .NET 10, ASP.NET Core Minimal API, Blazor Web App, EF Core + PostgreSQL, xUnit, Docker Compose, Prometheus, Grafana, Alertmanager, cAdvisor, Node Exporter, Grafana Alloy, Loki.

**Spec:** `docs/superpowers/specs/2026-09-04-vps-monitor-design.md`

## Global Constraints

- The monitor is independent of SuperPOS and must not reference or migrate the SuperPOS database.
- Only the gateway port is published; PostgreSQL, Docker proxy, Prometheus, Grafana, Loki, Alloy, cAdvisor, Node Exporter, and Alertmanager remain internal.
- The first public phase has authentication, rate limiting, security headers, audit logging, and no administrative Docker actions.
- Docker access is read-only through a restricted socket proxy.
- All secrets come from Coolify environment variables and are excluded from Git.
- The AI component can read filtered metrics/log summaries only and cannot execute commands or access Docker credentials.
- The default notification channel is email; WhatsApp, VPN/IP allowlisting, MFA, and approved restart/rollback actions are later phases.
- Every task ends with an independently runnable test or validation command.

---

### Task 1: Create the independent monitor solution and configuration contract

**Files:**
- Create: `VPSMonitor/VPSMonitor.slnx`
- Create: `VPSMonitor/src/VpsMonitor.Web/VpsMonitor.Web.csproj`
- Create: `VPSMonitor/src/VpsMonitor.Web/Program.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/appsettings.json`
- Create: `VPSMonitor/src/VpsMonitor.Web/appsettings.Development.json.example`
- Create: `VPSMonitor/.gitignore`
- Create: `VPSMonitor/README.md`
- Test: `VPSMonitor/src/VpsMonitor.Web.Tests/VpsMonitor.Web.Tests.csproj`

**Interfaces:**
- Produces an executable project targeting `net10.0`.
- Configuration keys: `ConnectionStrings:Default`, `Monitor:PublicPort`, `Monitor:SessionMinutes`, `DockerProxy:BaseUrl`, `Prometheus:BaseUrl`, `Loki:BaseUrl`, `Smtp:*`, and `Ai:*`.
- `GET /health` returns `{ "ok": true }` without exposing secrets.
- `GET /api/version` returns application version and build commit from environment variables.

- [ ] **Step 1: Create the project files and references**

Use ASP.NET Core Web SDK, EF Core PostgreSQL provider, Npgsql, BCrypt, and xUnit. Keep the web project buildable before adding domain features.

- [ ] **Step 2: Add safe configuration defaults**

Set internal service URLs to Docker Compose service names, leave SMTP/AI disabled by default, and add development example values without real credentials.

- [ ] **Step 3: Add the health and version endpoints**

Return only operational status and non-secret build metadata.

- [ ] **Step 4: Add a smoke test**

Test that `/health` returns HTTP 200 and `ok=true`.

- [ ] **Step 5: Run validation**

Run: `dotnet test VPSMonitor/src/VpsMonitor.Web.Tests/VpsMonitor.Web.Tests.csproj`
Expected: PASS.

---

### Task 2: Add the monitor database, users, sessions, configuration, and audit model

**Files:**
- Create: `VPSMonitor/src/VpsMonitor.Web/Data/MonitorDbContext.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Data/Entities/MonitorUser.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Data/Entities/MonitorSession.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Data/Entities/ProjectAssignment.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Data/Entities/HealthCheckDefinition.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Data/Entities/AuditEntry.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Migrations/<timestamp>_InitialMonitorSchema.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Security/PasswordHasher.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Security/SessionService.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Endpoints/AuthEndpoints.cs`
- Test: `VPSMonitor/src/VpsMonitor.Web.Tests/Security/SessionServiceTests.cs`
- Test: `VPSMonitor/src/VpsMonitor.Web.Tests/Security/PasswordHasherTests.cs`

**Interfaces:**
- `POST /api/auth/login` accepts `{ username, password }` and returns an HttpOnly, Secure, SameSite=Strict session cookie.
- `POST /api/auth/logout` revokes the current session.
- `GET /api/auth/me` returns username and role, never password hashes.
- `MonitorUser.Role` is `Owner` or `Viewer`; only `Owner` can change monitor configuration.
- `AuditEntry` stores UTC timestamp, user, action, target, request IP, user agent, success, and sanitized detail.
- The first startup seed creates the owner from `MONITOR_OWNER_USERNAME` and `MONITOR_OWNER_PASSWORD`; startup fails if these are absent in production.

- [ ] **Step 1: Write failing password and session tests**

Cover password verification, wrong-password rejection, expiry, revocation, and cookie-independent session lookup.

- [ ] **Step 2: Implement entities, context, and migration**

Use UTC timestamps and indexes on username, session token hash, audit timestamp, and project key.

- [ ] **Step 3: Implement session authentication middleware**

Hash session tokens at rest, rotate the token on login, expire sessions, and reject revoked sessions.

- [ ] **Step 4: Implement login/logout/me endpoints**

Record successful and failed login events in `AuditEntry`.

- [ ] **Step 5: Run tests**

Run: `dotnet test VPSMonitor/src/VpsMonitor.Web.Tests/VpsMonitor.Web.Tests.csproj --filter FullyQualifiedName~Security`
Expected: PASS.

---

### Task 3: Add the internal Docker read-only inventory and per-project metrics adapter

**Files:**
- Create: `VPSMonitor/src/VpsMonitor.Web/Infrastructure/Docker/DockerReadOnlyClient.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Infrastructure/Docker/DockerModels.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Infrastructure/Docker/ProjectGroupingService.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Endpoints/ProjectsEndpoints.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Endpoints/ContainersEndpoints.cs`
- Modify: `VPSMonitor/src/VpsMonitor.Web/Program.cs`
- Test: `VPSMonitor/src/VpsMonitor.Web.Tests/Infrastructure/ProjectGroupingServiceTests.cs`
- Test: `VPSMonitor/src/VpsMonitor.Web.Tests/Infrastructure/DockerReadOnlyClientTests.cs`

**Interfaces:**
- `DockerReadOnlyClient.ListContainersAsync()` returns container id, name, image, labels, state, status, created time, restart count, and project labels.
- `DockerReadOnlyClient.GetContainerStatsAsync(containerId)` returns CPU, memory, network RX/TX, and block I/O values.
- No client method may expose exec, create, start, stop, restart, remove, or image-pull operations.
- `GET /api/projects` returns projects with container count, status, restart count, and assignment source.
- `GET /api/projects/{projectKey}/containers` returns containers belonging to one project.
- Project grouping priority is Coolify/project label, then configured assignment, then `unassigned`.

- [ ] **Step 1: Write grouping tests**

Cover Coolify labels, explicit assignment overrides, missing labels, and stable `unassigned` behavior.

- [ ] **Step 2: Write read-only client tests**

Use a fake HTTP handler and verify only GET requests are generated and Docker errors become safe service-unavailable results.

- [ ] **Step 3: Implement the Docker models and client**

Use an internal base URL from configuration and cancellation tokens. Do not mount or parse the raw Docker socket in application code.

- [ ] **Step 4: Implement project grouping and endpoints**

Include only sanitized image/name/label data; never return environment variables or container command lines.

- [ ] **Step 5: Run tests**

Run: `dotnet test VPSMonitor/src/VpsMonitor.Web.Tests/VpsMonitor.Web.Tests.csproj --filter FullyQualifiedName~Infrastructure`
Expected: PASS.

---

### Task 4: Provision Prometheus, exporters, Grafana, Loki, Alloy, and Alertmanager

**Files:**
- Create: `VPSMonitor/deploy/docker-compose.yml`
- Create: `VPSMonitor/deploy/prometheus/prometheus.yml`
- Create: `VPSMonitor/deploy/prometheus/rules/monitor-alerts.yml`
- Create: `VPSMonitor/deploy/grafana/provisioning/datasources/datasources.yml`
- Create: `VPSMonitor/deploy/grafana/provisioning/dashboards/dashboards.yml`
- Create: `VPSMonitor/deploy/grafana/dashboards/vps-overview.json`
- Create: `VPSMonitor/deploy/grafana/dashboards/projects-overview.json`
- Create: `VPSMonitor/deploy/alloy/config.alloy`
- Create: `VPSMonitor/deploy/alertmanager/alertmanager.yml`
- Create: `VPSMonitor/deploy/docker-proxy/README.md`
- Modify: `VPSMonitor/README.md`

**Interfaces:**
- Compose publishes only the configured gateway port.
- Internal services use a private Compose network.
- Prometheus scrapes Node Exporter and cAdvisor.
- Alloy forwards container logs to Loki with project/container labels.
- Grafana is provisioned with Prometheus and Loki datasources without public exposure.
- Alert rules cover gateway down, service health failure, high CPU, high memory, disk threshold, repeated restarts, and high latency.
- Alertmanager groups duplicate alerts and sends email through environment-substituted SMTP settings.

- [ ] **Step 1: Write the Compose topology**

Use persistent named volumes for monitor PostgreSQL, Prometheus, Grafana, Loki, and Alertmanager. Add health checks and restart policies.

- [ ] **Step 2: Add exporter and scrape configuration**

Keep exporter ports internal and define stable scrape labels for VPS and container identity.

- [ ] **Step 3: Add dashboards**

Show VPS totals, project ranking by CPU/RAM/network, container detail, restarts, and health-check status.

- [ ] **Step 4: Add logs and alert routing**

Configure Alloy labels and Alertmanager email routing with secrets supplied only by environment variables.

- [ ] **Step 5: Validate the stack**

Run: `docker compose -f VPSMonitor/deploy/docker-compose.yml config`
Expected: valid Compose configuration with no bind mounts to the host Docker socket except the documented read-only proxy.

---

### Task 5: Add HTTP health checks, project assignments, and historical summaries

**Files:**
- Create: `VPSMonitor/src/VpsMonitor.Web/Monitoring/HealthCheckRunner.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Monitoring/PrometheusQueryClient.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Monitoring/MonitoringBackgroundService.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Data/Entities/HealthCheckResult.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Endpoints/HealthChecksEndpoints.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Endpoints/MetricsEndpoints.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Migrations/<timestamp>_AddHealthChecks.cs`
- Test: `VPSMonitor/src/VpsMonitor.Web.Tests/Monitoring/HealthCheckRunnerTests.cs`
- Test: `VPSMonitor/src/VpsMonitor.Web.Tests/Monitoring/PrometheusQueryClientTests.cs`

**Interfaces:**
- `HealthCheckDefinition` stores name, URL, project key, method, timeout, expected status, enabled, and interval.
- `GET /api/health-checks` is authenticated; owner-only writes are `POST`, `PUT`, and `DELETE`.
- `GET /api/metrics/projects/{projectKey}?range=1h` returns average/max CPU, memory, RX/TX, restarts, uptime, and health failures.
- The runner persists result timestamp, duration, status code, success, and sanitized error.
- Background execution uses bounded concurrency and never blocks API requests.

- [ ] **Step 1: Write health-check tests**

Cover success, timeout, unexpected status, disabled checks, and cancellation.

- [ ] **Step 2: Implement runner and persistence**

Use a dedicated HttpClient with timeout, no arbitrary redirects, and response-body limits.

- [ ] **Step 3: Implement Prometheus query adapter**

Accept only predefined query templates; do not accept raw PromQL from the browser.

- [ ] **Step 4: Implement endpoints and background loop**

Run checks on configured intervals and expose aggregated metrics.

- [ ] **Step 5: Run tests**

Run: `dotnet test VPSMonitor/src/VpsMonitor.Web.Tests/VpsMonitor.Web.Tests.csproj --filter FullyQualifiedName~Monitoring`
Expected: PASS.

---

### Task 6: Build the authenticated dashboard and audit viewer

**Files:**
- Create: `VPSMonitor/src/VpsMonitor.Web/Components/App.razor`
- Create: `VPSMonitor/src/VpsMonitor.Web/Components/Layout/MainLayout.razor`
- Create: `VPSMonitor/src/VpsMonitor.Web/Components/Pages/Login.razor`
- Create: `VPSMonitor/src/VpsMonitor.Web/Components/Pages/Overview.razor`
- Create: `VPSMonitor/src/VpsMonitor.Web/Components/Pages/Projects.razor`
- Create: `VPSMonitor/src/VpsMonitor.Web/Components/Pages/ProjectDetail.razor`
- Create: `VPSMonitor/src/VpsMonitor.Web/Components/Pages/Logs.razor`
- Create: `VPSMonitor/src/VpsMonitor.Web/Components/Pages/Alerts.razor`
- Create: `VPSMonitor/src/VpsMonitor.Web/Components/Pages/Audit.razor`
- Create: `VPSMonitor/src/VpsMonitor.Web/wwwroot/css/app.css`
- Create: `VPSMonitor/src/VpsMonitor.Web.Tests/Components/DashboardSmokeTests.cs`

**Interfaces:**
- Unauthenticated users see only the login page.
- Overview shows VPS health, active alerts, total project count, and top resource consumers.
- Projects shows one card/table row per project with CPU, RAM, network, restarts, uptime, and health.
- Project detail shows container breakdown and time-range selection.
- Logs supports project, container, level, text, and time filters through Loki queries prepared server-side.
- Alerts shows active/resolved state, severity, evidence, and notification status.
- Audit shows login, configuration, query, and action records visible only to Owner.
- No page renders secrets, environment variables, raw bearer tokens, or arbitrary query strings.

- [ ] **Step 1: Add the Blazor shell and auth guard**

Redirect unauthenticated requests to Login and preserve the return path only after validation.

- [ ] **Step 2: Add overview and project views**

Use server API calls and clear loading/error states.

- [ ] **Step 3: Add logs, alerts, and audit views**

Paginate server-side and show timestamps in local time while storing UTC.

- [ ] **Step 4: Add responsive styling**

Make the dashboard usable from a desktop browser and a phone without exposing a second service port.

- [ ] **Step 5: Run UI smoke tests**

Run: `dotnet test VPSMonitor/src/VpsMonitor.Web.Tests/VpsMonitor.Web.Tests.csproj --filter FullyQualifiedName~Components`
Expected: PASS.

---

### Task 7: Add email notifications and read-only AI diagnostics

**Files:**
- Create: `VPSMonitor/src/VpsMonitor.Web/Notifications/EmailNotificationService.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Notifications/AlertNotificationWorker.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/AI/DiagnosticsService.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/AI/AiClient.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Endpoints/DiagnosticsEndpoints.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Data/Entities/NotificationDelivery.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web.Tests/Notifications/EmailNotificationServiceTests.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web.Tests/AI/DiagnosticsServiceTests.cs`

**Interfaces:**
- Email is disabled when SMTP settings are absent and reports a clear dashboard status.
- Notifications are deduplicated by alert fingerprint and time window.
- `POST /api/diagnostics/analyze` accepts a project key and time range, loads only server-selected metric/log summaries, and returns severity, evidence, likely cause, and recommendation.
- The AI prompt explicitly forbids command execution and treats logs as untrusted data.
- `AiClient` uses configurable OpenAI-compatible `BaseUrl`, `ApiKey`, and `Model`; no key is committed.
- AI failure never prevents metrics collection or alert delivery.

- [ ] **Step 1: Write notification and AI tests**

Cover disabled SMTP, successful delivery through a fake sender, duplicate suppression, prompt sanitization, and provider timeout.

- [ ] **Step 2: Implement email delivery and notification persistence**

Use bounded retries, idempotency fingerprints, and sanitized delivery errors.

- [ ] **Step 3: Implement diagnostics context builder**

Limit logs by count/bytes, redact tokens/passwords, and include only approved metric fields.

- [ ] **Step 4: Implement AI endpoint and daily-summary hook**

Keep the scheduled summary disabled until AI credentials and recipient configuration are present.

- [ ] **Step 5: Run tests**

Run: `dotnet test VPSMonitor/src/VpsMonitor.Web.Tests/VpsMonitor.Web.Tests.csproj --filter FullyQualifiedName~Notifications|FullyQualifiedName~AI`
Expected: PASS.

---

### Task 8: Harden public deployment and create the Coolify runbook

**Files:**
- Create: `VPSMonitor/deploy/.env.example`
- Create: `VPSMonitor/deploy/firewall-rules.md`
- Create: `VPSMonitor/deploy/security-checklist.md`
- Create: `VPSMonitor/deploy/coolify-runbook.md`
- Modify: `VPSMonitor/deploy/docker-compose.yml`
- Modify: `VPSMonitor/src/VpsMonitor.Web/Program.cs`
- Test: `VPSMonitor/src/VpsMonitor.Web.Tests/Security/HeadersAndRateLimitTests.cs`

**Interfaces:**
- The gateway port is configurable and defaults to a documented high port only for development.
- Production requires `MONITOR_OWNER_USERNAME`, `MONITOR_OWNER_PASSWORD`, `MONITOR_SESSION_KEY`, database password, and encryption/signing secrets through Coolify.
- Production rejects default credentials, development exception pages, and insecure cookie settings.
- Security headers include CSP, HSTS when HTTPS is enabled, X-Content-Type-Options, Referrer-Policy, and frame restrictions.
- Rate limiting applies to login and all authenticated API endpoints.
- The runbook documents temporary public access and the later IP/VPN migration without changing application routes.

- [ ] **Step 1: Write security tests**

Verify missing production secrets fail startup, default credentials are rejected, headers are present, and repeated login attempts receive HTTP 429.

- [ ] **Step 2: Implement production validation and middleware**

Use environment-bound secrets, secure cookies, rate limiting, and sanitized exception responses.

- [ ] **Step 3: Write firewall and Coolify instructions**

Document one published gateway port, internal-only services, backups, log retention, health checks, and rollback.

- [ ] **Step 4: Validate the deployment files**

Run: `docker compose --env-file VPSMonitor/deploy/.env.example -f VPSMonitor/deploy/docker-compose.yml config`
Expected: valid configuration with no real secrets and no auxiliary published ports.

- [ ] **Step 5: Run the complete test suite**

Run: `dotnet test VPSMonitor/src/VpsMonitor.Web.Tests/VpsMonitor.Web.Tests.csproj`
Expected: PASS.

---

### Task 9: Add observability of the monitor itself and release documentation

**Files:**
- Create: `VPSMonitor/src/VpsMonitor.Web/Monitoring/SelfMetrics.cs`
- Create: `VPSMonitor/src/VpsMonitor.Web/Endpoints/SystemEndpoints.cs`
- Create: `VPSMonitor/deploy/grafana/dashboards/monitor-health.json`
- Create: `VPSMonitor/docs/operations.md`
- Create: `VPSMonitor/docs/whatsapp-phase.md`
- Modify: `VPSMonitor/README.md`
- Test: `VPSMonitor/src/VpsMonitor.Web.Tests/Monitoring/SelfMetricsTests.cs`

**Interfaces:**
- `GET /metrics` is internal-only and exposes monitor request count, latency, background-loop failures, and notification outcomes in Prometheus format.
- The dashboard reports monitor health separately from monitored projects.
- Operations documentation covers first deploy, credential rotation, restoring volumes, reading incidents, and enabling IP allowlisting/VPN.
- WhatsApp documentation records the required Meta/Twilio credentials and webhook/provider boundary without adding an unconfigured integration.

- [ ] **Step 1: Write self-metric tests**

Cover counter increments, latency buckets, and failure isolation.

- [ ] **Step 2: Implement internal self-metrics**

Do not include usernames, IP addresses, tokens, project secrets, or unbounded label values.

- [ ] **Step 3: Add monitor-health dashboard**

Show gateway availability, background failures, notification delivery, and storage status.

- [ ] **Step 4: Complete operations documentation**

Include exact environment variables, volume backup commands, and later IP/VPN steps.

- [ ] **Step 5: Run final validation**

Run:
`dotnet test VPSMonitor/src/VpsMonitor.Web.Tests/VpsMonitor.Web.Tests.csproj`
`docker compose -f VPSMonitor/deploy/docker-compose.yml config`
Expected: all tests PASS and Compose configuration valid.

---

## Delivery checkpoints

1. After Task 3: authenticated API can inventory projects and containers using read-only data.
2. After Task 5: historical project metrics and health checks work without the UI.
3. After Task 6: browser dashboard is usable.
4. After Task 7: email and read-only AI diagnostics work when configured.
5. After Task 8: public deployment has security controls and a Coolify runbook.
6. After Task 9: the monitor can monitor itself and be operated safely.

