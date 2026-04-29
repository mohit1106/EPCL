# Step 13 — Angular Frontend: Analysis & Approach

## Current State Summary

**Steps 1–12 (Backend) are fully complete and verified.**

| Component | Status | Details |
|-----------|--------|---------|
| 9 Microservices | ✅ Built | Identity, Station, Inventory, Sales, Fraud, Notification, Audit, Reporting, Loyalty |
| API Gateway | ✅ Built | Ocelot with 16 routes, JWT auth, rate limiting |
| SignalR Hubs | ✅ Built | AdminHub + DealerHub in ReportingService |
| Databases | ✅ Migrated | 9 EPCL_* databases created and seeded |
| Docker | ✅ Running | Redis, Elasticsearch, Kibana |
| Build | ✅ 0 errors | All 10 projects compile clean |

**Frontend directory (`src/Services/Frontend/`) is currently empty — fresh start.**

---

## What the Documents Say

### AGENT_START_HERE.md — Step 13 Specifications
- **42 stitch design folders** exist in `stitch-frontend-ui/` with `screen.png` + `code.html` per page
- Angular 17, SCSS, NgRx, @angular/material, Chart.js, @microsoft/signalr, Tailwind
- **42 pages** across 5 modules: Auth (3), Customer (8), Dealer (8), Admin (10), Shared (5) + App Shell
- `petrocore_obsidian/` is the **design system reference** (not a page)
- `_prototyped` folders are primary references over plain versions

### EPCL_ADDONS_PLAN.md — Features Built Into Backend
Features 1–4 (Gmail SMTP, HTML templates, Google OAuth, Razorpay) are already built into the backend services. The frontend needs to integrate with them:
- Google OAuth button on login page
- Razorpay checkout.js integration in wallet page
- Beautiful email templates are backend-only (already done)

### EPCL_EXTRA_FEATURES_PLAN.md — 5 New Features (Post-Step 12)
These are **bonus features** to add AFTER frontend. Explicitly says "Add after Step 12 is verified. Implement alongside or after Step 13."
1. AI Analytics Chatbot (Gemini) — new AIAnalyticsService on port 5010
2. Predictive Stock Intelligence — extend ReportingService
3. Document Vault — new DocumentService on port 5011
4. Smart Alerts & Anomaly Engine — extend FraudDetectionService
5. Driver Mobile API — extend SalesService + IdentityService

### TRACK_STEPS.md
Requires reading all `completed-steps/` walkthroughs before proceeding.

---

## Proposed Approach for Step 13

> [!IMPORTANT]
> Step 13 is the largest single step in the entire project — 42 pages, 5 modules, NgRx state management, SignalR integration, Google OAuth, Razorpay checkout, Charts, Maps, PWA, and i18n. This needs to be broken into sub-phases.

### Sub-Phase Breakdown

#### Phase 13A — Foundation (Scaffold + Design System + Core)
1. `ng new epcl-angular` in `src/Services/Frontend/epcl-angular/`
2. Install all packages (Material, NgRx, SignalR, Chart.js, Tailwind, etc.)
3. Extract design tokens from `petrocore_obsidian/` → Tailwind config + SCSS variables
4. Set up environments, proxy config, Inter/JetBrains Mono fonts
5. Build **Core module**: JwtInterceptor, ErrorInterceptor, AuthGuard, RoleGuard, AuthService, SignalRService
6. Build **NgRx AuthState** (login/logout/restore session/Google OAuth)
7. Build **App Shell** — sidebar + topbar layout with role-based navigation
8. Build **all shared components**: LoadingSpinner, Toast, ConfirmDialog, DataTable, TankGauge, StatusBadge, etc.

#### Phase 13B — Auth Module (3 pages)
9. `LoginPageComponent` — from `epcl_login/` stitch
10. `RegisterPageComponent` — from `epcl_register/` stitch
11. `ForgotPasswordPageComponent` — from `epcl_forgot_password/` stitch

#### Phase 13C — Customer Module (8 pages)
12. Dashboard, Wallet (Razorpay), Fuel Prices, Transaction History, Station Locator (Maps), Loyalty Dashboard, Vehicles, Referral Hub

#### Phase 13D — Dealer Module (8 pages)
13. Dashboard (tabbed), Inventory, Reports, Transactions, New Sale Flow (3-step), Fuel Order Confirmation, Replenishment Request, Shift Management

#### Phase 13E — Admin Module (10 pages)
14. Dashboard, Price Management, Audit Logs, Fraud Intelligence, Report Engine, User Management, Station Management, System Health, Replenishment Approval

#### Phase 13F — Shared/Profile + SignalR + PWA (5 pages + integration)
15. Profile Settings, Notification Settings, Support/Help Center, 404/Unauthorized
16. SignalR integration (AdminHub + DealerHub connections)
17. PWA service worker + Hindi i18n

---

## Decision Points for You

> [!WARNING]
> Before I start, I need your input on these:

1. **Extra Features Timing**: `EPCL_EXTRA_FEATURES_PLAN.md` describes 5 new features (AI Chatbot, Stock Predictions, Document Vault, Smart Alerts, Driver API). The doc says "implement alongside or after Step 13." 
   - **Option A**: Build the frontend FIRST (Step 13 complete), then add the extra backend services + their frontend components after.
   - **Option B**: Build the extra backend services now (before frontend), so the frontend can integrate everything in one pass.
   - **My recommendation**: Option A — complete Step 13 for the core platform, then add extra features as Step 13.5 (backend) + Step 13.6 (frontend).

2. **Scope per session**: Step 13 is massive (~42 pages). I'll work through the sub-phases sequentially (13A → 13B → 13C → etc.), with a build verification at each checkpoint. Each sub-phase will be a natural stopping point.

3. **Stitch design approach**: For every page, I will study the `screen.png` for layout/visual fidelity, then read `code.html` for component extraction. I will NOT copy-paste HTML — I'll rebuild as proper Angular components with real data bindings, matching the visual design exactly.

---

## Immediate Next Step

**Phase 13A — Foundation**: Scaffold the Angular workspace, install dependencies, extract the design system from `petrocore_obsidian/`, set up Tailwind, build the core interceptors/guards/services, NgRx auth store, App Shell layout, and all shared components.

This gives us a running Angular app with working auth flow, role-based routing, and all the building blocks needed for every subsequent page.

## Verification Plan

### After Phase 13A
- `ng serve` compiles with 0 errors
- Login page renders (basic placeholder)
- JWT interceptor attaches token on protected routes
- NgRx auth state persists on page refresh
- Role-based routing redirects correctly
- App Shell sidebar shows role-appropriate navigation items
- Tailwind design tokens match `petrocore_obsidian/` colors

### After Full Step 13
- All 42 pages render and match their stitch `screen.png` visually
- Google OAuth login works end-to-end
- Razorpay wallet top-up UI works
- SignalR live updates appear on Admin/Dealer dashboards
- All API calls go through dedicated services (never raw HttpClient in components)
