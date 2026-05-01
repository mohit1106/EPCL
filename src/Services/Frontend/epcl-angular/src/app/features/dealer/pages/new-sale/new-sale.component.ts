import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { Subject, takeUntil, forkJoin, of } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { SalesApiService, PumpDto, FuelPriceDto, RecordSaleCommand, VehicleDto, CustomerWalletDto } from '../../../../core/services/sales-api.service';
import { StationsApiService, FuelTypeDto } from '../../../../core/services/stations-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

interface DisplayPump {
  id: string;
  name: string;
  type: string;
  color: string;
  selected: boolean;
  online: boolean;
  fuelTypeId: string;
  pumpNumber: number;
  status: string;
  unitPrice: number;
}

@Component({
  selector: 'app-new-sale',
  templateUrl: './new-sale.component.html',
  styleUrls: ['./new-sale.component.scss'],
})
export class NewSaleComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private stationId = '';
  private userId = '';

  currentStep = 1;
  isSubmitting = false;
  txnId = 'TXN-' + Math.random().toString(36).substring(2, 8).toUpperCase();
  today = new Date();

  pumps: DisplayPump[] = [];
  selectedPump: DisplayPump | null = null;
  prices: FuelPriceDto[] = [];
  fuelTypes: FuelTypeDto[] = [];

  volume = 0;
  totalPriceInput = 0;
  vehicleNumber = '';
  customerPhone = '';
  customerId = '';
  unitPrice = 0;
  get totalPrice(): number { return Math.round(this.volume * this.unitPrice * 100) / 100; }

  volumePresets = ['+5 L', '+10 L', '+20 L', '+50 L'];
  pricePresets = ['₹500', '₹1000', '₹2000', '₹5000'];

  paymentMethods = [
    { name: 'Cash', icon: 'cash', selected: true, requiresRegistered: false },
    { name: 'UPI', icon: 'upi', selected: false, requiresRegistered: true },
    { name: 'Card', icon: 'card', selected: false, requiresRegistered: true },
    { name: 'Wallet', icon: 'wallet', selected: false, requiresRegistered: true },
  ];
  selectedPayment = 'Cash';

  // Barcode decoration
  barcodeLines = Array.from({ length: 24 }, () => ({
    w: Math.random() > 0.5 ? 3 : 2,
    h: 70 + Math.random() * 30,
  }));

  // Quick stats
  todaySales = 0;
  todayLitres = 0;
  todayRevenue = 0;
  avgPerSale = 0;

  // Vehicle lookup
  private vehicleLookup$ = new Subject<string>();
  linkedVehicle: VehicleDto | null = null;
  isLookingUp = false;
  lookupDone = false;

  // Customer wallet balance (for wallet payment)
  customerWallet: CustomerWalletDto | null = null;
  isLoadingWallet = false;

  private pumpColors = ['#1E40AF', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899'];

  // Pump status mapping
  pumpStatusLabels: Record<string, string> = {
    'Active': 'Online',
    'UnderMaintenance': 'Under Maintenance',
    'OutOfService': 'Out of Service',
    'Paused': 'Paused',
  };

  constructor(
    private router: Router,
    private store: Store,
    private salesApi: SalesApiService,
    private stationsApi: StationsApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.store.select(selectUser).pipe(
      takeUntil(this.destroy$),
      switchMap(user => {
        if (!user) return of(null);
        this.userId = user.id;
        if (user.profile?.stationId) {
          this.stationId = user.profile.stationId;
          return of('resolved');
        }
        return this.stationsApi.getMyStation(user.id).pipe(
          catchError(() => of(null))
        );
      })
    ).subscribe(result => {
      if (result && typeof result === 'object' && (result as any).id) {
        this.stationId = (result as any).id;
      }
      this.loadData();
    });

    // Vehicle lookup debounce
    this.vehicleLookup$.pipe(
      debounceTime(600),
      distinctUntilChanged(),
      switchMap(regNum => {
        const normalized = regNum.replace(/[-\s]/g, '').toUpperCase();
        if (normalized.length < 6) {
          this.linkedVehicle = null;
          this.customerWallet = null;
          this.isLookingUp = false;
          this.lookupDone = false;
          this.enforcePaymentRestrictions();
          return of(null);
        }
        this.isLookingUp = true;
        this.lookupDone = false;
        return this.salesApi.lookupVehicle(normalized).pipe(catchError(() => of(null)));
      }),
      takeUntil(this.destroy$)
    ).subscribe(v => {
      this.linkedVehicle = v;
      this.isLookingUp = false;
      this.lookupDone = true;
      this.enforcePaymentRestrictions();
      // If we found a registered vehicle, fetch customer wallet balance
      if (v?.customerId) {
        this.loadCustomerWallet(v.customerId);
      } else {
        this.customerWallet = null;
      }
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  getOnlinePumps(): number {
    return this.pumps.filter(p => p.online).length;
  }

  /** Check if no registered vehicle found — restrict to Cash only */
  get isUnregisteredVehicle(): boolean {
    // If no vehicle number entered at all, treat as unregistered
    const vn = this.vehicleNumber.replace(/[-\s]/g, '').toUpperCase();
    if (vn.length < 6) return true;
    // If lookup completed and vehicle not found, unregistered
    return this.lookupDone && !this.linkedVehicle;
  }

  /** Check if a payment method is available based on vehicle registration */
  isPaymentDisabled(method: { requiresRegistered: boolean }): boolean {
    return method.requiresRegistered && this.isUnregisteredVehicle;
  }

  /** Enforce payment restrictions when vehicle changes */
  private enforcePaymentRestrictions(): void {
    if (this.isUnregisteredVehicle) {
      const selectedMethod = this.paymentMethods.find(m => m.selected);
      if (selectedMethod && selectedMethod.requiresRegistered) {
        // Reset to Cash
        this.paymentMethods.forEach(m => m.selected = false);
        this.paymentMethods[0].selected = true;
        this.selectedPayment = 'Cash';
      }
    }
  }

  /** Load customer wallet balance */
  private loadCustomerWallet(customerId: string): void {
    this.isLoadingWallet = true;
    this.salesApi.getCustomerWalletBalance(customerId).pipe(
      takeUntil(this.destroy$),
      catchError(() => of(null))
    ).subscribe(wallet => {
      this.customerWallet = wallet;
      this.isLoadingWallet = false;
    });
  }

  /** Load fuel types first, then pumps and prices */
  private loadData(): void {
    this.stationsApi.getFuelTypes().pipe(
      takeUntil(this.destroy$),
      catchError(() => of([]))
    ).subscribe(fuelTypes => {
      this.fuelTypes = fuelTypes;
      if (this.stationId) {
        this.loadPumpsAndPrices();
      } else {
        this.toast.warning('No station assigned to your account. Contact admin.');
      }
    });
  }

  /** Map fuelTypeId to name using the loaded fuel types */
  private getFuelTypeName(fuelTypeId: string): string {
    const ft = this.fuelTypes.find(f => f.id === fuelTypeId);
    return ft?.name || 'Fuel';
  }

  /** Get price for a fuel type */
  private getFuelTypePrice(fuelTypeId: string): number {
    const match = this.prices.find(p => p.fuelTypeId === fuelTypeId);
    return match?.pricePerLitre ?? 0;
  }

  private loadPumpsAndPrices(): void {
    if (!this.stationId) return;

    forkJoin({
      pumps: this.salesApi.getStationPumps(this.stationId).pipe(catchError(() => of([] as PumpDto[]))),
      prices: this.salesApi.getFuelPrices().pipe(catchError(() => of([] as FuelPriceDto[]))),
    }).pipe(takeUntil(this.destroy$)).subscribe(({ pumps, prices }) => {
      this.prices = prices;

      if (pumps.length > 0) {
        this.pumps = pumps.map((p, i) => {
          const fuelName = this.getFuelTypeName(p.fuelTypeId);
          const price = this.getFuelTypePrice(p.fuelTypeId);
          return {
            id: p.id,
            name: p.pumpName || `PUMP ${String(i + 1).padStart(2, '0')}`,
            type: fuelName,
            color: this.pumpColors[i % this.pumpColors.length],
            selected: false,
            online: p.status === 'Active',
            fuelTypeId: p.fuelTypeId,
            pumpNumber: i + 1,
            status: p.status,
            unitPrice: price,
          };
        });
        this.selectedPump = this.pumps.find(p => p.online) || this.pumps[0];
        if (this.selectedPump) {
          this.selectedPump.selected = true;
        }
      } else {
        this.toast.warning('No pumps configured for this station. Add pumps from your Dashboard.');
      }

      this.updateUnitPrice();
      this.loadQuickStats();
    });
  }

  private updateUnitPrice(): void {
    if (this.selectedPump) {
      if (this.selectedPump.unitPrice > 0) {
        this.unitPrice = this.selectedPump.unitPrice;
      } else {
        const match = this.prices.find(p => p.fuelTypeId === this.selectedPump!.fuelTypeId);
        this.unitPrice = match?.pricePerLitre ?? this.prices[0]?.pricePerLitre ?? 96.72;
      }
    } else if (this.unitPrice === 0) {
      this.unitPrice = 96.72;
    }
  }

  private loadQuickStats(): void {
    const todayStr = new Date().toISOString().split('T')[0];
    this.salesApi.getDailySummary(this.stationId, todayStr).pipe(
      takeUntil(this.destroy$),
      catchError(() => of(null))
    ).subscribe(summary => {
      if (summary) {
        this.todaySales = summary.totalTransactions;
        this.todayLitres = summary.totalLitres;
        this.todayRevenue = summary.totalRevenue;
        this.avgPerSale = this.todaySales > 0 ? this.todayRevenue / this.todaySales : 0;
      }
    });
  }

  selectPump(pump: DisplayPump): void {
    if (!pump.online) return;
    this.pumps.forEach(p => p.selected = false);
    pump.selected = true;
    this.selectedPump = pump;
    this.updateUnitPrice();
    // Reset volume when pump changes
    this.volume = 0;
    this.totalPriceInput = 0;
  }

  selectPayment(method: { name: string; icon: string; selected: boolean; requiresRegistered: boolean }): void {
    if (this.isPaymentDisabled(method)) {
      this.toast.error('This payment method requires a registered vehicle.');
      return;
    }
    this.paymentMethods.forEach(m => m.selected = false);
    method.selected = true;
    this.selectedPayment = method.name;
  }

  nextStep(): void {
    if (this.currentStep === 1 && !this.selectedPump) {
      this.toast.error('Please select a pump.');
      return;
    }
    if (this.currentStep === 2 && this.volume <= 0) {
      this.toast.error('Please enter a valid volume.');
      return;
    }
    if (this.currentStep < 3) this.currentStep++;
  }

  prevStep(): void { if (this.currentStep > 1) this.currentStep--; }

  addVolume(amt: string): void {
    const num = parseInt(amt.replace(/[^0-9]/g, ''), 10);
    if (!isNaN(num)) {
      this.volume += num;
      this.totalPriceInput = this.totalPrice;
    }
  }

  setByPrice(amt: string): void {
    const price = parseInt(amt.replace(/[^0-9]/g, ''), 10);
    if (!isNaN(price) && this.unitPrice > 0) {
      this.volume = Math.round((price / this.unitPrice) * 100) / 100;
      this.totalPriceInput = this.totalPrice;
    }
  }

  /** When user types in total price field, auto-calc volume */
  onTotalPriceChange(): void {
    if (this.totalPriceInput > 0 && this.unitPrice > 0) {
      this.volume = Math.round((this.totalPriceInput / this.unitPrice) * 100) / 100;
    }
  }

  /** When user types in volume field, auto-calc total price display */
  onVolumeChange(): void {
    this.totalPriceInput = this.totalPrice;
  }

  cancelTransaction(): void {
    this.currentStep = 1;
    this.volume = 0;
    this.totalPriceInput = 0;
    this.vehicleNumber = '';
    this.customerPhone = '';
    this.customerId = '';
    this.linkedVehicle = null;
    this.customerWallet = null;
    this.lookupDone = false;
    if (this.selectedPump) {
      this.selectedPump.selected = false;
      this.selectedPump = null;
    }
    this.paymentMethods.forEach(m => m.selected = false);
    this.paymentMethods[0].selected = true;
    this.selectedPayment = 'Cash';
    this.txnId = 'TXN-' + Math.random().toString(36).substring(2, 8).toUpperCase();
    this.toast.info('Transaction cancelled.');
  }

  onVehicleNumberInput(): void {
    this.vehicleLookup$.next(this.vehicleNumber);
  }

  completeSale(): void {
    if (!this.selectedPump || this.volume <= 0) {
      this.toast.error('Please select a pump and enter a valid volume.');
      return;
    }

    // If in demo mode, show success
    if (!this.stationId || this.selectedPump.id.startsWith('demo-')) {
      this.toast.success(`Demo sale recorded! Receipt: ${this.txnId}`);
      this.cancelTransaction();
      return;
    }

    // Validate vehicle number — backend requires Indian RTO format XX00XX0000
    const vn = this.vehicleNumber.replace(/[-\s]/g, '').toUpperCase();
    const vehicleForApi = /^[A-Z]{2}[0-9]{2}[A-Z]{2}[0-9]{4}$/.test(vn) ? vn : 'MH00XX0000';

    // Generate a payment reference for non-cash, non-wallet
    let paymentRef: string | undefined;
    if (this.selectedPayment !== 'Cash' && this.selectedPayment !== 'Wallet') {
      paymentRef = `PAY-${Date.now()}-${Math.random().toString(36).substring(2, 6).toUpperCase()}`;
    }
    if (this.selectedPayment === 'Wallet') {
      paymentRef = `WALLET-PENDING-${Date.now()}`;
    }

    this.isSubmitting = true;
    const command: RecordSaleCommand = {
      stationId: this.stationId,
      pumpId: this.selectedPump.id,
      tankId: '00000000-0000-0000-0000-000000000000',
      fuelTypeId: this.selectedPump.fuelTypeId,
      customerUserId: this.linkedVehicle?.customerId,
      vehicleNumber: vehicleForApi,
      quantityLitres: Math.round(this.volume * 1000) / 1000,
      paymentMethod: this.selectedPayment,
      paymentReferenceId: paymentRef,
    };

    this.salesApi.recordSale(command).pipe(takeUntil(this.destroy$)).subscribe({
      next: (txn) => {
        // If wallet payment, create a wallet payment request
        if (this.selectedPayment === 'Wallet' && this.linkedVehicle?.customerId) {
          this.createWalletPaymentRequest(txn.id, this.linkedVehicle.customerId, txn.totalAmount);
        } else {
          this.toast.success(`Sale recorded! Receipt: ${txn.receiptNumber}`);
          this.isSubmitting = false;
          this.router.navigate(['/dealer/sales/confirmation', txn.id]);
        }
      },
      error: (err) => {
        const msg = err?.error?.message || err?.error?.title
          || err?.error?.errors?.join(', ')
          || (typeof err?.error === 'string' ? err.error : 'Sale failed. Please try again.');
        this.toast.error(msg);
        this.isSubmitting = false;
      },
    });
  }

  /** Create a wallet payment request after recording sale */
  private createWalletPaymentRequest(saleId: string, customerId: string, amount: number): void {
    this.salesApi.createWalletPaymentRequest({
      saleTransactionId: saleId,
      customerId,
      amount,
      description: `Fuel sale — ${this.selectedPump?.type || 'Fuel'} ${this.volume}L at ${this.selectedPump?.name}`,
      vehicleNumber: this.vehicleNumber,
      fuelTypeName: this.selectedPump?.type || 'Fuel',
      quantityLitres: this.volume,
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toast.success('Sale recorded! Wallet payment request sent to customer for approval.');
        this.isSubmitting = false;
        // Do NOT cancel. Redirect to confirmation page.
        this.router.navigate(['/dealer/sales/confirmation', saleId]);
      },
      error: (err) => {
        this.toast.error(err?.error?.message || 'Failed to send wallet request.');
        this.isSubmitting = false;
        // Still navigate so they see the Failed/Initiated state
        this.router.navigate(['/dealer/sales/confirmation', saleId]);
      }
    });
  }
}
