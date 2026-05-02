import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil, catchError, of } from 'rxjs';
import { DriversApiService, DriverDto, CreateDriverDto } from '../../../../core/services/drivers-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-admin-drivers',
  templateUrl: './drivers.component.html',
  styleUrls: ['./drivers.component.scss'],
})
export class AdminDriversComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  drivers: DriverDto[] = [];
  isLoading = true;

  // Modal
  showModal = false;
  editingId = '';
  formData = { fullName: '', phone: '', licenseNumber: '', vehicleNumber: '' };

  // Delete confirmation
  deletingId = '';

  constructor(
    private driversApi: DriversApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void { this.loadDrivers(); }
  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  loadDrivers(): void {
    this.isLoading = true;
    this.driversApi.getAll().pipe(takeUntil(this.destroy$), catchError(() => of([]))).subscribe(drivers => {
      this.drivers = drivers;
      this.isLoading = false;
    });
  }

  openCreateModal(): void {
    this.editingId = '';
    this.formData = { fullName: '', phone: '', licenseNumber: '', vehicleNumber: '' };
    this.showModal = true;
  }

  openEditModal(d: DriverDto): void {
    this.editingId = d.id;
    this.formData = { fullName: d.fullName, phone: d.phone, licenseNumber: d.licenseNumber, vehicleNumber: d.vehicleNumber };
    this.showModal = true;
  }

  closeModal(): void { this.showModal = false; }

  saveDriver(): void {
    if (!this.formData.fullName || !this.formData.phone) {
      this.toast.error('Name and phone are required.'); return;
    }

    if (this.editingId) {
      this.driversApi.update(this.editingId, this.formData).pipe(takeUntil(this.destroy$)).subscribe({
        next: () => { this.toast.success('Driver updated.'); this.closeModal(); this.loadDrivers(); },
        error: () => this.toast.error('Failed to update driver.'),
      });
    } else {
      this.driversApi.create(this.formData).pipe(takeUntil(this.destroy$)).subscribe({
        next: () => { this.toast.success('Driver created.'); this.closeModal(); this.loadDrivers(); },
        error: () => this.toast.error('Failed to create driver.'),
      });
    }
  }

  confirmDelete(id: string): void { this.deletingId = id; }
  cancelDelete(): void { this.deletingId = ''; }

  deleteDriver(): void {
    this.driversApi.delete(this.deletingId).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('Driver deleted.'); this.deletingId = ''; this.loadDrivers(); },
      error: (e) => this.toast.error(e?.error?.message || e?.error || 'Cannot delete driver.'),
    });
  }

  trackById(_: number, d: DriverDto): string { return d.id; }
}
