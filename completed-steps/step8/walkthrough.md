# Step 8 — Notification Service — Walkthrough

## Summary

The Notification Service is the platform's event-driven communication hub, consuming 9 event types from RabbitMQ and delivering notifications via Email (MailKit/SMTP), SMS (stub), and In-App channels. It includes 8 professional HTML email templates and a price alert subscription system.

---

## Architecture

```
NotificationService.API (ASP.NET 10 Web API)
  └── NotificationService.Infrastructure
       ├── Messaging/NotificationConsumerHostedService (RabbitMQ — 9 bindings)
       ├── Services/MailKitEmailService (Gmail SMTP)
       ├── Services/SmsService (Stub)
       ├── Services/EmailTemplateService (8 embedded HTML templates)
       └── Persistence/NotificationDbContext
            └── NotificationService.Application (MediatR CQRS)
                 └── NotificationService.Domain
```

---

## Event Consumers (9)

| Event | Source | Notification |
|-------|--------|-------------|
| SaleCompletedEvent | Sales | Receipt → Customer (InApp) |
| FraudAlertTriggeredEvent | Fraud | 🚨 Alert → Admin (InApp) |
| StockLevelLowEvent | Inventory | ⚠ Low Stock → Dealer (InApp) |
| StockLevelCriticalEvent | Inventory | 🔴 CRITICAL → Dealer (InApp) |
| ReplenishmentRequestedEvent | Inventory | Request → Admin (InApp) |
| ReplenishmentApprovedEvent | Inventory | ✅ Approved → Dealer (InApp) |
| FuelPriceUpdatedEvent | Sales | Price Update → all Dealers (InApp) |
| DipVarianceDetectedEvent | Inventory | 📊 Variance → Admin (InApp) |
| UserAccountLockedEvent | Identity | Account Locked → User+Admin (InApp) |

## Email Templates (8)

| Template | File | Description |
|----------|------|-------------|
| OTP/Verification | otp.html | Code box with security notice |
| Transaction Receipt | receipt.html | Station, fuel, amount table |
| Low Stock Alert | low-stock.html | Red banner, stock table |
| Fraud Alert | fraud-alert.html | Severity badge, review button |
| Welcome | welcome.html | Role-appropriate greeting |
| Replenishment Approved | replenishment-approved.html | Green success header |
| Daily Sales Summary | daily-summary.html | Stats cards, breakdown |
| Monthly Compliance | monthly-report.html | PPAC-style report |

## API Endpoints (7)

| Controller | Method | Route | Auth |
|-----------|--------|-------|------|
| Notifications | GET | /api/notifications/in-app | JWT |
| Notifications | PUT | /api/notifications/in-app/{id}/read | JWT |
| Notifications | PUT | /api/notifications/in-app/read-all | JWT |
| Notifications | GET | /api/notifications/logs | Admin |
| PriceAlerts | POST | /api/notifications/price-alerts | JWT |
| PriceAlerts | GET | /api/notifications/price-alerts | JWT |
| PriceAlerts | DELETE | /api/notifications/price-alerts/{id} | JWT |

## Build Verification

```
✅ NotificationService.Domain        → 0 errors
✅ NotificationService.Application   → 0 errors
✅ NotificationService.Infrastructure → 0 errors
✅ NotificationService.API            → 0 errors
✅ InitialCreate migration            → Generated
✅ Cross-project build (7 services)   → All pass
```
