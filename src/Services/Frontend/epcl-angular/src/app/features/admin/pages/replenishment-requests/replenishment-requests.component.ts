import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil, interval, switchMap, catchError, of } from 'rxjs';
import { InventoryApiService, ReplenishmentStatusDto } from '../../../../core/services/inventory-api.service';
import { DriversApiService, DriverDto } from '../../../../core/services/drivers-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-admin-replenishment-requests',
  templateUrl: './replenishment-requests.component.html',
  styleUrls: ['./replenishment-requests.component.scss'],
})
export class AdminReplenishmentRequestsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  requests: ReplenishmentStatusDto[] = [];
  filteredRequests: ReplenishmentStatusDto[] = [];
  availableDrivers: DriverDto[] = [];

  statusFilter = '';
  isLoading = true;

  // Assign driver modal
  showAssignModal = false;
  assigningRequestId = '';
  selectedDriverId = '';

  statusOptions = ['', 'Submitted', 'Approved', 'TankerAssigned', 'InTransit', 'Offloading', 'Complete', 'Rejected'];

  constructor(
    private inventoryApi: InventoryApiService,
    private driversApi: DriversApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.loadData();
    // Auto-refresh every 15s
    interval(15000).pipe(takeUntil(this.destroy$), switchMap(() => this.fetchRequests())).subscribe();
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  loadData(): void {
    this.isLoading = true;
    this.fetchRequests().subscribe();
  }

  private fetchRequests() {
    return this.inventoryApi.getAllReplenishmentRequests(1, 100).pipe(
      takeUntil(this.destroy$),
      catchError(() => of({ items: [] }))
    ).pipe(
      switchMap(result => {
        this.requests = result?.items || [];
        this.applyFilter();
        this.isLoading = false;
        return of(null);
      })
    );
  }

  applyFilter(): void {
    if (!this.statusFilter) {
      this.filteredRequests = this.requests;
    } else {
      this.filteredRequests = this.requests.filter(r => r.status === this.statusFilter);
    }
  }

  // ── Actions ──

  approve(req: ReplenishmentStatusDto): void {
    this.inventoryApi.approveReplenishment(req.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('Request approved.'); this.loadData(); },
      error: (e) => this.toast.error(e?.error?.message || 'Failed to approve.'),
    });
  }

  reject(req: ReplenishmentStatusDto): void {
    const reason = prompt('Rejection reason:');
    if (!reason) return;
    this.inventoryApi.rejectReplenishment(req.id, reason).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('Request rejected.'); this.loadData(); },
      error: (e) => this.toast.error(e?.error?.message || 'Failed to reject.'),
    });
  }

  openAssignModal(req: ReplenishmentStatusDto): void {
    this.assigningRequestId = req.id;
    this.selectedDriverId = '';
    this.showAssignModal = true;
    // Load available drivers
    this.driversApi.getAvailable().pipe(takeUntil(this.destroy$), catchError(() => of([]))).subscribe(drivers => {
      this.availableDrivers = drivers;
    });
  }

  closeAssignModal(): void { this.showAssignModal = false; }

  confirmAssignDriver(): void {
    if (!this.selectedDriverId) { this.toast.error('Please select a driver.'); return; }
    const driver = this.availableDrivers.find(d => d.id === this.selectedDriverId);
    if (!driver) return;

    this.inventoryApi.assignDriver(this.assigningRequestId, driver.id, driver.fullName, driver.phone, driver.driverCode)
      .pipe(takeUntil(this.destroy$)).subscribe({
        next: () => {
          this.toast.success(`Driver ${driver.fullName} assigned.`);
          // Mark driver as unavailable
          this.driversApi.update(driver.id, {}).subscribe(); // The backend handles availability via the replenishment flow
          this.closeAssignModal();
          this.loadData();
        },
        error: (e) => this.toast.error(e?.error?.message || 'Failed to assign driver.'),
      });
  }

  updateStatus(req: ReplenishmentStatusDto, newStatus: string): void {
    this.inventoryApi.updateReplenishmentStatus(req.id, newStatus).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success(`Status updated to ${newStatus}.`); this.loadData(); },
      error: (e) => this.toast.error(e?.error?.message || `Failed to update status.`),
    });
  }

  completeRequest(req: ReplenishmentStatusDto): void {
    this.inventoryApi.completeReplenishment(req.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toast.success('Replenishment completed!');
        // Release driver
        if (req.assignedDriverId) {
          this.driversApi.release(req.assignedDriverId).subscribe();
        }
        this.loadData();
      },
      error: (e) => this.toast.error(e?.error?.message || 'Failed to complete.'),
    });
  }

  getStatusLabel(s: string): string {
    const m: Record<string, string> = { Submitted: 'Pending', Approved: 'Approved', TankerAssigned: 'Tanker Assigned', InTransit: 'In Transit', Offloading: 'Offloading', Complete: 'Complete', Rejected: 'Rejected' };
    return m[s] || s;
  }

  getStatusClass(s: string): string {
    switch (s) {
      case 'Complete': return 'st-success';
      case 'Rejected': return 'st-danger';
      case 'Submitted': return 'st-pending';
      case 'InTransit': case 'Offloading': return 'st-active';
      case 'Approved': case 'TankerAssigned': return 'st-info';
      default: return '';
    }
  }

  getPriorityClass(p: string): string {
    switch (p) { case 'Urgent': return 'pr-urgent'; case 'High': return 'pr-high'; default: return 'pr-standard'; }
  }

  trackById(_: number, r: ReplenishmentStatusDto): string { return r.id; }
}
