# Step 12 — SignalR Real-Time — Walkthrough

## Summary

Added SignalR real-time capabilities to the Reporting Service. Two hubs (AdminHub for platform alerts, DealerHub for station-specific updates) consume 4 RabbitMQ event types via a bridge consumer and push live notifications to connected clients. JWT authentication for WebSocket connections uses query string token extraction.

---

## Architecture

```
RabbitMQ (4 event types)
  └── epcl.signalr.bridge.queue
       └── SignalRBridgeConsumerHostedService (Infrastructure)
            └── ISignalRNotificationService (interface)
                 └── SignalRNotificationService (API)
                      ├── IHubContext<AdminHub> → "Admins" group
                      └── IHubContext<DealerHub> → "Station-{id}" / "AllDealers" groups
```

---

## Hub Endpoints

| Hub | Path | Auth | Groups | Events |
|-----|------|------|--------|--------|
| AdminHub | /hubs/admin | Admin, SuperAdmin | Admins | NewFraudAlert, StockCritical, ReplenishmentRequested |
| DealerHub | /hubs/dealer | Dealer | Station-{id}, AllDealers | StockCritical, FuelPriceUpdated |

---

## Event → Hub Mapping

| RabbitMQ Routing Key | Hub Target | SignalR Event Name |
|---------------------|------------|-------------------|
| fraud.alert.triggered | AdminHub → Admins | NewFraudAlert |
| inventory.stock.critical | AdminHub → Admins + DealerHub → Station-{id} | StockCritical |
| inventory.replenishment.requested | AdminHub → Admins | ReplenishmentRequested |
| sales.price.updated | DealerHub → AllDealers | FuelPriceUpdated |

---

## JWT Authentication for SignalR

SignalR WebSocket connections can't use HTTP headers. Token is passed via query string:
```
/hubs/admin?access_token=eyJhbGci...
```
Extracted in `JwtBearerEvents.OnMessageReceived` → `context.Token = accessToken`

---

## Design Decisions

- **Interface pattern**: `ISignalRNotificationService` lives in Infrastructure (clean architecture), concrete `SignalRNotificationService` in API project (has access to `IHubContext<T>`)
- **Dedicated bridge queue**: `epcl.signalr.bridge.queue` binds to 4 routing keys, separate from other consumers to prevent interference
- **Group-based targeting**: AdminHub broadcasts to all admins, DealerHub targets specific stations for stock alerts

---

## Build Verification

```
✅ ReportingService.Domain         → 0 errors
✅ ReportingService.Application    → 0 errors
✅ ReportingService.Infrastructure → 0 errors
✅ ReportingService.API            → 0 errors
✅ Cross-project (10 services)     → All pass
```
