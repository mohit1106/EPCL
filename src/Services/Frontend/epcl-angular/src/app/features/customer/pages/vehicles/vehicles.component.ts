import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { SalesApiService, VehicleDto } from '../../../../core/services/sales-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

interface DisplayVehicle {
  id: string;
  name: string;
  plate: string;
  type: string;
  fuel: string;
  capacity: string;
  lastRefuel: string;
  status: string;
}

@Component({
  selector: 'app-vehicles',
  templateUrl: './vehicles.component.html',
  styleUrls: ['./vehicles.component.scss'],
})
export class VehiclesComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  vehicles: DisplayVehicle[] = [];
  isLoading = true;
  showAddModal = false;
  vehicleForm: FormGroup;
  isSubmitting = false;

  vehicleTypes = ['Car', 'Motorcycle', 'Truck', 'Bus', 'Van', 'Auto-rickshaw'];
  fuelPreferences = ['Petrol', 'Diesel', 'CNG', 'PremiumPetrol', 'PremiumDiesel'];

  constructor(
    private salesApi: SalesApiService,
    private toast: ToastService,
    private fb: FormBuilder
  ) {
    this.vehicleForm = this.fb.group({
      registrationNumber: ['', [Validators.required, Validators.pattern(/^[A-Z]{2}[0-9]{2}[A-Z]{2}[0-9]{4}$/)]],
      vehicleType: ['', [Validators.required]],
      fuelPreference: ['', [Validators.required]],
    });
  }

  ngOnInit(): void { this.loadVehicles(); }
  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  private loadVehicles(): void {
    this.isLoading = true;
    this.salesApi.getMyVehicles().pipe(takeUntil(this.destroy$)).subscribe({
      next: (v) => { 
        this.vehicles = v.map(dto => ({
          id: dto.id,
          name: dto.registrationNumber, // In a real app this might be a custom name
          plate: dto.registrationNumber,
          type: dto.vehicleType,
          fuel: dto.fuelPreference,
          capacity: '50L', // Hardcoded mockup
          lastRefuel: 'Unknown',
          status: 'Active'
        })); 
        this.isLoading = false; 
      },
      error: () => { this.isLoading = false; },
    });
  }

  openAddModal(): void { this.showAddModal = true; this.vehicleForm.reset(); }
  closeAddModal(): void { this.showAddModal = false; }

  addVehicle(): void {
    if (this.vehicleForm.invalid) { this.vehicleForm.markAllAsTouched(); return; }
    this.isSubmitting = true;
    this.salesApi.registerVehicle(this.vehicleForm.value).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toast.success('Vehicle registered!');
        this.closeAddModal();
        this.isSubmitting = false;
        this.loadVehicles();
      },
      error: () => { this.toast.error('Failed to register vehicle.'); this.isSubmitting = false; },
    });
  }

  deleteVehicle(id: string, regNum: string): void {
    if (confirm(`Remove vehicle ${regNum}?`)) {
      this.salesApi.deleteVehicle(id).pipe(takeUntil(this.destroy$)).subscribe({
        next: () => { this.toast.success('Vehicle removed.'); this.loadVehicles(); },
        error: () => this.toast.error('Failed to remove vehicle.'),
      });
    }
  }
}
