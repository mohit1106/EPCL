# Step 11 — Loyalty Service ✅

## Tasks

- [x] **11.1** Domain: LoyaltyAccount entity — Silver/Gold/Platinum tiers, RecalculateTier(), PointsBalance with CHECK >= 0
- [x] **11.2** Domain: LoyaltyTransaction entity — Earn/Redeem/Expire/Adjust/Referral types
- [x] **11.3** Domain: ReferralCode entity — unique 8-char alphanumeric, TotalReferrals/TotalPointsEarned stats
- [x] **11.4** Domain: ReferralRedemption entity — tracks who used which code (unique per RedeemedByCustomerId)
- [x] **11.5** Domain: ProcessedEvent for idempotency
- [x] **11.6** Domain: Repository interfaces (5) + InsufficientPointsException
- [x] **11.7** Application: EarnPointsCommand — 1pt per ₹10 (FLOOR), auto-creates account if missing
- [x] **11.8** Application: RedeemPointsCommand — 1pt = ₹0.50 discount, insufficient points check
- [x] **11.9** Application: ExpirePointsCommand — batch job for 12-month inactivity expiry
- [x] **11.10** Application: EarnReferralBonusCommand — 100 bonus points per referral, anti self-referral
- [x] **11.11** Application: CreateReferralCodeCommand — auto-generates unique 8-char code
- [x] **11.12** Application: GetLoyaltyBalance query (with tier progress to next tier)
- [x] **11.13** Application: GetLoyaltyHistory query (paginated)
- [x] **11.14** Application: GetMyReferralCode + GetReferralLeaderboard queries
- [x] **11.15** Infrastructure: LoyaltyDbContext — 5 tables (LoyaltyAccounts, LoyaltyTransactions, ReferralCodes, ReferralRedemptions, ProcessedEvents)
- [x] **11.16** Infrastructure: 5 repository implementations
- [x] **11.17** Infrastructure: LoyaltyConsumerHostedService — sales.completed → auto-earn points (skips if no CustomerUserId)
- [x] **11.18** Infrastructure: DependencyInjection registration
- [x] **11.19** EF Core migration — InitialCreate generated ✅
- [x] **11.20** API: LoyaltyController — 6 endpoints (balance, history, redeem, referral/my-code, referral/leaderboard, referral/apply)
- [x] **11.21** API: Middleware (with InsufficientPointsException), Program.cs, appsettings, Dockerfile
- [x] **11.22** Cross-project verification — All 10 services + Gateway build ✅
