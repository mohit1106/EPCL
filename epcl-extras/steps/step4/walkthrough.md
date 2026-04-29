# EPCL Implementation Walkthrough

## Progress

| Step | Component | Status |
|------|-----------|--------|
| 1 | Project Scaffolding | ✅ |
| 2 | Identity Service | ✅ |
| 3 | Station Service | ✅ |
| 4 | Ocelot API Gateway | ✅ |
| 5 | Inventory Service | ⬜ Next |

---

## Step 4 — Ocelot API Gateway

### What Was Built

The EPCL API Gateway is a single entry point that routes all client requests (`/gateway/*`) to the appropriate downstream microservice (`/api/*`), with JWT verification, rate limiting, circuit breaking, response caching, and correlation ID propagation.

### Port Mapping

| Service | Port | Gateway Route Prefix |
|---------|------|---------------------|
| EPCLGateway | 5000 | `/` (entry point) |
| Identity | 5217 | `/gateway/auth/*`, `/gateway/users/*` |
| Station | 5143 | `/gateway/stations/*` |
| Inventory | 5134 | `/gateway/inventory/*` |
| Sales | 5167 | `/gateway/sales/*`, `/gateway/fleet/*` |
| Reporting | 5062 | `/gateway/reports/*` |
| Fraud | 5237 | `/gateway/fraud/*` |
| Notification | 5037 | `/gateway/notifications/*` |
| Audit | 5268 | `/gateway/audit/*` |
| Loyalty | 5192 | `/gateway/loyalty/*` |

### Key Features

| Feature | Configuration |
|---------|--------------|
| **JWT Verification** | `AuthenticationProviderKey: "Bearer"` on all secured routes — same signing key as Identity Service |
| **Rate Limiting** | Auth: 10 req/min, All others: 100 req/min. Returns `429` with message |
| **Circuit Breaker** | 5 failures → 30s break → auto-reset (Polly via QoSOptions) |
| **Response Caching** | 300s TTL on `GET /gateway/stations/fuel-types` and `GET /gateway/sales/fuel-prices` (CacheManager) |
| **Correlation ID** | `X-Correlation-ID` auto-generated if missing, forwarded to all downstream services, pushed to Serilog |
| **Per-Service Timeouts** | Sales: 10s, Reports: 30s, All others: 15s |

### Files Created/Modified

| File | Purpose |
|------|---------|
| [ocelot.json](file:///d:/projects/epcl-fuel-management-system/src/Gateway/EPCLGateway/ocelot.json) | 17 route definitions covering all 9 services + fleet |
| [Program.cs](file:///d:/projects/epcl-fuel-management-system/src/Gateway/EPCLGateway/Program.cs) | Ocelot pipeline + JWT + Serilog + CORS + health + info endpoint |
| [EPCLGateway.csproj](file:///d:/projects/epcl-fuel-management-system/src/Gateway/EPCLGateway/EPCLGateway.csproj) | Ocelot, CacheManager, JWT, Serilog packages |
| [Dockerfile](file:///d:/projects/epcl-fuel-management-system/src/Gateway/EPCLGateway/Dockerfile) | Multi-stage Docker build |
| [appsettings.json](file:///d:/projects/epcl-fuel-management-system/src/Gateway/EPCLGateway/appsettings.json) | Base logging config |
| [appsettings.Development.json](file:///d:/projects/epcl-fuel-management-system/src/Gateway/EPCLGateway/appsettings.Development.json) | Dev logging config |

### Route Design

Routes use **specific-first ordering** to prevent catch-all routes from shadowing more specific ones:

1. `/gateway/stations/nearby` (public, no auth) — before stations catch-all
2. `/gateway/stations/fuel-types` (public, cached 300s) — before stations catch-all
3. `/gateway/stations/fuel-types/{id}` (admin auth) — before stations catch-all
4. `/gateway/stations/{everything}` (jwt auth) — catch-all for remaining stations
5. `/gateway/sales/fuel-prices` (public, cached 300s) — before sales catch-all
6. `/gateway/sales/{everything}` (jwt auth, 10s timeout) — catch-all for remaining sales

### Build Verification

```
✅ EPCLGateway                → Build succeeded
✅ IdentityService (all 4)   → Build succeeded
✅ StationService (all 4)    → Build succeeded
```

---

## Next: Step 5 — Inventory Service

Per the build sequence:
- Domain: Tank, StockLoading, DipReading, ReplenishmentRequest entities
- Business rules: Capacity enforcement, dip variance >2% → fraud flag
- Saga consumers: SaleInitiatedEvent, SaleCompletedEvent, SaleCancelledEvent
- API: TanksController, StockLoadingController, DipReadingController, ReplenishmentController
