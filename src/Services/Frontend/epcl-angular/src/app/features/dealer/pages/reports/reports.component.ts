import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil, catchError, of, timeout, forkJoin } from 'rxjs';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { ReportsApiService, DealerKpiDto, SalesSummaryDto } from '../../../../core/services/reports-api.service';
import { SalesApiService } from '../../../../core/services/sales-api.service';
import { StationsApiService, FuelTypeDto } from '../../../../core/services/stations-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-dealer-reports',
  templateUrl: './reports.component.html',
  styleUrls: ['./reports.component.scss'],
})
export class DealerReportsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private stationId = '';

  lastSync = new Date().toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit' });
  periods = ['Today', '7 Days', '30 Days'];
  period = '7 Days';

  // KPI data
  grossRevenue = 0;
  transactionsCount = 0;
  litresSold = 0;
  monthlyRevenue = 0;

  // Revenue chart — from real daily sales data
  revenueChartBars: { label: string; value: number; height: number }[] = [];
  chartMaxValue = 0;

  // Volume by grade — grouped by fuel type, NOT by date
  volumeByGrade: { name: string; value: string; pct: number; color: string }[] = [];
  topGrade = '';

  // Transaction log
  transactions: { date: string; ref: string; pump: string; fuelType: string; volume: string; amount: string; status: string }[] = [];

  isLoading = true;

  // Maps for resolving pump & fuel type names
  private pumpNameMap = new Map<string, string>();
  private pumpFuelTypeIdMap = new Map<string, string>();
  private fuelTypeNameMap = new Map<string, string>();

  private gradeColors = ['#1E40AF', '#10b981', '#f59e0b', '#6366f1', '#ef4444', '#8b5cf6', '#06b6d4', '#ec4899'];

  constructor(
    private store: Store,
    private reportsApi: ReportsApiService,
    private salesApi: SalesApiService,
    private stationsApi: StationsApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.store.select(selectUser).pipe(takeUntil(this.destroy$)).subscribe(user => {
      if (user) {
        this.stationId = user.profile?.stationId || '';
        this.loadReports();
      }
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  private loadReports(): void {
    if (!this.stationId) {
      this.isLoading = false;
      return;
    }
    this.isLoading = true;

    // Load pump data and fuel types first to build name maps
    forkJoin({
      pumps: this.salesApi.getStationPumps(this.stationId).pipe(
        timeout(8000), catchError(() => of([]))
      ),
      fuelTypes: this.stationsApi.getFuelTypes().pipe(
        timeout(8000), catchError(() => of([]))
      )
    }).pipe(takeUntil(this.destroy$)).subscribe(({ pumps, fuelTypes }) => {
      // Build fuel type name map: fuelTypeId -> name
      (fuelTypes as FuelTypeDto[]).forEach(ft => {
        this.fuelTypeNameMap.set(ft.id, ft.name);
      });

      // Build pump name map: pumpId -> pumpName and pumpId -> fuelTypeId
      const pumpList = Array.isArray(pumps) ? pumps : [];
      pumpList.forEach((p: any) => {
        this.pumpNameMap.set(p.id, p.pumpName || `Pump ${p.id.substring(0, 4).toUpperCase()}`);
        if (p.fuelTypeId) {
          this.pumpFuelTypeIdMap.set(p.id, p.fuelTypeId);
          // If pump has a fuelTypeName, use it
          if (p.fuelTypeName) {
            this.fuelTypeNameMap.set(p.fuelTypeId, p.fuelTypeName);
          }
        }
      });

      // Now load the actual report data
      this.loadKpis();
      this.loadSalesSummary();
      this.loadTransactions();
    });
  }

  private loadKpis(): void {
    this.reportsApi.getDealerKpi(this.stationId).pipe(
      timeout(8000),
      takeUntil(this.destroy$),
    ).subscribe({
      next: (kpi) => {
        this.grossRevenue = kpi.revenueToday;
        this.transactionsCount = kpi.transactionsToday;
        this.litresSold = kpi.litresToday;
        this.monthlyRevenue = kpi.revenueThisMonth;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  private loadSalesSummary(): void {
    const now = new Date();
    let dateFrom: string;
    if (this.period === '30 Days') {
      const d = new Date(now); d.setDate(d.getDate() - 30);
      dateFrom = d.toISOString().split('T')[0];
    } else if (this.period === '7 Days') {
      const d = new Date(now); d.setDate(d.getDate() - 7);
      dateFrom = d.toISOString().split('T')[0];
    } else {
      dateFrom = now.toISOString().split('T')[0];
    }

    this.reportsApi.getSalesSummary({
      stationId: this.stationId,
      dateFrom,
      dateTo: now.toISOString().split('T')[0]
    }).pipe(
      timeout(8000),
      takeUntil(this.destroy$),
    ).subscribe({
      next: (summary) => {
        this.buildRevenueChart(summary);
        this.buildVolumeByGrade(summary);
      },
      error: () => {}
    });
  }

  private loadTransactions(): void {
    this.salesApi.getStationTransactions(this.stationId, 1, 10).pipe(
      timeout(8000),
      catchError(() => of({ items: [], totalCount: 0 })),
      takeUntil(this.destroy$),
    ).subscribe((result: any) => {
      const items = Array.isArray(result?.items) ? result.items : [];

      // Compute revenue from transactions as a fallback if KPI returns 0
      if (this.grossRevenue === 0 && items.length > 0) {
        const today = new Date().toISOString().split('T')[0];
        let revToday = 0;
        let txToday = 0;
        let litToday = 0;
        items.forEach((t: any) => {
          const txDate = t.timestamp ? new Date(t.timestamp).toISOString().split('T')[0] : '';
          if (txDate === today && t.status === 'Completed' && !t.isVoided) {
            revToday += t.totalAmount || 0;
            txToday++;
            litToday += t.quantityLitres || 0;
          }
        });
        if (revToday > 0) {
          this.grossRevenue = revToday;
          this.transactionsCount = txToday;
          this.litresSold = litToday;
        }
      }

      this.transactions = items.map((t: any) => {
        // Resolve pump name from our map
        const pumpLabel = this.pumpNameMap.get(t.pumpId) || '—';

        // Resolve fuel type: try from fuelTypeId directly, then from pump's fuelTypeId
        let fuelTypeName = '—';
        if (t.fuelTypeId && this.fuelTypeNameMap.has(t.fuelTypeId)) {
          fuelTypeName = this.fuelTypeNameMap.get(t.fuelTypeId)!;
        } else if (t.pumpId && this.pumpFuelTypeIdMap.has(t.pumpId)) {
          const ftId = this.pumpFuelTypeIdMap.get(t.pumpId)!;
          fuelTypeName = this.fuelTypeNameMap.get(ftId) || '—';
        }

        return {
          date: t.timestamp ? new Date(t.timestamp).toLocaleString('en-IN', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' }) : '—',
          ref: t.receiptNumber || (t.id ? '#' + t.id.substring(0, 6).toUpperCase() : '—'),
          pump: pumpLabel,
          fuelType: fuelTypeName,
          volume: t.quantityLitres != null ? `${Number(t.quantityLitres).toFixed(1)}L` : '—',
          amount: t.totalAmount != null ? `₹${Number(t.totalAmount).toLocaleString('en-IN')}` : '—',
          status: t.status || 'Unknown',
        };
      });
    });
  }

  onPeriodChange(p: string): void {
    this.period = p;
    this.loadSalesSummary();
  }

  private buildRevenueChart(summary: SalesSummaryDto): void {
    if (!summary?.data?.length) {
      this.revenueChartBars = [];
      return;
    }
    const values = summary.data.map(d => d.litres || d.value || 0);
    this.chartMaxValue = Math.max(...values, 1);
    this.revenueChartBars = summary.data.map(d => ({
      label: d.label,
      value: d.litres || d.value || 0,
      height: Math.max(((d.litres || d.value || 0) / this.chartMaxValue) * 85, 5),
    }));
  }

  private buildVolumeByGrade(summary: SalesSummaryDto): void {
    // Use byFuelType grouping — this groups by fuel grade, not by date
    if (!summary?.byFuelType?.length) {
      this.volumeByGrade = [];
      this.topGrade = '—';
      return;
    }

    const totalLitres = summary.byFuelType.reduce((sum, f) => sum + f.litres, 0);

    this.volumeByGrade = summary.byFuelType.map((f, i) => {
      const name = this.fuelTypeNameMap.get(f.fuelTypeId) || `Fuel ${i + 1}`;
      return {
        name,
        value: `${f.litres.toLocaleString('en-IN')}L`,
        pct: totalLitres > 0 ? Math.round((f.litres / totalLitres) * 100) : 0,
        color: this.gradeColors[i % this.gradeColors.length],
      };
    });

    // Top grade = one with most litres
    const topEntry = summary.byFuelType[0];
    this.topGrade = this.fuelTypeNameMap.get(topEntry.fuelTypeId) || '—';
  }

  // Calculate donut chart offset for each segment
  getDonutOffset(index: number): number {
    let offset = 25; // Start at 12 o'clock (25% offset for SVG)
    for (let i = 0; i < index; i++) {
      offset -= this.volumeByGrade[i]?.pct || 0;
    }
    return offset;
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'completed': return 'status-success';
      case 'initiated': case 'stockreserved': return 'status-pending';
      case 'voided': return 'status-flagged';
      default: return 'status-default';
    }
  }

  // ═══ Export — generates proper CSV with all KPI data ═══
  exportDailySales(): void {
    if (this.transactions.length === 0) {
      this.toast.error('No transaction data to export.');
      return;
    }
    const headers = ['Date/Time', 'Reference', 'Pump', 'Fuel Type', 'Volume', 'Amount', 'Status'];
    const rows = this.transactions.map(t => [t.date, t.ref, t.pump, t.fuelType, t.volume, t.amount, t.status]);
    this.downloadCsv(headers, rows, 'daily-sales-report');
  }

  exportRevenueAnalysis(): void {
    const headers = ['Metric', 'Value'];
    const rows: string[][] = [
      ['Report', 'Station Revenue Analysis'],
      ['Generated', new Date().toLocaleString('en-IN')],
      ['Station ID', this.stationId],
      ['', ''],
      ['Gross Revenue (Today)', `Rs ${this.grossRevenue.toLocaleString('en-IN')}`],
      ['Transactions (Today)', String(this.transactionsCount)],
      ['Litres Sold (Today)', `${this.litresSold.toLocaleString('en-IN')}L`],
      ['Monthly Revenue', `Rs ${this.monthlyRevenue.toLocaleString('en-IN')}`],
    ];

    if (this.volumeByGrade.length > 0) {
      rows.push(['', ''], ['Fuel Grade', 'Volume']);
      this.volumeByGrade.forEach(g => rows.push([g.name, g.value]));
    }

    this.downloadCsv(headers, rows, 'revenue-analysis');
  }

  exportInventoryReport(): void {
    const headers = ['Fuel Grade', 'Volume', 'Share (%)'];
    const rows = this.volumeByGrade.length > 0
      ? this.volumeByGrade.map(g => [g.name, g.value, `${g.pct}%`])
      : [['No data available', '—', '—']];
    this.downloadCsv(headers, rows, 'inventory-report');
  }

  private downloadCsv(headers: string[], rows: string[][], filename: string): void {
    const csv = [headers.join(','), ...rows.map(r => r.map(c => `"${c}"`).join(','))].join('\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `${filename}-${new Date().toISOString().split('T')[0]}.csv`;
    link.click();
    URL.revokeObjectURL(url);
    this.toast.success(`${filename.replace(/-/g, ' ')} exported successfully.`);
  }

  formatCurrency(value: number): string {
    if (value >= 100000) return `₹${(value / 100000).toFixed(1)}L`;
    if (value >= 1000) return `₹${(value / 1000).toFixed(1)}K`;
    return `₹${value.toLocaleString('en-IN')}`;
  }
}
