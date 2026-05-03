import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil, forkJoin, interval, startWith, switchMap, of, catchError } from 'rxjs';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { SalesApiService, FuelPriceDto, TransactionDto } from '../../../../core/services/sales-api.service';
import { LoyaltyApiService, LoyaltyBalanceDto } from '../../../../core/services/loyalty-api.service';
import { StationsApiService, NearbyStationDto, FuelTypeDto, ParkingBookingDto } from '../../../../core/services/stations-api.service';
import { PaymentsApiService } from '../../../../core/services/payments-api.service';

@Component({
  selector: 'app-customer-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  user$ = this.store.select(selectUser);

  walletBalance = 0;
  loyaltyPoints = 0;
  lastVolume = 0;
  lastVolumeUnit = 'L';
  memberTier = 'Silver';
  isLoading = true;
  private fuelTypes: FuelTypeDto[] = [];

  priceTicker: { name: string; price: number; change: number; direction: string }[] = [];
  nearestStation: { name: string; distance: string; hours: string } | null = null;
  recentTransactions: { id: string; terminal: string; fuelType: string; volume: string; cost: number; status: string }[] = [];

  constructor(
    private store: Store,
    private salesApi: SalesApiService,
    private loyaltyApi: LoyaltyApiService,
    private stationsApi: StationsApiService,
    private paymentsApi: PaymentsApiService
  ) {}

  ngOnInit(): void {
    // Load fuel types first, then dashboard data
    this.stationsApi.getFuelTypes().pipe(
      takeUntil(this.destroy$),
      catchError(() => of([]))
    ).subscribe(ft => {
      this.fuelTypes = ft;
      this.loadDashboardData();
    });
    // Refresh fuel prices every 5 minutes
    interval(300000).pipe(
      startWith(0),
      takeUntil(this.destroy$),
      switchMap(() => this.salesApi.getFuelPrices().pipe(catchError(() => of([]))))
    ).subscribe(prices => this.mapPriceTicker(prices));
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadDashboardData(): void {
    const emptyPage = { items: [], totalCount: 0, page: 1, pageSize: 5, totalPages: 0 };
    const defaultLoyalty: LoyaltyBalanceDto = { points: 0, tier: 'Silver', lifetimePoints: 0, nextTier: 'Gold', pointsToNextTier: 500 };
    const defaultWallet = { balance: 0, lastUpdated: '' };

    forkJoin({
      prices: this.salesApi.getFuelPrices().pipe(catchError(() => of([]))),
      transactions: this.salesApi.getMyTransactions(1, 5).pipe(catchError(() => of(emptyPage))),
      loyalty: this.loyaltyApi.getBalance().pipe(catchError(() => of(defaultLoyalty))),
      wallet: this.paymentsApi.getWalletBalance().pipe(catchError(() => of(defaultWallet))),
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: ({ prices, transactions, loyalty, wallet }) => {
        this.mapPriceTicker(prices);
        this.recentTransactions = (transactions.items || []).map((t: TransactionDto) => {
          const item = {
            id: t.id.substring(0, 8),
            terminal: 'Station ' + (t.stationId ? t.stationId.substring(0, 4) : '—'),
            fuelType: t.fuelTypeName || 'Fuel',
            volume: t.quantityLitres + ' L',
            cost: t.totalAmount,
            status: t.status
          };
          if (t.stationId) {
            this.stationsApi.getStationById(t.stationId).pipe(takeUntil(this.destroy$)).subscribe(s => {
              if (s && s.name) {
                item.terminal = s.name;
              }
            });
          }
          return item;
        });
        this.loyaltyPoints = loyalty.points;
        this.memberTier = loyalty.tier;
        this.walletBalance = wallet.balance;
        if (transactions.items && transactions.items.length > 0) {
          this.lastVolume = transactions.items[0].quantityLitres;
        }
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; },
    });

    // Load nearby stations with geolocation
    this.loadNearbyStations();
  }

  private loadNearbyStations(): void {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (pos) => this.fetchNearbyStations(pos.coords.latitude, pos.coords.longitude),
        () => this.fetchNearbyStations(19.076, 72.877) // Default: Mumbai
      );
    } else {
      this.fetchNearbyStations(19.076, 72.877);
    }
  }

  private fetchNearbyStations(lat: number, lng: number): void {
    this.stationsApi.getNearbyStations(lat, lng, 5).pipe(takeUntil(this.destroy$)).subscribe({
      next: (stations) => {
        if (stations.length > 0) {
          const s = stations[0];
          this.nearestStation = {
            name: s.name,
            distance: `${s.distanceKm.toFixed(1)} km away`,
            hours: s.is24x7 ? 'Open 24/7' : `${s.operatingHoursStart} - ${s.operatingHoursEnd}`,
          };
        }
      },
    });
  }

  private mapPriceTicker(prices: FuelPriceDto[]): void {
    this.priceTicker = prices.map(p => ({
      name: this.fuelTypes.find(f => f.id === p.fuelTypeId)?.name || 'Fuel',
      price: p.pricePerLitre,
      change: 0,
      direction: 'stable',
    }));
  }



  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'completed': case 'success': case 'confirmed': return 'status-success';
      case 'initiated': case 'pending': return 'status-pending';
      case 'voided': case 'failed': case 'cancelled': return 'status-flagged';
      default: return 'status-default';
    }
  }


}
