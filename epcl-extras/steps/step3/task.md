# Step 1 — Project Scaffolding

## Tasks

- [x] **1.1** Create `.gitignore` at root
- [x] **1.2** Create `.env` at root with ALL required variables
- [x] **1.3** Create `docker-compose.yml` with Redis, Elasticsearch, Kibana ONLY
- [x] **1.4** Create `README.md` with setup instructions
- [x] **1.5** Create `src/` directory structure — all 9 service folders + Gateway + Frontend
- [x] **1.6** For each of the 9 microservices: create `.sln` + 4 `.csproj` files + reference chain
  - [x] IdentityService
  - [x] StationService
  - [x] InventoryService
  - [x] SalesService
  - [x] ReportingService
  - [x] NotificationService
  - [x] FraudDetectionService
  - [x] AuditService
  - [x] LoyaltyService
- [x] **1.7** For each service: create `tests/` folder with UnitTests + IntegrationTests `.csproj`
- [x] **1.8** Create Gateway project (EPCLGateway)
- [x] **1.9** Verify: `dotnet build` compiles clean for each service
- [x] **1.10** Create `landing/index.html` placeholder

---

# Step 2 — Identity Service ✅

## Tasks

- [x] **2.1** Install NuGet packages across all 4 layers
- [x] **2.2** Domain Layer — Entities (User, UserProfile, RefreshToken, OtpRequest, ProcessedEvent)
- [x] **2.3** Domain Layer — Enums (UserRole, AuthProvider, OtpPurpose)
- [x] **2.4** Domain Layer — Exceptions (Domain, NotFound, InvalidCredentials, AccountLocked, DuplicateEntity)
- [x] **2.5** Domain Layer — Interfaces (IUserRepository, IRefreshTokenRepository, IOtpRepository, IProcessedEventRepository)
- [x] **2.6** Domain Layer — Integration Events
- [x] **2.7** Application Layer — Common types (Result, PagedResult)
- [x] **2.8** Application Layer — Service interfaces (IJwtService, IEmailService, IEmailTemplateService, IGoogleAuthService, IRabbitMqPublisher)
- [x] **2.9** Application Layer — DTOs (all request/response DTOs)
- [x] **2.10** Application Layer — AutoMapper profile
- [x] **2.11** Application Layer — 11 Commands with Validators and Handlers
- [x] **2.12** Application Layer — 2 Queries with Handlers
- [x] **2.13** Application Assembly Marker
- [x] **2.14–2.23** Infrastructure — DbContext, Configs, Repos, JWT, Email, Google, RabbitMQ, DI
- [x] **2.24–2.30** API — Middleware, Controllers, Program.cs, appsettings, Dockerfile
- [x] **2.31** EF Core migration — InitialCreate
- [x] **2.32** Full solution build — **BUILD SUCCEEDED** ✅

---

# Step 3 — Station Service ✅

## Tasks

- [x] **3.1** Install NuGet packages across all 4 layers
- [x] **3.2** Domain Layer — Entities
  - [x] Station (16 columns: StationCode, StationName, DealerUserId, Address, GPS, License, OperatingHours, Is24x7)
  - [x] FuelType (Name, Description, IsActive)
  - [x] ProcessedEvent (idempotency)
- [x] **3.3** Domain Layer — Exceptions (DomainException, NotFoundException, DuplicateEntityException)
- [x] **3.4** Domain Layer — Interfaces (IStationRepository, IFuelTypeRepository, IProcessedEventRepository)
- [x] **3.5** Domain Layer — Integration Events (StationCreated, StationUpdated, StationDeactivated, AuditEvent)
- [x] **3.6** Application Layer — Common (PagedResult)
- [x] **3.7** Application Layer — DTOs (StationDto, FuelTypeDto, all request types)
- [x] **3.8** Application Layer — AutoMapper profile (TimeOnly formatting)
- [x] **3.9** Application Layer — Interfaces (IRabbitMqPublisher)
- [x] **3.10** Application Layer — 6 Commands
  - [x] CreateStation (with duplicate code/license checks)
  - [x] UpdateStation (partial update)
  - [x] DeactivateStation (soft delete)
  - [x] AssignDealer
  - [x] CreateFuelType
  - [x] UpdateFuelType
- [x] **3.11** Application Layer — 4 Queries
  - [x] GetStations (paginated with city/state/active/search filters)
  - [x] GetStationById
  - [x] GetNearbyStations (Haversine distance calculation)
  - [x] GetFuelTypes
- [x] **3.12** Application Layer — 2 Validators (CreateStation, CreateFuelType)
- [x] **3.13** Application Assembly Marker
- [x] **3.14** Infrastructure — StationsDbContext (inline entity configs + 5 fuel type seed data)
- [x] **3.15** Infrastructure — Repositories (Station with bounding-box nearby, FuelType, ProcessedEvent)
- [x] **3.16** Infrastructure — RabbitMqPublisher (epcl.events topic exchange)
- [x] **3.17** Infrastructure — DependencyInjection.cs
- [x] **3.18** API — StationsController (7 endpoints)
- [x] **3.19** API — FuelTypesController (3 endpoints)
- [x] **3.20** API — GlobalExceptionMiddleware + CorrelationIdMiddleware
- [x] **3.21** API — Program.cs (Serilog, JWT, Swagger, CORS, Health Checks)
- [x] **3.22** API — appsettings.json / appsettings.Development.json
- [x] **3.23** API — Dockerfile
- [x] **3.24** EF Core migration — InitialCreate (with seed data)
- [x] **3.25** Full solution build — **BUILD SUCCEEDED** ✅
- [x] **3.26** Cross-service build verification — Identity + Station both pass ✅
