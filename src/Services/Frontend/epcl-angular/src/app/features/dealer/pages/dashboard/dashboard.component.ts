import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil, interval, combineLatest } from 'rxjs';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { ReportsApiService, DealerKpiDto, StockPredictionDto } from '../../../../core/services/reports-api.service';
import { StationsApiService } from '../../../../core/services/stations-api.service';
import { SalesApiService, TransactionDto, PumpDto } from '../../../../core/services/sales-api.service';
import { SignalRService } from '../../../../core/services/signalr.service';
import { InventoryApiService, TankDto } from '../../../../core/services/inventory-api.service';

interface DisplayTank {
  id: string;
  name: string;
  pct: number;
  current: number;
  capacity: number;
  unit: string;
  color: string;
  daysUntilEmpty?: number | null;
  predictedEmptyAt?: string | null;
}

interface DisplayPump {
  id: string;
  icon: string;
  status: string;
  volume: string;
  isMaintenance: boolean;
}

interface DisplayTransaction {
  id: string;
  volume: string;
  product: string;
  amount: string;
  pump: string;
  time: string;
}

@Component({
  selector: 'app-dealer-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DealerDashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private stationId = '';

  lastSync = new Date().toLocaleTimeString();
  ambientTemp = 28;

  kpis: { label: string; value: string; icon: string; trend: string; trendClass: string }[] = [];
  tanks: DisplayTank[] = [];
  pumps: DisplayPump[] = [];
  transactions: DisplayTransaction[] = [];

  constructor(
    private store: Store,
    private reportsApi: ReportsApiService,
    private stationsApi: StationsApiService,
    private salesApi: SalesApiService,
    private inventoryApi: InventoryApiService,
    private signalR: SignalRService
  ) {}

  ngOnInit(): void {
    this.store.select(selectUser).pipe(takeUntil(this.destroy$)).subscribe(user => {
      if (user) {
        this.stationId = user.profile?.stationId || '';
        this.loadDashboard();
      }
    });

    // Refresh every 30s
    interval(30000).pipe(takeUntil(this.destroy$)).subscribe(() => {
      this.lastSync = new Date().toLocaleTimeString();
      if (this.stationId) { this.loadDashboard(); }
    });

    // SignalR updates
    this.signalR.stockLevelCritical$.pipe(takeUntil(this.destroy$)).subscribe((alert: any) => {
      // Could show toast or refresh tanks
      if (this.stationId) this.loadTanks();
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  private loadDashboard(): void {
    if (!this.stationId) return;
    this.loadKpi();
    this.loadTanks();
    this.loadPumps();
    this.loadTransactions();
  }

  private loadKpi(): void {
    this.reportsApi.getDealerKpi(this.stationId).pipe(takeUntil(this.destroy$)).subscribe(kpi => {
      this.kpis = [
        { label: 'GROSS REVENUE', value: `₹${kpi.revenueToday.toLocaleString()}`, icon: 'dollar-sign', trend: 'Today', trendClass: 'up' },
        { label: 'TRANSACTIONS', value: String(kpi.transactionsToday), icon: 'clipboard-list', trend: 'Processed', trendClass: '' },
        { label: 'LITRES SOLD', value: `${kpi.litresToday.toLocaleString()} L`, icon: 'fuel', trend: 'Total volume', trendClass: '' },
        { label: 'MONTHLY REV', value: `₹${kpi.revenueThisMonth.toLocaleString()}`, icon: 'bar-chart', trend: 'This month', trendClass: 'up' },
      ];
    });
  }

  private loadTanks(): void {
    combineLatest([
      this.inventoryApi.getTanks(this.stationId),
      this.reportsApi.getStockPredictions(this.stationId)
    ]).pipe(takeUntil(this.destroy$)).subscribe(([tanks, predictions]) => {
      this.tanks = tanks.map((t) => {
        const pred = predictions.find(p => p.tankId === t.id);
        const pct = Math.round((t.currentStockLitres / t.capacityLitres) * 100);
        let color = '#3b82f6';
        if (pct < (t.criticalThresholdLitres / t.capacityLitres) * 100) color = '#ef4444';
        else if (pct < (t.minThresholdLitres / t.capacityLitres) * 100) color = '#f59e0b';
        
        return {
          id: t.id,
          name: t.fuelTypeName,
          pct: pct,
          current: t.currentStockLitres,
          capacity: t.capacityLitres,
          unit: 'L',
          color: color,
          daysUntilEmpty: pred?.daysUntilEmpty,
          predictedEmptyAt: pred?.predictedEmptyAt
        };
      });
    });
  }

  private loadPumps(): void {
    this.salesApi.getStationPumps(this.stationId).pipe(takeUntil(this.destroy$)).subscribe(result => {
      this.pumps = result.map((p, i) => ({
        id: p.pumpName || `PUMP-${String(i + 1).padStart(2, '0')}`,
        icon: 'fuel',
        status: p.status.toUpperCase(),
        volume: '0.00',
        isMaintenance: p.status === 'Maintenance',
      }));
    });
  }

  private loadTransactions(): void {
    this.salesApi.getStationTransactions(this.stationId, 1, 5).pipe(takeUntil(this.destroy$)).subscribe(result => {
      this.transactions = result.items.map(t => {
        const timeStr = new Date(t.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        return {
          id: t.receiptNumber || t.id.substring(0, 8),
          volume: `${t.quantityLitres.toFixed(2)} L`,
          product: t.fuelTypeName || 'Fuel',
          amount: `₹${t.totalAmount.toFixed(2)}`,
          pump: `PUMP-${t.pumpId.substring(0, 2)}`,
          time: timeStr,
        };
      });
    });
  }
}
