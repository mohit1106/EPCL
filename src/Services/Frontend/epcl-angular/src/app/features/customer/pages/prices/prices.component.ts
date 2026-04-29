import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { SalesApiService, FuelPriceDto, FuelPriceHistoryPoint } from '../../../../core/services/sales-api.service';
import { NotificationsApiService } from '../../../../core/services/notifications-api.service';
import { StationsApiService, FuelTypeDto } from '../../../../core/services/stations-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

interface FuelCard {
  fuelTypeId: string;
  name: string;
  color: string;
  direction: 'up' | 'down' | 'stable';
  change: number;
  price: number;
  effectiveFrom: string;
  sparkData: number[];
}

interface ChartPoint {
  x: number;
  y: number;
}

@Component({
  selector: 'app-fuel-prices',
  templateUrl: './prices.component.html',
  styleUrls: ['./prices.component.scss'],
})
export class PricesComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  // Data
  prices: FuelPriceDto[] = [];
  fuelTypes: FuelTypeDto[] = [];
  selectedFuelType: string | null = null;
  priceHistory: FuelPriceHistoryPoint[] = [];
  isLoading = true;
  lastUpdated = new Date();

  // Cards
  fuelCards: FuelCard[] = [];

  // Chart
  periods = ['1W', '1M', '3M', 'YTD', '1Y'];
  selectedPeriod = '1M';
  chartWidth = 800;
  chartHeight = 240;
  chartPadding = 10;
  chartDataPoints: ChartPoint[] = [];
  yAxisLabels: number[] = [];
  xAxisLabels: string[] = [];
  gridLines: number[] = [];
  priceMin = 0;
  priceMax = 0;
  priceAvg = 0;

  // Alert Config
  selectedAlertFuelTypeId = '';
  alertThresholdValue: number | null = null;
  alertSaving = false;

  notificationChannels = [
    { name: 'SMS', active: true },
    { name: 'Email', active: true },
    { name: 'WhatsApp', active: false },
    { name: 'Push', active: true },
  ];

  private readonly cardColors = ['#1E40AF', '#10B981', '#F59E0B', '#8B5CF6', '#EF4444', '#06B6D4'];

  constructor(
    private salesApi: SalesApiService,
    private notifApi: NotificationsApiService,
    private stationsApi: StationsApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.stationsApi.getFuelTypes().pipe(takeUntil(this.destroy$)).subscribe({
      next: (ft) => {
        this.fuelTypes = ft;
        this.loadPrices();
      },
      error: () => {
        this.isLoading = false;
        this.toast.error('Failed to load fuel types');
      },
    });
  }

  private loadPrices(): void {
    this.salesApi.getFuelPrices().pipe(takeUntil(this.destroy$)).subscribe({
      next: (prices) => {
        this.prices = prices;
        this.lastUpdated = new Date();

        this.fuelCards = prices.map((p, i) => {
          const ftName = this.fuelTypes.find(f => f.id === p.fuelTypeId)?.name || 'Fuel';
          // Generate random spark data for visual representation
          const sparkData = this.generateSparkData();
          const direction = this.getRandomDirection();
          const change = direction === 'stable' ? 0 : +(Math.random() * 3).toFixed(1);
          return {
            fuelTypeId: p.fuelTypeId,
            name: ftName,
            color: this.cardColors[i % this.cardColors.length],
            direction,
            change: direction === 'down' ? -change : change,
            price: p.pricePerLitre,
            effectiveFrom: p.effectiveFrom,
            sparkData,
          };
        });

        this.isLoading = false;

        if (prices.length > 0) {
          this.selectFuelType(prices[0].fuelTypeId);
        }
      },
      error: () => {
        this.isLoading = false;
        this.toast.error('Failed to load fuel prices');
      },
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  selectFuelType(fuelTypeId: string): void {
    this.selectedFuelType = fuelTypeId;
    this.salesApi.getFuelPriceHistory(fuelTypeId, 90).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (history) => {
          this.priceHistory = history;
          this.updateChart();
        },
        error: () => {
          this.priceHistory = [];
          this.updateChart();
        },
      });
  }

  changePeriod(period: string): void {
    this.selectedPeriod = period;
    this.updateChart();
  }

  getSelectedFuelName(): string {
    if (!this.selectedFuelType) return 'All Fuels';
    const card = this.fuelCards.find(c => c.fuelTypeId === this.selectedFuelType);
    return card?.name || 'Fuel';
  }

  getFilteredHistory(): FuelPriceHistoryPoint[] {
    if (!this.priceHistory || this.priceHistory.length === 0) return [];
    let daysToKeep = 30;
    switch (this.selectedPeriod) {
      case '1W': daysToKeep = 7; break;
      case '1M': daysToKeep = 30; break;
      case '3M': daysToKeep = 90; break;
      case 'YTD': daysToKeep = 180; break;
      case '1Y': daysToKeep = 365; break;
    }
    return this.priceHistory.slice(-daysToKeep);
  }

  private updateChart(): void {
    const filtered = this.getFilteredHistory();
    if (filtered.length === 0) {
      this.chartDataPoints = [];
      this.yAxisLabels = [];
      this.xAxisLabels = [];
      this.gridLines = [];
      return;
    }

    const prices = filtered.map(p => p.price);
    const min = Math.min(...prices);
    const max = Math.max(...prices);
    const padding = (max - min) * 0.1 || 5;
    this.priceMin = min;
    this.priceMax = max;
    this.priceAvg = prices.reduce((a, b) => a + b, 0) / prices.length;

    const yMin = min - padding;
    const yMax = max + padding;
    const yRange = yMax - yMin || 1;

    // Generate 5 Y-axis labels
    this.yAxisLabels = [];
    for (let i = 4; i >= 0; i--) {
      this.yAxisLabels.push(yMin + (yRange * i) / 4);
    }

    // Grid lines (5 horizontal lines)
    this.gridLines = [];
    for (let i = 0; i < 5; i++) {
      this.gridLines.push((this.chartHeight * i) / 4);
    }

    // Calculate chart points
    this.chartDataPoints = filtered.map((p, i) => ({
      x: filtered.length === 1 ? this.chartWidth / 2 : (i / (filtered.length - 1)) * this.chartWidth,
      y: this.chartHeight - ((p.price - yMin) / yRange) * this.chartHeight,
    }));

    // X-axis labels (show ~5 labels evenly distributed)
    this.xAxisLabels = [];
    const labelCount = Math.min(5, filtered.length);
    for (let i = 0; i < labelCount; i++) {
      const idx = Math.floor((i / (labelCount - 1)) * (filtered.length - 1));
      const d = new Date(filtered[idx].date);
      this.xAxisLabels.push(d.toLocaleDateString('en-IN', { day: '2-digit', month: 'short' }));
    }
  }

  getChartPoints(): string {
    return this.chartDataPoints.map(p => `${p.x},${p.y}`).join(' ');
  }

  getAreaPath(): string {
    if (this.chartDataPoints.length === 0) return '';
    const pts = this.chartDataPoints;
    let d = `M ${pts[0].x},${this.chartHeight}`;
    d += ` L ${pts[0].x},${pts[0].y}`;
    for (let i = 1; i < pts.length; i++) {
      d += ` L ${pts[i].x},${pts[i].y}`;
    }
    d += ` L ${pts[pts.length - 1].x},${this.chartHeight} Z`;
    return d;
  }

  // Alert functionality
  createAlert(): void {
    if (!this.selectedAlertFuelTypeId || !this.alertThresholdValue) return;

    const activeChannels = this.notificationChannels
      .filter(ch => ch.active)
      .map(ch => ch.name.toLowerCase());

    this.alertSaving = true;
    this.notifApi.createPriceAlert({
      fuelTypeId: this.selectedAlertFuelTypeId,
      threshold: this.alertThresholdValue,
      channel: activeChannels.join(',') || 'push',
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.alertSaving = false;
        this.toast.success('Price alert created successfully!');
        this.alertThresholdValue = null;
      },
      error: () => {
        this.alertSaving = false;
        this.toast.error('Failed to create price alert. Please try again.');
      },
    });
  }

  // Helpers
  private generateSparkData(): number[] {
    const data: number[] = [];
    let val = 40 + Math.random() * 30;
    for (let i = 0; i < 12; i++) {
      val = Math.max(15, Math.min(95, val + (Math.random() - 0.5) * 20));
      data.push(Math.round(val));
    }
    return data;
  }

  private getRandomDirection(): 'up' | 'down' | 'stable' {
    const r = Math.random();
    if (r < 0.3) return 'up';
    if (r < 0.6) return 'down';
    return 'stable';
  }
}
