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

- [x] All 15 items complete (entities, enums, saga consumers, API, migration, build)

---

# Step 6 — Sales Service + Saga ✅

## Tasks

- [x] **6.1** Domain: All 11 entities created
  - [x] Transaction (17 columns: ReceiptNumber, QuantityLitres, PricePerLitre, TotalAmount, PaymentMethod, Status, FraudCheckStatus)
  - [x] Pump (StationId, FuelTypeId, PumpName, NozzleCount, Status with maintenance tracking)
  - [x] FuelPrice (PricePerLitre, EffectiveFrom, IsActive, SetByUserId)
  - [x] VoidedTransaction (OriginalTransactionId, Reason, VoidedByUserId — 1:1 with Transaction)
  - [x] Shift (OpeningStockJson, ClosingStockJson, TotalLitresSold, TotalRevenue, DiscrepancyFlagged)
  - [x] RegisteredVehicle (RegistrationNumber UNIQUE, VehicleType enum, FuelTypePreference)
  - [x] FleetAccount (CompanyName, CreditLimit, CurrentBalance)
  - [x] FleetVehicle (DailyLimitLitres, MonthlyLimitAmount — FK to FleetAccount + RegisteredVehicle)
  - [x] CustomerWallet (Balance CHECK >= 0, TotalLoaded)
  - [x] WalletTransaction (Type, Amount, RazorpayOrderId, RazorpayPaymentId, Status)
  - [x] ProcessedEvent (idempotency)
- [x] **6.2** Domain: Enums
  - [x] TransactionStatus (6): Initiated, StockReserved, Completed, Voided, FraudFlagged, FraudCleared
  - [x] FraudCheckStatus (3): Pending, Cleared, Flagged
  - [x] PumpStatus (4): Active, UnderMaintenance, OutOfService, Paused
  - [x] PaymentMethod (5): Cash, UPI, Card, FleetCard, Wallet
  - [x] WalletTransactionType (3): TopUp, Debit, Refund
  - [x] WalletTransactionStatus (4): Pending, Captured, Failed, Refunded
  - [x] VehicleType (4): TwoWheeler, FourWheeler, Commercial, CNG
- [x] **6.3** Application: RecordFuelSaleCommand with ALL 7 business rules
  - [x] Pump.Status must be Active → PumpNotActiveException
  - [x] Snapshot PricePerLitre from active FuelPrice
  - [x] TotalAmount = Math.Round(Qty * Price, 2, MidpointRounding.AwayFromZero)
  - [x] VehicleNumber regex: ^[A-Z]{2}[0-9]{2}[A-Z]{2}[0-9]{4}$ (source-generated)
  - [x] PaymentReferenceId required when PaymentMethod != Cash
  - [x] Quantity max 3 decimal places
  - [x] Auto-generate ReceiptNumber: yyyyMMdd-{StationCode}-{4-digit-seq}
  - [x] Create Transaction Status=Initiated, publish SaleInitiatedEvent (Saga Step 1)
- [x] **6.4** Application: Wallet commands — CreateWalletOrder, VerifyWalletPayment
- [x] **6.5** Application: All other commands
  - [x] VoidTransaction, RegisterPump, UpdatePumpStatus
  - [x] SetFuelPrice (deactivate old → activate new, publish FuelPriceUpdatedEvent)
  - [x] StartShift, EndShift
  - [x] RegisterVehicle, CreateFleetAccount, AddVehicleToFleet, RemoveFleetVehicle
- [x] **6.6** Application: All queries
  - [x] GetTransactions (paginated + station/dealer/customer/vehicle/status filters)
  - [x] GetTransactionById
  - [x] GetPumpsByStation
  - [x] GetActiveFuelPrices
  - [x] GetActiveShift
  - [x] GetCustomerVehicles
  - [x] GetFleetAccounts
  - [x] GetWalletBalance, GetWalletHistory
- [x] **6.7** Infrastructure: EPCL_Sales DbContext with all 11 entity configurations
- [x] **6.8** Infrastructure: RazorpayService (order creation, HMAC verification, capture, refund)
- [x] **6.9** Infrastructure: SagaConsumerHostedService for Saga Steps 3a + 3b
  - [x] StockReservedEvent → Complete transaction, publish SaleCompletedEvent
  - [x] StockReservationFailedEvent → Void transaction, publish SaleCancelledEvent
- [x] **6.10** Infrastructure: RabbitMqPublisher (epcl.events topic exchange)
- [x] **6.11** Infrastructure: 10 repository implementations
- [x] **6.12** Infrastructure: DependencyInjection registration
- [x] **6.13** EF Core migration — InitialCreate generated ✅
- [x] **6.14** API: TransactionsController (8 endpoints)
- [x] **6.15** API: PumpsController (3 endpoints)
- [x] **6.16** API: FuelPricesController (2 endpoints)
- [x] **6.17** API: ShiftsController (3 endpoints)
- [x] **6.18** API: PaymentsController (4 Razorpay/wallet endpoints)
- [x] **6.19** API: FleetController (4 fleet account endpoints)
- [x] **6.20** API: VehiclesController (2 customer vehicle endpoints)
- [x] **6.21** API: GlobalExceptionMiddleware + CorrelationIdMiddleware
- [x] **6.22** API: Program.cs (Serilog, JWT, Swagger, CORS, Health Checks, .env loading)
- [x] **6.23** API: appsettings.json + appsettings.Development.json
- [x] **6.24** API: Dockerfile
- [x] **6.25** Cross-project verification — All 5 microservices + Gateway build ✅
