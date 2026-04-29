# EPCL Implementation Walkthrough

## Completed Steps

| Step | Service | Status |
|------|---------|--------|
| Step 1 | Project Scaffolding | ✅ Complete |
| Step 2 | Identity Service | ✅ Complete |
| Step 3 | Station Service | ✅ Complete |

---

## Step 3 — Station Service

### What Was Built

The Station Service manages fuel station CRUD, fuel type administration, and nearby station locator functionality. It follows the identical Clean Architecture pattern established by the Identity Service.

### Architecture

```
StationService.API (ASP.NET 10 Web API — port varies)
  └── StationService.Infrastructure (EF Core, RabbitMQ)
       └── StationService.Application (MediatR CQRS, FluentValidation, AutoMapper)
            └── StationService.Domain (Entities, Exceptions, Interfaces, Events)
```

### Domain Layer

| Entity | Columns | Key Features |
|--------|---------|-------------|
| [Station](file:///d:/projects/epcl-fuel-management-system/src/Services/StationService/src/StationService.Domain/Entities/Station.cs) | 16 | GPS coords (DECIMAL 9,6), operating hours (TimeOnly), MoPNG license |
| [FuelType](file:///d:/projects/epcl-fuel-management-system/src/Services/StationService/src/StationService.Domain/Entities/FuelType.cs) | 4 | Petrol, Diesel, CNG, PremiumPetrol, PremiumDiesel |
| [ProcessedEvent](file:///d:/projects/epcl-fuel-management-system/src/Services/StationService/src/StationService.Domain/Entities/ProcessedEvent.cs) | 4 | RabbitMQ idempotency |

### Application Layer — Commands (6)

| Command | Key Behavior |
|---------|-------------|
| CreateStation | Duplicate check on StationCode + LicenseNumber, publish `StationCreatedEvent` |
| UpdateStation | Partial update (nullable fields), publish `StationUpdatedEvent` |
| DeactivateStation | Soft delete (IsActive=false), publish `StationDeactivatedEvent` |
| AssignDealer | Change DealerUserId, publish event |
| CreateFuelType | Duplicate name check |
| UpdateFuelType | Partial update with active toggle |

### Application Layer — Queries (4)

| Query | Key Behavior |
|-------|-------------|
| GetStations | Pagination + filters: city, state, isActive, search term |
| GetStationById | Single station with profile |
| GetNearbyStations | **Haversine formula** distance calc, bounding-box pre-filter for perf |
| GetFuelTypes | Optional isActive filter |

### Infrastructure Layer

| Component | Details |
|-----------|---------|
| [StationsDbContext](file:///d:/projects/epcl-fuel-management-system/src/Services/StationService/src/StationService.Infrastructure/Persistence/StationsDbContext.cs) | Inline configs, composite City+State index, **5 seeded fuel types** |
| [Repositories](file:///d:/projects/epcl-fuel-management-system/src/Services/StationService/src/StationService.Infrastructure/Repositories/Repositories.cs) | Station (bounding-box nearby), FuelType, ProcessedEvent |
| [RabbitMqPublisher](file:///d:/projects/epcl-fuel-management-system/src/Services/StationService/src/StationService.Infrastructure/Messaging/RabbitMqPublisher.cs) | Same pattern as Identity — epcl.events topic exchange |

### API Layer

| Controller | Endpoints | Auth |
|-----------|-----------|------|
| [StationsController](file:///d:/projects/epcl-fuel-management-system/src/Services/StationService/src/StationService.API/Controllers/StationsController.cs) | 7 (list, get, create, update, delete, nearby, assign-dealer) | JWT + Admin roles |
| [FuelTypesController](file:///d:/projects/epcl-fuel-management-system/src/Services/StationService/src/StationService.API/Controllers/FuelTypesController.cs) | 3 (list, create, update) | Public list, Admin write |

### API Endpoints (10 total)

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| GET | `/api/stations` | JWT | Paginated with city/state/active/search |
| GET | `/api/stations/{id}` | JWT | Single station detail |
| POST | `/api/stations` | Admin | Create station with GPS + license |
| PUT | `/api/stations/{id}` | Admin | Partial update |
| DELETE | `/api/stations/{id}` | Admin | Soft delete |
| GET | `/api/stations/nearby` | Public | `?lat&lng&radiusKm&fuelTypeId` |
| PUT | `/api/stations/{id}/dealer` | Admin | Assign dealer |
| GET | `/api/stations/fuel-types` | Public | All fuel types |
| POST | `/api/stations/fuel-types` | Admin | Create fuel type |
| PUT | `/api/stations/fuel-types/{id}` | Admin | Update fuel type |

### Database: EPCL_Stations

**Tables:** Stations, FuelTypes, ProcessedEvents

**Indexes:**
- `IX_Stations_StationCode` (unique)
- `IX_Stations_LicenseNumber` (unique)
- `IX_Stations_DealerUserId`
- `IX_Stations_City_State` (composite)
- `IX_FuelTypes_Name` (unique)
- `IX_ProcessedEvents_EventId` (unique)

**Seed Data:** 5 fuel types (Petrol, Diesel, CNG, PremiumPetrol, PremiumDiesel)

### Build Verification

```
✅ StationService.Domain          → Build succeeded
✅ StationService.Application     → Build succeeded
✅ StationService.Infrastructure  → Build succeeded (+ migration generated)
✅ StationService.API             → Build succeeded
✅ Cross-service check            → Identity + Station both pass
```

---

## Next: Step 4 — Ocelot API Gateway

Per the build sequence, the next step is:
- Install Ocelot 23.x on EPCLGateway project
- Create `ocelot.json` with routes for all 9 services
- JWT verification on secured routes
- Rate limiting (100/min global, 10/min on /auth/)
- Circuit breaker (Polly)
- Response caching on fuel-types and fuel-prices
- Correlation ID injection
- Timeouts per service
