import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil, interval, startWith } from 'rxjs';
import { ReportsApiService, AdminKpiDto, SalesSummaryDto, StockPredictionDto } from '../../../../core/services/reports-api.service';
import { FraudApiService, FraudAlertDto } from '../../../../core/services/fraud-api.service';
import { StationsApiService } from '../../../../core/services/stations-api.service';
import { SignalRService } from '../../../../core/services/signalr.service';
import { HealthApiService } from '../../../../core/services/health-api.service';

@Component({
  selector: 'app-admin-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class AdminDashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  kpi: AdminKpiDto | null = null;
  salesSummary: SalesSummaryDto | null = null;
  recentAlerts: FraudAlertDto[] = [];
  isLoading = true;
  lastUpdated = new Date();

  // KPI cards — populated from API
  kpis: { label: string; icon: string; value: string; unit: string; sub: string; trend: string; trendClass: string }[] = [];

  // Station map nodes — populated from API
  mapNodes: { name: string; status: string }[] = [];

  // Fraud alerts — populated from API
  fraudAlerts: { severity: string; time: string; title: string; desc: string; meta: string; action: string }[] = [];

  // Volume chart — populated from API
  volumeChartBars: number[] = [];

  // Stock Risk — populated from API
  atRiskTanks: StockPredictionDto[] = [];

  // System health — populated from API
  systemNodes: { name: string; value: string; status: string }[] = [];

  constructor(
    private reportsApi: ReportsApiService,
    private fraudApi: FraudApiService,
    private stationsApi: StationsApiService,
    private signalR: SignalRService,
    private healthApi: HealthApiService
  ) {}

  ngOnInit(): void {
    this.loadDashboard();

    // Refresh every 60s
    interval(60000).pipe(startWith(0), takeUntil(this.destroy$)).subscribe(() => {
      this.lastUpdated = new Date();
    });

    // SignalR live fraud alerts
    this.signalR.newFraudAlert$.pipe(takeUntil(this.destroy$)).subscribe(alert => {
      const mapped: FraudAlertDto = {
        id: alert.id,
        transactionId: alert.transactionId,
        stationId: alert.stationId,
        stationName: `Station ${alert.stationId.substring(0, 4)}`,
        ruleTriggered: alert.ruleTriggered,
        severity: alert.severity,
        status: 'Open',
        description: alert.description,
        createdAt: alert.createdAt,
      };
      this.recentAlerts.unshift(mapped);
      if (this.recentAlerts.length > 5) this.recentAlerts.pop();
      if (this.kpi) this.kpi.fraudAlertsToday++;
      this.mapFraudAlerts();
    });

    // SignalR stock critical
    this.signalR.stockLevelCritical$.pipe(takeUntil(this.destroy$)).subscribe(() => this.loadDashboard());
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  private loadDashboard(): void {
    // Load KPIs
    this.reportsApi.getAdminKpi().pipe(takeUntil(this.destroy$)).subscribe({
      next: (kpi) => {
        this.kpi = kpi;
        this.kpis = [
          { label: 'TOTAL STATIONS', icon: 'store', value: String(kpi.totalStations), unit: '', sub: 'Online', trend: 'STABLE', trendClass: 'stable' },
          { label: 'TODAY REVENUE', icon: 'dollar-sign', value: `₹${kpi.totalRevenueToday.toLocaleString()}`, unit: '', sub: 'Revenue', trend: '+12.4%', trendClass: 'up' },
          { label: 'LITRES SOLD', icon: 'fuel', value: kpi.totalLitresToday.toLocaleString(), unit: 'L', sub: 'Volume', trend: 'Today', trendClass: '' },
          { label: 'FRAUD ALERTS', icon: 'alert-triangle', value: String(kpi.fraudAlertsToday), unit: '', sub: 'Today', trend: kpi.fraudAlertsToday > 0 ? 'REVIEW' : 'CLEAR', trendClass: kpi.fraudAlertsToday > 0 ? 'warn' : 'up' },
          { label: 'TRANSACTIONS', icon: 'clipboard-list', value: String(kpi.totalTransactionsToday), unit: '', sub: 'Today', trend: 'Processed', trendClass: '' },
          { label: 'ACTIVE DEALERS', icon: 'users', value: String(kpi.activeDealers), unit: '', sub: 'Registered', trend: 'GROWING', trendClass: 'up' },
        ];
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; },
    });

    // Load sales summary for chart
    const today = new Date().toISOString().split('T')[0];
    this.reportsApi.getSalesSummary({ dateFrom: today, dateTo: today, groupBy: 'hour' })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (s) => {
          this.salesSummary = s;
          // Map summary data to chart bars (12 items for 12 hours)
          if (s.data && s.data.length > 0) {
            this.volumeChartBars = s.data.map(d => d.value);
          } else {
            this.volumeChartBars = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
          }
        },
      });

    // Load fraud alerts
    this.fraudApi.getAlerts(1, 5, { status: 'Open' }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (r) => {
          this.recentAlerts = r.items;
          this.mapFraudAlerts();
        },
      });

    // Load stations for map
    this.stationsApi.getStations(1, 50).pipe(takeUntil(this.destroy$)).subscribe({
      next: (result) => {
        this.mapNodes = result.items.map((s: any) => ({
          name: s.name || s.stationCode,
          status: s.isActive ? 'ok' : 'down',
        }));
      },
    });

    // Load system health
    this.loadSystemHealth();

    // Load at-risk tanks
    this.reportsApi.GetAtRiskStockPredictions(7).pipe(takeUntil(this.destroy$)).subscribe({
      next: (preds) => {
        this.atRiskTanks = preds;
      }
    });
  }

  private mapFraudAlerts(): void {
    this.fraudAlerts = this.recentAlerts.map(a => ({
      severity: a.severity === 'High' ? 'CRITICAL' : 'WARN',
      time: new Date(a.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' }),
      title: a.ruleTriggered,
      desc: a.description || `Fraud rule triggered at station`,
      meta: `Alert ID: ${a.id.substring(0, 8)}`,
      action: a.status === 'Open' ? 'REVIEW' : a.status,
    }));
  }

  private loadSystemHealth(): void {
    this.systemNodes = [
      { name: 'Loading...', value: 'CHECKING', status: 'warn' },
    ];

    this.healthApi.checkAllServices().pipe(takeUntil(this.destroy$)).subscribe({
      next: (results) => {
        this.systemNodes = results.map(r => ({
          name: r.name,
          value: r.status === 'Healthy' ? 'ONLINE' : 'OFFLINE',
          status: r.status === 'Healthy' ? 'ok' : 'down',
        }));
      },
      error: () => {
        this.systemNodes = [{ name: 'Health Check', value: 'FAILED', status: 'down' }];
      },
    });
  }

  getSeverityClass(severity: string): string {
    return severity === 'CRITICAL' ? 'severity-critical' : 'severity-warn';
  }
}
