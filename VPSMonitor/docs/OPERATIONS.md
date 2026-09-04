# VPS Monitor Operations & Maintenance Guide

## Architecture Overview
The VPS Monitor stack consists of 10 microservices orchestrated via Docker Compose:
- **`vps-monitor-gateway`**: ASP.NET Core web server & Blazor UI (Port 5080)
- **`vps-monitor-postgres`**: PostgreSQL database storing user credentials, sessions, and audit entries
- **`docker-proxy`**: Read-only socket proxy enforcing non-mutating Docker daemon access
- **`prometheus`**: Time-series metrics engine (Scrapes node-exporter, cadvisor, gateway)
- **`node-exporter`**: Host OS system resource exporter
- **`cadvisor`**: Container-level CPU/RAM/Disk exporter
- **`grafana`**: Dashboards and visualization portal
- **`loki` + `alloy`**: Log collection and aggregation
- **`alertmanager`**: Alerting engine with email notification routing

## Daily Operations & Monitoring

### Check Gateway Health
```bash
curl -f http://localhost:5080/health
```

### Self-Monitoring Metrics
```bash
curl http://localhost:5080/metrics
```

### Viewing Logs
```bash
docker compose -f deploy/docker-compose.yml logs -f gateway
```

## Backup & Recovery

### PostgreSQL Audit Database Backup
```bash
docker exec -t vps-monitor-postgres pg_dump -U vps_monitor vps_monitor > vps_monitor_backup.sql
```

### Restore Database
```bash
cat vps_monitor_backup.sql | docker exec -i vps-monitor-postgres psql -U vps_monitor -d vps_monitor
```
