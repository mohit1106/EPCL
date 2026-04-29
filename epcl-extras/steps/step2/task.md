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

# Step 2 — Identity Service

## Tasks

- [x] **2.1** Install NuGet packages across all 4 layers
- [x] **2.2** Domain Layer — Entities
  - [x] User (with Google OAuth fields)
  - [x] UserProfile
  - [x] RefreshToken
  - [x] OtpRequest
  - [x] ProcessedEvent
- [x] **2.3** Domain Layer — Enums (UserRole, AuthProvider, OtpPurpose)
- [x] **2.4** Domain Layer — Exceptions (Domain, NotFound, InvalidCredentials, AccountLocked, DuplicateEntity)
- [x] **2.5** Domain Layer — Interfaces (IUserRepository, IRefreshTokenRepository, IOtpRepository, IProcessedEventRepository)
- [x] **2.6** Domain Layer — Integration Events
- [x] **2.7** Application Layer — Common types (Result, PagedResult)
- [x] **2.8** Application Layer — Service interfaces (IJwtService, IEmailService, IEmailTemplateService, IGoogleAuthService, IRabbitMqPublisher)
- [x] **2.9** Application Layer — DTOs (all request/response DTOs)
- [x] **2.10** Application Layer — AutoMapper profile
- [x] **2.11** Application Layer — Commands
  - [x] RegisterUser (Command + Validator + Handler)
  - [x] LoginUser (Command + Validator + Handler)
  - [x] GoogleLogin (Command + Handler)
  - [x] RefreshToken (Command + Handler)
  - [x] LogoutUser (Command + Handler)
  - [x] ForgotPassword (Command + Handler)
  - [x] ResetPassword (Command + Validator + Handler)
  - [x] ChangePassword (Command + Handler)
  - [x] VerifyOtp (Command + Handler)
  - [x] UpdateUserRole (Command + Handler)
  - [x] LockUser (Command + Handler)
- [x] **2.12** Application Layer — Queries
  - [x] GetCurrentUser (Query + Handler)
  - [x] GetAllUsers (Query + Handler)
- [x] **2.13** Application Assembly Marker
- [x] **2.14** Infrastructure — IdentityDbContext
- [x] **2.15** Infrastructure — Entity Configurations (User, UserProfile, RefreshToken, OtpRequest, ProcessedEvent)
- [x] **2.16** Infrastructure — Repositories (User, RefreshToken, Otp, ProcessedEvent)
- [x] **2.17** Infrastructure — JwtService (access + refresh tokens)
- [x] **2.18** Infrastructure — GmailSmtpEmailService (MailKit)
- [x] **2.19** Infrastructure — EmailTemplateService (embedded resources)
- [x] **2.20** Infrastructure — Email Templates (OTP, Welcome)
- [x] **2.21** Infrastructure — GoogleAuthService (Google.Apis.Auth)
- [x] **2.22** Infrastructure — RabbitMqPublisher (topic exchange)
- [x] **2.23** Infrastructure — DependencyInjection registration
- [x] **2.24** API — GlobalExceptionMiddleware
- [x] **2.25** API — CorrelationIdMiddleware
- [x] **2.26** API — AuthController (9 endpoints)
- [x] **2.27** API — UsersController (6 endpoints)
- [x] **2.28** API — Program.cs (Serilog, JWT, Swagger, CORS, Health Checks)
- [x] **2.29** API — appsettings.json / appsettings.Development.json
- [x] **2.30** API — Dockerfile
- [x] **2.31** EF Core migration — InitialCreate
- [x] **2.32** Full solution build verification — **BUILD SUCCEEDED** ✅
