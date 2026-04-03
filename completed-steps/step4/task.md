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

## Tasks

- [x] **4.1** EPCLGateway project already scaffolded (port 5000)
- [x] **4.2** Install Ocelot + Ocelot.Cache.CacheManager (latest .NET 10 compatible)
- [x] **4.3** Create `ocelot.json` — 17 routes covering all 9 services + fleet
  - [x] Auth (Identity:5217) — `/gateway/auth/*` → `/api/auth/*`
  - [x] Users (Identity:5217) — `/gateway/users/*` → `/api/users/*`
  - [x] Stations (Station:5143) — `/gateway/stations/*` → `/api/stations/*`
  - [x] Fuel Types (Station:5143) — `/gateway/stations/fuel-types` → `/api/stations/fuel-types`
  - [x] Nearby Stations (Station:5143) — `/gateway/stations/nearby` (public)
  - [x] Inventory (Inventory:5134) — `/gateway/inventory/*`
  - [x] Sales (Sales:5167) — `/gateway/sales/*`
  - [x] Fuel Prices (Sales:5167) — `/gateway/sales/fuel-prices` (cached 300s)
  - [x] Reports (Reporting:5062) — `/gateway/reports/*`
  - [x] Fraud (FraudDetection:5237) — `/gateway/fraud/*`
  - [x] Notifications (Notification:5037) — `/gateway/notifications/*`
  - [x] Audit (Audit:5268) — `/gateway/audit/*`
  - [x] Loyalty (Loyalty:5192) — `/gateway/loyalty/*`
  - [x] Fleet (Sales:5167) — `/gateway/fleet/*`
- [x] **4.4** JWT verification — `AuthenticationProviderKey: "Bearer"` on all secured routes
- [x] **4.5** Rate limiting — 10 req/min on `/auth/`, 100 req/min on all other routes
- [x] **4.6** Circuit breaker — 5 failures → 30s break (via QoSOptions on each route)
- [x] **4.7** Response caching — 300s TTL on fuel-types and fuel-prices (CacheManager)
- [x] **4.8** Correlation ID — `X-Correlation-ID` injected via middleware + RequestIdKey
- [x] **4.9** Per-service timeouts — Sales 10s, Reports 30s, others 15s
- [x] **4.10** Dockerfile created
- [x] **4.11** Serilog → Console + File + Elasticsearch (`epcl-gateway-*` index)
- [x] **4.12** CORS for localhost:4200
- [x] **4.13** Health check on `/health`
- [x] **4.14** Root info endpoint (`/`) listing all routes
- [x] **4.15** Full build — **BUILD SUCCEEDED** ✅
- [x] **4.16** Cross-project verification — Gateway + Identity + Station all pass ✅
