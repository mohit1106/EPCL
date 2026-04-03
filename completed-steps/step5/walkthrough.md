# EPCL Implementation Walkthrough

## Progress

| Step | Component | Status |
|------|-----------|--------|
| 1 | Project Scaffolding | ✅ |
| 2 | Identity Service | ✅ |
| 3 | Station Service | ✅ |
| 4 | Ocelot API Gateway | ✅ |
| 5 | Inventory Service | ✅ |
| 6 | Sales Service (+ Saga) | ⬜ Next |

---

## Step 5 — Inventory Service

### What Was Built

The Inventory Service maintains a consistent ledger of bulk fuel storage across EPCL stations. It plays a central role in the Saga pattern by reserving and dispensing stock during sales.

### Core Domain Entities

| Entity | Fields | Business logic |
|--------|--------|----------------|
| **Tank** | Capacity, CurrentStock, Reserved, Thresholds | Computes `AvailableStock`, holds Status |
| **StockLoading** | Quantity, Identity, Invoicing, Snapshot | Tracks inventory intake, verifies constraints |
| **DipReading** | Physical, System, Variance | Automated fraud detection based on variance |
| **ReplenishmentRequest**| RequestedQty, Urgency, Status | Fully tracked approval lifecycle |

### Business Rules Enforced

1. **Capacity Limit**: `CurrentStockLitres + QuantityLoadedLitres <= CapacityLitres`. Rejected (`InsufficientCapacityException`) if exceeded.
2. **Invoice Mandate**: `InvoiceNumber` is strictly required when a loading is recorded.
3. **Low Stock Alerts**: `SaleCompletedEvent` deducts stock. Stock thresholds triggers publishing of `StockLevelLowEvent`, `StockLevelCriticalEvent`, or `StockOutOfFuelEvent`.
4. **Dip Variance Fraud**: When difference between physical dip and system stock > 2%, the reading gets flagged and publishes a `DipVarianceDetectedEvent`.
5. **Saga Stock Reservation**: Stock is only reserved for `SaleInitiatedEvent` if `AvailableStock >= Quantity`. Fails over softly if underfunded.

### RabbitMQ Topology (Saga Step 2, 4, 4b)

**Publisher (`RabbitMqPublisher`)** triggers events on exchange `epcl.events` for async notifications.
**Consumer (`SagaConsumerHostedService`)** binds queue `epcl.inventory.queue` to specific topics for distributed transaction coordination (idempotent setup).

* **SaleInitiatedEvent**: Validates constraints -> adds to `ReservedLitres`.
* **SaleCompletedEvent**: Removes from `ReservedLitres` -> subtracts from `CurrentStockLitres` -> computes thresholds.
* **SaleCancelledEvent**: Deducts from `ReservedLitres` (compensation).

### Endpoints (Gateway Route Prefix: `/gateway/inventory/*`)

| Endpoint Path | Methods | Handler | Role |
|---------------|---------|---------|------|
| `stations/{stationId}/tanks` | `GET` | TanksController | Dealer/Admin |
| `tanks/{tankId}` | `GET`, `PUT` | TanksController | Admin Config |
| `tanks` | `POST` | TanksController | Admin Setup |
| `stock-loading` | `POST` | TanksController | Dealer Data Entry |
| `stock-loading/{tankId}`| `GET` | TanksController | Dealer/Admin |
| `tanks/{tankId}/dip-reading`| `PUT` | TanksController | Dealer Data Entry |
| `dip-readings/{tankId}` | `GET` | TanksController | Dealer/Admin |
| `stock-summary` | `GET` | TanksController | Global Admin View|
| `low-stock-alerts` | `GET` | TanksController | Proactive Admin View |
| `replenishment-requests`| `POST`, `GET`, `PUT` | ReplenishmentCont. | Lifecyle Mgmt |

### Build Constraints Satisfied
`Build succeeded` for all layers of `InventoryService.slnx` and all dependent solutions within EPCL's scope to guarantee clean abstraction.

---

## Next: Step 6 — Sales Service + Saga orchestration

Per the build sequence, Sales Service will:
- Establish domain items: Transactions, Pumps, FuelPrice, VoidedTransaction, Shifts.
- Execute the front half of the transaction saga.
- Handle state transitions for pumps and prices.
