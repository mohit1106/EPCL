import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil, catchError, of, switchMap } from 'rxjs';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { StationsApiService, StationDto, ParkingBookingDto, ParkingSlotDto } from '../../../../core/services/stations-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-parking-tickets',
  templateUrl: './parking-tickets.component.html',
  styleUrls: ['./parking-tickets.component.scss'],
})
export class ParkingTicketsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  isLoading = true;
  isToggling = false;
  bookings: ParkingBookingDto[] = [];
  hasStation = false;
  stationName = '';
  stationId = '';

  // Stats
  activeCount = 0;
  expiredCount = 0;
  totalRevenue = 0;

  // Parking availability state
  isParkingEnabled = true;

  constructor(
    private store: Store,
    private stationsApi: StationsApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.store.select(selectUser).pipe(
      takeUntil(this.destroy$),
      switchMap(user => {
        if (!user) return of(null);
        if (user.profile?.stationId) {
          return this.stationsApi.getStationById(user.profile.stationId).pipe(
            catchError(() => this.stationsApi.getMyStation(user.id))
          );
        }
        return this.stationsApi.getMyStation(user.id);
      })
    ).subscribe(station => {
      if (station) {
        this.stationId = station.id;
        this.stationName = station.stationName || station.name || 'My Station';
        this.hasStation = true;
        this.loadBookings();
        this.checkSlotAvailability();
      } else {
        this.hasStation = false;
        this.isLoading = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadBookings(): void {
    this.isLoading = true;
    this.stationsApi.getStationParkingBookings(this.stationId, 1, 100).pipe(
      takeUntil(this.destroy$),
      catchError(() => of([]))
    ).subscribe(data => {
      this.bookings = data;
      const now = new Date();
      this.activeCount = data.filter(b => b.status === 'Confirmed' && new Date(b.expiresAt) > now).length;
      this.expiredCount = data.filter(b => b.status === 'Confirmed' && new Date(b.expiresAt) <= now).length;
      this.totalRevenue = data.filter(b => b.status === 'Confirmed').reduce((sum, b) => sum + b.amount, 0);
      this.isLoading = false;
    });
  }

  private checkSlotAvailability(): void {
    this.stationsApi.getParkingSlots(this.stationId).pipe(
      takeUntil(this.destroy$),
      catchError(() => of([]))
    ).subscribe((slots: ParkingSlotDto[]) => {
      // If ALL slots are unavailable, parking is disabled. If any are available, it's enabled.
      if (slots.length > 0) {
        this.isParkingEnabled = slots.some(s => s.isAvailable);
      }
    });
  }

  toggleAvailability(): void {
    if (this.isToggling) return;
    this.isToggling = true;
    const newState = !this.isParkingEnabled;

    this.stationsApi.toggleStationParkingAvailability(newState, this.stationId).pipe(
      takeUntil(this.destroy$),
      catchError(() => {
        this.toast.error('Failed to toggle parking availability.');
        this.isToggling = false;
        return of(null);
      })
    ).subscribe(res => {
      if (res) {
        this.isParkingEnabled = newState;
        this.toast.success(newState ? 'Parking slots are now available for customers.' : 'All parking slots have been disabled.');
      }
      this.isToggling = false;
    });
  }

  getStatusClass(booking: ParkingBookingDto): string {
    if (booking.status === 'Confirmed') {
      return new Date(booking.expiresAt) > new Date() ? 'badge-success' : 'badge-neutral';
    }
    switch (booking.status?.toLowerCase()) {
      case 'completed': case 'success': return 'badge-success';
      case 'initiated': case 'pending': return 'badge-warning';
      case 'voided': case 'failed': case 'cancelled': return 'badge-danger';
      default: return 'badge-neutral';
    }
  }

  getStatusLabel(booking: ParkingBookingDto): string {
    if (booking.status === 'Confirmed') {
      return new Date(booking.expiresAt) > new Date() ? 'Active' : 'Expired';
    }
    return booking.status;
  }

  getVehicleLabel(slotType: string): string {
    const labels: Record<string, string> = { TwoWheeler: '2-Wheeler', FourWheeler: '4-Wheeler', HGV: 'Heavy Vehicle' };
    return labels[slotType] || slotType;
  }

  getDurationLabel(hours: number): string {
    if (hours >= 24) return '24 Hrs (Full Day)';
    return hours === 1 ? '1 Hour' : `${hours} Hours`;
  }
}
