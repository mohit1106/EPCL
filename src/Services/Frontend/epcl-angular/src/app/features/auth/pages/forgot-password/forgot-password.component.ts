import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthApiService } from '../../../../core/services/auth-api.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.scss'],
})
export class ForgotPasswordComponent {
  currentStep = 1; // 1=email, 2=OTP, 3=new password
  isLoading = false;
  email = '';

  emailForm: FormGroup;
  otpForm: FormGroup;
  resetForm: FormGroup;

  constructor(
    private fb: FormBuilder,
    private authApi: AuthApiService,
    private toast: ToastService
  ) {
    this.emailForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
    });

    this.otpForm = this.fb.group({
      digit1: ['', [Validators.required, Validators.pattern(/^\d$/)]],
      digit2: ['', [Validators.required, Validators.pattern(/^\d$/)]],
      digit3: ['', [Validators.required, Validators.pattern(/^\d$/)]],
      digit4: ['', [Validators.required, Validators.pattern(/^\d$/)]],
      digit5: ['', [Validators.required, Validators.pattern(/^\d$/)]],
      digit6: ['', [Validators.required, Validators.pattern(/^\d$/)]],
    });

    this.resetForm = this.fb.group({
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]],
    });
  }

  requestOtp(): void {
    if (this.emailForm.invalid) {
      this.emailForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.email = this.emailForm.value.email;

    this.authApi.forgotPassword({ email: this.email })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: () => {
          this.toast.success('Access code sent to your email.');
          this.currentStep = 2;
        },
        error: () => this.toast.error('Failed to send access code.'),
      });
  }

  verifyOtp(): void {
    if (this.otpForm.invalid) return;

    const otp = Object.values(this.otpForm.value).join('');
    this.isLoading = true;

    this.authApi.verifyOtp(this.email, otp)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: () => {
          this.toast.success('Code verified. Set your new password.');
          this.currentStep = 3;
        },
        error: () => this.toast.error('Invalid or expired code.'),
      });
  }

  resetPassword(): void {
    if (this.resetForm.invalid || this.passwordsMismatch) return;

    const otp = Object.values(this.otpForm.value).join('');
    this.isLoading = true;

    this.authApi.resetPassword({
      email: this.email,
      otp,
      newPassword: this.resetForm.value.newPassword,
    })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: () => {
          this.toast.success('Password reset successful. You can now login.');
          this.currentStep = 1;
        },
        error: () => this.toast.error('Failed to reset password.'),
      });
  }

  onOtpDigitInput(event: Event, nextId: string): void {
    const input = event.target as HTMLInputElement;
    if (input.value.length === 1 && nextId) {
      const next = document.getElementById(nextId) as HTMLInputElement;
      next?.focus();
    }
  }

  get passwordsMismatch(): boolean {
    const pass = this.resetForm.get('newPassword')?.value;
    const confirm = this.resetForm.get('confirmPassword')?.value;
    return confirm?.length > 0 && pass !== confirm;
  }
}
