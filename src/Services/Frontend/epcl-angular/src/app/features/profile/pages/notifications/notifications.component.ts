import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { NotificationsApiService, NotificationPrefsDto } from '../../../../core/services/notifications-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-notifications',
  templateUrl: './notifications.component.html',
  styleUrls: ['./notifications.component.scss'],
})
export class NotificationsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  globalMute = false;
  lastUpdate = new Date().toLocaleTimeString();
  activeSyncs = '3/3';
  protocol = 'TLS 1.3';

  channels = [
    { icon: 'lock', name: 'Security Events', desc: 'Login attempts, password changes, lockouts', email: true, push: true, sms: true },
    { icon: 'fuel', name: 'Transaction Receipts', desc: 'Real-time purchase confirmations', email: true, push: true, sms: false },
    { icon: 'bar-chart', name: 'Price Alerts', desc: 'Fuel price changes for watched types', email: true, push: true, sms: false },
    { icon: 'trending-up', name: 'Loyalty Rewards', desc: 'Points earned, tier upgrades, redemptions', email: true, push: false, sms: false },
    { icon: 'alert-triangle', name: 'Fraud Alerts', desc: 'Suspicious activity notifications', email: true, push: true, sms: true },
    { icon: 'database', name: 'Inventory Alerts', desc: 'Low stock, replenishment, dip variance', email: true, push: true, sms: false },
    { icon: 'settings', name: 'System Maintenance', desc: 'Scheduled downtime, updates', email: true, push: false, sms: false },
  ];

  constructor(private notifApi: NotificationsApiService, private toast: ToastService) {}

  ngOnInit(): void {
    this.notifApi.getPreferences().pipe(takeUntil(this.destroy$)).subscribe({
      next: (prefs) => {
        this.globalMute = !(prefs.emailEnabled || prefs.smsEnabled || prefs.pushEnabled);
        this.channels[0].email = prefs.securityAlerts;
        this.channels[1].email = prefs.transactionReceipts;
        this.channels[2].email = prefs.priceAlerts;
        this.channels[3].email = prefs.loyaltyRewards;
        this.lastUpdate = new Date().toLocaleTimeString();
      },
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  savePreferences(): void {
    const prefs: Partial<NotificationPrefsDto> = {
      emailEnabled: !this.globalMute,
      smsEnabled: !this.globalMute,
      pushEnabled: !this.globalMute,
      securityAlerts: this.channels[0].email,
      transactionReceipts: this.channels[1].email,
      priceAlerts: this.channels[2].email,
      loyaltyRewards: this.channels[3].email,
    };
    this.notifApi.updatePreferences(prefs).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('Preferences saved.'); this.lastUpdate = new Date().toLocaleTimeString(); },
      error: () => this.toast.error('Failed to save preferences.'),
    });
  }
}
