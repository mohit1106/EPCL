import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { SalesApiService, FuelPriceDto, FuelPriceHistoryPoint } from '../../../../core/services/sales-api.service';
import { StationsApiService, FuelTypeDto } from '../../../../core/services/stations-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-admin-prices',
  templateUrl: './prices.component.html',
  styleUrls: ['./prices.component.scss'],
})
export class AdminPricesComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  prices: FuelPriceDto[] = [];
  fuelTypes: FuelTypeDto[] = [];
  priceHistory: FuelPriceHistoryPoint[] = [];
  selectedFuelTypeId: string | null = null;
  isLoading = true;

  // Update form
  updateFuelTypeId = '';
  updatePrice = 0;
  updateEffectiveFrom = '';
  isUpdating = false;
  showUpdateModal = false;

  fuelPrices = [
    { name: 'PETROL 95', price: 1.45, change: '+0.02', changeClass: 'text-danger', color: '#10b981', lastUpdate: '14:02 UTC' },
    { name: 'DIESEL', price: 1.32, change: '-0.01', changeClass: 'text-success', color: '#f59e0b', lastUpdate: '14:02 UTC' }
  ];
  chartBars = [50, 45, 60, 55, 70, 65, 80, 75, 90, 85, 100, 95];
  selectedFuel = 'PETROL 95 (REGULAR)';
  newPrice = 1.48;
  projectedImpact = '+$14,200';
  complianceShift = '-0.4%';

  auditTrail = [
    { timestamp: '2023-11-09T14:22:01Z', fuelId: 'F-95', changeType: 'BASE INC', adjustment: '+0.02', operator: 'SYS_ADMIN', status: 'COMMITTED' },
    { timestamp: '2023-11-08T09:14:22Z', fuelId: 'F-DSL', changeType: 'BASE DEC', adjustment: '-0.01', operator: 'SYS_ADMIN', status: 'COMMITTED' }
  ];

  constructor(
    private salesApi: SalesApiService,
    private stationsApi: StationsApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.loadPrices();
    this.stationsApi.getFuelTypes().pipe(takeUntil(this.destroy$)).subscribe(ft => this.fuelTypes = ft);
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  private loadPrices(): void {
    this.isLoading = true;
    this.salesApi.getFuelPrices().pipe(takeUntil(this.destroy$)).subscribe({
      next: (prices) => {
        this.prices = prices;
        this.isLoading = false;
        if (prices.length > 0 && !this.selectedFuelTypeId) {
          this.selectFuelType(prices[0].fuelTypeId);
        }
      },
      error: () => { this.isLoading = false; },
    });
  }

  selectFuelType(fuelTypeId: string): void {
    this.selectedFuelTypeId = fuelTypeId;
    this.salesApi.getFuelPriceHistory(fuelTypeId, 90).pipe(takeUntil(this.destroy$))
      .subscribe(h => this.priceHistory = h);
  }

  openUpdateModal(fuelTypeId: string, currentPrice: number): void {
    this.updateFuelTypeId = fuelTypeId;
    this.updatePrice = currentPrice;
    this.updateEffectiveFrom = new Date().toISOString().split('T')[0];
    this.showUpdateModal = true;
  }

  closeUpdateModal(): void { this.showUpdateModal = false; }

  updateFuelPrice(): void {
    if (this.updatePrice <= 0) { this.toast.error('Price must be positive.'); return; }
    if (!confirm(`Are you sure you want to update the price to ₹${this.updatePrice}/L? This will affect all stations.`)) return;
    this.isUpdating = true;
    this.salesApi.setFuelPrice(this.updateFuelTypeId, this.updatePrice, this.updateEffectiveFrom)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toast.success('Price updated successfully!');
          this.isUpdating = false;
          this.showUpdateModal = false;
          this.loadPrices();
        },
        error: () => { this.toast.error('Failed to update price.'); this.isUpdating = false; },
      });
  }

  getStatusClass(status: string): string {
    return status === 'COMMITTED' ? 'status-ok' : 'status-err';
  }
}
