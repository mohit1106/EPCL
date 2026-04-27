import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { StationsApiService, StationDto, FuelTypeDto } from '../../../../core/services/stations-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

interface DisplayAdminStation {
  id: string;
  name: string;
  statusClass: string;
  status: string;
  code: string;
  pumps: number;
  totalPumps: number;
  throughput: string;
  fuels: { name: string; pct: number }[];
  rawDto: StationDto;
}

@Component({
  selector: 'app-admin-stations',
  templateUrl: './stations.component.html',
  styleUrls: ['./stations.component.scss'],
})
export class AdminStationsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  stations: DisplayAdminStation[] = [];
  rawStations: StationDto[] = [];
  
  activeStations = 412;
  offlineStations = 8;
  totalCount = 0;
  page = 1;
  pageSize = 20;
  isLoading = true;
  selectedStation: StationDto | null = null;
  showAddModal = false;
  isSubmitting = false;
  viewMode: 'list' | 'map' = 'list';
  cityFilter = '';

  // Form for add/edit
  stationForm: Partial<StationDto> = {};

  telemetryStation = 'North Terminal Hub';
  tankData = [
    { name: 'REGULAR', pct: 78 },
    { name: 'DIESEL', pct: 42 }
  ];
  liveTxns = [
    { time: '10:42:15', pump: 'PUMP 04', type: 'DIESEL', amount: '$45.20' },
    { time: '10:41:59', pump: 'PUMP 01', type: 'REGULAR', amount: '$12.50' }
  ];
  envPressure = '14.7';
  tempVariance = '±0.2';

  inventory = [
    { name: 'West Side Depot', code: 'WSD-001', region: 'WEST', status: 'ACTIVE', statusClass: 'status-ok' },
    { name: 'East Junction', code: 'EJ-092', region: 'EAST', status: 'OFFLINE', statusClass: 'status-error' }
  ];

  constructor(private stationsApi: StationsApiService, private toast: ToastService) {}

  ngOnInit(): void { this.loadStations(); }
  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  loadStations(): void {
    this.isLoading = true;
    const filters: { isActive?: boolean; city?: string } = {};
    if (this.cityFilter) filters.city = this.cityFilter;
    this.stationsApi.getStations(this.page, this.pageSize, filters).pipe(takeUntil(this.destroy$)).subscribe({
      next: (result) => {
        this.rawStations = result.items;
        this.stations = result.items.map(s => ({
          id: s.id,
          name: s.name,
          status: s.isActive ? 'ONLINE' : 'OFFLINE',
          statusClass: s.isActive ? 'status-ok' : 'status-err',
          code: s.code || `UID-${s.id.substring(0, 6).toUpperCase()}`,
          pumps: s.isActive ? 4 : 0,
          totalPumps: 4,
          throughput: s.isActive ? '142.5' : '0.0',
          fuels: [
            { name: 'REGULAR', pct: 85 },
            { name: 'DIESEL', pct: 60 }
          ],
          rawDto: s
        }));
        this.totalCount = result.totalCount;
        this.activeStations = this.stations.filter(st => st.rawDto.isActive).length;
        this.offlineStations = this.stations.filter(st => !st.rawDto.isActive).length;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; },
    });
  }

  onPageChange(page: number): void { this.page = page; this.loadStations(); }
  toggleView(): void { this.viewMode = this.viewMode === 'list' ? 'map' : 'list'; }

  viewStation(station: StationDto): void { this.selectedStation = station; }
  closeDetail(): void { this.selectedStation = null; }

  openAddModal(): void {
    this.stationForm = {};
    this.showAddModal = true;
  }

  openEditModal(station: StationDto): void {
    this.stationForm = { ...station };
    this.showAddModal = true;
  }

  closeAddModal(): void { this.showAddModal = false; }

  saveStation(): void {
    this.isSubmitting = true;
    const op = this.stationForm.id
      ? this.stationsApi.updateStation(this.stationForm.id, this.stationForm)
      : this.stationsApi.createStation(this.stationForm);

    op.pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toast.success(this.stationForm.id ? 'Station updated.' : 'Station created.');
        this.isSubmitting = false;
        this.showAddModal = false;
        this.loadStations();
      },
      error: () => { this.toast.error('Failed to save station.'); this.isSubmitting = false; },
    });
  }

  deactivateStation(id: string): void {
    if (!confirm('Deactivate this station? It will no longer appear in the system.')) return;
    this.stationsApi.deactivateStation(id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('Station deactivated.'); this.loadStations(); this.closeDetail(); },
      error: () => this.toast.error('Failed to deactivate station.'),
    });
  }
}
