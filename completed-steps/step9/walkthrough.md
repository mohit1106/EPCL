# Step 9 — Audit Service — Walkthrough

## Summary

The Audit Service is a dedicated, append-only compliance log that captures every state change across the entire platform. It consumes all `audit.#` events from RabbitMQ and stores them in the EPCL_Audit database as immutable records. No update or delete operations exist anywhere in the codebase — this is enforced at both the repository interface level (no update/delete methods) and the domain design level.

---

## Architecture

```
AuditService.API (ASP.NET 10 Web API — Admin-only)
  └── AuditService.Infrastructure
       ├── Messaging/AuditEventConsumerHostedService (RabbitMQ — audit.# wildcard)
       ├── Persistence/AuditDbContext (single table)
       └── Repositories/AuditLogRepository (append + read only)
            └── AuditService.Application (MediatR)
                 └── AuditService.Domain (AuditLog entity — immutable)
```

---

## Key Design: Append-Only Architecture

The entire service enforces immutability:
- **Repository interface**: Only `AppendAsync`, `GetByIdAsync`, `GetPagedAsync`, `ExportAsync` — no `UpdateAsync`, no `DeleteAsync`
- **Command**: Only `AppendAuditLogCommand` exists — no update/delete commands
- **API**: Only GET and POST (export) endpoints — no PUT or DELETE
- **Idempotency**: `EventId` is deduplicated via unique index — duplicate events are silently skipped

---

## Database Schema (EPCL_Audit)

### AuditLogs

| Column | Type | Notes |
|--------|------|-------|
| Id | UNIQUEIDENTIFIER | PK |
| EventId | UNIQUEIDENTIFIER | UNIQUE — RabbitMQ event dedup |
| EntityType | NVARCHAR(100) | Transaction, Tank, User, etc. |
| EntityId | UNIQUEIDENTIFIER | ID of changed entity |
| Operation | VARCHAR(10) | Create, Update, Delete |
| OldValues | NVARCHAR(MAX) | JSON snapshot before change |
| NewValues | NVARCHAR(MAX) | JSON snapshot after change |
| ChangedByUserId | UNIQUEIDENTIFIER | User who triggered change |
| ChangedByRole | NVARCHAR(20) | Admin, Dealer, etc. |
| IpAddress | VARCHAR(45) | Requestor IP |
| CorrelationId | NVARCHAR(50) | X-Correlation-ID |
| ServiceName | NVARCHAR(50) | Publishing microservice |
| Timestamp | DATETIME2 | Exact UTC change time |

**Indexes**: EventId (unique), EntityType, EntityId, ChangedByUserId, Timestamp, ServiceName

---

## RabbitMQ Consumer

```
Exchange: epcl.events (topic)
Queue: epcl.audit.queue
Binding: audit.# (wildcard — catches all audit events)

Expected routing keys from other services:
  audit.user.created, audit.user.updated, audit.user.deleted
  audit.transaction.created, audit.transaction.completed, audit.transaction.voided
  audit.tank.created, audit.tank.updated
  audit.station.created, audit.station.updated
  audit.price.updated
  audit.replenishment.requested, audit.replenishment.approved
  audit.fraud.alert.created, audit.fraud.alert.dismissed
  ...any future audit.* events are automatically captured
```

---

## API Endpoints (3 — Admin only)

| Method | Route | Description |
|--------|-------|-------------|
| GET | /api/audit/logs | Paginated with filters (entityType, userId, operation, serviceName, dateRange) |
| GET | /api/audit/logs/{id} | Single entry with full old/new values JSON |
| POST | /api/audit/logs/export | Export all matching records (compliance) |

---

## Build Verification

```
✅ AuditService.Domain         → 0 errors
✅ AuditService.Application    → 0 errors
✅ AuditService.Infrastructure → 0 errors
✅ AuditService.API            → 0 errors
✅ InitialCreate migration     → Generated
✅ Cross-project (8 services)  → All pass
```
