import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil, interval, combineLatest, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { ReportsApiService } from '../../../../core/services/reports-api.service';
import { StationsApiService, FuelTypeDto } from '../../../../core/services/stations-api.service';
import { SalesApiService, TransactionDto, PumpDto } from '../../../../core/services/sales-api.service';
import { SignalRService } from '../../../../core/services/signalr.service';
import { InventoryApiService } from '../../../../core/services/inventory-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

interface DisplayTank {
  id: string; name: string; pct: number; current: number; capacity: number; unit: string; color: string; daysUntilEmpty?: number | null;
}

interface DisplayPump {
  id: string; pumpName: string; fuelTypeName: string; fuelTypeId: string; status: string; isActive: boolean; updating: boolean;
}

interface DisplayTransaction {
  id: string; volume: string; product: string; amount: string; pump: string; payment: string; status: string; time: string;
}

@Component({
  selector: 'app-dealer-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DealerDashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private stationId = '';
  stationName = 'My Station';
  stationCode = '';
  hasStation = false;

  lastSync = new Date().toLocaleTimeString();
  kpis: { label: string; value: string; sub: string; color: string }[] = [];
  tanks: DisplayTank[] = [];
  pumps: DisplayPump[] = [];
  transactions: DisplayTransaction[] = [];
  fuelTypes: FuelTypeDto[] = [];
  private fuelTypeMap = new Map<string, string>();

  // Add pump form
  showAddPump = false;
  newPumpName = '';
  newPumpFuelTypeId = '';
  isAddingPump = false;

  constructor(
    private store: Store,
    private reportsApi: ReportsApiService,
    private stationsApi: StationsApiService,
    private salesApi: SalesApiService,
    private inventoryApi: InventoryApiService,
    private signalR: SignalRService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.store.select(selectUser).pipe(takeUntil(this.destroy$)).subscribe(user => {
      if (user) {
        this.stationId = user.profile?.stationId || '';
        this.hasStation = !!this.stationId;
        if (this.hasStation) this.loadDashboard();
      }
    });

    interval(30000).pipe(takeUntil(this.destroy$)).subscribe(() => {
      this.lastSync = new Date().toLocaleTimeString();
      if (this.stationId) this.loadDashboard();
    });

    this.signalR.stockLevelCritical$.pipe(takeUntil(this.destroy$)).subscribe(() => {
      if (this.stationId) this.loadTanks();
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  private loadDashboard(): void {
    if (!this.stationId) return;
    this.stationsApi.getStationById(this.stationId).pipe(takeUntil(this.destroy$), catchError(() => of(null))).subscribe(s => {
      this.stationName = (s as any)?.stationName || (s as any)?.name || 'My Station';
      this.stationCode = (s as any)?.stationCode || (s as any)?.code || '';
    });
    this.stationsApi.getFuelTypes().pipe(takeUntil(this.destroy$), catchError(() => of([]))).subscribe(fts => {
      this.fuelTypes = fts;
      fts.forEach(ft => this.fuelTypeMap.set(ft.id, ft.name));
      this.loadPumps();
      this.loadTransactions();
    });
    this.loadKpi();
    this.loadTanks();
  }

  private loadKpi(): void {
    this.reportsApi.getDealerKpi(this.stationId).pipe(takeUntil(this.destroy$), catchError(() => of({ revenueToday: 0, transactionsToday: 0, litresToday: 0, revenueThisMonth: 0 }))).subscribe((kpi: any) => {
      this.kpis = [
        { label: 'TODAY REVENUE', value: `₹${(kpi.revenueToday || 0).toLocaleString()}`, sub: 'Today', color: '#2563eb' },
        { label: 'TRANSACTIONS', value: String(kpi.transactionsToday || 0), sub: 'Processed today', color: '#7c3aed' },
        { label: 'LITRES SOLD', value: `${(kpi.litresToday || 0).toLocaleString()} L`, sub: 'Total volume', color: '#059669' },
        { label: 'MONTHLY REV', value: `₹${(kpi.revenueThisMonth || 0).toLocaleString()}`, sub: 'This month', color: '#ea580c' },
      ];
    });
  }

  private loadTanks(): void {
    combineLatest([
      this.inventoryApi.getTanks(this.stationId).pipe(catchError(() => of([]))),
      this.reportsApi.getStockPredictions(this.stationId).pipe(catchError(() => of([])))
    ]).pipe(takeUntil(this.destroy$)).subscribe(([tanks, predictions]) => {
      this.tanks = tanks.map((t: any) => {
        const pred = predictions.find((p: any) => p.tankId === t.id);
        const pct = t.capacityLitres > 0 ? Math.round((t.currentStockLitres / t.capacityLitres) * 100) : 0;
        let color = '#3b82f6';
        if (pct < 15) color = '#ef4444';
        else if (pct < 30) color = '#f59e0b';
        return { id: t.id, name: t.fuelTypeName || 'Unknown', pct, current: t.currentStockLitres, capacity: t.capacityLitres, unit: 'L', color, daysUntilEmpty: pred?.daysUntilEmpty };
      });
    });
  }

  private loadPumps(): void {
    this.salesApi.getStationPumps(this.stationId).pipe(takeUntil(this.destroy$), catchError(() => of([]))).subscribe((result: PumpDto[]) => {
      this.pumps = result.map(p => ({
        id: p.id, pumpName: p.pumpName || 'Unknown Pump',
        fuelTypeName: this.fuelTypeMap.get(p.fuelTypeId) || 'Fuel', fuelTypeId: p.fuelTypeId,
        status: p.status, isActive: p.status === 'Active', updating: false,
      }));
    });
  }

  private loadTransactions(): void {
    this.salesApi.getStationTransactions(this.stationId, 1, 5).pipe(takeUntil(this.destroy$), catchError(() => of({ items: [], totalCount: 0, page: 1, pageSize: 5, totalPages: 0 }))).subscribe((result: any) => {
      this.transactions = result.items.map((t: TransactionDto) => ({
        id: t.receiptNumber || t.id.substring(0, 8),
        volume: `${t.quantityLitres.toFixed(2)} L`,
        product: this.fuelTypeMap.get(t.fuelTypeId) || t.fuelTypeName || 'Fuel',
        amount: `₹${t.totalAmount.toFixed(2)}`,
        pump: 'Pump',
        payment: t.paymentMethod,
        status: t.status,
        time: new Date(t.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
      }));
    });
  }

  // ═══ Pump Management ═══
  togglePumpStatus(pump: DisplayPump): void {
    if (pump.updating) return;
    pump.updating = true;
    const newStatus = pump.isActive ? 'UnderMaintenance' : 'Active';
    this.salesApi.updatePumpStatus(pump.id, newStatus).pipe(takeUntil(this.destroy$), catchError(err => {
      this.toast.error(err?.error?.message || 'Failed to update pump status.');
      pump.updating = false;
      return of(null);
    })).subscribe(result => {
      if (result) {
        pump.status = newStatus;
        pump.isActive = newStatus === 'Active';
        this.toast.success(`${pump.pumpName} is now ${pump.isActive ? 'Active' : 'Under Maintenance'}`);
      }
      pump.updating = false;
    });
  }

  openAddPump(): void {
    this.showAddPump = true;
    this.newPumpName = '';
    this.newPumpFuelTypeId = this.fuelTypes.length > 0 ? this.fuelTypes[0].id : '';
  }

  closeAddPump(): void { this.showAddPump = false; }

  addPump(): void {
    if (!this.newPumpName.trim() || !this.newPumpFuelTypeId) {
      this.toast.error('Please enter pump name and select fuel type.');
      return;
    }
    this.isAddingPump = true;
    this.salesApi.createPump(this.stationId, this.newPumpFuelTypeId, this.newPumpName.trim(), 1).pipe(
      takeUntil(this.destroy$),
      catchError(err => { this.toast.error(err?.error?.message || 'Failed to create pump.'); this.isAddingPump = false; return of(null); })
    ).subscribe(result => {
      if (result) {
        this.toast.success(`Pump "${this.newPumpName}" created successfully!`);
        this.showAddPump = false;
        this.loadPumps();
      }
      this.isAddingPump = false;
    });
  }

  deletePump(pump: DisplayPump): void {
    if (!confirm(`Delete "${pump.pumpName}"? This cannot be undone.`)) return;
    pump.updating = true;
    this.salesApi.deletePump(pump.id).pipe(
      takeUntil(this.destroy$),
      catchError(err => { this.toast.error(err?.error?.message || 'Failed to delete pump.'); pump.updating = false; return of(null); })
    ).subscribe(() => {
      this.toast.success(`${pump.pumpName} deleted.`);
      this.pumps = this.pumps.filter(p => p.id !== pump.id);
    });
  }

  get activePumpCount(): number { return this.pumps.filter(p => p.isActive).length; }
  get maintenancePumpCount(): number { return this.pumps.filter(p => !p.isActive).length; }
}
