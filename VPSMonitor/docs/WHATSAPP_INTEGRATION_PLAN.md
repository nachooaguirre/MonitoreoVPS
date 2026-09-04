# WhatsApp Integration Strategy & Phase Plan

## Purpose
This document outlines the architecture and implementation phase plan for integrating WhatsApp notifications for critical VPS alerts (e.g. Host Down, Container Crash Loops, High Resource Saturation).

## Architecture Plan

```
[Prometheus / Alertmanager] ──> [Alert Webhook / Event Bus] ──> [WhatsApp Dispatcher Service] ──> [Evolution API / Meta WhatsApp Cloud API] ──> [Admin Mobile]
```

## Phase Breakdown

### Phase 1: Webhook Ingestion Engine (Future Work)
- Add `POST /api/alerts/webhook` endpoint in `vps-monitor-gateway` to receive firing alert payloads from Prometheus Alertmanager.
- Filter alerts by severity (`severity == "critical"`).

### Phase 2: WhatsApp API Adapter
- Create `IWhatsAppNotificationService` interface.
- Implement Meta WhatsApp Business Cloud API / Evolution API client with template message dispatching.

### Phase 3: Rate Limiting & Anti-Spam Safeguards
- Enforce alert throttling (max 1 WhatsApp message per alert group per 15 minutes) to avoid spamming admin numbers.
- Provide opt-in configuration keys in `appsettings.json`:
  ```json
  "WhatsApp": {
    "Enabled": false,
    "ApiUrl": "https://api.whatsapp.com/v1/messages",
    "RecipientPhone": "+5491100000000"
  }
  ```
