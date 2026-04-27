import { Component, AfterViewInit, ViewChild, ElementRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { login } from '../../../../store/auth/auth.actions';
import { selectAuthLoading, selectAuthError } from '../../../../store/auth/auth.selectors';

declare var Chart: any;

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})
export class LoginComponent implements AfterViewInit {
  @ViewChild('brandChart') brandChartRef!: ElementRef<HTMLCanvasElement>;

  loginForm: FormGroup;
  showPassword = false;
  isLoading$: Observable<boolean>;
  error$: Observable<string | null>;
  googleDisabled = true;

  // Role selector
  selectedRole: 'customer' | 'dealer' | 'admin' = 'customer';
  roles = [
    {
      value: 'customer' as const,
      label: 'Customer',
      icon: 'user',
      description: 'Track fuel purchases, wallet & loyalty',
    },
    {
      value: 'dealer' as const,
      label: 'Dealer',
      icon: 'station',
      description: 'Manage pumps, inventory & sales',
    },
    {
      value: 'admin' as const,
      label: 'Admin',
      icon: 'shield',
      description: 'Full system access & analytics',
    },
  ];

  constructor(
    private fb: FormBuilder,
    private store: Store
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      rememberMe: [false],
    });

    this.isLoading$ = this.store.select(selectAuthLoading);
    this.error$ = this.store.select(selectAuthError);
  }

  ngAfterViewInit(): void {
    this.initBrandChart();
  }

  selectRole(role: 'customer' | 'dealer' | 'admin'): void {
    this.selectedRole = role;
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  onSubmit(): void {
    if (this.loginForm.valid) {
      const { email, password } = this.loginForm.value;
      this.store.dispatch(login({ request: { email, password } }));
    } else {
      this.loginForm.markAllAsTouched();
    }
  }

  onGoogleLogin(): void {
    // Google OAuth — disabled for now
  }

  get emailInvalid(): boolean {
    const ctrl = this.loginForm.get('email');
    return !!(ctrl && ctrl.invalid && ctrl.touched);
  }

  get passwordInvalid(): boolean {
    const ctrl = this.loginForm.get('password');
    return !!(ctrl && ctrl.invalid && ctrl.touched);
  }

  private initBrandChart(): void {
    if (!this.brandChartRef?.nativeElement) return;
    const ctx = this.brandChartRef.nativeElement.getContext('2d');
    if (!ctx) return;

    // Generate realistic fuel consumption data
    const labels = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    const diesel = [420, 380, 445, 490, 520, 480, 510, 540, 505, 560, 590, 620];
    const petrol = [310, 340, 365, 390, 410, 385, 450, 470, 430, 485, 510, 530];

    // Create gradient fills
    const dieselGrad = ctx.createLinearGradient(0, 0, 0, 180);
    dieselGrad.addColorStop(0, 'rgba(30, 64, 175, 0.3)');
    dieselGrad.addColorStop(1, 'rgba(30, 64, 175, 0.01)');

    const petrolGrad = ctx.createLinearGradient(0, 0, 0, 180);
    petrolGrad.addColorStop(0, 'rgba(245, 158, 11, 0.3)');
    petrolGrad.addColorStop(1, 'rgba(245, 158, 11, 0.01)');

    new Chart(ctx, {
      type: 'line',
      data: {
        labels,
        datasets: [
          {
            label: 'Diesel (kL)',
            data: diesel,
            borderColor: '#1E40AF',
            backgroundColor: dieselGrad,
            borderWidth: 2,
            fill: true,
            tension: 0.4,
            pointRadius: 0,
            pointHoverRadius: 4,
          },
          {
            label: 'Petrol (kL)',
            data: petrol,
            borderColor: '#F59E0B',
            backgroundColor: petrolGrad,
            borderWidth: 2,
            fill: true,
            tension: 0.4,
            pointRadius: 0,
            pointHoverRadius: 4,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { intersect: false, mode: 'index' as const },
        plugins: {
          legend: {
            display: true,
            position: 'top' as const,
            labels: {
              color: 'rgba(255,255,255,0.7)',
              font: { size: 11, family: 'Inter' },
              boxWidth: 12,
              padding: 12,
            },
          },
          tooltip: {
            backgroundColor: 'rgba(15,23,42,0.9)',
            titleFont: { family: 'Inter' },
            bodyFont: { family: 'Inter' },
            cornerRadius: 8,
            padding: 10,
          },
        },
        scales: {
          x: {
            grid: { display: false },
            ticks: { color: 'rgba(255,255,255,0.4)', font: { size: 10 } },
            border: { display: false },
          },
          y: {
            grid: { color: 'rgba(255,255,255,0.06)' },
            ticks: { color: 'rgba(255,255,255,0.4)', font: { size: 10 } },
            border: { display: false },
          },
        },
      },
    });
  }
}
