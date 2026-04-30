import { Component, OnInit, OnDestroy, HostListener, ElementRef } from '@angular/core';
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
  activeMenuId: string | null = null;

  vehicleTypes = [
    { value: 'Car', label: 'Car', icon: 'car' },
    { value: 'Motorcycle', label: 'Motorcycle', icon: 'motorcycle' },
    { value: 'Truck', label: 'Truck', icon: 'truck' },
    { value: 'Bus', label: 'Bus', icon: 'bus' },
    { value: 'Van', label: 'Van', icon: 'van' },
    { value: 'AutoRickshaw', label: 'Auto Rickshaw', icon: 'auto' },
  ];

  // Premium card gradient themes
  private cardThemes = [
    { bg: 'linear-gradient(135deg, #0F172A 0%, #1E293B 50%, #334155 100%)', accent: '#60A5FA' },
    { bg: 'linear-gradient(135deg, #1A1A2E 0%, #16213E 50%, #0F3460 100%)', accent: '#818CF8' },
    { bg: 'linear-gradient(135deg, #0C0C1D 0%, #1B1B3A 50%, #2D2D5F 100%)', accent: '#A78BFA' },
    { bg: 'linear-gradient(135deg, #0D1117 0%, #161B22 50%, #21262D 100%)', accent: '#7DD3FC' },
    { bg: 'linear-gradient(135deg, #1C1917 0%, #292524 50%, #44403C 100%)', accent: '#FCD34D' },
    { bg: 'linear-gradient(135deg, #14120E 0%, #1F1D18 50%, #3B3730 100%)', accent: '#F9A825' },
  ];

  constructor(
    private salesApi: SalesApiService,
    private stationsApi: StationsApiService,
    private toast: ToastService,
    private fb: FormBuilder,
    private elRef: ElementRef
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

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    if (!this.activeMenuId) return;
    // Check if click is inside any card-menu element
    const menus = this.elRef.nativeElement.querySelectorAll('.card-menu');
    let insideMenu = false;
    menus.forEach((menu: HTMLElement) => {
      if (menu.contains(event.target as Node)) insideMenu = true;
    });
    if (!insideMenu) this.activeMenuId = null;
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

  toggleMenu(vehicleId: string, event: Event): void {
    event.stopPropagation();
    this.activeMenuId = this.activeMenuId === vehicleId ? null : vehicleId;
  }

  deleteVehicle(v: VehicleDto, event: Event): void {
    event.stopPropagation();
    this.activeMenuId = null;
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

  getCardTheme(index: number): { bg: string; accent: string } {
    return this.cardThemes[index % this.cardThemes.length];
  }

  get activeCount(): number {
    return this.vehicles.filter(v => v.isActive).length;
  }
}
