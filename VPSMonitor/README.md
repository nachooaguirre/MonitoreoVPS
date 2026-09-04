# VPSMonitor

Independent VPS monitor foundation for the Supermer deployment. This solution is isolated from SuperPOS and provides real-time infrastructure, container health, logging, metrics, alerting, and project-based Docker monitoring.

## What is included

- `GET /health` returns `{"ok":true}`.
- `GET /api/version` returns application version and build commit.
- `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/me` with secure BCrypt sessions and PostgreSQL audit logging.
- `GET /api/projects` returns containers grouped by Coolify / Docker Compose project keys.
- `GET /api/containers/{id}/stats` returns real-time CPU, RAM, and I/O metrics via read-only Docker socket proxy.
- Complete Docker Compose deployment stack with Prometheus, node-exporter, cAdvisor, Grafana, Loki, Alloy, and Alertmanager.

## Configuration & Secrets

Copy `deploy/.env.example` to `deploy/.env` and update secrets before production deployment:

```bash
MONITOR_OWNER_USERNAME=admin
MONITOR_OWNER_PASSWORD=your_secure_password
POSTGRES_PASSWORD=db_secure_password
GRAFANA_ADMIN_PASSWORD=grafana_secure_password
```

## Deployment

Deploy the entire stack with Docker Compose:

```bash
cd deploy
docker compose up -d
```

## Development & Run Locally

```bash
dotnet run --project src/VpsMonitor.Web/VpsMonitor.Web.csproj
```

## Test

```bash
dotnet test src/VpsMonitor.Web.Tests/VpsMonitor.Web.Tests.csproj
```

## Deployment

The deployment configuration is located in the `deploy/` directory. It uses Docker Compose to provision the following services:

- **gateway**: The VPS Monitor API
- **postgres**: PostgreSQL database for the API
- **docker-proxy**: A read-only proxy to secure access to the Docker socket
- **prometheus**: Metrics collection and alerting rules
- **node-exporter**: System metrics (CPU, Memory, Disk)
- **cadvisor**: Container metrics
- **grafana**: Dashboards (provisioned automatically)
- **loki** & **alloy**: Log aggregation and collection
- **alertmanager**: Alert routing and notification

### How to deploy

1. Navigate to the `deploy/` directory:
   ```bash
   cd deploy
   ```
2. Copy the `.env.example` file to `.env` and fill in the required values (especially passwords and SMTP settings):
   ```bash
   cp .env.example .env
   ```
3. Start the services:
   ```bash
   docker compose up -d
   ```
