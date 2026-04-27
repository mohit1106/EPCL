import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { SalesApiService, FuelPriceDto, FuelPriceHistoryPoint } from '../../../../core/services/sales-api.service';
import { NotificationsApiService } from '../../../../core/services/notifications-api.service';
import { StationsApiService, FuelTypeDto } from '../../../../core/services/stations-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-fuel-prices',
  templateUrl: './prices.component.html',
  styleUrls: ['./prices.component.scss'],
})
export class PricesComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  prices: FuelPriceDto[] = [];
  fuelTypes: FuelTypeDto[] = [];
  selectedFuelType: string | null = null;
  priceHistory: FuelPriceHistoryPoint[] = [];
  isLoading = true;

  fuelCards: { name: string; color: string; direction: string; change: number; price: number }[] = [];
  periods = ['1W', '1M', '3M', 'YTD', '1Y'];
  selectedPeriod = '1M';
  
  fuelCategories = ['Petrol 95 (Regular)', 'Petrol 98 (Premium)', 'Diesel (Auto)', 'CNG'];
  selectedCategory = 'Petrol 95 (Regular)';
  alertThreshold = '< $1.15';

  notificationChannels = [
    { icon: '📱', name: 'SMS', active: true },
    { icon: '✉', name: 'Email', active: true },
    { icon: '💬', name: 'WhatsApp', active: false },
    { icon: '🔔', name: 'Push', active: true },
  ];

  constructor(
    private salesApi: SalesApiService,
    private notifApi: NotificationsApiService,
    private stationsApi: StationsApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.stationsApi.getFuelTypes().pipe(takeUntil(this.destroy$)).subscribe(ft => {
      this.fuelTypes = ft;
      this.loadPrices();
    });
  }

  private loadPrices(): void {
    this.salesApi.getFuelPrices().pipe(takeUntil(this.destroy$)).subscribe({
      next: (prices) => {
        this.prices = prices;
        
        // Map API data to UI structure
        this.fuelCards = prices.map((p, i) => {
          const colors = ['#6366f1', '#10b981', '#f59e0b', '#ef4444'];
          const ftName = this.fuelTypes.find(f => f.id === p.fuelTypeId)?.name || 'Fuel';
          return {
            name: ftName,
            color: colors[i % colors.length],
            direction: 'stable',
            change: 0,
            price: p.pricePerLitre
          };
        });

        this.isLoading = false;
        if (prices.length > 0) {
          this.selectFuelType(prices[0].fuelTypeId);
        }
      },
      error: () => { this.isLoading = false; },
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  selectFuelType(fuelTypeId: string): void {
    this.selectedFuelType = fuelTypeId;
    this.salesApi.getFuelPriceHistory(fuelTypeId, 30).pipe(takeUntil(this.destroy$))
      .subscribe(history => this.priceHistory = history);
  }

  setPriceAlert(fuelTypeId: string, threshold: number): void {
    this.notifApi.createPriceAlert({ fuelTypeId, threshold, channel: 'push' })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => this.toast.success('Price alert created!'),
        error: () => this.toast.error('Failed to create price alert.'),
      });
  }

  getPriceChange(price: FuelPriceDto): { value: number; direction: string } {
    // Calculate from history if available
    return { value: 0, direction: 'stable' };
  }

  getChartPoints(): string {
    if (!this.priceHistory || this.priceHistory.length === 0) return '';
    let daysToKeep = 30;
    if (this.selectedPeriod === '1W') daysToKeep = 7;
    else if (this.selectedPeriod === '1M') daysToKeep = 30;
    else if (this.selectedPeriod === '3M') daysToKeep = 90;
    
    // For mock purposes, just slice the array if we have enough data
    const filteredHistory = this.priceHistory.slice(-daysToKeep);
    if (filteredHistory.length === 0) return '';

    const min = Math.min(...filteredHistory.map(p => p.price));
    const max = Math.max(...filteredHistory.map(p => p.price));
    const range = max - min || 1;
    
    return filteredHistory.map((p, i) => {
      const x = (i / (filteredHistory.length - 1)) * 100;
      const y = 100 - (((p.price - min) / range) * 80 + 10); // leave 10% padding
      return `${x},${y}`;
    }).join(' ');
  }
  getFilteredHistory(): FuelPriceHistoryPoint[] {
    if (!this.priceHistory || this.priceHistory.length === 0) return [];
    let daysToKeep = 30;
    if (this.selectedPeriod === '1W') daysToKeep = 7;
    else if (this.selectedPeriod === '1M') daysToKeep = 30;
    else if (this.selectedPeriod === '3M') daysToKeep = 90;
    return this.priceHistory.slice(-daysToKeep);
  }
}
