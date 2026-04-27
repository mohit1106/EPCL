import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { SalesApiService, TransactionDto, TransactionFilters } from '../../../../core/services/sales-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

interface DisplayTransaction {
  date: string;
  time: string;
  station: string;
  stationId: string;
  liters: number;
  rate: number;
  total: number;
  status: string;
}

@Component({
  selector: 'app-transactions',
  templateUrl: './transactions.component.html',
  styleUrls: ['./transactions.component.scss'],
})
export class TransactionsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  transactions: DisplayTransaction[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 20;
  isLoading = true;
  filters: any = {};

  volume24h = 142.5;
  revenue = 254800;
  fuelGrades = ['All Grades', 'Petrol 95', 'Diesel', 'CNG'];
  
  totalRecords = 0;
  totalPages = 1;

  constructor(private salesApi: SalesApiService, private toast: ToastService) {}

  ngOnInit(): void { this.loadTransactions(); }
  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  loadTransactions(): void {
    this.isLoading = true;
    this.salesApi.getMyTransactions(this.page, this.pageSize, this.filters)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.transactions = result.items.map(t => {
            const dateObj = new Date(t.timestamp);
            return {
              date: dateObj.toLocaleDateString(),
              time: dateObj.toLocaleTimeString(),
              station: 'Station ' + t.stationId.substring(0, 4),
              stationId: t.stationId.substring(0, 8),
              liters: t.quantityLitres,
              rate: t.totalAmount / (t.quantityLitres || 1), // Assuming total / qty = rate
              total: t.totalAmount,
              status: t.status
            };
          });
          this.totalCount = result.totalCount;
          this.totalRecords = result.totalCount;
          this.totalPages = result.totalPages || 1;
          this.isLoading = false;
        },
        error: () => { this.isLoading = false; },
      });
  }

  onPageChange(page: number): void { this.page = page; this.loadTransactions(); }

  onFilterChange(filters: TransactionFilters): void {
    this.filters = filters;
    this.page = 1;
    this.loadTransactions();
  }

  downloadReceipt(txId: string): void {
    this.salesApi.getTransactionReceipt(txId).pipe(takeUntil(this.destroy$)).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `receipt-${txId}.pdf`;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: () => this.toast.error('Failed to download receipt.'),
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
