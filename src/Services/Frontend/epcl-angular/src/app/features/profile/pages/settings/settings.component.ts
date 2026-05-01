import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil } from 'rxjs';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { UsersApiService, UpdateProfileDto } from '../../../../core/services/users-api.service';
import { AuthApiService, UserDto } from '../../../../core/services/auth-api.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { loadCurrentUserSuccess } from '../../../../store/auth/auth.actions';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss'],
})
export class SettingsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  user: { id: string; fullName: string; email: string; phone: string; role: string } = {
    id: '', fullName: '', email: '', phone: '', role: '',
  };

  tabs = [
    { label: 'Personal', icon: 'user', active: true },
    { label: 'Security', icon: 'shield', active: false },
    { label: 'Notifications', icon: 'bell', active: false },
  ];

  notifications = [
    { type: 'Security Alerts', desc: 'Login attempts, password changes', email: true, sms: true, push: true },
    { type: 'Transaction Receipts', desc: 'Fuel purchase confirmations', email: true, sms: false, push: true },
    { type: 'Price Alerts', desc: 'Fuel price changes', email: true, sms: false, push: true },
    { type: 'Loyalty Rewards', desc: 'Points earned, tier changes', email: true, sms: false, push: false },
    { type: 'System Updates', desc: 'Maintenance, new features', email: true, sms: false, push: false },
  ];

  twoFactorEnabled = false;
  isUpdatingProfile = false;
  isChangingPassword = false;

  // Password change
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';

  constructor(
    private store: Store,
    private usersApi: UsersApiService,
    private authApi: AuthApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.store.select(selectUser).pipe(takeUntil(this.destroy$)).subscribe(u => {
      if (u) {
        this.user = { id: u.id, fullName: u.fullName, email: u.email, phone: u.phoneNumber, role: u.role };
      }
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  setTab(label: string): void {
    this.tabs.forEach(t => t.active = t.label === label);
  }

  saveProfile(): void {
    this.isUpdatingProfile = true;
    const profile: UpdateProfileDto = {
      fullName: this.user.fullName,
      phoneNumber: this.user.phone,
    };
    this.usersApi.updateProfile(profile).pipe(takeUntil(this.destroy$)).subscribe({
      next: (userData) => {
        this.store.dispatch(loadCurrentUserSuccess({ user: userData }));
        this.toast.success('Profile updated.');
        this.isUpdatingProfile = false;
      },
      error: () => { this.toast.error('Failed to update profile.'); this.isUpdatingProfile = false; },
    });
  }

  changePassword(): void {
    if (this.newPassword !== this.confirmPassword) {
      this.toast.error('Passwords do not match.');
      return;
    }
    this.isChangingPassword = true;
    this.authApi.changePassword({
      currentPassword: this.currentPassword,
      newPassword: this.newPassword,
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toast.success('Password changed successfully.');
        this.currentPassword = '';
        this.newPassword = '';
        this.confirmPassword = '';
        this.isChangingPassword = false;
      },
      error: () => { this.toast.error('Failed to change password.'); this.isChangingPassword = false; },
    });
  }

  copyId(): void {
    navigator.clipboard.writeText(this.user.id).then(() => {
      this.toast.success('User ID copied to clipboard!');
    });
  }
}
