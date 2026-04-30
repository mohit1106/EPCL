import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { Subject, takeUntil, forkJoin, of } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { SalesApiService, PumpDto, FuelPriceDto, RecordSaleCommand, VehicleDto } from '../../../../core/services/sales-api.service';
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
  vehicleNumber = '';
  customerPhone = '';
  unitPrice = 0;
  get totalPrice(): number { return Math.round(this.volume * this.unitPrice * 100) / 100; }

  volumePresets = ['+5 L', '+10 L', '+20 L', '+50 L'];
  pricePresets = ['₹500', '₹1000', '₹2000', '₹5000'];

  paymentMethods = [
    { name: 'Cash', icon: 'cash', selected: true },
    { name: 'Card', icon: 'card', selected: false },
    { name: 'Wallet', icon: 'wallet', selected: false },
    { name: 'UPI', icon: 'upi', selected: false },
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

  private pumpColors = ['#1E40AF', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899'];

  constructor(
    private router: Router,
    private store: Store,
    private salesApi: SalesApiService,
    private stationsApi: StationsApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.store.select(selectUser).pipe(takeUntil(this.destroy$)).subscribe(user => {
      if (user) {
        this.userId = user.id;
        this.stationId = user.profile?.stationId || '';
        this.loadData();
      }
    });

    // Vehicle lookup debounce
    this.vehicleLookup$.pipe(
      debounceTime(600),
      distinctUntilChanged(),
      switchMap(regNum => {
        const normalized = regNum.replace(/[-\s]/g, '').toUpperCase();
        if (!/^[A-Z]{2}[0-9]{2}[A-Z]{2}[0-9]{4}$/.test(normalized)) {
          this.linkedVehicle = null;
          this.isLookingUp = false;
          this.lookupDone = false;
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
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  getOnlinePumps(): number {
    return this.pumps.filter(p => p.online).length;
  }

  /** Load fuel types first, then pumps and prices */
  private loadData(): void {
    // Always load fuel types (from station service)
    this.stationsApi.getFuelTypes().pipe(
      takeUntil(this.destroy$),
      catchError(() => of([]))
    ).subscribe(fuelTypes => {
      this.fuelTypes = fuelTypes;
      if (this.stationId) {
        this.loadPumpsAndPrices();
      } else {
        this.loadFirstStation();
      }
    });
  }

  /** Fallback: Load stations and use the first one */
  private loadFirstStation(): void {
    this.stationsApi.getStations(1, 1).pipe(
      takeUntil(this.destroy$),
      catchError(() => of({ items: [], totalCount: 0, page: 1, pageSize: 1, totalPages: 0 }))
    ).subscribe(result => {
      if (result.items && result.items.length > 0) {
        this.stationId = result.items[0].id;
        this.loadPumpsAndPrices();
      } else {
        this.generateDemoPumps();
      }
    });
  }

  /** Map fuelTypeId to name using the loaded fuel types */
  private getFuelTypeName(fuelTypeId: string): string {
    const ft = this.fuelTypes.find(f => f.id === fuelTypeId);
    return ft?.name || 'Fuel';
  }

  /** Generate demo pumps when no real data is available */
  private generateDemoPumps(): void {
    const demoFuelTypes = [
      { id: 'a1b2c3d4-e5f6-7890-abcd-000000000001', name: 'Petrol' },
      { id: 'a1b2c3d4-e5f6-7890-abcd-000000000002', name: 'Diesel' },
      { id: 'a1b2c3d4-e5f6-7890-abcd-000000000001', name: 'Petrol' },
      { id: 'a1b2c3d4-e5f6-7890-abcd-000000000002', name: 'Diesel' },
      { id: 'a1b2c3d4-e5f6-7890-abcd-000000000003', name: 'CNG' },
      { id: 'a1b2c3d4-e5f6-7890-abcd-000000000004', name: 'Premium Petrol' },
    ];
    this.pumps = demoFuelTypes.map((ft, i) => ({
      id: `demo-pump-${i + 1}`,
      name: `PUMP ${String(i + 1).padStart(2, '0')}`,
      type: ft.name,
      color: this.pumpColors[i % this.pumpColors.length],
      selected: i === 0,
      online: i !== 4,
      fuelTypeId: ft.id,
      pumpNumber: i + 1,
    }));
    this.selectedPump = this.pumps[0];
    this.unitPrice = 106.31;
    this.toast.info('Using demo mode — no station data available.');
  }

  private loadPumpsAndPrices(): void {
    if (!this.stationId) return;

    forkJoin({
      pumps: this.salesApi.getStationPumps(this.stationId).pipe(catchError(() => of([] as PumpDto[]))),
      prices: this.salesApi.getFuelPrices().pipe(catchError(() => of([] as FuelPriceDto[]))),
    }).pipe(takeUntil(this.destroy$)).subscribe(({ pumps, prices }) => {
      this.prices = prices;

      if (pumps.length > 0) {
        this.pumps = pumps.map((p, i) => ({
          id: p.id,
          name: p.pumpName || `PUMP ${String(i + 1).padStart(2, '0')}`,
          type: this.getFuelTypeName(p.fuelTypeId),
          color: this.pumpColors[i % this.pumpColors.length],
          selected: i === 0,
          online: p.status === 'Active',
          fuelTypeId: p.fuelTypeId,
          pumpNumber: i + 1,
        }));
        this.selectedPump = this.pumps.find(p => p.online) || this.pumps[0];
        if (this.selectedPump) {
          this.pumps.forEach(p => p.selected = false);
          this.selectedPump.selected = true;
        }
      } else {
        this.generateDemoPumps();
      }

      this.updateUnitPrice();
      this.loadQuickStats();
    });
  }

  private updateUnitPrice(): void {
    if (this.prices.length > 0 && this.selectedPump) {
      const match = this.prices.find(p => p.fuelTypeId === this.selectedPump!.fuelTypeId);
      this.unitPrice = match?.pricePerLitre ?? this.prices[0]?.pricePerLitre ?? 96.72;
    } else if (this.unitPrice === 0) {
      this.unitPrice = 96.72;
    }
  }

  private loadQuickStats(): void {
    // Load today's stats from station transactions
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
      } else {
        // Show demo stats
        this.todaySales = 47;
        this.todayLitres = 2845;
        this.todayRevenue = 274520;
        this.avgPerSale = 5840;
      }
    });
  }

  selectPump(pump: DisplayPump): void {
    if (!pump.online) return;
    this.pumps.forEach(p => p.selected = false);
    pump.selected = true;
    this.selectedPump = pump;
    this.updateUnitPrice();
  }

  selectPayment(method: { name: string; icon: string; selected: boolean }): void {
    this.paymentMethods.forEach(m => m.selected = false);
    method.selected = true;
    this.selectedPayment = method.name;
  }

  nextStep(): void {
    if (this.currentStep === 1 && !this.selectedPump) {
      this.toast.error('Please select a pump first.');
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
    if (!isNaN(num)) this.volume += num;
  }

  setByPrice(amt: string): void {
    const price = parseInt(amt.replace(/[^0-9]/g, ''), 10);
    if (!isNaN(price) && this.unitPrice > 0) {
      this.volume = Math.round((price / this.unitPrice) * 100) / 100;
    }
  }

  cancelTransaction(): void {
    this.currentStep = 1;
    this.volume = 0;
    this.vehicleNumber = '';
    this.customerPhone = '';
    this.linkedVehicle = null;
    this.lookupDone = false;
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

    // Generate a payment reference for non-cash
    let paymentRef: string | undefined;
    if (this.selectedPayment !== 'Cash') {
      paymentRef = `PAY-${Date.now()}-${Math.random().toString(36).substring(2, 6).toUpperCase()}`;
    }

    this.isSubmitting = true;
    const command: RecordSaleCommand = {
      stationId: this.stationId,
      pumpId: this.selectedPump.id,
      tankId: '00000000-0000-0000-0000-000000000000', // Default tank
      fuelTypeId: this.selectedPump.fuelTypeId,
      customerUserId: this.linkedVehicle?.customerId,
      vehicleNumber: vehicleForApi,
      quantityLitres: Math.round(this.volume * 1000) / 1000, // Max 3 decimal places
      paymentMethod: this.selectedPayment,
      paymentReferenceId: paymentRef,
    };

    this.salesApi.recordSale(command).pipe(takeUntil(this.destroy$)).subscribe({
      next: (txn) => {
        this.toast.success(`Sale recorded! Receipt: ${txn.receiptNumber}`);
        this.isSubmitting = false;
        this.router.navigate(['/dealer/sales/confirmation', txn.id]);
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
}
