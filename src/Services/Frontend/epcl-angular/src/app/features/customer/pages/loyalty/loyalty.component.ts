import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { LoyaltyApiService, LoyaltyBalanceDto, LoyaltyHistoryDto } from '../../../../core/services/loyalty-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-loyalty',
  templateUrl: './loyalty.component.html',
  styleUrls: ['./loyalty.component.scss'],
})
export class LoyaltyComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  balance: LoyaltyBalanceDto = { points: 0, tier: 'Silver', lifetimePoints: 0, nextTier: 'Gold', pointsToNextTier: 1000 };
  history: LoyaltyHistoryDto[] = [];
  redeemAmount = 0;
  isLoading = true;
  isRedeeming = false;

  totalPoints = 84500;
  lifetimeSavings = 1240.50;
  tierProgress = 68;
  currentTier = 'Silver';
  nextTier = 'Gold';
  pointsToNext = 15500;

  perks = [
    { icon: '🚀', badge: 'Active', name: 'Priority Routing', desc: 'Jump the queue during peak hours' },
    { icon: '💵', badge: 'Active', name: '2% Cash Back', desc: 'On all premium grade fuels' },
  ];

  voucherFilter = 'ACTIVE';
  vouchers = [
    { value: '₹5,000', name: 'Fleet Maintenance Credit', expiry: 'Exp. 30 Nov 2023' },
    { value: 'FREE', name: 'Coffee @ Lounge', expiry: 'Exp. 15 Oct 2023' },
  ];

  referralCode = 'EPC-X92-JD';

  tierThresholds = [
    { name: 'Silver', min: 0, max: 999 },
    { name: 'Gold', min: 1000, max: 4999 },
    { name: 'Platinum', min: 5000, max: Infinity },
  ];

  constructor(private loyaltyApi: LoyaltyApiService, private toast: ToastService) {}

  ngOnInit(): void {
    this.loadData();
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  private loadData(): void {
    this.isLoading = true;
    this.loyaltyApi.getBalance().pipe(takeUntil(this.destroy$)).subscribe({
      next: (b) => { this.balance = b; this.isLoading = false; },
      error: () => { this.isLoading = false; },
    });
    this.loyaltyApi.getHistory().pipe(takeUntil(this.destroy$)).subscribe({
      next: (h) => { this.history = h; },
    });
    
    // Assign mapped values from balance once loaded in real app, keeping mock defaults for compilation
  }

  get redeemValue(): number {
    return this.redeemAmount * 0.10;
  }

  get progressPercentage(): number {
    if (this.balance.pointsToNextTier <= 0) return 100;
    const current = this.tierThresholds.find(t => t.name === this.balance.tier);
    if (!current) return 0;
    const range = (current.max === Infinity ? this.balance.lifetimePoints : current.max) - current.min;
    const progress = this.balance.lifetimePoints - current.min;
    return Math.min(100, Math.max(0, (progress / range) * 100));
  }

  redeem(): void {
    if (this.redeemAmount <= 0 || this.redeemAmount > this.balance.points) {
      this.toast.error('Invalid redemption amount.');
      return;
    }
    this.isRedeeming = true;
    this.loyaltyApi.redeemPoints(this.redeemAmount).pipe(takeUntil(this.destroy$)).subscribe({
      next: (result) => {
        this.balance.points = result.newBalance;
        this.toast.success(`Redeemed ${this.redeemAmount} points for ₹${result.redeemedValue.toFixed(2)}!`);
        this.redeemAmount = 0;
        this.isRedeeming = false;
        this.loadData();
      },
      error: () => { this.toast.error('Redemption failed.'); this.isRedeeming = false; },
    });
  }
}
