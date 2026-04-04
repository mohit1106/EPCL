# Step 11 — Loyalty Service — Walkthrough

## Summary

The Loyalty Service manages customer loyalty points, tier progression, points redemption, and a referral program. It consumes SaleCompletedEvent from RabbitMQ to auto-credit points (1pt per ₹10 spent), implements tier-based rewards (Silver → Gold → Platinum), and provides a referral system where existing customers earn 100 bonus points per successful referral.

---

## Architecture

```
LoyaltyService.API (ASP.NET 10 Web API — 6 endpoints)
  └── Controllers/LoyaltyController
       └── LoyaltyService.Infrastructure
            ├── Messaging/LoyaltyConsumerHostedService (sales.completed)
            ├── Persistence/LoyaltyDbContext (5 tables)
            └── Repositories/ (5 implementations)
                 └── LoyaltyService.Application (MediatR CQRS)
                      ├── Commands/ (EarnPoints, RedeemPoints, ExpirePoints, EarnReferralBonus, CreateReferralCode)
                      └── Queries/ (Balance, History, ReferralCode, Leaderboard)
                           └── LoyaltyService.Domain (4 entities)
```

---

## Business Rules

| Rule | Implementation |
|------|---------------|
| Points per purchase | 1 pt per ₹10 — `FLOOR(TotalAmount / 10)` |
| Redemption rate | 1 pt = ₹0.50 discount |
| Silver tier | 0-999 lifetime points |
| Gold tier | 1000-4999 lifetime points |
| Platinum tier | 5000+ lifetime points |
| Points expiry | All points expire after 12 months of inactivity |
| Referral bonus | 100 pts credited to referrer when new customer joins using their code |
| Self-referral | Blocked — cannot use own referral code |
| Duplicate referral | Each new customer can only apply one referral code |

---

## Database Schema (EPCL_Loyalty)

| Table | Key Constraints |
|-------|----------------|
| LoyaltyAccounts | PK(Id), UNIQUE(CustomerId), DEFAULT Tier='Silver' |
| LoyaltyTransactions | PK(Id), FK→LoyaltyAccounts, INDEX(LoyaltyAccountId), INDEX(SaleTransactionId) |
| ReferralCodes | PK(Id), UNIQUE(CustomerId), UNIQUE(Code) — 8-char alphanumeric |
| ReferralRedemptions | PK(Id), FK→ReferralCodes, UNIQUE(RedeemedByCustomerId) |
| ProcessedEvents | PK(Id), UNIQUE(EventId) |

---

## API Endpoints (6)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | /api/loyalty/balance | JWT | Balance + tier + progress to next tier |
| GET | /api/loyalty/history | JWT | Paginated points history |
| POST | /api/loyalty/redeem | JWT | Redeem points for ₹ discount |
| GET | /api/loyalty/referral/my-code | JWT | Get/create own referral code + stats |
| GET | /api/loyalty/referral/leaderboard | JWT | Top 10 referrers this month |
| POST | /api/loyalty/referral/apply | JWT | Apply referral code after registration |

---

## Event Flow

```
SaleCompletedEvent (from Sales Service)
  → epcl.loyalty.queue (binding: sales.completed)
  → LoyaltyConsumerHostedService
  → Idempotency check (ProcessedEvents)
  → If CustomerUserId present: EarnPointsCommand
    → FLOOR(TotalAmount / 10) = points
    → Update balance + lifetime + recalculate tier
    → Log LoyaltyTransaction (Type=Earn)
```

---

## Build Verification

```
✅ LoyaltyService.Domain         → 0 errors
✅ LoyaltyService.Application    → 0 errors
✅ LoyaltyService.Infrastructure → 0 errors
✅ LoyaltyService.API            → 0 errors
✅ InitialCreate migration       → Generated
✅ Cross-project (10 services)   → All pass
```
