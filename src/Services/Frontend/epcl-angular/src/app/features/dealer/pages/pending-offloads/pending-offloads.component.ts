import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil, catchError, of, forkJoin, interval, switchMap } from 'rxjs';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { InventoryApiService, ReplenishmentStatusDto, TankDto } from '../../../../core/services/inventory-api.service';
import { StationsApiService, FuelTypeDto } from '../../../../core/services/stations-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-pending-offloads',
  templateUrl: './pending-offloads.component.html',
  styleUrls: ['./pending-offloads.component.scss'],
  standalone: false
})
export class PendingOffloadsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private stationId = '';

  offloadRequests: ReplenishmentStatusDto[] = [];
  fuelTypeMap = new Map<string, string>(); // fuelTypeId → name
  tanks: TankDto[] = [];

  // Offloading Verification Modal
  showOffloadModal = false;
  offloadOrderId = '';
  offloadDriverCode = '';
  offloadingRequest: ReplenishmentStatusDto | null = null;
  isVerifying = false;

  isLoading = true;

  constructor(
    private store: Store,
    private inventoryApi: InventoryApiService,
    private stationsApi: StationsApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.store.select(selectUser).pipe(takeUntil(this.destroy$)).subscribe(user => {
      if (user) {
        this.stationId = user.profile?.stationId || '';
        this.loadData();
        // Real-time polling every 10s
        interval(10000).pipe(
          takeUntil(this.destroy$),
          switchMap(() => this.fetchOffloadRequests())
        ).subscribe();
      }
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  loadData(): void {
    if (!this.stationId) return;
    this.isLoading = true;

    forkJoin({
      tanks: this.inventoryApi.getTanks(this.stationId).pipe(catchError(() => of([]))),
      fuelTypesData: this.stationsApi.getFuelTypes().pipe(catchError(() => of([] as FuelTypeDto[]))),
      requests: this.inventoryApi.getReplenishmentRequests(this.stationId).pipe(catchError(() => of({ items: [] as ReplenishmentStatusDto[] }))),
    }).pipe(takeUntil(this.destroy$)).subscribe(({ tanks, fuelTypesData, requests }) => {
      this.tanks = tanks;
      this.fuelTypeMap.clear();
      fuelTypesData.forEach(ft => {
        this.fuelTypeMap.set(ft.id, ft.name);
      });

      const items = requests?.items || [];
      this.offloadRequests = items.filter(r => r.status === 'Offloading' && !r.dealerVerifiedAt);

      this.isLoading = false;
    });
  }

  private fetchOffloadRequests() {
    return this.inventoryApi.getReplenishmentRequests(this.stationId).pipe(
      takeUntil(this.destroy$),
      catchError(() => of({ items: [] as ReplenishmentStatusDto[] }))
    ).pipe(
      switchMap(result => {
        const items = result?.items || [];
        this.offloadRequests = items.filter(r => r.status === 'Offloading' && !r.dealerVerifiedAt);
        return of(null);
      })
    );
  }

  getReqFuelName(req: ReplenishmentStatusDto): string {
    if (req.fuelTypeName && req.fuelTypeName !== 'Fuel' && req.fuelTypeName !== 'Unknown Fuel') {
      return req.fuelTypeName;
    }
    const tank = this.tanks.find(t => t.id === req.tankId);
    if (tank) {
      return this.fuelTypeMap.get(tank.fuelTypeId) || 'Unknown Fuel';
    }
    return req.fuelTypeName || 'Fuel';
  }

  openOffloadModal(req: ReplenishmentStatusDto): void {
    this.showOffloadModal = true;
    this.offloadOrderId = req.orderNumber || '';
    this.offloadDriverCode = '';
    this.offloadingRequest = req;
  }

  closeOffloadModal(): void {
    this.showOffloadModal = false;
    this.offloadOrderId = '';
    this.offloadDriverCode = '';
    this.offloadingRequest = null;
  }

  verifyOffloading(): void {
    if (!this.offloadingRequest) { this.toast.error('No request selected.'); return; }
    if (!this.offloadDriverCode.trim()) { this.toast.error('Please enter the Driver ID code.'); return; }

    this.isVerifying = true;
    this.inventoryApi.verifyOffloading(
      this.offloadingRequest.id,
      this.offloadOrderId.trim(),
      this.offloadDriverCode.trim()
    ).pipe(takeUntil(this.destroy$)).subscribe({
      next: (res) => {
        this.toast.success(res?.message || 'Offloading verified successfully!');
        this.isVerifying = false;
        this.closeOffloadModal();
        this.loadData();
      },
      error: (err) => {
        this.toast.error(err?.error?.message || err?.error?.detail || 'Verification failed.');
        this.isVerifying = false;
      },
    });
  }
}
