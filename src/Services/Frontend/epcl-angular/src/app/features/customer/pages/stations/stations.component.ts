import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { StationsApiService, NearbyStationDto } from '../../../../core/services/stations-api.service';
import * as L from 'leaflet';

interface DisplayStation {
  id: string;
  name: string;
  statusClass: string;
  status: string;
  distance: string;
  address: string;
  regular: number;
  change: number;
  premium?: number;
  amenities: string[];
  lat: number;
  lng: number;
}

@Component({
  selector: 'app-stations',
  templateUrl: './stations.component.html',
  styleUrls: ['./stations.component.scss'],
})
export class StationsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  stations: DisplayStation[] = [];
  selectedStation: DisplayStation | null = null;
  isLoading = true;
  
  filters = ['All Stations', 'Fleet Ready', 'EV Charging', '24/7 Access'];
  activeFilter = 'All Stations';
  userLat = 19.076;
  userLng = 72.877;
  map!: L.Map;
  markers: L.Marker[] = [];

  constructor(private stationsApi: StationsApiService) {}

  ngOnInit(): void {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (pos) => { this.userLat = pos.coords.latitude; this.userLng = pos.coords.longitude; this.loadStations(); },
        () => this.loadStations()
      );
    } else { this.loadStations(); }
    setTimeout(() => this.initMap(), 100);
  }

  ngOnDestroy(): void {
    if (this.map) { this.map.remove(); }
    this.destroy$.next();
    this.destroy$.complete();
  }

  initMap(): void {
    this.map = L.map('leaflet-map').setView([this.userLat, this.userLng], 12);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors'
    }).addTo(this.map);
  }

  updateMapMarkers(): void {
    if (!this.map) return;
    this.markers.forEach(m => m.remove());
    this.markers = [];
    
    // Add user location
    const userIcon = L.divIcon({ className: 'user-marker', html: '<div style="width:16px;height:16px;background:#3b82f6;border-radius:50%;border:3px solid white;box-shadow:0 0 10px rgba(0,0,0,0.5);"></div>' });
    L.marker([this.userLat, this.userLng], { icon: userIcon }).addTo(this.map).bindPopup('Your Location');

    // Add station markers
    const stationIcon = L.divIcon({ className: 'station-marker', html: '<div style="width:20px;height:20px;background:#10b981;border-radius:50%;border:2px solid white;box-shadow:0 0 10px rgba(0,0,0,0.5);"></div>' });
    
    this.stations.forEach(s => {
      const marker = L.marker([s.lat, s.lng], { icon: stationIcon }).addTo(this.map);
      marker.on('click', () => {
        this.selectStation(s);
      });
      this.markers.push(marker);
    });

    if (this.stations.length > 0) {
      const group = new L.FeatureGroup(this.markers);
      this.map.fitBounds(group.getBounds().pad(0.1));
    }
  }

  loadStations(): void {
    this.isLoading = true;
    const filters: { hasCng?: boolean; is24x7?: boolean } = {};
    if (this.activeFilter === '24/7 Access') filters.is24x7 = true;
    this.stationsApi.getNearbyStations(this.userLat, this.userLng, 10, filters)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.stations = res.map(s => ({
            id: s.id,
            name: s.name,
            status: s.isActive ? 'OPERATIONAL' : 'OFFLINE',
            statusClass: s.isActive ? 'active' : '',
            distance: `${s.distanceKm.toFixed(1)} km`,
            address: `${s.city}, ${s.state}`,
            regular: 95.5 + (Math.random() * 2), // Mock price
            change: 0,
            premium: 102.3, // Mock price
            amenities: s.is24x7 ? ['24/7 Access'] : [],
            lat: s.latitude,
            lng: s.longitude
          }));
          this.updateMapMarkers();
          this.isLoading = false; 
        },
        error: () => { this.isLoading = false; },
      });
  }

  selectStation(station: DisplayStation): void { 
    this.selectedStation = station; 
    if (this.map) {
      this.map.setView([station.lat, station.lng], 15);
    }
  }
  closeDetails(): void { 
    this.selectedStation = null; 
    if (this.map && this.stations.length > 0) {
      const group = new L.FeatureGroup(this.markers);
      this.map.fitBounds(group.getBounds().pad(0.1));
    }
  }



  getDirections(station: DisplayStation): void {
    // We don't have lat/lng in DisplayStation, but we didn't map it. In a real app we'd keep the DTO or add lat/lng to DisplayStation.
    // For now, this placeholder handles compilation.
    window.open(`https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(station.address)}`, '_blank');
  }

  getStockStatusColor(status: string): string {
    switch (status?.toLowerCase()) {
      case 'adequate': return '#10b981';
      case 'low': return '#f59e0b';
      case 'critical': case 'outofstock': return '#ef4444';
      default: return '#6366f1';
    }
  }
}
