import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil } from 'rxjs';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { SalesApiService, TransactionDto, TransactionFilters } from '../../../../core/services/sales-api.service';

interface DisplayTransaction {
  hash: string;
  date: string;
  time: string;
  terminal: string;
  payload: string;
  payRef: string;
  volume: number;
  amount: number;
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

  transactions: DisplayTransaction[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 25;
  isLoading = true;
  filters: TransactionFilters = {};

  totalVolume = 0;
  grossRevenue = 0;
  totalRecords = 0;
  totalPages = 1;

  constructor(private store: Store, private salesApi: SalesApiService) {}

  ngOnInit(): void {
    this.store.select(selectUser).pipe(takeUntil(this.destroy$)).subscribe(user => {
      if (user) {
        this.stationId = user.profile?.stationId || '';
        this.loadTransactions();
      }
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  loadTransactions(): void {
    if (!this.stationId) return;
    this.isLoading = true;
    this.salesApi.getStationTransactions(this.stationId, this.page, this.pageSize, this.filters)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.transactions = result.items.map(t => this.mapTransaction(t));
          this.totalCount = result.totalCount;
          this.totalRecords = result.totalCount;
          this.totalPages = result.totalPages;
          this.totalVolume = result.items.reduce((sum, t) => sum + t.quantityLitres, 0);
          this.grossRevenue = result.items.reduce((sum, t) => sum + t.totalAmount, 0);
          this.isLoading = false;
        },
        error: () => { this.isLoading = false; },
      });
  }

  private mapTransaction(t: TransactionDto): DisplayTransaction {
    const ts = new Date(t.timestamp);
    return {
      hash: t.receiptNumber || t.id.substring(0, 12),
      date: ts.toLocaleDateString(),
      time: ts.toLocaleTimeString(),
      terminal: `P_${t.pumpId.substring(0, 4)}`,
      payload: t.fuelTypeName,
      payRef: t.paymentMethod,
      volume: t.quantityLitres,
      amount: t.totalAmount,
      status: t.status,
    };
  }

  onPageChange(page: number): void { this.page = page; this.loadTransactions(); }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'completed': return 'status-success';
      case 'initiated': case 'stockreserved': return 'status-pending';
      case 'voided': return 'status-flagged';
      default: return 'status-default';
    }
  }
}
