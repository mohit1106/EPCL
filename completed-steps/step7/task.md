# Step 7 — Fraud Detection Service ✅

## Tasks

- [x] **7.1** Domain: FraudAlert, FraudRuleEvaluation entities (+ ProcessedEvent for idempotency)
- [x] **7.2** Domain: AlertSeverity (Low/Medium/High), AlertStatus (Open/UnderReview/Dismissed/Escalated) enums
- [x] **7.3** Domain: IntegrationEvents (SaleCompletedEvent, DipVarianceDetectedEvent, FraudAlertTriggeredEvent)
- [x] **7.4** Domain: Repository interfaces with specialized context queries for rules
- [x] **7.5** Application: IFraudRule interface — `EvaluateAsync(SaleCompletedEvent) → (Triggered, Description)`
- [x] **7.6** Application: ALL 10 fraud rules implemented:
  - [x] `OversellRule` — tx.QuantityLitres > current tank stock
  - [x] `RapidTransactionRule` — >5 transactions on same pump in 2 minutes (**HIGH**)
  - [x] `DipVarianceRule` — triggered by DipVarianceDetectedEvent, >2% variance (**HIGH**)
  - [x] `OddHoursRule` — transaction outside 05:00-23:00 IST (**MEDIUM**)
  - [x] `RoundNumberRule` — last 5 consecutive from same pump are whole numbers (**MEDIUM**)
  - [x] `PriceMismatchRule` — TotalAmount != Round(Qty*Price, 2) beyond ₹1 (**HIGH**)
  - [x] `DuplicateTransactionRule` — same vehicle+pump+qty within 30 min (**MEDIUM**)
  - [x] `VolumeSpikeRule` — daily total >150% of 4-week average for day-of-week (**MEDIUM**)
  - [x] `NewDealerRule` — >100 transactions today (**MEDIUM**)
  - [x] `VoidPatternRule` — dealer voided >3 transactions today (**HIGH**)
- [x] **7.7** Application: EvaluateFraudRulesCommandHandler — runs all rules, creates FraudAlert per triggered rule, publishes FraudAlertTriggeredEvent
- [x] **7.8** Application: DismissAlert, InvestigateAlert, EscalateAlert, BulkDismissAlerts commands
- [x] **7.9** Application: GetFraudAlerts (paginated+filtered), GetFraudAlertById, GetFraudStats queries
- [x] **7.10** Infrastructure: EPCL_Fraud DbContext with FraudAlerts, FraudRuleEvaluations, ProcessedEvents
- [x] **7.11** Infrastructure: FraudAlertRepository, FraudRuleEvaluationRepository, ProcessedEventRepository
- [x] **7.12** Infrastructure: RabbitMqPublisher
- [x] **7.13** Infrastructure: FraudConsumerHostedService — SaleCompletedEvent + DipVarianceDetectedEvent consumers
- [x] **7.14** Infrastructure: DependencyInjection — registers all 10 rules as IFraudRule
- [x] **7.15** EF Core migration — InitialCreate generated ✅
- [x] **7.16** API: FraudAlertsController (7 endpoints)
- [x] **7.17** API: GlobalExceptionMiddleware + CorrelationIdMiddleware
- [x] **7.18** API: Program.cs (Serilog, JWT, Swagger, CORS, Health Checks)
- [x] **7.19** API: appsettings.json + appsettings.Development.json + Dockerfile
- [x] **7.20** Cross-project verification — All 6 services + Gateway build ✅
