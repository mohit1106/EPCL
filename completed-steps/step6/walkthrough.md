# Step 6 — Sales Service + Saga — Walkthrough

## Summary

The Sales Service is now fully implemented across all 4 Clean Architecture layers with **25 completed tasks**, 11 domain entities, 26 API endpoints, and a clean build. This is the largest service and the heart of the EPCL platform, handling fuel sale transactions, the choreography saga, pump/price/shift management, fleet accounts, and Razorpay wallet integration.

---

## Architecture

```
SalesService.API (ASP.NET 10 Web API — port 5167)
  └── SalesService.Infrastructure (EF Core, RabbitMQ, Razorpay SDK)
       └── SalesService.Application (MediatR CQRS, FluentValidation)
            └── SalesService.Domain (11 Entities, 7 Enums, Events, Exceptions)
```

---

## Domain Layer

### Entities (11)
| Entity | Purpose | Key Columns |
|--------|---------|-------------|
| [Transaction](file:///d:/projects/epcl-fuel-management-system/src/Services/SalesService/src/SalesService.Domain/Entities/Entities.cs) | Core fuel sale record | ReceiptNumber (UNIQUE), QuantityLitres, PricePerLitre, TotalAmount, PaymentMethod, Status |
| Pump | Physical pump at station | StationId, FuelTypeId, PumpName, NozzleCount, Status |
| FuelPrice | Price per litre per fuel type | FuelTypeId, PricePerLitre, EffectiveFrom, IsActive |
| VoidedTransaction | Void audit trail | OriginalTransactionId (UNIQUE 1:1), Reason, VoidedByUserId |
| Shift | Dealer shift tracking | OpeningStockJson, ClosingStockJson, TotalLitresSold, TotalRevenue |
| RegisteredVehicle | Customer vehicle registration | RegistrationNumber (UNIQUE), VehicleType, FuelTypePreference |
| FleetAccount | Corporate fuel accounts | CompanyName, CreditLimit, CurrentBalance |
| FleetVehicle | Vehicle-to-fleet mapping | DailyLimitLitres, MonthlyLimitAmount |
| CustomerWallet | Prepaid digital wallet | Balance (CHECK >= 0), TotalLoaded |
| WalletTransaction | Wallet top-up/debit/refund | RazorpayOrderId, RazorpayPaymentId, Status |
| ProcessedEvent | Idempotency for RabbitMQ | EventId (UNIQUE) |

### Enums (7)
TransactionStatus (6 values), FraudCheckStatus (3), PumpStatus (4), PaymentMethod (5 inc. Wallet), WalletTransactionType (3), WalletTransactionStatus (4), VehicleType (4)

---

## Application Layer

### Business Rules (RecordFuelSale)
1. **Pump active check** — Reject if pump is not `Active` → `PumpNotActiveException`
2. **Price snapshot** — Fetch current active FuelPrice for the fuel type
3. **Total calculation** — `Math.Round(Qty × Price, 2, MidpointRounding.AwayFromZero)`
4. **Vehicle validation** — Source-generated regex `^[A-Z]{2}[0-9]{2}[A-Z]{2}[0-9]{4}$`
5. **Payment reference** — Required for non-Cash payments
6. **Quantity precision** — Max 3 decimal places (Weights & Measures Act)
7. **Receipt number** — `yyyyMMdd-{StationCode}-{4-digit-seq}` (daily counter per station)

### Commands (13)
RecordFuelSale, VoidTransaction, RegisterPump, UpdatePumpStatus, SetFuelPrice, StartShift, EndShift, RegisterVehicle, CreateFleetAccount, AddVehicleToFleet, RemoveFleetVehicle, CreateWalletOrder, VerifyWalletPayment

### Queries (9)
GetTransactions (paginated), GetTransactionById, GetPumpsByStation, GetActiveFuelPrices, GetActiveShift, GetCustomerVehicles, GetFleetAccounts, GetWalletBalance, GetWalletHistory

---

## Saga Flow

```
┌─────────────────────────────────────────────────────────────────────
│ 1. Dealer records sale → RecordFuelSale → Status=Initiated
│    └─ Publish: SaleInitiatedEvent (routing key: sales.initiated)
│
│ 2. Inventory Service consumes SaleInitiatedEvent
│    ├─ Success → Reserve stock → Publish: StockReservedEvent
│    └─ Failure → Publish: StockReservationFailedEvent
│
│ 3a. Sales consumes StockReservedEvent
│    └─ Status → Completed → Publish: SaleCompletedEvent
│
│ 3b. Sales consumes StockReservationFailedEvent
│    └─ Status → Voided → Publish: SaleCancelledEvent
│
│ 4. Inventory consumes SaleCompletedEvent
│    └─ Deduct stock permanently, check thresholds
│
│ 4b. Inventory consumes SaleCancelledEvent
│    └─ Release reserved stock (compensation)
└─────────────────────────────────────────────────────────────────────
```

---

## API Endpoints (26 across 6 controllers)

| Controller | Endpoint Count | Key Routes |
|-----------|---------------|------------|
| TransactionsController | 8 | POST /transactions, GET /transactions/my, POST /{id}/void |
| PumpsController | 3 | GET /station/{id}, POST /, PUT /{id}/status |
| FuelPricesController | 2 | GET / (public, cached), POST / (admin) |
| ShiftsController | 3 | POST /start, POST /end, GET /current |
| PaymentsController | 4 | POST /wallet/create-order, POST /wallet/verify, GET /balance, GET /history |
| FleetController | 4 | GET /accounts, POST /accounts, POST/DELETE vehicles |
| VehiclesController | 2 | GET /, POST / |

---

## Deviations from Plan

| Area | AGENT_START_HERE Spec | Actual | Reason |
|------|----------------------|--------|--------|
| Razorpay SDK | `Razorpay v2.*` | `Razorpay v3.0.0` | v2 not available, v3 installed; signature verification done with manual HMAC-SHA256 |
| Controller naming | Not specified | 6 controllers instead of 5 | Split PaymentsController + FleetController + VehiclesController for cleaner route structure |
| Webhook endpoint | Spec says `GET /razorpay/webhook` | Not implemented yet | Webhooks require public URL; deferred to deployment phase |
| `database update` | Step says "Run migration → EPCL_Sales created" | Only `migrations add` executed | Same as prior steps — no SQL Server connection during build |

---

## Build Verification

```
✅ SalesService.Domain        → 0 errors
✅ SalesService.Application   → 0 errors
✅ SalesService.Infrastructure → 0 errors
✅ SalesService.API            → 0 errors
✅ InitialCreate migration     → Generated
✅ Cross-project build          → Identity + Station + Inventory + Sales + Gateway all pass
```

---

## NuGet Packages

| Layer | Packages |
|-------|----------|
| Application | MediatR 12.4.1, FluentValidation 11.11.0, AutoMapper 13.0.1 |
| Infrastructure | EF Core SqlServer 9.0.4, RabbitMQ.Client 7.1.2, Razorpay 3.0.0 |
| API | Serilog.AspNetCore 9.0.0, Swashbuckle 7.3.1, JwtBearer 10.0.0, FluentValidation.AspNetCore 11.3.0 |
