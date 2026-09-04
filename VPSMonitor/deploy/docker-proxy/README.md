# Docker Socket Proxy

This directory configures a read-only Docker socket proxy using `tecnativa/docker-socket-proxy`.

## Why is this needed?

The VPS Monitor API needs to query Docker to see the status of containers and projects. However, exposing the raw `/var/run/docker.sock` directly to the API container is a major security risk. If the API container were compromised, the attacker would have full root access to the host via the Docker socket (e.g., they could start privileged containers).

## Security Restrictions

This proxy mitigates the risk by intercepting HTTP requests to the Docker socket and strictly filtering what is allowed. 
We have configured it to be **READ-ONLY** and we only allow the specific endpoints the monitor needs:

*   `CONTAINERS=1` (Allows listing and inspecting containers)
*   `IMAGES=1` (Allows listing images)
*   `INFO=1` (Allows getting system info)
*   `VERSION=1` (Allows getting docker version)

**All other access is explicitly denied**, including:
*   `POST=0`, `EXEC=0`, `SECRETS=0`, `AUTH=0`, `VOLUMES=0`, `NETWORKS=0`, `SWARM=0`

This ensures that even if the API container is compromised, it cannot be used to modify containers, start new processes, or access Docker secrets.
