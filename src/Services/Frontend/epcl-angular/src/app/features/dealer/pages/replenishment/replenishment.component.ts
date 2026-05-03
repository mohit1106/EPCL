import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil, interval, switchMap, catchError, of, forkJoin } from 'rxjs';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { InventoryApiService, ReplenishmentStatusDto, TankDto } from '../../../../core/services/inventory-api.service';
import { SalesApiService, PumpDto } from '../../../../core/services/sales-api.service';
import { StationsApiService, FuelTypeDto } from '../../../../core/services/stations-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-replenishment',
  templateUrl: './replenishment.component.html',
  styleUrls: ['./replenishment.component.scss'],
})
export class ReplenishmentComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private stationId = '';

  // Data
  pumps: PumpDto[] = [];
  tanks: TankDto[] = [];
  fuelTypeMap = new Map<string, string>(); // fuelTypeId → name

  // Fuel types derived from tanks (each tank maps to a fuel type)
  fuelTypes: { id: string; name: string }[] = [];
  selectedFuelType = '';
  selectedPumpId = '';
  requestVolume = 100;
  requestWindow = 'Next 24 hours';
  priority = 'Standard';
  windowOptions = ['Next 24 hours', 'Next 48 hours', 'Next Week', 'Scheduled'];

  // Active deployments
  activeRequests: ReplenishmentStatusDto[] = [];
  completedRequests: ReplenishmentStatusDto[] = [];

  isLoading = true;
  isSubmitting = false;

  statusSteps = ['Submitted', 'Approved', 'TankerAssigned', 'InTransit', 'Offloading', 'Complete'];

  constructor(
    private store: Store,
    private inventoryApi: InventoryApiService,
    private salesApi: SalesApiService,
    private stationsApi: StationsApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.store.select(selectUser).pipe(takeUntil(this.destroy$)).subscribe(user => {
      if (user) {
        this.stationId = user.profile?.stationId || '';
        this.loadData();
        // Auto-refresh every 10s for real-time updates
        interval(10000).pipe(
          takeUntil(this.destroy$),
          switchMap(() => this.fetchRequests())
        ).subscribe();
      }
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  private loadData(): void {
    if (!this.stationId) { this.isLoading = false; return; }
    this.isLoading = true;

    forkJoin({
      pumps: this.salesApi.getStationPumps(this.stationId).pipe(catchError(() => of([]))),
      tanks: this.inventoryApi.getTanks(this.stationId).pipe(catchError(() => of([]))),
      fuelTypesData: this.stationsApi.getFuelTypes().pipe(catchError(() => of([] as FuelTypeDto[]))),
    }).pipe(takeUntil(this.destroy$)).subscribe(({ pumps, tanks, fuelTypesData }) => {
      this.pumps = pumps;
      this.tanks = tanks;

      // Build master fuel type name map from station service
      this.fuelTypeMap.clear();
      fuelTypesData.forEach(ft => {
        this.fuelTypeMap.set(ft.id, ft.name);
      });

      // Derive selectable fuel types from tanks (each tank has one fuel type)
      const seen = new Set<string>();
      this.fuelTypes = [];
      tanks.forEach(t => {
        if (!seen.has(t.fuelTypeId)) {
          seen.add(t.fuelTypeId);
          this.fuelTypes.push({
            id: t.fuelTypeId,
            name: this.fuelTypeMap.get(t.fuelTypeId) || 'Unknown Fuel',
          });
        }
      });

      // Also add pump fuel types not already in tank list
      pumps.forEach(p => {
        if (!seen.has(p.fuelTypeId)) {
          seen.add(p.fuelTypeId);
          this.fuelTypes.push({
            id: p.fuelTypeId,
            name: this.fuelTypeMap.get(p.fuelTypeId) || 'Unknown Fuel',
          });
        }
      });

      if (this.fuelTypes.length > 0 && !this.selectedFuelType) {
        this.selectedFuelType = this.fuelTypes[0].id;
      }

      this.fetchRequests().subscribe();
    });
  }

  private fetchRequests() {
    return this.inventoryApi.getReplenishmentRequests(this.stationId).pipe(
      takeUntil(this.destroy$),
      catchError(() => of({ items: [] as ReplenishmentStatusDto[] }))
    ).pipe(
      switchMap(result => {
        const items = result?.items || [];
        this.activeRequests = items.filter(r => r.status !== 'Complete' && r.status !== 'Rejected');
        this.completedRequests = items.filter(r => r.status === 'Complete' || r.status === 'Rejected');
        this.isLoading = false;
        return of(items);
      })
    );
  }

  get filteredPumps(): PumpDto[] {
    return this.pumps.filter(p => p.fuelTypeId === this.selectedFuelType);
  }

  getSelectedFuelName(): string {
    return this.fuelTypes.find(f => f.id === this.selectedFuelType)?.name || '';
  }

  /** Get fuel name for a request */
  getFuelName(req: ReplenishmentStatusDto): string {
    if (req.fuelTypeName && req.fuelTypeName !== 'Fuel') return req.fuelTypeName;
    // Look up from tank → fuelTypeId → fuelTypeMap
    const tank = this.tanks.find(t => t.id === req.tankId);
    if (tank) {
      const name = this.fuelTypeMap.get(tank.fuelTypeId);
      if (name) return name;
    }
    return req.fuelTypeName || 'Fuel';
  }

  submitRequest(): void {
    if (!this.selectedFuelType) { this.toast.error('Please select a fuel type.'); return; }
    if (!this.selectedPumpId) { this.toast.error('Please select a target pump.'); return; }
    if (this.requestVolume < 10) { this.toast.error('Minimum request volume is 10 litres.'); return; }

    this.isSubmitting = true;

    const selectedPump = this.pumps.find(p => p.id === this.selectedPumpId);
    const fuelName = this.getSelectedFuelName();

    // Find matching tank by fuelTypeId
    const matchingTank = this.tanks.find(t => t.fuelTypeId === this.selectedFuelType);
    if (!matchingTank) { this.toast.error('No matching tank found for this fuel type.'); this.isSubmitting = false; return; }

    this.inventoryApi.createReplenishmentRequest({
      stationId: this.stationId,
      tankId: matchingTank.id,
      requestedQuantityLitres: this.requestVolume,
      urgencyLevel: this.priority === 'Urgent' ? 'Critical' : this.priority === 'High' ? 'High' : 'Normal',
      notes: `Pump: ${selectedPump?.pumpName || 'N/A'}`,
      targetPumpName: selectedPump?.pumpName || '',
      fuelTypeName: fuelName,
      priority: this.priority,
      requestedWindow: this.requestWindow,
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toast.success('Replenishment request submitted successfully!');
        this.isSubmitting = false;
        this.requestVolume = 100;
        this.fetchRequests().subscribe();
      },
      error: (err) => {
        this.toast.error(err?.error?.message || err?.error?.detail || 'Failed to submit request.');
        this.isSubmitting = false;
      },
    });
  }

  getStepIndex(status: string): number {
    return this.statusSteps.indexOf(status);
  }

  getStatusLabel(status: string): string {
    const labels: Record<string, string> = {
      Submitted: 'Pending Review', Approved: 'Approved', TankerAssigned: 'Tanker Assigned',
      InTransit: 'In Transit', Offloading: 'Offloading', Complete: 'Complete', Rejected: 'Rejected',
    };
    return labels[status] || status;
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Complete': return 'status-success';
      case 'Rejected': return 'status-danger';
      case 'Submitted': return 'status-pending';
      case 'InTransit': case 'Offloading': return 'status-active';
      case 'Approved': case 'TankerAssigned': return 'status-info';
      default: return '';
    }
  }

  getPriorityClass(p: string): string {
    switch (p) {
      case 'Urgent': return 'priority-urgent';
      case 'High': return 'priority-high';
      default: return 'priority-standard';
    }
  }

  trackById(_: number, item: ReplenishmentStatusDto): string { return item.id; }
}
