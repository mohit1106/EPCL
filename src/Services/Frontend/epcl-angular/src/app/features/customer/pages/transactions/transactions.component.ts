import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { SalesApiService, TransactionDto, TransactionFilters } from '../../../../core/services/sales-api.service';
import { StationsApiService, FuelTypeDto } from '../../../../core/services/stations-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

interface SpendingBar {
  label: string;
  shortLabel: string;
  value: number;
  percent: number;
  color: string;
}

interface DonutSegment {
  name: string;
  count: number;
  percent: number;
  color: string;
  dash: string;
  offset: number;
}

@Component({
  selector: 'app-transactions',
  templateUrl: './transactions.component.html',
  styleUrls: ['./transactions.component.scss'],
})
export class TransactionsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  // Data
  filteredTransactions: TransactionDto[] = [];
  fuelTypes: FuelTypeDto[] = [];
  isLoading = true;

  // Pagination
  page = 1;
  pageSize = 15;
  totalCount = 0;
  totalPages = 1;
  pageNumbers: number[] = [];

  // Filters
  filters: TransactionFilters = {};

  // Summary KPIs
  totalSpent = 0;
  totalVolume = 0;
  avgPerTransaction = 0;

  // Charts
  spendingBars: SpendingBar[] = [];
  donutSegments: DonutSegment[] = [];

  // Fuel color map
  private fuelColors: Record<string, string> = {
    Petrol: '#1E40AF',
    Diesel: '#10B981',
    CNG: '#F59E0B',
    PremiumPetrol: '#8B5CF6',
    PremiumDiesel: '#EF4444',
    Unknown: '#94A3B8',
  };

  constructor(
    private salesApi: SalesApiService,
    private stationsApi: StationsApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.stationsApi.getFuelTypes().pipe(takeUntil(this.destroy$)).subscribe({
      next: (ft) => { this.fuelTypes = ft; },
      error: () => { /* Continue without fuel types */ },
    });
    this.loadTransactions();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadTransactions(): void {
    this.isLoading = true;
    this.salesApi.getMyTransactions(this.page, this.pageSize, this.filters)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          // Enrich with fuel type names from lookup
          this.filteredTransactions = result.items.map(t => ({
            ...t,
            fuelTypeName: t.fuelTypeName || this.fuelTypes.find(f => f.id === t.fuelTypeId)?.name || 'Unknown',
          }));
          this.totalCount = result.totalCount;
          this.totalPages = result.totalPages || Math.ceil(result.totalCount / this.pageSize) || 1;
          this.isLoading = false;
          this.computeKPIs(this.filteredTransactions);
          this.buildPageNumbers();
          this.buildCharts(this.filteredTransactions);
        },
        error: () => {
          this.isLoading = false;
          this.filteredTransactions = [];
          this.toast.error('Failed to load transactions');
        },
      });
  }

  private computeKPIs(transactions: TransactionDto[]): void {
    this.totalSpent = transactions.reduce((sum, t) => sum + t.totalAmount, 0);
    this.totalVolume = transactions.reduce((sum, t) => sum + t.quantityLitres, 0);
    this.avgPerTransaction = transactions.length > 0 ? this.totalSpent / transactions.length : 0;
  }

  // ─── Charts ────────────────────────────
  private buildCharts(transactions: TransactionDto[]): void {
    this.buildSpendingChart(transactions);
    this.buildDonutChart(transactions);
  }

  private buildSpendingChart(transactions: TransactionDto[]): void {
    // Group by date
    const dailyMap: Record<string, number> = {};
    transactions.forEach(t => {
      const d = new Date(t.timestamp);
      const key = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
      dailyMap[key] = (dailyMap[key] || 0) + t.totalAmount;
    });

    const entries = Object.entries(dailyMap).sort(([a], [b]) => a.localeCompare(b));
    const maxVal = Math.max(...entries.map(([, v]) => v), 1);
    const colors = ['#1E40AF', '#3B82F6', '#10B981', '#F59E0B', '#8B5CF6', '#EF4444'];

    this.spendingBars = entries.map(([date, value], i) => {
      const d = new Date(date);
      return {
        label: d.toLocaleDateString('en-IN', { day: '2-digit', month: 'short' }),
        shortLabel: d.toLocaleDateString('en-IN', { day: '2-digit', month: 'short' }).replace(' ', '\n'),
        value,
        percent: (value / maxVal) * 100,
        color: colors[i % colors.length],
      };
    });
  }

  private buildDonutChart(transactions: TransactionDto[]): void {
    const countMap: Record<string, number> = {};
    transactions.forEach(t => {
      const name = t.fuelTypeName || 'Unknown';
      countMap[name] = (countMap[name] || 0) + 1;
    });

    const total = transactions.length;
    const circumference = 2 * Math.PI * 54; // r=54
    let currentOffset = 0;

    this.donutSegments = Object.entries(countMap).map(([name, count]) => {
      const pct = count / total;
      const dashLen = pct * circumference;
      const gap = circumference - dashLen;
      const seg: DonutSegment = {
        name,
        count,
        percent: pct * 100,
        color: this.fuelColors[name] || '#94A3B8',
        dash: `${dashLen} ${gap}`,
        offset: -currentOffset,
      };
      currentOffset += dashLen;
      return seg;
    });
  }

  // ─── Filters ───────────────────────────
  applyFilters(): void {
    this.page = 1;
    this.loadTransactions();
  }

  setFuelFilter(fuelTypeId: string): void {
    this.filters = { ...this.filters, fuelTypeId: fuelTypeId || undefined };
    this.applyFilters();
  }

  hasActiveFilters(): boolean {
    return !!(this.filters.dateFrom || this.filters.dateTo || this.filters.fuelTypeId ||
              this.filters.paymentMethod || this.filters.status);
  }

  clearFilters(): void {
    this.filters = {};
    this.applyFilters();
  }

  // ─── Pagination ────────────────────────
  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages || p === this.page) return;
    this.page = p;
    this.loadTransactions();
  }

  private buildPageNumbers(): void {
    const pages: number[] = [];
    const total = this.totalPages;
    const current = this.page;

    if (total <= 7) {
      for (let i = 1; i <= total; i++) pages.push(i);
    } else {
      pages.push(1);
      if (current > 3) pages.push(-1);
      const start = Math.max(2, current - 1);
      const end = Math.min(total - 1, current + 1);
      for (let i = start; i <= end; i++) pages.push(i);
      if (current < total - 2) pages.push(-1);
      pages.push(total);
    }
    this.pageNumbers = pages;
  }

  // ─── Helpers ───────────────────────────
  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'completed': return 'status-success';
      case 'initiated': case 'stockreserved': return 'status-pending';
      case 'voided': return 'status-flagged';
      default: return 'status-default';
    }
  }

  getFuelColor(fuelName: string): string {
    return this.fuelColors[fuelName] || '#64748B';
  }

  // ─── Export CSV ────────────────────────
  exportCSV(): void {
    if (this.filteredTransactions.length === 0) return;

    const headers = ['Date', 'Time', 'Receipt No', 'Station', 'Fuel Type', 'Volume (L)', 'Rate (INR/L)', 'Total (INR)', 'Payment', 'Status', 'Vehicle'];
    const rows = this.filteredTransactions.map(t => {
      const d = new Date(t.timestamp);
      const dateStr = d.toLocaleDateString('en-IN', { day: '2-digit', month: '2-digit', year: 'numeric' });
      const timeStr = d.toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit' });
      return [
        dateStr,
        timeStr,
        t.receiptNumber || t.id.substring(0, 8),
        'Station ' + t.stationId.substring(0, 4),
        t.fuelTypeName || 'N/A',
        t.quantityLitres.toFixed(1),
        t.pricePerLitre.toFixed(2),
        t.totalAmount.toFixed(2),
        t.paymentMethod,
        t.status,
        t.vehicleNumber || '',
      ].map(v => `"${v}"`).join(',');
    });

    const csv = [headers.join(','), ...rows].join('\r\n');
    const bom = '\uFEFF'; // UTF-8 BOM for Excel
    const blob = new Blob([bom + csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `epcl-transactions-${new Date().toISOString().split('T')[0]}.csv`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
    this.toast.success('CSV exported successfully!');
  }

  // ─── Print ─────────────────────────────
  printTransactions(): void {
    window.print();
  }
}
