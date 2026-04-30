import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { catchError, of } from 'rxjs';
import { SalesApiService, VehicleDto } from '../../../../core/services/sales-api.service';
import { StationsApiService, FuelTypeDto } from '../../../../core/services/stations-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-vehicles',
  templateUrl: './vehicles.component.html',
  styleUrls: ['./vehicles.component.scss'],
})
export class VehiclesComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  vehicles: VehicleDto[] = [];
  fuelTypes: FuelTypeDto[] = [];
  isLoading = true;
  showRegisterModal = false;
  vehicleForm: FormGroup;
  isSubmitting = false;

  vehicleTypes = [
    { value: 'Car', label: 'Car', icon: 'car' },
    { value: 'Motorcycle', label: 'Motorcycle', icon: 'motorcycle' },
    { value: 'Truck', label: 'Truck', icon: 'truck' },
    { value: 'Bus', label: 'Bus', icon: 'bus' },
    { value: 'Van', label: 'Van', icon: 'van' },
    { value: 'AutoRickshaw', label: 'Auto Rickshaw', icon: 'auto' },
  ];

  constructor(
    private salesApi: SalesApiService,
    private stationsApi: StationsApiService,
    private toast: ToastService,
    private fb: FormBuilder
  ) {
    this.vehicleForm = this.fb.group({
      registrationNumber: ['', [Validators.required, Validators.pattern(/^[A-Z]{2}[0-9]{2}[A-Z]{2}[0-9]{4}$/)]],
      vehicleType: ['Car', [Validators.required]],
      fuelTypePreference: [''],
      nickname: [''],
    });
  }

  ngOnInit(): void {
    this.loadFuelTypes();
    this.loadVehicles();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadFuelTypes(): void {
    this.stationsApi.getFuelTypes().pipe(
      takeUntil(this.destroy$),
      catchError(() => of([]))
    ).subscribe(ft => this.fuelTypes = ft);
  }

  private loadVehicles(): void {
    this.isLoading = true;
    this.salesApi.getMyVehicles().pipe(takeUntil(this.destroy$)).subscribe({
      next: (v) => {
        this.vehicles = v;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; },
    });
  }

  openRegisterModal(): void {
    this.showRegisterModal = true;
    this.vehicleForm.reset({ vehicleType: 'Car', fuelTypePreference: '', nickname: '' });
  }

  closeRegisterModal(): void {
    this.showRegisterModal = false;
  }

  registerVehicle(): void {
    if (this.vehicleForm.invalid) {
      this.vehicleForm.markAllAsTouched();
      return;
    }
    this.isSubmitting = true;
    const val = this.vehicleForm.value;
    const payload: any = {
      registrationNumber: val.registrationNumber,
      vehicleType: val.vehicleType,
    };
    if (val.fuelTypePreference) payload.fuelTypePreference = val.fuelTypePreference;
    if (val.nickname?.trim()) payload.nickname = val.nickname.trim();

    this.salesApi.registerVehicle(payload).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toast.success('Vehicle registered successfully!');
        this.closeRegisterModal();
        this.isSubmitting = false;
        this.loadVehicles();
      },
      error: (err) => {
        const msg = err?.error?.message || err?.error?.title || 'Failed to register vehicle.';
        this.toast.error(msg);
        this.isSubmitting = false;
      },
    });
  }

  deleteVehicle(v: VehicleDto): void {
    if (!confirm(`Remove vehicle ${v.registrationNumber}${v.nickname ? ' (' + v.nickname + ')' : ''}?`)) return;
    this.salesApi.deleteVehicle(v.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toast.success('Vehicle removed.');
        this.loadVehicles();
      },
      error: () => this.toast.error('Failed to remove vehicle.'),
    });
  }

  getVehicleIcon(type: string): string {
    return this.vehicleTypes.find(t => t.value === type)?.icon || 'car';
  }

  getVehicleLabel(type: string): string {
    return this.vehicleTypes.find(t => t.value === type)?.label || type;
  }

  getFuelName(fuelTypeId?: string): string {
    if (!fuelTypeId) return 'Not set';
    const ft = this.fuelTypes.find(f => f.id === fuelTypeId);
    return ft?.name || 'Fuel';
  }

  formatPlate(reg: string): string {
    if (reg.length === 10) {
      return reg.substring(0, 2) + ' ' + reg.substring(2, 4) + ' ' + reg.substring(4, 6) + ' ' + reg.substring(6);
    }
    return reg;
  }

  get activeCount(): number {
    return this.vehicles.filter(v => v.isActive).length;
  }
}
