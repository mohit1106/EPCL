# Step 10 — Reporting Service — Walkthrough

## Summary

The Reporting Service provides the platform's analytics and report generation capabilities. It consumes SaleCompletedEvent from RabbitMQ to build real-time DailySalesSummaries, and provides PDF (QuestPDF) and Excel (ClosedXML) export with EPCL branding, KPI dashboards for Admin and Dealer roles, and scheduled report management.

---

## Architecture

```
ReportingService.API (ASP.NET 10 Web API — 12 endpoints)
  ├── Controllers/ReportsController (7 endpoints)
  ├── Controllers/KpiController (2 endpoints)
  └── Controllers/ScheduledReportsController (3 endpoints)
       └── ReportingService.Infrastructure
            ├── Messaging/ReportingConsumerHostedService (sales.completed, inventory.stock.loaded)
            ├── Persistence/ReportingDbContext (5 tables)
            └── Repositories/ (5 implementations)
                 └── ReportingService.Application (MediatR CQRS)
                      ├── Commands/ (PDF, Excel, Schedule CRUD)
                      └── Queries/ (KPIs, summaries, status)
                           └── ReportingService.Domain
```

---

## Database Schema (EPCL_Reports)

| Table | Purpose | Key Constraints |
|-------|---------|-----------------|
| DailySalesSummaries | Per-station, per-fuel, per-day aggregates | UNIQUE(StationId, FuelTypeId, Date) |
| MonthlyStationReports | Monthly breakdown with petrol/diesel/CNG | Indexed by StationId |
| GeneratedReports | Tracks PDF/Excel file generation lifecycle | Status: Pending → Generating → Ready/Failed |
| ScheduledReports | Cron-based scheduled report definitions | IsActive flag |
| ProcessedEvents | Idempotency for event deduplication | UNIQUE(EventId) |

---

## API Endpoints (12)

| Controller | Method | Route | Auth |
|-----------|--------|-------|------|
| Reports | GET | /api/reports/sales-summary | JWT |
| Reports | GET | /api/reports/station-performance | Admin |
| Reports | GET | /api/reports/dealer-summary/{stationId} | JWT |
| Reports | POST | /api/reports/export/pdf | JWT |
| Reports | POST | /api/reports/export/excel | JWT |
| Reports | GET | /api/reports/exports/{id}/status | JWT |
| Reports | GET | /api/reports/exports/{id}/download | JWT |
| KPI | GET | /api/reports/kpi/admin | Admin |
| KPI | GET | /api/reports/kpi/dealer/{stationId} | JWT |
| Schedule | POST | /api/reports/schedule | Admin |
| Schedule | GET | /api/reports/schedule | Admin |
| Schedule | DELETE | /api/reports/schedule/{id} | Admin |

---

## PDF Generation (QuestPDF)

EPCL-branded PDF with:
- Header: EPCL logo + report type label
- Body: Data table with alternating row colors
- Footer: Generation timestamp
- Summary: Total litres + revenue highlight

## Excel Generation (ClosedXML)

Formatted Excel with:
- Merged header row (EPCL branding)
- Styled column headers (dark blue background, white text)
- Auto-adjusted column widths
- TOTAL row with bold formatting

---

## Build Verification

```
✅ ReportingService.Domain         → 0 errors
✅ ReportingService.Application    → 0 errors
✅ ReportingService.Infrastructure → 0 errors
✅ ReportingService.API            → 0 errors
✅ InitialCreate migration         → Generated
✅ Cross-project (9 services)      → All pass
```
