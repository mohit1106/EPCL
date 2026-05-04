import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil, of, catchError, timeout } from 'rxjs';
import { SalesApiService, FuelPriceDto, FuelPriceHistoryPoint } from '../../../../core/services/sales-api.service';
import { StationsApiService, FuelTypeDto } from '../../../../core/services/stations-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

interface PriceCard {
  fuelTypeId: string;
  fuelTypeName: string;
  pricePerLitre: number;
  effectiveFrom: string;
  isActive: boolean;
  color: string;
}

@Component({
  selector: 'app-admin-prices',
  templateUrl: './prices.component.html',
  styleUrls: ['./prices.component.scss'],
})
export class AdminPricesComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  isLoading = true;

  // Raw data
  prices: FuelPriceDto[] = [];
  allPrices: FuelPriceDto[] = [];
  fuelTypes: FuelTypeDto[] = [];
  priceHistory: FuelPriceHistoryPoint[] = [];

  // Merged price cards
  priceCards: PriceCard[] = [];

  // KPIs
  totalFuelTypes = 0;
  averagePrice = 0;
  lastUpdated = '';

  // Chart
  selectedChartFuelId = '';
  chartBars: { label: string; value: number; height: number }[] = [];
  chartMaxPrice = 0;
  chartMinPrice = 0;

  // Update form
  updateFuelTypeId = '';
  updatePrice: number | null = null;
  updateEffectiveFrom = '';
  useImmediateEffective = false;
  isUpdating = false;

  // Fuel type colors
  private fuelColors: Record<string, string> = {
    'Petrol': '#3B82F6',
    'Diesel': '#F59E0B',
    'Premium Petrol': '#8B5CF6',
    'CNG': '#10B981',
    'LPG': '#EF4444',
    'EV Charging': '#06B6D4',
  };

  // Track how many loads are pending
  private pendingLoads = 0;

  constructor(
    private salesApi: SalesApiService,
    private stationsApi: StationsApiService,
    private toast: ToastService,
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ═══ Data Loading — each source loads independently, no forkJoin ═══
  private loadData(): void {
    this.isLoading = true;
    this.pendingLoads = 2; // prices + fuelTypes (allPrices is bonus, non-blocking)

    // 1. Load active prices
    this.salesApi.getFuelPrices().pipe(
      timeout(8000),
      catchError(() => of([] as FuelPriceDto[])),
      takeUntil(this.destroy$),
    ).subscribe(prices => {
      this.prices = Array.isArray(prices) ? prices : [];
      this.onLoadComplete();
    });

    // 2. Load fuel types
    this.stationsApi.getFuelTypes().pipe(
      timeout(8000),
      catchError(() => of([] as FuelTypeDto[])),
      takeUntil(this.destroy$),
    ).subscribe(fuelTypes => {
      this.fuelTypes = Array.isArray(fuelTypes) ? fuelTypes : [];
      this.onLoadComplete();
    });

    // 3. Load ALL prices for audit trail (non-blocking — page works without it)
    this.salesApi.getAllFuelPrices().pipe(
      timeout(8000),
      catchError(() => of([] as FuelPriceDto[])),
      takeUntil(this.destroy$),
    ).subscribe(allPrices => {
      this.allPrices = Array.isArray(allPrices) ? allPrices : [];
    });
  }

  private onLoadComplete(): void {
    this.pendingLoads--;
    if (this.pendingLoads <= 0) {
      this.buildPriceCards();
      this.computeKPIs();
      if (this.priceCards.length > 0 && !this.selectedChartFuelId) {
        this.selectChartFuel(this.priceCards[0].fuelTypeId);
      }
      this.isLoading = false;
    }
  }

  private buildPriceCards(): void {
    const colorKeys = Object.keys(this.fuelColors);
    const defaultColors = ['#3B82F6', '#F59E0B', '#8B5CF6', '#10B981', '#EF4444', '#06B6D4', '#EC4899', '#84CC16'];

    this.priceCards = this.fuelTypes.map((ft, idx) => {
      const price = this.prices.find(p => p.fuelTypeId === ft.id && p.isActive);
      const colorMatch = colorKeys.find(k => ft.name.toLowerCase().includes(k.toLowerCase()));
      const color = colorMatch ? this.fuelColors[colorMatch] : defaultColors[idx % defaultColors.length];

      return {
        fuelTypeId: ft.id,
        fuelTypeName: ft.name,
        pricePerLitre: price?.pricePerLitre ?? 0,
        effectiveFrom: price?.effectiveFrom ?? '',
        isActive: price?.isActive ?? false,
        color,
      };
    });
  }

  private computeKPIs(): void {
    this.totalFuelTypes = this.fuelTypes.length;
    const activePrices = this.priceCards.filter(c => c.pricePerLitre > 0);
    this.averagePrice = activePrices.length > 0
      ? activePrices.reduce((sum, c) => sum + c.pricePerLitre, 0) / activePrices.length
      : 0;

    const dates = this.prices.filter(p => p.createdAt).map(p => new Date(p.createdAt).getTime());
    if (dates.length > 0) {
      const latest = new Date(Math.max(...dates));
      this.lastUpdated = latest.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })
        + ' ' + latest.toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit' });
    } else {
      this.lastUpdated = '—';
    }
  }

  // ═══ Chart ═══
  selectChartFuel(fuelTypeId: string): void {
    this.selectedChartFuelId = fuelTypeId;
    this.salesApi.getFuelPriceHistory(fuelTypeId, 30).pipe(takeUntil(this.destroy$)).subscribe(history => {
      this.priceHistory = history;
      this.buildChart();
    });
  }

  private buildChart(): void {
    if (this.priceHistory.length === 0) {
      this.chartBars = [];
      return;
    }

    const prices = this.priceHistory.map(h => h.price);
    this.chartMaxPrice = Math.max(...prices);
    this.chartMinPrice = Math.min(...prices);
    const range = this.chartMaxPrice - this.chartMinPrice || 1;

    this.chartBars = this.priceHistory.map(h => ({
      label: new Date(h.date).toLocaleDateString('en-IN', { day: '2-digit', month: 'short' }),
      value: h.price,
      height: ((h.price - this.chartMinPrice) / range) * 80 + 15,
    }));
  }

  getSelectedFuelName(): string {
    return this.priceCards.find(c => c.fuelTypeId === this.selectedChartFuelId)?.fuelTypeName || '';
  }

  // ═══ Update Price ═══
  prefillUpdateForm(card: PriceCard): void {
    this.updateFuelTypeId = card.fuelTypeId;
    this.updatePrice = card.pricePerLitre || null;
    this.updateEffectiveFrom = new Date().toISOString().split('T')[0];
    this.useImmediateEffective = false;
  }

  setImmediate(): void {
    this.useImmediateEffective = true;
    this.updateEffectiveFrom = new Date().toISOString();
  }

  clearImmediate(): void {
    this.useImmediateEffective = false;
    this.updateEffectiveFrom = new Date().toISOString().split('T')[0];
  }

  submitPriceUpdate(): void {
    if (!this.updateFuelTypeId) { this.toast.error('Select a fuel type.'); return; }
    if (!this.updatePrice || this.updatePrice <= 0) { this.toast.error('Price must be positive.'); return; }
    if (!this.useImmediateEffective && !this.updateEffectiveFrom) { this.toast.error('Set an effective date or choose Apply Immediately.'); return; }

    // Always send a full ISO datetime string
    let effectiveDate: string;
    if (this.useImmediateEffective) {
      effectiveDate = new Date().toISOString();
    } else {
      // Convert date-only to full ISO datetime (noon UTC to avoid timezone issues)
      effectiveDate = new Date(this.updateEffectiveFrom + 'T12:00:00Z').toISOString();
    }

    const effectiveLabel = this.useImmediateEffective ? 'immediately' : this.updateEffectiveFrom;
    const fuelName = this.fuelTypes.find(f => f.id === this.updateFuelTypeId)?.name || 'fuel';
    if (!confirm(`Update ${fuelName} to ₹${this.updatePrice}/L effective ${effectiveLabel}? This will apply across all stations.`)) return;

    this.isUpdating = true;
    this.salesApi.setFuelPrice(this.updateFuelTypeId, this.updatePrice, effectiveDate)
      .pipe(
        timeout(10000),
        takeUntil(this.destroy$),
      )
      .subscribe({
        next: (result) => {
          this.toast.success(`${fuelName} price updated to ₹${this.updatePrice}/L ${this.useImmediateEffective ? '(effective immediately)' : ''}`);
          this.isUpdating = false;
          this.updatePrice = null;
          this.useImmediateEffective = false;
          // Reload data to reflect changes
          this.loadData();
        },
        error: (err) => {
          const msg = err?.error?.message || err?.error?.title || 'Failed to update price. Check if SalesService is running.';
          this.toast.error(msg);
          this.isUpdating = false;
        },
      });
  }

  // ═══ Export ═══
  exportPricingLedger(): void {
    if (this.priceCards.length === 0) {
      this.toast.error('No pricing data to export.');
      return;
    }

    const headers = ['Fuel Type', 'Price (₹/L)', 'Effective From', 'Status'];
    const rows = this.priceCards.map(c => [
      c.fuelTypeName,
      c.pricePerLitre.toFixed(2),
      c.effectiveFrom ? new Date(c.effectiveFrom).toLocaleDateString('en-IN') : 'N/A',
      c.isActive ? 'Active' : 'Inactive',
    ]);

    const csv = [headers.join(','), ...rows.map(r => r.join(','))].join('\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `epcl-fuel-prices-${new Date().toISOString().split('T')[0]}.csv`;
    link.click();
    URL.revokeObjectURL(url);
    this.toast.success('Pricing ledger exported.');
  }

  // ═══ Audit Trail ═══
  get auditEntries(): { fuelName: string; price: string; effectiveFrom: string; createdAt: string; isActive: boolean }[] {
    const source = this.allPrices.length > 0 ? this.allPrices : this.prices;
    if (!source || source.length === 0) return [];
    return source
      .map(p => ({
        fuelName: this.fuelTypes.find(f => f.id === p.fuelTypeId)?.name || (p.fuelTypeId ? p.fuelTypeId.substring(0, 8) : 'Unknown'),
        price: '₹' + (p.pricePerLitre ?? 0).toFixed(2),
        effectiveFrom: p.effectiveFrom ? new Date(p.effectiveFrom).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' }) : '—',
        createdAt: p.createdAt ? new Date(p.createdAt).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' }) + ' ' + new Date(p.createdAt).toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit' }) : '—',
        isActive: p.isActive ?? false,
      }))
      .sort((a, b) => {
        const dateA = new Date(b.createdAt).getTime();
        const dateB = new Date(a.createdAt).getTime();
        return (isNaN(dateA) ? 0 : dateA) - (isNaN(dateB) ? 0 : dateB);
      });
  }

  // ═══ Helpers ═══
  formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
