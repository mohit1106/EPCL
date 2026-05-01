import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, takeUntil, interval, Subscription, forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { SalesApiService, TransactionDto } from '../../../../core/services/sales-api.service';
import { StationsApiService, FuelTypeDto } from '../../../../core/services/stations-api.service';

@Component({
  selector: 'app-sale-confirmation',
  templateUrl: './sale-confirmation.component.html',
  styleUrls: ['./sale-confirmation.component.scss'],
})
export class SaleConfirmationComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  transaction: TransactionDto | null = null;
  isLoading = true;
  private pollSub?: Subscription;
  private fuelTypes: FuelTypeDto[] = [];

  // Template-bound properties
  txnId = '';
  status = 'Processing';
  fuelVolume = 0;
  totalAmount = 0;
  fuelType = '';
  pricePerLitre = 0;
  station = '';
  stationName = '';
  paymentMethod = '';
  vehicleNumber = '';
  isWalletPayment = false;
  receiptNumber = '';

  fulfillmentSteps = [
    { name: 'ORDER RECEIVED', done: true },
    { name: 'STOCK RESERVED', done: true },
    { name: 'PAYMENT VERIFIED', done: false },
    { name: 'DISPENSING', done: false },
  ];

  constructor(
    private route: ActivatedRoute,
    private salesApi: SalesApiService,
    private stationsApi: StationsApiService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Load fuel types first, then load transaction
    this.stationsApi.getFuelTypes().pipe(
      takeUntil(this.destroy$),
      catchError(() => of([]))
    ).subscribe(fuelTypes => {
      this.fuelTypes = fuelTypes;
      const txnId = this.route.snapshot.paramMap.get('id');
      if (txnId) {
        this.loadTransaction(txnId);
      }
    });
  }

  ngOnDestroy(): void {
    this.stopPolling();
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadTransaction(txnId: string): void {
    this.salesApi.getTransactionById(txnId).pipe(takeUntil(this.destroy$)).subscribe({
      next: (txn) => {
        this.updateFromTransaction(txn);
        this.isLoading = false;

        // Only poll for Wallet payments that are still pending
        if (this.isWalletPayment && this.status === 'Initiated' && !this.pollSub) {
          this.startPolling(txn.id);
        } else if (this.status !== 'Initiated') {
          this.stopPolling();
        }
      },
      error: () => { this.isLoading = false; },
    });
  }

  private updateFromTransaction(txn: TransactionDto): void {
    this.transaction = txn;
    this.txnId = txn.receiptNumber || txn.id.substring(0, 12);
    this.receiptNumber = txn.receiptNumber;
    this.status = txn.status;
    this.fuelVolume = txn.quantityLitres;
    this.totalAmount = txn.totalAmount;
    this.pricePerLitre = txn.pricePerLitre;
    this.vehicleNumber = txn.vehicleNumber;
    this.paymentMethod = txn.paymentMethod;
    this.isWalletPayment = txn.paymentMethod === 'Wallet';
    this.updateFulfillmentSteps(txn.status);

    // Resolve fuel type name from local list
    const ft = this.fuelTypes.find(f => f.id === txn.fuelTypeId);
    this.fuelType = ft?.name || txn.fuelTypeName || 'Fuel';

    // Resolve station name (only once)
    if (!this.stationName && txn.stationId) {
      this.station = txn.stationId.substring(0, 8);
      this.stationsApi.getStationById(txn.stationId).pipe(
        takeUntil(this.destroy$),
        catchError(() => of(null))
      ).subscribe(s => {
        if (s) {
          this.stationName = s.stationName || s.name || txn.stationId.substring(0, 8);
          this.station = this.stationName;
        }
      });
    }
  }

  private startPolling(txnId: string): void {
    this.pollSub = interval(4000).pipe(takeUntil(this.destroy$)).subscribe(() => {
      this.loadTransaction(txnId);
    });
  }

  private stopPolling(): void {
    if (this.pollSub) {
      this.pollSub.unsubscribe();
      this.pollSub = undefined;
    }
  }

  private updateFulfillmentSteps(status: string): void {
    const stepMap: Record<string, number> = {
      'Initiated': 1,
      'StockReserved': 2,
      'Completed': 4,
      'Voided': 0,
      'Failed': 0,
    };
    const doneCount = stepMap[status] ?? 1;
    this.fulfillmentSteps.forEach((s, i) => { s.done = i < doneCount; });
  }

  navigateToStation(): void {
    this.router.navigate(['/dealer/dashboard']);
  }

  newSale(): void {
    this.router.navigate(['/dealer/sales/new']);
  }

  printReceipt(): void { window.print(); }

  downloadReceipt(): void {
    if (!this.transaction) return;
    // Generate a simple text receipt for download
    const lines = [
      '═══════════════════════════════════════',
      '              EPCL FUEL RECEIPT        ',
      '═══════════════════════════════════════',
      '',
      `Receipt No:    ${this.receiptNumber}`,
      `Date:          ${new Date(this.transaction.timestamp).toLocaleString()}`,
      `Station:       ${this.stationName || this.station}`,
      '',
      '───────────────────────────────────────',
      `Fuel Type:     ${this.fuelType}`,
      `Volume:        ${this.fuelVolume.toFixed(2)} L`,
      `Rate:          ₹${this.pricePerLitre.toFixed(2)} / L`,
      `Total Amount:  ₹${this.totalAmount.toFixed(2)}`,
      '───────────────────────────────────────',
      '',
      `Payment:       ${this.paymentMethod}`,
      `Vehicle:       ${this.vehicleNumber || 'N/A'}`,
      `Status:        ${this.status}`,
      '',
      '═══════════════════════════════════════',
      '         Thank you for fueling!        ',
      '           EPCL Fuel Management        ',
      '═══════════════════════════════════════',
    ];
    const blob = new Blob([lines.join('\n')], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `receipt-${this.receiptNumber}.txt`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
