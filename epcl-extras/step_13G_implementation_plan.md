# Step 13G — Full Backend Integration

## Goal

Wire every Angular page (32 pages across 6 modules) to the live backend services through the Ocelot Gateway at `http://localhost:5000`. After completion, zero hardcoded/mock data should remain — every page shows real data from real APIs.

## Current State

### What Exists (Frontend — 32 static pages)
- **Auth Module (3 pages)**: Login has NgRx `login` action dispatching to `AuthApiService.login()` — already partially wired. Register and ForgotPassword have forms but no API binding.
- **Customer Module (8 pages)**: Dashboard, Wallet, Prices, Transactions, Stations, Loyalty, Vehicles, Referral — all showing hardcoded mock data.
- **Dealer Module (8 pages)**: Dashboard, New Sale, Confirmation, Transactions, Inventory, Shift, Replenishment, Reports — all static.
- **Admin Module (8 pages)**: Command Center, Users, Stations, Prices, Fraud, Audit, Reports, System Health — all static.
- **Profile Module (2 pages)**: Settings, Notifications — static.
- **Support Module (1 page)**: Help Center — static (stays static per plan).
- **Error Pages (2)**: 404, 403 — no API needed (stays static).

### What Exists (Infrastructure)
- **Core Services**: `AuthApiService`, `SignalRService`, `JwtInterceptor`, `ErrorInterceptor` — all correctly built.
- **NgRx Store**: Auth state with actions, effects, reducer, selectors — fully wired for login/register/logout/restore.
- **Proxy Config**: Missing — needs creation.
- **Environment**: `apiUrl: 'http://localhost:5000'` — needs changing to `''` (empty) when proxy is added.
- **Backend**: All 9 services + Gateway exist with full API endpoints (Steps 1-12 complete).

---

## User Review Required

> [!IMPORTANT]
> **Backend services must be running** before any frontend integration can be tested.
> This plan creates all the API service files, wires all components, and sets up the proxy config.
> Testing each module requires the corresponding backend service to be running on its expected port.

> [!WARNING]
> **Environment change**: `environment.apiUrl` will change from `'http://localhost:5000'` to `''` (empty string) to work with Angular's proxy config. This is the approach specified in STEP_13G.md.

---

## Proposed Changes

The work is organized into 7 phases, matching the module order specified in STEP_13G.md.

---

### Phase 0 — Infrastructure Setup

#### [NEW] [proxy.conf.json](file:///d:/projects/epcl-fuel-management-system/src/Services/Frontend/epcl-angular/src/proxy.conf.json)
- Proxy `/gateway` → `http://localhost:5000`
- Proxy `/hubs` → `http://localhost:5000` (WebSocket)

#### [MODIFY] [angular.json](file:///d:/projects/epcl-fuel-management-system/src/Services/Frontend/epcl-angular/angular.json)
- Add `"proxyConfig": "src/proxy.conf.json"` under `serve > options`

#### [MODIFY] [environment.ts](file:///d:/projects/epcl-fuel-management-system/src/Services/Frontend/epcl-angular/src/environments/environment.ts)
- `apiUrl: ''` (empty — proxy handles routing)
- `signalrUrl: ''` (empty — proxy handles ws)

---

### Phase 1 — API Services Layer (NEW files — one per backend domain)

Create dedicated Angular service files for each backend domain. All HTTP calls go through these services — never directly from components.

#### [NEW] Core API Services
| File | Endpoints Covered |
|------|-------------------|
| `stations-api.service.ts` | GET stations, nearby, by ID; POST/PUT station; GET fuel-types |
| `inventory-api.service.ts` | GET tanks, stock history; POST dip-reading, stock-loading, replenishment |
| `sales-api.service.ts` | GET/POST transactions, fuel-prices, pumps, shifts, vehicles; price history |
| `payments-api.service.ts` | GET wallet balance/history; POST create-order, verify |
| `fraud-api.service.ts` | GET alerts; PUT dismiss/investigate/escalate; POST bulk-dismiss |
| `audit-api.service.ts` | GET logs (paginated); POST export |
| `reports-api.service.ts` | GET KPIs, sales-summary; POST export PDF/Excel; GET/POST schedules |
| `loyalty-api.service.ts` | GET balance, history, referral code, leaderboard; POST redeem |
| `notifications-api.service.ts` | GET in-app prefs, price-alerts; PUT prefs; POST/DELETE price-alerts |
| `users-api.service.ts` | GET users (paginated); PUT role, lock; GET /users/me; PUT /users/me |
| `health-api.service.ts` | GET /health for each service |

---

### Phase 2 — Auth Module Integration (3 pages)

#### [MODIFY] [login.component.ts](file:///d:/projects/epcl-fuel-management-system/src/Services/Frontend/epcl-angular/src/app/features/auth/pages/login/login.component.ts)
- Wire form submit → NgRx `login` action (already done)
- Wire Google OAuth via `@abacritt/angularx-social-login` → dispatch `googleLogin`
- Add OTP login flow: phone input → `POST /gateway/auth/send-otp` → 6-box OTP → `POST /gateway/auth/verify-otp`
- Add lockout countdown timer on 423 response
- Remove `console.log` from Google login handler

#### [MODIFY] Register + ForgotPassword components
- Wire register form → NgRx `register` action → `POST /gateway/auth/register`
- Wire forgot password 3-step flow to API endpoints

---

### Phase 3 — Customer Module Integration (8 pages)

Wire each page to its corresponding API endpoints, replace all hardcoded data with `Observable` subscriptions.

| Page | Key API Calls |
|------|--------------|
| Dashboard | `getFuelPrices()`, `getRecentTransactions(limit:5)`, `getLoyaltyBalance()`, `getNearbyStations()` |
| Fuel Prices | `GET /gateway/sales/fuel-prices`, `GET .../history`, `POST .../price-alerts` |
| Transactions | `GET /gateway/sales/transactions/my` + pagination/filters |
| Station Locator | `GET /gateway/stations/nearby` + Google Maps integration |
| Wallet | `GET .../wallet/balance`, `POST .../create-order` → Razorpay → `POST .../verify` |
| Loyalty | `GET /gateway/loyalty/balance`, `GET .../history` |
| Referral | `GET /gateway/loyalty/referral/my-code`, `GET .../leaderboard` |
| Vehicles | `GET/POST/DELETE /gateway/sales/vehicles/my` |

---

### Phase 4 — Dealer Module Integration (8 pages)

| Page | Key API Calls |
|------|--------------|
| Dashboard | `GET /gateway/reports/kpi/dealer/{stationId}`, tanks, pumps, daily-summary + SignalR |
| New Sale | Pump selection, quantity calc, payment method, `POST /gateway/sales/transactions` |
| Confirmation | `GET /gateway/sales/transactions/:id`, QR code gen |
| Inventory | `GET tanks`, `POST dip-reading`, `POST stock-loading`, history chart |
| Replenishment | `POST replenishment-requests`, status timeline |
| Shift | `GET active shift`, `POST start/end`, live totals polling |
| Reports | `GET KPIs`, `POST export PDF/Excel`, polling for status |
| Transactions | `GET /gateway/sales/transactions/station/{stationId}` + filters |

---

### Phase 5 — Admin Module Integration (8 pages)

| Page | Key API Calls |
|------|--------------|
| Dashboard | KPIs, fraud alerts, stations map, sales summary + SignalR |
| Price Control | `GET/POST fuel-prices`, history, confirmation dialog |
| Fraud Intelligence | `GET alerts` + filters, detail slide-over, dismiss/investigate/escalate |
| Audit Logs | `GET logs` + pagination/filters, JSON diff viewer, export |
| Report Engine | Dynamic report builder, preview, PDF/Excel export, schedules |
| User Management | `GET users`, role change, lock/unlock, create user modal |
| Station Management | `GET stations`, map toggle, add/edit form, detail slide-over |
| System Health | Poll `/health` per service every 30s, queue depths |

---

### Phase 6 — Profile & Shared Integration

| Page | Key API Calls |
|------|--------------|
| Profile Settings | `GET/PUT /users/me`, `PUT /auth/change-password` |
| Notification Settings | `GET/PUT notification prefs`, `GET/POST/DELETE price-alerts` |
| Help Center | Static — no API changes needed |

---

### Phase 7 — SignalR + PWA

- Wire SignalR connections after login based on role (already in `auth.effects.ts`)
- Update `signalrUrl` to use proxy
- Add `@angular/pwa` with caching for fuel prices and static assets
- Remove all `console.log` statements

---

## Open Questions

> [!IMPORTANT]
> **Q1**: Are all 10 backend services currently running? If not, should I focus on creating the service layer + component wiring first (which can be verified once services are up)?

> [!IMPORTANT]
> **Q2**: The Google OAuth requires a valid `GOOGLE_CLIENT_ID` in environment.ts. Do you have one configured, or should I stub it with a placeholder and skip Google OAuth testing for now?

> [!IMPORTANT]
> **Q3**: The Razorpay integration requires a test key (`rzp_test_*`). Current env has `rzp_test_xxxxxxxxxxxxxxxx`. Do you have a real test key to use?

---

## Verification Plan

### Automated Tests
- `ng build --configuration production` — must produce 0 errors
- `ng serve` with proxy — all API calls route through proxy to gateway

### Manual Verification (after backend is running)
Per STEP_13G.md Final Verification section:
1. Full auth flow (register → login → session restore → logout)
2. Google OAuth end-to-end
3. New Sale Saga (pump selection → transaction → DB verification)
4. Razorpay wallet top-up
5. Fraud alert real-time via SignalR
6. Loyalty points calculation
7. Low stock alert on dealer dashboard

### Code Quality
- Zero `console.log` in production code
- Zero `any` types without justification
- All HTTP calls go through dedicated service files
