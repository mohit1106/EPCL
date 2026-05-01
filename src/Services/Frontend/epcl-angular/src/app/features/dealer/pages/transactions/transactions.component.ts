import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil, forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { SalesApiService, TransactionDto, TransactionFilters, PumpDto } from '../../../../core/services/sales-api.service';
import { StationsApiService, FuelTypeDto } from '../../../../core/services/stations-api.service';

interface DisplayTransaction {
  id: string;
  receiptNumber: string;
  date: string;
  time: string;
  pumpName: string;
  fuelType: string;
  vehicleNumber: string;
  volume: number;
  rate: number;
  amount: number;
  paymentMethod: string;
  status: string;
}

@Component({
  selector: 'app-dealer-transactions',
  templateUrl: './transactions.component.html',
  styleUrls: ['./transactions.component.scss'],
})
export class DealerTransactionsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private stationId = '';
  stationName = '';

  transactions: DisplayTransaction[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 15;
  isLoading = true;

  // Filter options
  fuelTypes: FuelTypeDto[] = [];
  pumps: PumpDto[] = [];
  statusOptions = ['All', 'Completed', 'Initiated', 'Voided', 'Failed'];
  paymentOptions = ['All', 'Cash', 'UPI', 'Card', 'Wallet', 'FleetCard'];

  // Filter bindings
  filterDateFrom = '';
  filterDateTo = '';
  filterFuelType = '';
  filterPaymentMethod = '';
  filterStatus = '';
  filterPumpId = '';

  // Summary stats
  totalVolume = 0;
  grossRevenue = 0;
  totalRecords = 0;
  totalPages = 1;
  completedCount = 0;
  pendingCount = 0;
  avgTransaction = 0;

  // Charts
  paymentBreakdown: { label: string; count: number; pct: number; color: string }[] = [];
  statusBreakdown: { label: string; count: number; pct: number; color: string }[] = [];

  // Pump lookup map
  private pumpMap = new Map<string, string>();
  private fuelTypeMap = new Map<string, string>();

  constructor(
    private store: Store,
    private salesApi: SalesApiService,
    private stationsApi: StationsApiService
  ) {}

  ngOnInit(): void {
    this.store.select(selectUser).pipe(takeUntil(this.destroy$)).subscribe(user => {
      if (user) {
        this.stationId = user.profile?.stationId || '';
        this.loadReferenceData();
      }
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  private loadReferenceData(): void {
    if (!this.stationId) return;
    forkJoin({
      station: this.stationsApi.getStationById(this.stationId).pipe(catchError(() => of(null))),
      fuelTypes: this.stationsApi.getFuelTypes().pipe(catchError(() => of([]))),
      pumps: this.salesApi.getStationPumps(this.stationId).pipe(catchError(() => of([]))),
    }).pipe(takeUntil(this.destroy$)).subscribe(({ station, fuelTypes, pumps }) => {
      this.stationName = (station as any)?.stationName || (station as any)?.name || 'My Station';
      this.fuelTypes = fuelTypes;
      this.pumps = pumps;
      fuelTypes.forEach(ft => this.fuelTypeMap.set(ft.id, ft.name));
      pumps.forEach(p => this.pumpMap.set(p.id, p.pumpName));
      this.loadTransactions();
    });
  }

  loadTransactions(): void {
    if (!this.stationId) return;
    this.isLoading = true;
    const filters: TransactionFilters = {};
    if (this.filterDateFrom) filters.dateFrom = this.filterDateFrom;
    if (this.filterDateTo) filters.dateTo = this.filterDateTo;
    if (this.filterFuelType) filters.fuelTypeId = this.filterFuelType;
    if (this.filterPaymentMethod && this.filterPaymentMethod !== 'All') filters.paymentMethod = this.filterPaymentMethod;
    if (this.filterStatus && this.filterStatus !== 'All') filters.status = this.filterStatus;

    this.salesApi.getStationTransactions(this.stationId, this.page, this.pageSize, filters)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.transactions = result.items.map(t => this.mapTransaction(t));
          this.totalCount = result.totalCount;
          this.totalRecords = result.totalCount;
          this.totalPages = result.totalPages || Math.ceil(result.totalCount / this.pageSize);
          this.totalVolume = result.items.reduce((sum, t) => sum + t.quantityLitres, 0);
          this.grossRevenue = result.items.reduce((sum, t) => sum + t.totalAmount, 0);
          this.completedCount = result.items.filter(t => t.status === 'Completed').length;
          this.pendingCount = result.items.filter(t => t.status === 'Initiated').length;
          this.avgTransaction = this.transactions.length > 0 ? this.grossRevenue / this.transactions.length : 0;
          this.buildCharts(result.items);
          this.isLoading = false;
        },
        error: () => { this.isLoading = false; },
      });
  }

  private mapTransaction(t: TransactionDto): DisplayTransaction {
    const ts = new Date(t.timestamp);
    return {
      id: t.id,
      receiptNumber: t.receiptNumber || t.id.substring(0, 12),
      date: ts.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' }),
      time: ts.toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit' }),
      pumpName: this.pumpMap.get(t.pumpId) || `Pump ${t.pumpId?.substring(0, 4) || '??'}`,
      fuelType: this.fuelTypeMap.get(t.fuelTypeId) || t.fuelTypeName || 'Fuel',
      vehicleNumber: t.vehicleNumber || '—',
      volume: t.quantityLitres,
      rate: t.pricePerLitre,
      amount: t.totalAmount,
      paymentMethod: t.paymentMethod,
      status: t.status,
    };
  }

  private buildCharts(items: TransactionDto[]): void {
    const payColors: Record<string, string> = { Cash: '#059669', UPI: '#7c3aed', Card: '#2563eb', Wallet: '#ea580c', FleetCard: '#0891b2' };
    const statusColors: Record<string, string> = { Completed: '#059669', Initiated: '#f59e0b', Voided: '#ef4444', Failed: '#dc2626' };

    // Payment breakdown
    const payCounts: Record<string, number> = {};
    items.forEach(t => { payCounts[t.paymentMethod] = (payCounts[t.paymentMethod] || 0) + 1; });
    this.paymentBreakdown = Object.entries(payCounts).map(([label, count]) => ({
      label, count, pct: items.length > 0 ? Math.round(count / items.length * 100) : 0,
      color: payColors[label] || '#64748b',
    }));

    // Status breakdown
    const statusCounts: Record<string, number> = {};
    items.forEach(t => { statusCounts[t.status] = (statusCounts[t.status] || 0) + 1; });
    this.statusBreakdown = Object.entries(statusCounts).map(([label, count]) => ({
      label, count, pct: items.length > 0 ? Math.round(count / items.length * 100) : 0,
      color: statusColors[label] || '#64748b',
    }));
  }

  // Pagination
  onPageChange(newPage: number): void {
    if (newPage < 1 || newPage > this.totalPages) return;
    this.page = newPage;
    this.loadTransactions();
  }

  get pageNumbers(): number[] {
    const pages: number[] = [];
    const start = Math.max(1, this.page - 2);
    const end = Math.min(this.totalPages, this.page + 2);
    for (let i = start; i <= end; i++) pages.push(i);
    return pages;
  }

  get displayStart(): number { return Math.min((this.page - 1) * this.pageSize + 1, this.totalRecords); }
  get displayEnd(): number { return Math.min(this.page * this.pageSize, this.totalRecords); }

  applyFilters(): void { this.page = 1; this.loadTransactions(); }

  clearFilters(): void {
    this.filterDateFrom = '';
    this.filterDateTo = '';
    this.filterFuelType = '';
    this.filterPaymentMethod = '';
    this.filterStatus = '';
    this.filterPumpId = '';
    this.page = 1;
    this.loadTransactions();
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'completed': return 'st-success';
      case 'initiated': case 'stockreserved': return 'st-pending';
      case 'voided': case 'failed': return 'st-danger';
      default: return 'st-default';
    }
  }

  exportCsv(): void {
    const headers = ['Receipt', 'Date', 'Time', 'Pump', 'Fuel Type', 'Vehicle', 'Volume (L)', 'Rate (₹)', 'Amount (₹)', 'Payment', 'Status'];
    const rows = this.transactions.map(t => [
      t.receiptNumber, t.date, t.time, t.pumpName, t.fuelType,
      t.vehicleNumber, t.volume.toFixed(2), t.rate?.toFixed(2) || '0', t.amount.toFixed(2),
      t.paymentMethod, t.status,
    ]);
    const csv = [headers.join(','), ...rows.map(r => r.map(v => `"${v}"`).join(','))].join('\n');
    const blob = new Blob(['\ufeff' + csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `transactions-${this.stationName.replace(/\s/g, '_')}-${new Date().toISOString().split('T')[0]}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }

  printReport(): void { window.print(); }
}
