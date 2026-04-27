import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil } from 'rxjs';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { ReportsApiService, DealerKpiDto, SalesSummaryDto } from '../../../../core/services/reports-api.service';
import { SalesApiService, TransactionDto } from '../../../../core/services/sales-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-dealer-reports',
  templateUrl: './reports.component.html',
  styleUrls: ['./reports.component.scss'],
})
export class DealerReportsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private stationId = '';

  lastSync = new Date().toLocaleTimeString();
  periods = ['Today', '7 Days', '30 Days', 'Custom'];
  period = 'Today';

  kpis: { label: string; value: string; icon: string; trend: string; trendLabel: string; trendClass: string }[] = [];

  revenueChartBars: number[] = [40, 60, 55, 72, 80, 65, 90, 85, 78, 92, 68, 75];
  chartLabels = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

  volumeByGrade: { name: string; value: string; pct: number; color: string }[] = [];
  topGrade = '';

  transactions: { date: string; ref: string; pump: string; volume: string; amount: string; status: string }[] = [];

  exports = [
    { icon: '📋', name: 'Daily Sales Report', format: 'PDF / 2.1MB' },
    { icon: '📊', name: 'Revenue Analysis', format: 'XLSX / 1.8MB' },
    { icon: '📈', name: 'Inventory Report', format: 'PDF / 3.4MB' },
  ];

  aiInsight = 'Based on current trends, diesel demand is expected to increase 12% this week. Consider increasing stock levels for Tank B.';

  isLoading = true;

  constructor(
    private store: Store,
    private reportsApi: ReportsApiService,
    private salesApi: SalesApiService,
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
    if (!this.stationId) return;
    this.isLoading = true;
    this.reportsApi.getDealerKpi(this.stationId).pipe(takeUntil(this.destroy$)).subscribe({
      next: (kpi) => {
        this.kpis = [
          { label: 'GROSS REVENUE', value: `₹${kpi.revenueToday.toLocaleString()}`, icon: '💲', trend: '+12.4%', trendLabel: 'vs prev period', trendClass: 'up' },
          { label: 'TRANSACTIONS', value: String(kpi.transactionsToday), icon: '📋', trend: String(kpi.transactionsToday), trendLabel: 'processed', trendClass: '' },
          { label: 'LITRES SOLD', value: `${kpi.litresToday.toLocaleString()}L`, icon: '⛽', trend: 'Active', trendLabel: 'dispensing', trendClass: '' },
          { label: 'MONTHLY REV', value: `₹${kpi.revenueThisMonth.toLocaleString()}`, icon: '📊', trend: 'This month', trendLabel: 'total', trendClass: 'up' },
        ];
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; },
    });

    this.reportsApi.getSalesSummary({ stationId: this.stationId, groupBy: 'fuelType' })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (s) => {
          if (s.data?.length > 0) {
            const maxVal = Math.max(...s.data.map(d => d.litres));
            this.volumeByGrade = s.data.map((d, i) => ({
              name: d.label,
              value: `${d.litres.toLocaleString()}L`,
              pct: maxVal > 0 ? Math.round((d.litres / maxVal) * 100) : 0,
              color: ['#6366f1', '#10b981', '#f59e0b', '#ef4444'][i % 4],
            }));
            this.topGrade = s.data.reduce((max, d) => d.litres > max.litres ? d : max, s.data[0]).label;
          }
        },
      });

    this.salesApi.getStationTransactions(this.stationId, 1, 5).pipe(takeUntil(this.destroy$)).subscribe({
      next: (result) => {
        this.transactions = result.items.map(t => ({
          date: new Date(t.timestamp).toLocaleString(),
          ref: t.receiptNumber || t.id.substring(0, 8),
          pump: `P-${t.pumpId.substring(0, 2)}`,
          volume: `${t.quantityLitres}L`,
          amount: `₹${t.totalAmount}`,
          status: t.status,
        }));
      },
    });
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'completed': return 'status-success';
      case 'initiated': case 'stockreserved': return 'status-pending';
      case 'voided': return 'status-flagged';
      default: return 'status-default';
    }
  }
}
