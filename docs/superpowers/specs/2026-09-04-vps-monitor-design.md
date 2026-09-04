# VPS Monitor — Diseño técnico

**Fecha:** 2026-09-04  
**Estado:** Diseño aprobado para iniciar implementación  
**Alcance:** Monitor privado del VPS del propietario, independiente de SuperPOS

## Objetivo

Crear un proyecto independiente desplegable en Coolify que permita observar el VPS y cada proyecto/contenedor por separado, detectar problemas, conservar históricos, centralizar logs y enviar alertas. La primera versión será accesible temporalmente por un puerto público protegido; posteriormente se restringirá a una IP fija o VPN sin cambiar el modelo interno.

## Alcance de la primera versión

- Estado general del VPS: CPU, carga, memoria, disco, red y uptime.
- Inventario de aplicaciones, contenedores, imágenes, volúmenes y redes visibles desde Coolify.
- Métricas por proyecto y contenedor: CPU, RAM, red entrante/saliente, almacenamiento, reinicios, uptime y estado.
- Chequeos HTTP configurables para APIs y servicios críticos.
- Prometheus para series históricas y Grafana para dashboards operativos.
- Loki/Alloy para logs centralizados con filtros por proyecto y contenedor.
- Alertmanager para reglas de umbral y deduplicación.
- Email en la primera fase; WhatsApp mediante proveedor oficial en una fase posterior.
- Servicio propio para inventario de proyectos, salud, resumen y correlación de eventos.
- Agente de IA de solo lectura que analiza métricas y logs filtrados, genera diagnóstico y resumen diario.
- Auditoría de accesos y de futuras acciones administrativas.

## Seguridad

El único componente publicado será el gateway del monitor en un puerto externo. Prometheus, Grafana, Loki, cAdvisor, Node Exporter, Alertmanager, Docker y PostgreSQL no tendrán puertos públicos.

- Login obligatorio, contraseñas almacenadas con hash seguro y sesiones con expiración.
- Rate limiting, bloqueo progresivo ante intentos fallidos y cabeceras de seguridad.
- Secretos únicamente en variables/secretos de Coolify.
- Socket Docker accesible mediante proxy de solo lectura y con endpoints limitados.
- Sin shell remoto, ejecución de comandos ni reinicios automáticos en la primera versión.
- Logs de autenticación, errores, cambios de configuración y acciones.
- Puerto aleatorio de alto rango durante la fase pública.
- Preparación para whitelist de IP, VPN WireGuard/Tailscale y/o mTLS.
- Advertencia explícita: la fase pública requiere HTTPS para proteger credenciales; si se usa una IP sin certificado válido, se deberá instalar un certificado confiable o aceptar que HTTP no es adecuado para credenciales reales.

## Arquitectura

```text
Internet
  |
  | puerto único del gateway
  v
Monitor Gateway/API + Dashboard
  |-- Prometheus <--- Node Exporter (VPS)
  |                 cAdvisor (contenedores)
  |-- Grafana
  |-- Loki <------- Alloy (logs)
  |-- Alertmanager
  |-- PostgreSQL del monitor (configuración, usuarios, auditoría)
  `-- Analizador IA (solo lectura)
```

El monitor será una aplicación independiente, con su propio `docker-compose.yml`, volumen de datos y configuración. No reutilizará la base de datos ni el código de negocio de SuperPOS. Coolify podrá desplegarlo como una aplicación separada en el mismo VPS.

## Modelo de consumo

Cada contenedor se asociará a un `ProjectKey` de Coolify. El dashboard calculará, para el período elegido:

- consumo absoluto y porcentaje del total del VPS;
- promedio, máximo y tendencia;
- memoria reservada versus utilizada;
- tráfico recibido y enviado;
- reinicios y tiempo activo;
- almacenamiento de volúmenes e imágenes cuando esté disponible;
- errores HTTP y latencia de los chequeos asociados.

Cuando Coolify no exponga una relación confiable entre contenedor y proyecto, se utilizarán etiquetas Docker y una tabla de asignaciones manuales auditada.

## Flujo de alertas e IA

1. Exporters y chequeos generan métricas.
2. Prometheus evalúa reglas de CPU, memoria, disco, disponibilidad, reinicios y latencia.
3. Alertmanager agrupa y deduplica incidentes.
4. El servicio propio agrega contexto: proyecto, último deploy, contenedor afectado y eventos cercanos.
5. La IA redacta diagnóstico, severidad, evidencia y recomendación.
6. Se envía email; WhatsApp se incorpora después mediante Meta Cloud API o Twilio.

La IA no podrá ejecutar acciones ni recibir credenciales de Docker. Toda acción futura, como reiniciar un contenedor, deberá requerir autorización explícita y quedar auditada.

## Fases

### Fase 1 — Observación segura

Gateway autenticado, inventario, métricas por VPS/proyecto/contenedor, Grafana, logs, health checks, email y auditoría.

### Fase 2 — Inteligencia operativa

Correlación con deploys de Coolify, resumen diario, anomalías, proyección de disco/capacidad y verificación de backups.

### Fase 3 — Acceso reforzado y notificaciones

Whitelist de IP fija o VPN, MFA, HTTPS con dominio/certificado confiable y WhatsApp oficial.

### Fase 4 — Acciones aprobadas

Reinicio o rollback controlado, siempre con confirmación, permisos separados y registro completo.

## Criterios de aceptación

- El gateway responde únicamente en el puerto configurado y exige autenticación.
- Ningún servicio auxiliar aparece publicado en Internet.
- Se visualizan al menos el VPS, SuperPOS y los demás proyectos de Coolify separados.
- Las métricas muestran CPU, memoria, red, reinicios y uptime por proyecto/contenedor.
- Se puede consultar histórico y filtrar logs por proyecto.
- Un servicio detenido genera una alerta deduplicada y un email.
- El monitor sobrevive al reinicio del VPS y conserva sus históricos.
- La IA puede explicar una anomalía usando evidencia, sin capacidad de ejecutar comandos.
- La configuración permite activar whitelist/VPN posteriormente sin cambiar el dashboard.

## Fuera de alcance inicial

- Exponer Docker, PostgreSQL o los recolectores directamente.
- Modificar o administrar SuperPOS.
- Reinicios/deploys automáticos.
- WhatsApp sin credenciales y cuenta del proveedor configuradas.
- Garantizar seguridad absoluta mientras el servicio esté publicado en Internet.
