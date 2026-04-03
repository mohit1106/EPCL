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
