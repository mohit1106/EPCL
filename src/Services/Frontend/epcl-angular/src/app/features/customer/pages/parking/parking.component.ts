import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, takeUntil, of, catchError } from 'rxjs';
import { StationsApiService, ParkingBookingDto } from '../../../../core/services/stations-api.service';

@Component({
  selector: 'app-parking',
  templateUrl: './parking.component.html',
  styleUrls: ['./parking.component.scss'],
})
export class ParkingComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  isLoading = true;
  parkingBookings: ParkingBookingDto[] = [];
  activeBookings: ParkingBookingDto[] = [];
  expiredBookings: ParkingBookingDto[] = [];
  activeParkingCount = 0;
  totalParkingSpent = 0;

  constructor(
    private stationsApi: StationsApiService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadParkingBookings();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadParkingBookings(): void {
    this.stationsApi.getMyParkingBookings(1, 50).pipe(
      takeUntil(this.destroy$),
      catchError(() => of([]))
    ).subscribe(bookings => {
      this.parkingBookings = bookings;
      const now = new Date();
      this.activeBookings = bookings.filter(
        b => b.status === 'Confirmed' && new Date(b.expiresAt) > now
      );
      this.expiredBookings = bookings.filter(
        b => b.status === 'Confirmed' && new Date(b.expiresAt) <= now
      );
      this.activeParkingCount = this.activeBookings.length;
      this.totalParkingSpent = bookings
        .filter(b => b.status === 'Confirmed')
        .reduce((sum, b) => sum + b.amount, 0);
      this.isLoading = false;
    });
  }

  getParkingStatusClass(booking: ParkingBookingDto): string {
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

  getParkingStatusLabel(booking: ParkingBookingDto): string {
    if (booking.status === 'Confirmed') {
      return new Date(booking.expiresAt) > new Date() ? 'Active' : 'Expired';
    }
    return booking.status;
  }

  getVehicleIcon(slotType: string): string {
    // Return SVG path for vehicle type (rendered in template)
    return slotType;
  }

  getVehicleLabel(slotType: string): string {
    const labels: Record<string, string> = { TwoWheeler: '2-Wheeler', FourWheeler: '4-Wheeler', HGV: 'Heavy Vehicle' };
    return labels[slotType] || slotType;
  }

  getDurationLabel(hours: number): string {
    if (hours >= 24) return '24 Hrs (Full Day)';
    return hours === 1 ? '1 Hour' : `${hours} Hours`;
  }

  goToStations(): void {
    this.router.navigate(['/customer/stations']);
  }
}
