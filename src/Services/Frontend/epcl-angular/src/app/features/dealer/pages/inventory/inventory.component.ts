import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil, catchError, of, forkJoin, interval, switchMap } from 'rxjs';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { InventoryApiService, TankDto, ReplenishmentStatusDto, StockLoadingDto } from '../../../../core/services/inventory-api.service';
import { SalesApiService, PumpDto } from '../../../../core/services/sales-api.service';
import { StationsApiService, FuelTypeDto } from '../../../../core/services/stations-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

interface DisplayTank {
  id: string;
  serialNumber: string;
  fuelTypeId: string;
  fuelName: string;
  status: string;
  statusClass: string;
  current: number;
  capacity: number;
  available: number;
  reserved: number;
  pct: number;
  color: string;
  pumps: PumpDto[];
}

@Component({
  selector: 'app-inventory',
  templateUrl: './inventory.component.html',
  styleUrls: ['./inventory.component.scss'],
})
export class InventoryComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private stationId = '';

  totalCapacity = 0;
  currentLoadout = 0;
  utilizationPct = 0;
  tankCount = 0;

  tanks: DisplayTank[] = [];
  pumps: PumpDto[] = [];
  fuelTypeMap = new Map<string, string>(); // fuelTypeId → name

  // Offloading Verification
  showOffloadModal = false;
  offloadOrderId = '';
  offloadDriverCode = '';
  offloadingRequest: ReplenishmentStatusDto | null = null;
  offloadRequests: ReplenishmentStatusDto[] = [];
  isVerifying = false;

  // Loading Log (aggregated from all tanks)
  loadingLog: StockLoadingDto[] = [];

  isLoading = true;

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
        this.loadInventory();
        // Real-time polling every 10s
        interval(10000).pipe(
          takeUntil(this.destroy$),
          switchMap(() => this.fetchOffloadRequests())
        ).subscribe();
      }
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  loadInventory(): void {
    if (!this.stationId) return;
    this.isLoading = true;

    forkJoin({
      tanks: this.inventoryApi.getTanks(this.stationId).pipe(catchError(() => of([]))),
      pumps: this.salesApi.getStationPumps(this.stationId).pipe(catchError(() => of([]))),
      fuelTypesData: this.stationsApi.getFuelTypes().pipe(catchError(() => of([] as FuelTypeDto[]))),
      requests: this.inventoryApi.getReplenishmentRequests(this.stationId).pipe(catchError(() => of({ items: [] as ReplenishmentStatusDto[] }))),
    }).pipe(takeUntil(this.destroy$)).subscribe(({ tanks, pumps, fuelTypesData, requests }) => {
      this.pumps = pumps;
      this.totalCapacity = 0;
      this.currentLoadout = 0;

      // Build fuel name map from station service
      this.fuelTypeMap.clear();
      fuelTypesData.forEach(ft => {
        this.fuelTypeMap.set(ft.id, ft.name);
      });

      this.tanks = tanks.map(t => {
        this.totalCapacity += t.capacityLitres;
        this.currentLoadout += t.currentStockLitres;

        const pct = t.capacityLitres > 0 ? Math.round((t.currentStockLitres / t.capacityLitres) * 100) : 0;
        let statusStr = 'OPTIMAL';
        let statusClass = 'optimal';
        let color = '#10b981';

        if (pct < 15) { statusStr = 'CRITICAL'; statusClass = 'critical'; color = '#ef4444'; }
        else if (pct < 30) { statusStr = 'LOW'; statusClass = 'warning'; color = '#f59e0b'; }

        if (t.status !== 'Available') { statusStr = 'OFFLINE'; statusClass = 'offline'; color = '#94a3b8'; }

        const tankPumps = pumps.filter(p => p.fuelTypeId === t.fuelTypeId);
        const fuelName = this.fuelTypeMap.get(t.fuelTypeId) || 'Unknown Fuel';

        return {
          id: t.id,
          serialNumber: t.tankSerialNumber || `TANK-${t.id.substring(0, 4).toUpperCase()}`,
          fuelTypeId: t.fuelTypeId,
          fuelName,
          status: statusStr,
          statusClass,
          current: t.currentStockLitres,
          capacity: t.capacityLitres,
          available: t.availableStock,
          reserved: t.reservedLitres,
          pct,
          color,
          pumps: tankPumps,
        };
      });

      this.tankCount = this.tanks.length;
      this.utilizationPct = this.totalCapacity > 0 ? Math.round((this.currentLoadout / this.totalCapacity) * 100) : 0;

      // Get offloadable requests
      const items = requests?.items || [];
      this.offloadRequests = items.filter(r => r.status === 'Offloading' && !r.dealerVerifiedAt);

      // Load stock loading history for ALL tanks, then merge + sort by timestamp
      if (this.tanks.length > 0) {
        const loadingCalls = this.tanks.map(t =>
          this.inventoryApi.getStockLoadingHistory(t.id, 1, 10).pipe(catchError(() => of([])))
        );
        forkJoin(loadingCalls).pipe(takeUntil(this.destroy$)).subscribe(results => {
          const allLoadings: StockLoadingDto[] = [];
          results.forEach(logs => allLoadings.push(...logs));
          // Sort by timestamp descending, take most recent 20
          allLoadings.sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime());
          this.loadingLog = allLoadings.slice(0, 20);
        });
      }

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

  /** Look up fuel name for a replenishment request */
  getReqFuelName(req: ReplenishmentStatusDto): string {
    // If the stored fuelTypeName is a real name (not the generic fallback), use it
    if (req.fuelTypeName && req.fuelTypeName !== 'Fuel' && req.fuelTypeName !== 'Unknown Fuel') {
      return req.fuelTypeName;
    }
    // Otherwise look up from tank → fuelTypeId → fuelTypeMap
    const tank = this.tanks.find(t => t.id === req.tankId);
    if (tank) return tank.fuelName;
    return req.fuelTypeName || 'Fuel';
  }

  openOffloadModal(req?: ReplenishmentStatusDto): void {
    this.showOffloadModal = true;
    this.offloadOrderId = req?.orderNumber || '';
    this.offloadDriverCode = '';
    this.offloadingRequest = req || null;
  }

  closeOffloadModal(): void {
    this.showOffloadModal = false;
    this.offloadOrderId = '';
    this.offloadDriverCode = '';
    this.offloadingRequest = null;
  }

  lookupOrder(): void {
    if (!this.offloadOrderId.trim()) { this.toast.error('Please enter the Order ID.'); return; }
    const found = this.offloadRequests.find(r => r.orderNumber.toLowerCase() === this.offloadOrderId.trim().toLowerCase());
    if (found) {
      this.offloadingRequest = found;
    } else {
      this.toast.error('No offloading request found with this Order ID.');
      this.offloadingRequest = null;
    }
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
        this.loadInventory(); // Refresh everything to show updated stock levels + new loading log
      },
      error: (err) => {
        this.toast.error(err?.error?.message || err?.error?.detail || 'Verification failed.');
        this.isVerifying = false;
      },
    });
  }
}
