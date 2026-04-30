import { Component, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import { Subject, takeUntil, forkJoin } from 'rxjs';
import { StationsApiService, StationDto, NearbyStationDto, FuelTypeDto, ParkingSlotDto } from '../../../../core/services/stations-api.service';
import { SalesApiService } from '../../../../core/services/sales-api.service';
import { ToastService } from '../../../../shared/services/toast.service';
import * as L from 'leaflet';

declare var Razorpay: any;

interface DisplayStation {
  id: string;
  name: string;
  code: string;
  status: string;
  statusClass: string;
  distance: number;
  distanceStr: string;
  address: string;
  city: string;
  state: string;
  pinCode: string;
  lat: number;
  lng: number;
  is24x7: boolean;
  isActive: boolean;
  operatingHoursStart: string;
  operatingHoursEnd: string;
  fuelPrices: { name: string; price: number }[];
  amenities: string[];
  parkingSlots: ParkingSlotDto[];
  parkingAvailable: { twoWheeler: number; fourWheeler: number; hgv: number };
}

interface ParkingPricing {
  [slotType: string]: { [duration: string]: number };
}

@Component({
  selector: 'app-stations',
  templateUrl: './stations.component.html',
  styleUrls: ['./stations.component.scss'],
})
export class StationsComponent implements OnInit, OnDestroy, AfterViewInit {
  private destroy$ = new Subject<void>();
  stations: DisplayStation[] = [];
  filteredStations: DisplayStation[] = [];
  fuelTypes: FuelTypeDto[] = [];
  fuelPrices: { fuelTypeId: string; name: string; price: number }[] = [];
  parkingPricing: ParkingPricing = {};

  selectedStation: DisplayStation | null = null;
  isLoading = true;
  searchQuery = '';

  // Filters
  selectedFuelFilter = '';
  selectedCity = '';
  show24x7Only = false;
  cities: string[] = [];

  // Map
  userLat = 19.076;
  userLng = 72.877;
  map!: L.Map;
  markers: L.Marker[] = [];
  userMarker: L.Marker | null = null;

  // Parking booking
  showParkingModal = false;
  bookingSlotType = 'FourWheeler';
  bookingDuration = 1;
  bookingPrice = 0;
  isBooking = false;
  razorpayKeyId = '';

  constructor(
    private stationsApi: StationsApiService,
    private salesApi: SalesApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (pos) => {
          this.userLat = pos.coords.latitude;
          this.userLng = pos.coords.longitude;
          this.loadData();
        },
        () => this.loadData()
      );
    } else {
      this.loadData();
    }
  }

  ngAfterViewInit(): void {
    setTimeout(() => this.initMap(), 200);
  }

  ngOnDestroy(): void {
    if (this.map) this.map.remove();
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ─── Data Loading ──────────────────────────────────
  private loadData(): void {
    this.isLoading = true;
    forkJoin({
      stations: this.stationsApi.getNearbyStations(this.userLat, this.userLng, 500),
      fuelTypes: this.stationsApi.getFuelTypes(),
      pricing: this.stationsApi.getParkingPricing(),
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: ({ stations, fuelTypes, pricing }) => {
        this.fuelTypes = fuelTypes;
        this.parkingPricing = pricing || {};
        this.buildStations(stations);
        this.isLoading = false;
        // Load fuel prices AFTER fuelTypes are available
        this.loadFuelPrices();
      },
      error: () => {
        this.isLoading = false;
        this.toast.error('Failed to load stations');
      },
    });
  }

  private loadFuelPrices(): void {
    this.salesApi.getActiveFuelPrices().pipe(takeUntil(this.destroy$)).subscribe({
      next: (prices) => {
        this.fuelPrices = prices.map(p => ({
          fuelTypeId: p.fuelTypeId,
          name: this.fuelTypes.find(f => f.id === p.fuelTypeId)?.name || p.fuelTypeId,
          price: p.pricePerLitre,
        }));
        // Enrich stations with prices
        this.stations.forEach(s => {
          s.fuelPrices = this.fuelPrices.map(p => ({ name: p.name, price: p.price }));
        });
        this.filteredStations = [...this.filteredStations]; // Trigger change detection
      },
      error: () => {},
    });
  }

  private buildStations(raw: NearbyStationDto[]): void {
    this.stations = raw.map(s => {
      const station: DisplayStation = {
        id: s.id,
        name: s.stationName || s.name || `Station ${s.stationCode || s.code}`,
        code: s.stationCode || s.code || '',
        status: s.isActive ? 'OPERATIONAL' : 'OFFLINE',
        statusClass: s.isActive ? 'active' : 'offline',
        distance: s.distanceKm || 0,
        distanceStr: `${(s.distanceKm || 0).toFixed(1)} km`,
        address: s.addressLine1 || s.address || '',
        city: s.city || '',
        state: s.state || '',
        pinCode: s.pinCode || '',
        lat: Number(s.latitude),
        lng: Number(s.longitude),
        is24x7: s.is24x7,
        isActive: s.isActive,
        operatingHoursStart: s.operatingHoursStart || '06:00',
        operatingHoursEnd: s.operatingHoursEnd || '22:00',
        fuelPrices: this.fuelPrices.length > 0 ? this.fuelPrices.map(p => ({ name: p.name, price: p.price })) : [],
        amenities: this.buildAmenities(s),
        parkingSlots: [],
        parkingAvailable: { twoWheeler: 0, fourWheeler: 0, hgv: 0 },
      };
      return station;
    });

    // Collect unique cities
    this.cities = [...new Set(this.stations.map(s => s.city))].sort();
    this.applyFilters();
    this.updateMapMarkers();
  }

  private buildAmenities(s: any): string[] {
    const amenities: string[] = [];
    if (s.is24x7) amenities.push('24/7 Open');
    if (s.hasCng) amenities.push('CNG');
    amenities.push('Parking');
    return amenities;
  }

  // ─── Filtering ─────────────────────────────────────
  applyFilters(): void {
    let result = [...this.stations];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(s =>
        s.name.toLowerCase().includes(q) || s.city.toLowerCase().includes(q) || s.address.toLowerCase().includes(q) || s.code.toLowerCase().includes(q)
      );
    }
    if (this.selectedCity) {
      result = result.filter(s => s.city === this.selectedCity);
    }
    if (this.show24x7Only) {
      result = result.filter(s => s.is24x7);
    }
    if (this.selectedFuelFilter) {
      // Keep all stations since all serve all fuel types in this system
      // But if we had per-station fuel type data, we'd filter here
    }

    this.filteredStations = result;
    this.updateMapMarkers();
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.selectedCity = '';
    this.selectedFuelFilter = '';
    this.show24x7Only = false;
    this.applyFilters();
  }

  hasActiveFilters(): boolean {
    return !!(this.searchQuery || this.selectedCity || this.selectedFuelFilter || this.show24x7Only);
  }

  // ─── Map ───────────────────────────────────────────
  private initMap(): void {
    const el = document.getElementById('leaflet-map');
    if (!el) return;
    this.map = L.map('leaflet-map').setView([this.userLat, this.userLng], 12);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors',
    }).addTo(this.map);

    // User location marker
    const userIcon = L.divIcon({
      className: 'user-marker-icon',
      html: '<div style="width:18px;height:18px;background:#3B82F6;border-radius:50%;border:3px solid white;box-shadow:0 2px 8px rgba(59,130,246,0.5);"></div>',
      iconSize: [18, 18],
      iconAnchor: [9, 9],
    });
    this.userMarker = L.marker([this.userLat, this.userLng], { icon: userIcon }).addTo(this.map);
    this.userMarker.bindPopup('<b>You are here</b>');
  }

  private updateMapMarkers(): void {
    if (!this.map) return;
    this.markers.forEach(m => m.remove());
    this.markers = [];

    const stationIcon = L.divIcon({
      className: 'station-marker-icon',
      html: '<div style="width:14px;height:14px;background:#10B981;border-radius:50%;border:2px solid white;box-shadow:0 2px 6px rgba(0,0,0,0.3);"></div>',
      iconSize: [14, 14],
      iconAnchor: [7, 7],
    });

    this.filteredStations.forEach(s => {
      const marker = L.marker([s.lat, s.lng], { icon: stationIcon }).addTo(this.map);
      marker.bindPopup(`<b>${s.name}</b><br>${s.address}, ${s.city}<br><i>${s.distanceStr}</i>`);
      marker.on('click', () => this.selectStation(s));
      this.markers.push(marker);
    });

    if (this.filteredStations.length > 0) {
      const allPoints = [...this.markers.map(m => m.getLatLng())];
      if (this.userMarker) allPoints.push(this.userMarker.getLatLng());
      const group = L.featureGroup(this.markers);
      this.map.fitBounds(group.getBounds().pad(0.15));
    }
  }

  // ─── Station Selection ─────────────────────────────
  selectStation(station: DisplayStation): void {
    this.selectedStation = station;
    if (this.map) {
      this.map.setView([station.lat, station.lng], 15, { animate: true });
    }
    // Load parking slots for this station
    this.stationsApi.getParkingSlots(station.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: (slots) => {
        station.parkingSlots = slots;
        station.parkingAvailable = {
          twoWheeler: slots.filter(s => s.slotType === 'TwoWheeler' && s.isAvailable).length,
          fourWheeler: slots.filter(s => s.slotType === 'FourWheeler' && s.isAvailable).length,
          hgv: slots.filter(s => s.slotType === 'HGV' && s.isAvailable).length,
        };
      },
      error: () => {},
    });
  }

  closeDetails(): void {
    this.selectedStation = null;
    this.showParkingModal = false;
    if (this.map && this.filteredStations.length > 0) {
      const group = L.featureGroup(this.markers);
      this.map.fitBounds(group.getBounds().pad(0.15));
    }
  }

  // ─── Navigation ────────────────────────────────────
  navigateToStation(station: DisplayStation): void {
    const dest = `${station.lat},${station.lng}`;
    const origin = `${this.userLat},${this.userLng}`;
    window.open(`https://www.google.com/maps/dir/?api=1&origin=${origin}&destination=${dest}&travelmode=driving`, '_blank');
  }

  // ─── Parking Booking ───────────────────────────────
  openParkingModal(): void {
    this.showParkingModal = true;
    this.bookingSlotType = 'FourWheeler';
    this.bookingDuration = 1;
    this.updateBookingPrice();
  }

  closeParkingModal(): void {
    this.showParkingModal = false;
  }

  updateBookingPrice(): void {
    const typePrice = this.parkingPricing[this.bookingSlotType];
    this.bookingPrice = typePrice ? (typePrice[String(this.bookingDuration)] || 0) : 0;
  }

  confirmBooking(): void {
    if (!this.selectedStation || this.isBooking) return;
    this.isBooking = true;

    this.stationsApi.bookParking(this.selectedStation.id, this.bookingSlotType, this.bookingDuration)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.razorpayKeyId = res.keyId;
          this.openRazorpay(res);
        },
        error: (err) => {
          this.isBooking = false;
          this.toast.error(err.error?.message || 'Failed to create booking');
        },
      });
  }

  private openRazorpay(orderData: any): void {
    const options = {
      key: orderData.keyId,
      amount: orderData.amount * 100,
      currency: orderData.currency || 'INR',
      name: 'EPCL Fuel Management',
      description: `Parking - ${this.bookingSlotType} (${this.bookingDuration}h) - Slot ${orderData.slotNumber}`,
      order_id: orderData.orderId,
      handler: (response: any) => {
        this.handlePaymentSuccess(response);
      },
      modal: {
        ondismiss: () => {
          this.isBooking = false;
          this.toast.error('Payment cancelled');
        },
      },
      prefill: { email: '', contact: '' },
      theme: { color: '#1E40AF' },
    };

    const rzp = new Razorpay(options);
    rzp.open();
  }

  private handlePaymentSuccess(response: any): void {
    this.stationsApi.confirmParkingPayment(response.razorpay_order_id, response.razorpay_payment_id, response.razorpay_signature)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.isBooking = false;
          this.showParkingModal = false;
          this.toast.success(res.message || 'Parking booked successfully! Confirmation sent to email.');
          // Refresh parking slots
          if (this.selectedStation) {
            this.selectStation(this.selectedStation);
          }
        },
        error: () => {
          this.isBooking = false;
          this.toast.error('Payment verification failed');
        },
      });
  }

  // ─── Helpers ───────────────────────────────────────
  getFuelColor(name: string): string {
    const colors: Record<string, string> = {
      Petrol: '#1E40AF', Diesel: '#10B981', CNG: '#F59E0B',
      PremiumPetrol: '#8B5CF6', PremiumDiesel: '#EF4444',
    };
    return colors[name] || '#64748B';
  }

  getTotalParking(station: DisplayStation): number {
    return station.parkingAvailable.twoWheeler + station.parkingAvailable.fourWheeler + station.parkingAvailable.hgv;
  }
}
