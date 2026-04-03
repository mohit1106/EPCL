# Step 1 — Project Scaffolding ✅

- [x] All 10 items complete (git, env, docker, services, tests, gateway, landing)

---

# Step 2 — Identity Service ✅

- [x] All 32 items complete (domain, application, infrastructure, API, migration, build)

---

# Step 3 — Station Service ✅

- [x] All 26 items complete (domain, application, infrastructure, API, migration, build)

---

# Step 4 — Ocelot API Gateway ✅

- [x] All 16 items complete (installation, configuration, JWT, rate limit, cache, routing, build)

---

# Step 5 — Inventory Service ✅

## Tasks

- [x] **5.1** Domain: Create `Tank`, `StockLoading`, `DipReading`, `ReplenishmentRequest` entities
- [x] **5.2** Domain: Create `TankStatus`, `ReplenishmentStatus`, `UrgencyLevel` enums
- [x] **5.3** Domain: Create `IProcessedEventRepository` for idempotency
- [x] **5.4** Domain: Set up Integration Events for RabbitMQ
- [x] **5.5** Application: `RecordStockLoadingCommand` (capacity check + invoice required)
- [x] **5.6** Application: `RecordDipReadingCommand` (variance check >2% sets IsFraudFlagged)
- [x] **5.7** Application: All Replenishment Request Commands (Submit, Approve, Reject, Dispatch, Deliver)
- [x] **5.8** Application: All queries (Tanks, History, Replenishments, Summary, Alerts)
- [x] **5.9** Infrastructure: `EPCL_Inventory` DbContext configured fully
- [x] **5.10** Infrastructure: `RabbitMqPublisher` for dispatching domain events
- [x] **5.11** Infrastructure: `SagaConsumerHostedService` implemented for choreography
  - [x] Consume `SaleInitiatedEvent` (reserve stock, rule: reserve enough stock)
  - [x] Consume `SaleCompletedEvent` (deduct stock permanently, check thresholds)
  - [x] Consume `SaleCancelledEvent` (compensation: release reserved stock)
- [x] **5.12** Infrastructure: Run EF Code First migrations to create constraints/tables
- [x] **5.13** API: `TanksController` mapped to all endpoints with correct Auth setup
- [x] **5.14** API: `ReplenishmentController` mapped to all endpoints
- [x] **5.15** Cross-project verification — All 4 microservices + Gateway build ✅
