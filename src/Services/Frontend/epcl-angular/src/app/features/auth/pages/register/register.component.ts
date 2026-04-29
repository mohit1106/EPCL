import { Component, AfterViewInit, ViewChild, ElementRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { register } from '../../../../store/auth/auth.actions';
import { selectAuthLoading, selectAuthError, selectRegisterMessage } from '../../../../store/auth/auth.selectors';

declare var Chart: any;

interface RoleOption {
  value: string;
  label: string;
  description: string;
}

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
})
export class RegisterComponent implements AfterViewInit {
  @ViewChild('brandChart') brandChartRef!: ElementRef<HTMLCanvasElement>;

  currentStep = 1;
  showPassword = false;
  isLoading$: Observable<boolean>;
  error$: Observable<string | null>;
  registerMessage$: Observable<string | null>;

  roles: RoleOption[] = [
    { value: 'Customer', label: 'Customer', description: 'Track fuel purchases & loyalty' },
    { value: 'Dealer', label: 'Dealer', description: 'Manage pumps, inventory & sales' },
  ];

  identityForm: FormGroup;
  detailsForm: FormGroup;
  selectedRole = '';

  constructor(
    private fb: FormBuilder,
    private store: Store
  ) {
    this.identityForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.required, Validators.pattern(/^[6-9]\d{9}$/)]],
    });

    this.detailsForm = this.fb.group({
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]],
      referralCode: [''],
    });

    this.isLoading$ = this.store.select(selectAuthLoading);
    this.error$ = this.store.select(selectAuthError);
    this.registerMessage$ = this.store.select(selectRegisterMessage);
  }

  ngAfterViewInit(): void {
    this.initBrandChart();
  }

  selectRole(role: string): void {
    this.selectedRole = role;
  }

  nextStep(): void {
    if (this.currentStep === 1) {
      if (!this.selectedRole) return;
      this.currentStep = 2;
    } else if (this.currentStep === 2) {
      if (this.identityForm.valid) {
        this.currentStep = 3;
      } else {
        this.identityForm.markAllAsTouched();
      }
    }
  }

  prevStep(): void {
    if (this.currentStep > 1) this.currentStep--;
  }

  onSubmit(): void {
    if (this.detailsForm.valid && this.identityForm.valid && this.selectedRole) {
      if (this.detailsForm.value.password !== this.detailsForm.value.confirmPassword) {
        return;
      }

      this.store.dispatch(register({
        request: {
          fullName: this.identityForm.value.fullName,
          email: this.identityForm.value.email,
          phoneNumber: this.identityForm.value.phoneNumber,
          password: this.detailsForm.value.password,
          confirmPassword: this.detailsForm.value.confirmPassword,
          role: this.selectedRole,
          referralCode: this.detailsForm.value.referralCode || undefined,
        },
      }));
    } else {
      this.detailsForm.markAllAsTouched();
    }
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  get passwordsMismatch(): boolean {
    const pass = this.detailsForm.get('password')?.value;
    const confirm = this.detailsForm.get('confirmPassword')?.value;
    return confirm?.length > 0 && pass !== confirm;
  }

  private initBrandChart(): void {
    if (!this.brandChartRef?.nativeElement) return;
    const ctx = this.brandChartRef.nativeElement.getContext('2d');
    if (!ctx) return;

    const labels = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    const stations = [620, 645, 672, 698, 720, 738, 756, 780, 800, 820, 835, 847];
    const transactions = [1.8, 1.85, 1.9, 1.95, 2.0, 2.05, 2.1, 2.15, 2.2, 2.25, 2.3, 2.4];

    const stationGrad = ctx.createLinearGradient(0, 0, 0, 180);
    stationGrad.addColorStop(0, 'rgba(16, 185, 129, 0.3)');
    stationGrad.addColorStop(1, 'rgba(16, 185, 129, 0.01)');

    const txnGrad = ctx.createLinearGradient(0, 0, 0, 180);
    txnGrad.addColorStop(0, 'rgba(245, 158, 11, 0.3)');
    txnGrad.addColorStop(1, 'rgba(245, 158, 11, 0.01)');

    new Chart(ctx, {
      type: 'line',
      data: {
        labels,
        datasets: [
          {
            label: 'Stations',
            data: stations,
            borderColor: '#10B981',
            backgroundColor: stationGrad,
            borderWidth: 2,
            fill: true,
            tension: 0.4,
            pointRadius: 0,
            pointHoverRadius: 4,
            yAxisID: 'y',
          },
          {
            label: 'Txns (M)',
            data: transactions,
            borderColor: '#F59E0B',
            backgroundColor: txnGrad,
            borderWidth: 2,
            fill: true,
            tension: 0.4,
            pointRadius: 0,
            pointHoverRadius: 4,
            yAxisID: 'y1',
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
            position: 'left' as const,
            grid: { color: 'rgba(255,255,255,0.06)' },
            ticks: { color: 'rgba(255,255,255,0.4)', font: { size: 10 } },
            border: { display: false },
          },
          y1: {
            position: 'right' as const,
            grid: { display: false },
            ticks: { color: 'rgba(255,255,255,0.4)', font: { size: 10 } },
            border: { display: false },
          },
        },
      },
    });
  }
}
