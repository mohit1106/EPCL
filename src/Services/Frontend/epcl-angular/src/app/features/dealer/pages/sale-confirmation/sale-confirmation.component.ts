import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { SalesApiService, TransactionDto } from '../../../../core/services/sales-api.service';

@Component({
  selector: 'app-sale-confirmation',
  templateUrl: './sale-confirmation.component.html',
  styleUrls: ['./sale-confirmation.component.scss'],
})
export class SaleConfirmationComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  transaction: TransactionDto | null = null;
  isLoading = true;

  // Template-bound properties
  txnId = '';
  status = 'Processing';
  fuelVolume = 0;
  totalAmount = 0;
  fuelType = '';
  tax = 0;
  station = '';
  district = '';

  fulfillmentSteps = [
    { name: 'ORDER RECEIVED', done: true },
    { name: 'STOCK RESERVED', done: true },
    { name: 'PAYMENT VERIFIED', done: false },
    { name: 'DISPENSING', done: false },
  ];

  constructor(private route: ActivatedRoute, private salesApi: SalesApiService) {}

  ngOnInit(): void {
    const txnId = this.route.snapshot.paramMap.get('id');
    if (txnId) {
      this.salesApi.getTransactionById(txnId).pipe(takeUntil(this.destroy$)).subscribe({
        next: (txn) => {
          this.transaction = txn;
          this.txnId = txn.receiptNumber || txn.id.substring(0, 12);
          this.status = txn.status;
          this.fuelVolume = txn.quantityLitres;
          this.totalAmount = txn.totalAmount;
          this.fuelType = txn.fuelTypeName;
          this.tax = txn.totalAmount * 0.18; // GST 18%
          this.station = txn.stationId.substring(0, 8);
          this.district = 'Station District';
          this.updateFulfillmentSteps(txn.status);
          this.isLoading = false;
        },
        error: () => { this.isLoading = false; },
      });
    }
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  private updateFulfillmentSteps(status: string): void {
    const stepMap: Record<string, number> = {
      'Initiated': 1,
      'StockReserved': 2,
      'Completed': 4,
      'Voided': 0,
    };
    const doneCount = stepMap[status] ?? 1;
    this.fulfillmentSteps.forEach((s, i) => { s.done = i < doneCount; });
  }

  navigateToStation(): void {
    // Would navigate to station map
  }

  printReceipt(): void { window.print(); }

  downloadReceipt(): void {
    if (!this.transaction) return;
    this.salesApi.getTransactionReceipt(this.transaction.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `receipt-${this.txnId}.pdf`;
        a.click();
        URL.revokeObjectURL(url);
      },
    });
  }
}
