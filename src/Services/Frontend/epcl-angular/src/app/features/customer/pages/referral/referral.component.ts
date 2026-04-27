import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { LoyaltyApiService, ReferralCodeDto, ReferralLeaderDto } from '../../../../core/services/loyalty-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-referral',
  templateUrl: './referral.component.html',
  styleUrls: ['./referral.component.scss'],
})
export class ReferralComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  referralCode = '';
  leaderboard: ReferralLeaderDto[] = [];
  isLoading = true;

  totalReferrals = 12;
  onboards = 4;
  pendingCredits = 250;

  tiers = [
    { name: 'Bronze Node', range: '0-5 Refs', perks: ['Basic Dashboard Access', 'Standard Comm Rate'], active: false },
    { name: 'Silver Hub', range: '5-20 Refs', perks: ['Priority Support', '+2% Volume Bonus', 'API Access Beta'], active: true },
    { name: 'Gold Terminal', range: '20+ Refs', perks: ['Dedicated Account Mgr', '+5% Volume Bonus', 'Hardware Grants'], active: false }
  ];

  recentActivity = [
    { initials: 'JD', name: 'John Doe Logistics', role: 'Fleet Operator', date: '21 OCT 2023', status: 'JOINED' },
    { initials: 'ST', name: 'Sarah Transit Co.', role: 'Independent Driver', date: '19 OCT 2023', status: 'INVITED' },
    { initials: 'EX', name: 'Express Freight Link', role: 'Fleet Operator', date: '15 OCT 2023', status: 'PENDING VERIFICATION' }
  ];

  constructor(private loyaltyApi: LoyaltyApiService, private toast: ToastService) {}

  ngOnInit(): void {
    this.loyaltyApi.getMyReferralCode().pipe(takeUntil(this.destroy$)).subscribe({
      next: (code) => { this.referralCode = code.code; this.isLoading = false; },
      error: () => { this.isLoading = false; },
    });
    this.loyaltyApi.getLeaderboard().pipe(takeUntil(this.destroy$)).subscribe({
      next: (lb) => { this.leaderboard = lb; },
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  copyCode(): void {
    if (this.referralCode) {
      navigator.clipboard.writeText(this.referralCode).then(() => {
        this.toast.success('Referral code copied!');
      });
    }
  }

  shareWhatsApp(): void {
    if (this.referralCode) {
      window.open(`https://wa.me/?text=Use my EPCL referral code ${this.referralCode} to earn bonus points!`, '_blank');
    }
  }
}
