# Step 9 — Audit Service ✅

## Tasks

- [x] **9.1** Domain: AuditLog entity — APPEND-ONLY, no update/delete operations (12 columns matching Section 5.7 schema)
- [x] **9.2** Domain: AuditEvent integration event (consumed from all services)
- [x] **9.3** Domain: IAuditLogRepository interface — insert + read only, no update/delete methods
- [x] **9.4** Application: AppendAuditLogCommand + Handler — the ONLY write operation, with EventId-based idempotency
- [x] **9.5** Application: GetAuditLogs query (paginated, filtered by entityType/userId/operation/serviceName/dateRange)
- [x] **9.6** Application: GetAuditLogById query (single entry with old/new values)
- [x] **9.7** Application: ExportAuditLog query (all matching records, no pagination — for compliance export)
- [x] **9.8** Infrastructure: AuditDbContext — single AuditLogs table with 5 indexes (EventId unique, EntityType, EntityId, ChangedByUserId, Timestamp, ServiceName)
- [x] **9.9** Infrastructure: AuditLogRepository — append + read only implementation
- [x] **9.10** Infrastructure: AuditEventConsumerHostedService — binds to `audit.#` wildcard routing key
- [x] **9.11** Infrastructure: DependencyInjection registration
- [x] **9.12** EF Core migration — InitialCreate generated ✅
- [x] **9.13** API: AuditController — 3 endpoints (GET logs, GET logs/{id}, POST logs/export)
- [x] **9.14** API: GlobalExceptionMiddleware + CorrelationIdMiddleware
- [x] **9.15** API: Program.cs, appsettings.json, appsettings.Development.json, Dockerfile
- [x] **9.16** Cross-project verification — All 8 services + Gateway build ✅
