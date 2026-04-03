# Step 7 — Fraud Detection Service — Walkthrough

## Summary

The Fraud Detection Service is now fully implemented — a dedicated fraud analytics engine that evaluates every completed sale through 10 configurable fraud rules and generates alerts for admin review. It integrates into the choreography saga by consuming `SaleCompletedEvent` and `DipVarianceDetectedEvent`.

---

## Architecture

```
FraudDetectionService.API (ASP.NET 10 Web API)
  └── FraudDetectionService.Infrastructure (EF Core, RabbitMQ)
       └── FraudDetectionService.Application (MediatR, 10 FraudRules)
            └── FraudDetectionService.Domain (FraudAlert, FraudRuleEvaluation, Events)
```

---

## Fraud Rule Engine

The core innovation is the **pluggable rule engine**: each rule implements `IFraudRule` and is registered via DI. The `EvaluateFraudRulesHandler` iterates all injected rules for each transaction.

### All 10 Rules

| # | Rule | Trigger Condition | Severity |
|---|------|------------------|----------|
| 1 | OversellRule | tx.QuantityLitres > tank stock | High |
| 2 | **RapidTransactionRule** | >5 transactions on same pump in 2 min | **High** |
| 3 | **DipVarianceRule** | Physical vs system dip variance >2% | **High** |
| 4 | OddHoursRule | Transaction outside 05:00-23:00 IST | Medium |
| 5 | RoundNumberRule | Last 5 consecutive transactions on pump are whole numbers | Medium |
| 6 | **PriceMismatchRule** | TotalAmount ≠ Round(Qty × Price, 2) beyond ₹1 | **High** |
| 7 | DuplicateTransactionRule | Same vehicle + pump + quantity within 30 min | Medium |
| 8 | VolumeSpikeRule | Daily volume >150% of 4-week avg for day-of-week | Medium |
| 9 | NewDealerRule | >100 transactions today (proxy for new account) | Medium |
| 10 | **VoidPatternRule** | Dealer voided >3 transactions today | **High** |

### Evaluation Flow

```
SaleCompletedEvent (from Sales Service)
  → FraudConsumerHostedService
    → EvaluateFraudRulesCommand (MediatR)
      → Iterate all 10 IFraudRule implementations
        → Create FraudRuleEvaluation for each (audit log)
        → Create FraudAlert for each triggered rule
        → Publish FraudAlertTriggeredEvent (→ Notification Service)

DipVarianceDetectedEvent (from Inventory Service)
  → FraudConsumerHostedService
    → DipVarianceRule.EvaluateDipVariance() — bypasses standard flow
      → Create FraudAlert directly if variance >2%
```

---

## API Endpoints (7)

| Method | Route | Description |
|--------|-------|-------------|
| GET | /api/fraud/alerts | Paginated alerts with status/severity/station/date filters |
| GET | /api/fraud/alerts/{id} | Single alert with details |
| PUT | /api/fraud/alerts/{id}/dismiss | Dismiss with notes |
| PUT | /api/fraud/alerts/{id}/investigate | Mark under review |
| PUT | /api/fraud/alerts/{id}/escalate | Escalate with notes |
| POST | /api/fraud/alerts/bulk-dismiss | Bulk dismiss multiple alerts |
| GET | /api/fraud/stats | Alert statistics by status |

All endpoints require `Admin` or `SuperAdmin` role.

---

## Database Schema (EPCL_Fraud)

### FraudAlerts
| Column | Type | Notes |
|--------|------|-------|
| Id | UNIQUEIDENTIFIER | PK |
| TransactionId | UNIQUEIDENTIFIER | Indexed, logical FK |
| StationId | UNIQUEIDENTIFIER | Indexed |
| RuleTriggered | NVARCHAR(50) | Rule class name |
| Severity | VARCHAR(10) | High/Medium/Low |
| Description | NVARCHAR(1000) | Why rule triggered |
| Status | VARCHAR(20) | Open/UnderReview/Dismissed/Escalated |
| ReviewedByUserId | UNIQUEIDENTIFIER | Admin who reviewed |
| ReviewedAt | DATETIME2 | Review timestamp |
| ReviewNotes | NVARCHAR(500) | Admin notes |

### FraudRuleEvaluations
| Column | Type | Notes |
|--------|------|-------|
| Id | UNIQUEIDENTIFIER | PK |
| TransactionId | UNIQUEIDENTIFIER | Indexed |
| RuleName | NVARCHAR(50) | Rule evaluated |
| Passed | BIT | true=no fraud |
| Details | NVARCHAR(500) | Result explanation |

---

## Event Flow Integration

```
RabbitMQ Exchange: epcl.events (topic)

Consumed by Fraud Service:
  - sales.completed → triggers all 10 rules
  - inventory.dip.variance → triggers DipVarianceRule

Published by Fraud Service:
  - fraud.alert.triggered → consumed by Notification Service (Step 8)
```

---

## Build Verification

```
✅ FraudDetectionService.Domain        → 0 errors
✅ FraudDetectionService.Application   → 0 errors
✅ FraudDetectionService.Infrastructure → 0 errors
✅ FraudDetectionService.API            → 0 errors
✅ InitialCreate migration              → Generated
✅ Cross-project build (6 services)     → All pass
```
