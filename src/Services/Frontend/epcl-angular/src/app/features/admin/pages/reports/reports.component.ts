import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil, catchError, of, timeout, forkJoin } from 'rxjs';
import { ReportsApiService, AdminKpiDto, SalesSummaryDto } from '../../../../core/services/reports-api.service';
import { StationsApiService, FuelTypeDto } from '../../../../core/services/stations-api.service';
import { FraudApiService } from '../../../../core/services/fraud-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-admin-reports',
  templateUrl: './reports.component.html',
  styleUrls: ['./reports.component.scss'],
})
export class AdminReportsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  isLoading = true;
  isExporting = false;
  selectedPeriod = 'week';
  periods = ['today', 'week', 'month'];

  // KPIs from API
  totalRevenue = 0;
  totalTransactions = 0;
  totalLitres = 0;
  totalStations = 0;
  activeDealers = 0;
  fraudAlerts = 0;

  // Chart data from sales summary — daily revenue trend
  chartBars: { label: string; value: number; height: number }[] = [];
  chartMaxValue = 0;

  // Fuel distribution — grouped by fuel type
  fuelDistribution: { name: string; litres: number; revenue: number; transactions: number; pct: number; color: string }[] = [];

  // Fuel type name map
  private fuelTypeNameMap = new Map<string, string>();
  private distColors = ['#1E40AF', '#10b981', '#f59e0b', '#6366f1', '#ef4444', '#8b5cf6'];

  // Report generator form
  reportType = 'daily-sales';
  reportDateFrom = '';
  reportDateTo = '';

  reportTypes = [
    { value: 'daily-sales', label: 'Daily Sales Report' },
    { value: 'station-performance', label: 'Station Performance' },
    { value: 'inventory-summary', label: 'Inventory Summary' },
    { value: 'fraud-summary', label: 'Fraud Analysis' },
    { value: 'loyalty-summary', label: 'Loyalty Program' },
  ];

  constructor(
    private reportsApi: ReportsApiService,
    private stationsApi: StationsApiService,
    private fraudApi: FraudApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    const today = new Date();
    this.reportDateTo = today.toISOString().split('T')[0];
    const weekAgo = new Date(today);
    weekAgo.setDate(weekAgo.getDate() - 7);
    this.reportDateFrom = weekAgo.toISOString().split('T')[0];

    // Load fuel types first, then load reports
    this.stationsApi.getFuelTypes().pipe(
      timeout(8000),
      catchError(() => of([])),
      takeUntil(this.destroy$)
    ).subscribe((fuelTypes: FuelTypeDto[]) => {
      fuelTypes.forEach(ft => this.fuelTypeNameMap.set(ft.id, ft.name));
      this.loadReports();
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  private loadReports(): void {
    this.isLoading = true;

    forkJoin({
      kpi: this.reportsApi.getAdminKpi().pipe(catchError(() => of({ totalRevenueToday: 0, totalTransactionsToday: 0, totalLitresToday: 0 } as AdminKpiDto))),
      stations: this.stationsApi.getStations(1, 1000).pipe(catchError(() => of({ items: [], totalCount: 0 }))),
      fraud: this.fraudApi.getAlerts(1, 1, { status: 'Open' }).pipe(catchError(() => of({ totalCount: 0 })))
    }).pipe(
      timeout(10000),
      takeUntil(this.destroy$)
    ).subscribe(({ kpi, stations, fraud }) => {
      this.totalRevenue = kpi.totalRevenueToday || 0;
      this.totalTransactions = kpi.totalTransactionsToday || 0;
      this.totalLitres = kpi.totalLitresToday || 0;
      
      const stItems = stations?.items || [];
      this.totalStations = stItems.filter((s: any) => s.isActive).length;
      
      // Active dealers are unique dealerUserIds that aren't 0000...
      const dealers = new Set(stItems.map((s: any) => s.dealerUserId).filter((id: string) => id && id !== '00000000-0000-0000-0000-000000000000'));
      this.activeDealers = dealers.size;
      
      this.fraudAlerts = fraud?.totalCount || 0;
      
      this.isLoading = false;
    });

    this.loadSalesSummary();
  }

  loadSalesSummary(): void {
    const now = new Date();
    let dateFrom = now.toISOString().split('T')[0];
    if (this.selectedPeriod === 'week') {
      const d = new Date(now); d.setDate(d.getDate() - 7); dateFrom = d.toISOString().split('T')[0];
    } else if (this.selectedPeriod === 'month') {
      const d = new Date(now); d.setMonth(d.getMonth() - 1); dateFrom = d.toISOString().split('T')[0];
    }

    this.reportsApi.getSalesSummary({ dateFrom, dateTo: now.toISOString().split('T')[0] }).pipe(
      timeout(8000),
      catchError(() => of({ data: [], byFuelType: [], totalRevenue: 0, totalLitres: 0, totalTransactions: 0 } as SalesSummaryDto)),
      takeUntil(this.destroy$),
    ).subscribe(summary => {
      this.buildChart(summary);
      this.buildFuelDistribution(summary);
    });
  }

  onPeriodChange(period: string): void {
    this.selectedPeriod = period;
    this.loadSalesSummary();
  }

  private buildChart(summary: SalesSummaryDto): void {
    if (!summary?.data?.length) {
      this.chartBars = [];
      return;
    }
    const values = summary.data.map(d => d.litres || d.value || 0);
    this.chartMaxValue = Math.max(...values, 1);
    this.chartBars = summary.data.map(d => ({
      label: d.label,
      value: d.litres || d.value || 0,
      height: Math.max(((d.litres || d.value || 0) / this.chartMaxValue) * 90, 5),
    }));
  }

  private buildFuelDistribution(summary: SalesSummaryDto): void {
    if (!summary?.byFuelType?.length) {
      this.fuelDistribution = [];
      return;
    }

    const totalLitres = summary.byFuelType.reduce((sum, f) => sum + f.litres, 0);

    this.fuelDistribution = summary.byFuelType.map((f, i) => ({
      name: this.fuelTypeNameMap.get(f.fuelTypeId) || `Fuel Type ${i + 1}`,
      litres: f.litres,
      revenue: f.revenue,
      transactions: f.transactions,
      pct: totalLitres > 0 ? Math.round((f.litres / totalLitres) * 100) : 0,
      color: this.distColors[i % this.distColors.length],
    }));
  }

  // ═══ Export — with proper fallback ═══
  exportPdf(): void {
    this.isExporting = true;
    const filters: Record<string, string> = { dateFrom: this.reportDateFrom, dateTo: this.reportDateTo };
    this.reportsApi.exportPdf(this.reportType, filters).pipe(
      timeout(10000),
      catchError(() => {
        this.generateLocalReport();
        return of(null);
      }),
      takeUntil(this.destroy$),
    ).subscribe(report => {
      if (report && report.id) {
        this.pollAndDownload(report.id);
      }
    });
  }

  exportExcel(): void {
    this.isExporting = true;
    const filters: Record<string, string> = { dateFrom: this.reportDateFrom, dateTo: this.reportDateTo };
    this.reportsApi.exportExcel(this.reportType, filters).pipe(
      timeout(10000),
      catchError(() => {
        this.generateLocalReport();
        return of(null);
      }),
      takeUntil(this.destroy$),
    ).subscribe(report => {
      if (report && report.id) {
        this.pollAndDownload(report.id);
      }
    });
  }

  private generateLocalReport(): void {
    const headers = ['Metric', 'Value'];
    const rows: string[][] = [
      ['Report Type', this.reportTypes.find(r => r.value === this.reportType)?.label || this.reportType],
      ['Period', `${this.reportDateFrom} to ${this.reportDateTo}`],
      ['Generated', new Date().toLocaleString('en-IN')],
      ['', ''],
      ['Total Revenue', `Rs ${this.totalRevenue.toLocaleString('en-IN')}`],
      ['Total Transactions', String(this.totalTransactions)],
      ['Total Litres Dispensed', `${this.totalLitres.toLocaleString('en-IN')}L`],
      ['Active Stations', String(this.totalStations)],
      ['Active Dealers', String(this.activeDealers)],
      ['Fraud Alerts', String(this.fraudAlerts)],
    ];

    if (this.fuelDistribution.length > 0) {
      rows.push(['', ''], ['Fuel Type', 'Litres', 'Revenue', 'Transactions']);
      this.fuelDistribution.forEach(f =>
        rows.push([f.name, `${f.litres.toLocaleString('en-IN')}L`, `Rs ${f.revenue.toLocaleString('en-IN')}`, String(f.transactions)])
      );
    }

    if (this.chartBars.length > 0) {
      rows.push(['', ''], ['Date', 'Volume (L)']);
      this.chartBars.forEach(b => rows.push([b.label, b.value.toFixed(0)]));
    }

    const csv = [headers.join(','), ...rows.map(r => r.join(','))].join('\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `${this.reportType}-${this.reportDateFrom}-to-${this.reportDateTo}.csv`;
    link.click();
    URL.revokeObjectURL(url);
    this.isExporting = false;
    this.toast.success('Report exported as CSV.');
  }

  private pollAndDownload(reportId: string): void {
    let attempts = 0;
    const maxAttempts = 15;
    const poll = setInterval(() => {
      attempts++;
      if (attempts > maxAttempts) {
        clearInterval(poll);
        this.isExporting = false;
        this.toast.error('Report generation timed out. Generating local export...');
        this.generateLocalReport();
        return;
      }

      this.reportsApi.getExportStatus(reportId).pipe(
        catchError(() => {
          clearInterval(poll);
          this.isExporting = false;
          this.generateLocalReport();
          return of(null);
        }),
        takeUntil(this.destroy$),
      ).subscribe(status => {
        if (!status) return;
        if (status.status === 'Ready' || status.status === 'Completed') {
          clearInterval(poll);
          this.isExporting = false;
          this.reportsApi.downloadExport(reportId).pipe(takeUntil(this.destroy$)).subscribe({
            next: (blob) => {
              const url = URL.createObjectURL(blob);
              const a = document.createElement('a');
              a.href = url;
              a.download = `report-${reportId}`;
              a.click();
              URL.revokeObjectURL(url);
              this.toast.success('Report downloaded.');
            },
            error: () => { this.generateLocalReport(); },
          });
        } else if (status.status === 'Failed') {
          clearInterval(poll);
          this.isExporting = false;
          this.toast.error('Report generation failed. Generating local export...');
          this.generateLocalReport();
        }
      });
    }, 2000);
  }

  formatCurrency(value: number): string {
    if (value >= 100000) return `₹${(value / 100000).toFixed(1)}L`;
    if (value >= 1000) return `₹${(value / 1000).toFixed(1)}K`;
    return `₹${value.toLocaleString('en-IN')}`;
  }

  getPeriodLabel(): string {
    switch (this.selectedPeriod) {
      case 'today': return "Today's";
      case 'week': return 'Last 7 Days';
      case 'month': return 'Last 30 Days';
      default: return '';
    }
  }

  // Calculate donut chart offset for each fuel segment
  getDonutOffset(index: number): number {
    let offset = 25;
    for (let i = 0; i < index; i++) {
      offset -= this.fuelDistribution[i]?.pct || 0;
    }
    return offset;
  }
}
