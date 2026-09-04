# Coolify Integration Runbook

## Overview
VPS Monitor integrates directly with Coolify deployments by parsing container labels automatically attached by Coolify (`coolify.projectId`) and Docker Compose (`com.docker.compose.project`).

## Label Mapping & Discovery
When containers are created inside Coolify:
- Coolify injects `coolify.projectId` label.
- The `ProjectGroupingService` detects this label and automatically groups all containers belonging to that project under `ProjectKey`.
- If a project uses Compose, `com.docker.compose.project` is used as a secondary grouping label.

## Security Setup
1. Mount host `/var/run/docker.sock` ONLY to `docker-proxy`.
2. Do NOT expose `docker-proxy` (port 2375) publicly. Keep it bound only inside `monitor-net`.
3. Set environment variables `MONITOR_OWNER_USERNAME` and `MONITOR_OWNER_PASSWORD` in Coolify environment settings.

## Troubleshooting Unassigned Containers
If containers show as `unassigned`:
- Check container labels in Coolify via `docker inspect <container_id>`.
- Add explicit label `coolify.projectId=your-project-name` in the Coolify Advanced Settings / Labels section.
