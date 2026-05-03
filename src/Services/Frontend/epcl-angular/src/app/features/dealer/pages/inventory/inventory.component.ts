import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil, catchError, of, forkJoin, interval } from 'rxjs';
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
  lastRefresh = '';

  tanks: DisplayTank[] = [];
  pumps: PumpDto[] = [];
  fuelTypeMap = new Map<string, string>();

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

        // Auto-refresh every 15 seconds to catch inventory changes from sales
        interval(15000).pipe(takeUntil(this.destroy$)).subscribe(() => {
          this.loadInventory();
        });
      }
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  loadInventory(): void {
    if (!this.stationId) return;
    // Don't show full loading spinner on refresh — only on first load
    if (this.tanks.length === 0) this.isLoading = true;

    forkJoin({
      tanks: this.inventoryApi.getTanks(this.stationId).pipe(catchError(() => of([]))),
      pumps: this.salesApi.getStationPumps(this.stationId).pipe(catchError(() => of([]))),
      fuelTypesData: this.stationsApi.getFuelTypes().pipe(catchError(() => of([] as FuelTypeDto[]))),
    }).pipe(takeUntil(this.destroy$)).subscribe(({ tanks, pumps, fuelTypesData }) => {
      this.pumps = pumps;
      this.totalCapacity = 0;
      this.currentLoadout = 0;

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
      this.lastRefresh = new Date().toLocaleTimeString();

      // Load stock loading history for ALL tanks
      if (this.tanks.length > 0) {
        const loadingCalls = this.tanks.map(t =>
          this.inventoryApi.getStockLoadingHistory(t.id, 1, 10).pipe(catchError(() => of([])))
        );
        forkJoin(loadingCalls).pipe(takeUntil(this.destroy$)).subscribe(results => {
          const allLoadings: StockLoadingDto[] = [];
          results.forEach(logs => allLoadings.push(...logs));
          allLoadings.sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime());
          this.loadingLog = allLoadings.slice(0, 20);
        });
      }

      this.isLoading = false;
    });
  }
}
