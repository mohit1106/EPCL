import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil, forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { StationsApiService, StationDto, FuelTypeDto } from '../../../../core/services/stations-api.service';
import { SalesApiService, PumpDto } from '../../../../core/services/sales-api.service';
import { UsersApiService, UserListDto } from '../../../../core/services/users-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

interface DisplayStation {
  id: string;
  name: string;
  code: string;
  city: string;
  state: string;
  address: string;
  isActive: boolean;
  is24x7: boolean;
  operatingHours: string;
  dealerUserId: string;
  dealerDisplay: string;
  pumpCount: number;
  pumps: PumpDto[];
  fuelTypesOnStation: string[];
  rawDto: StationDto;
  showPumps: boolean;
}

interface DealerRequest {
  id: string;
  dealerUserId: string;
  dealerEmail: string;
  dealerName: string;
  message: string;
  status: string;
  createdAt: string;
}

@Component({
  selector: 'app-admin-stations',
  templateUrl: './stations.component.html',
  styleUrls: ['./stations.component.scss'],
})
export class AdminStationsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  stations: DisplayStation[] = [];
  fuelTypes: FuelTypeDto[] = [];
  fuelTypeMap = new Map<string, string>();

  totalCount = 0;
  page = 1;
  pageSize = 20;
  isLoading = true;
  searchTerm = '';

  // Stats
  activeCount = 0;
  offlineCount = 0;
  totalPumps = 0;

  // Station form modal
  showStationModal = false;
  isEditMode = false;
  isSubmitting = false;
  stationForm: any = {};

  // Dealer assignment modal
  showAssignModal = false;
  assignStationId = '';
  dealerSearchTerm = '';
  dealerSearchResults: UserListDto[] = [];
  isSearchingDealer = false;
  selectedDealerId = '';
  isAssigning = false;

  // Dealer requests
  dealerRequests: DealerRequest[] = [];

  constructor(
    private stationsApi: StationsApiService,
    private salesApi: SalesApiService,
    private usersApi: UsersApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.loadFuelTypes();
    this.loadStations();
    this.loadDealerRequests();
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  private loadFuelTypes(): void {
    this.stationsApi.getFuelTypes().pipe(takeUntil(this.destroy$), catchError(() => of([]))).subscribe(fts => {
      this.fuelTypes = fts;
      fts.forEach(ft => this.fuelTypeMap.set(ft.id, ft.name));
    });
  }

  loadStations(): void {
    this.isLoading = true;
    this.stationsApi.getStations(this.page, this.pageSize).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (result) => {
        this.totalCount = result.totalCount;
        const stationList = result.items;
        this.activeCount = stationList.filter(s => s.isActive).length;
        this.offlineCount = stationList.filter(s => !s.isActive).length;

        // Load pumps for each station
        const pumpCalls = stationList.map(s =>
          this.salesApi.getStationPumps(s.id).pipe(catchError(() => of([] as PumpDto[])))
        );

        if (pumpCalls.length > 0) {
          forkJoin(pumpCalls).pipe(takeUntil(this.destroy$)).subscribe(pumpResults => {
            this.stations = stationList.map((s, i) => this.mapStation(s, pumpResults[i]));
            this.totalPumps = this.stations.reduce((sum, st) => sum + st.pumpCount, 0);
            this.isLoading = false;
          });
        } else {
          this.stations = [];
          this.isLoading = false;
        }
      },
      error: () => { this.isLoading = false; this.toast.error('Failed to load stations.'); },
    });
  }

  private mapStation(s: StationDto, pumps: PumpDto[]): DisplayStation {
    const dealerId = s.dealerUserId || '';
    const isUnassigned = !dealerId || dealerId === '00000000-0000-0000-0000-000000000000';
    const fuelTypeIds = new Set(pumps.map(p => p.fuelTypeId));
    const fuelTypesOnStation = Array.from(fuelTypeIds).map(id => this.fuelTypeMap.get(id) || 'Unknown');

    return {
      id: s.id,
      name: s.stationName || s.name || 'Unknown',
      code: s.stationCode || s.code || s.id.substring(0, 8),
      city: s.city || '',
      state: s.state || '',
      address: s.addressLine1 || s.address || '',
      isActive: s.isActive,
      is24x7: s.is24x7,
      operatingHours: s.is24x7 ? '24/7' : `${s.operatingHoursStart || '06:00'} – ${s.operatingHoursEnd || '22:00'}`,
      dealerUserId: dealerId,
      dealerDisplay: isUnassigned ? 'Unassigned' : dealerId.substring(0, 8) + '…',
      pumpCount: pumps.length,
      pumps: pumps,
      fuelTypesOnStation,
      rawDto: s,
      showPumps: false,
    };
  }

  togglePumps(station: DisplayStation): void { station.showPumps = !station.showPumps; }

  // ═══ Station CRUD ═══
  openAddStation(): void {
    this.isEditMode = false;
    this.stationForm = {
      stationCode: '', stationName: '', addressLine1: '', city: '', state: '',
      pinCode: '', latitude: 0, longitude: 0, licenseNumber: '',
      operatingHoursStart: '06:00', operatingHoursEnd: '22:00', is24x7: false,
      dealerUserId: '00000000-0000-0000-0000-000000000000',
    };
    this.showStationModal = true;
  }

  openEditStation(station: DisplayStation): void {
    this.isEditMode = true;
    this.stationForm = {
      id: station.id,
      stationName: station.name,
      addressLine1: station.address,
      city: station.city,
      state: station.state,
      pinCode: station.rawDto.pinCode || '',
      latitude: station.rawDto.latitude || 0,
      longitude: station.rawDto.longitude || 0,
      operatingHoursStart: station.rawDto.operatingHoursStart || '06:00',
      operatingHoursEnd: station.rawDto.operatingHoursEnd || '22:00',
      is24x7: station.is24x7,
    };
    this.showStationModal = true;
  }

  closeStationModal(): void { this.showStationModal = false; }

  saveStation(): void {
    this.isSubmitting = true;
    const op = this.isEditMode
      ? this.stationsApi.updateStation(this.stationForm.id, this.stationForm)
      : this.stationsApi.createStation(this.stationForm);

    op.pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toast.success(this.isEditMode ? 'Station updated.' : 'Station created.');
        this.isSubmitting = false;
        this.showStationModal = false;
        this.loadStations();
      },
      error: (err) => {
        this.toast.error(err?.error?.message || 'Failed to save station.');
        this.isSubmitting = false;
      },
    });
  }

  deactivateStation(station: DisplayStation): void {
    if (!confirm(`Deactivate "${station.name}"? It will be marked as offline.`)) return;
    this.stationsApi.deactivateStation(station.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('Station deactivated.'); this.loadStations(); },
      error: () => this.toast.error('Failed to deactivate station.'),
    });
  }

  permanentlyRemoveStation(station: DisplayStation): void {
    if (!confirm(`⚠️ PERMANENTLY REMOVE "${station.name}" (${station.code})?\n\nThis will deactivate the station and it cannot be undone. All associated data will be marked inactive.`)) return;
    if (!confirm(`Are you absolutely sure? Type OK to confirm removal of "${station.name}".`)) return;
    this.stationsApi.deactivateStation(station.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toast.success(`Station "${station.name}" has been permanently removed.`);
        // Remove from local list immediately so it disappears from UI
        this.stations = this.stations.filter(s => s.id !== station.id);
        this.totalCount = Math.max(0, this.totalCount - 1);
        this.activeCount = this.stations.filter(s => s.isActive).length;
        this.offlineCount = this.stations.filter(s => !s.isActive).length;
        this.totalPumps = this.stations.reduce((sum, s) => sum + s.pumpCount, 0);
      },
      error: (err) => this.toast.error(err?.error?.message || 'Failed to remove station.'),
    });
  }

  // ═══ Dealer Assignment ═══
  openAssignDealer(station: DisplayStation): void {
    this.assignStationId = station.id;
    this.selectedDealerId = station.dealerUserId || '';
    this.dealerSearchTerm = '';
    this.dealerSearchResults = [];
    this.showAssignModal = true;
  }

  closeAssignModal(): void { this.showAssignModal = false; }

  searchDealers(): void {
    if (!this.dealerSearchTerm.trim() || this.dealerSearchTerm.trim().length < 3) return;
    this.isSearchingDealer = true;
    this.usersApi.getUsers(1, 10, { role: 'Dealer', search: this.dealerSearchTerm.trim() }).pipe(
      takeUntil(this.destroy$),
      catchError(() => of({ items: [], totalCount: 0, page: 1, pageSize: 10, totalPages: 0 }))
    ).subscribe(result => {
      this.dealerSearchResults = result.items;
      this.isSearchingDealer = false;
    });
  }

  selectDealer(dealer: UserListDto): void {
    this.selectedDealerId = dealer.id;
    this.dealerSearchTerm = dealer.email;
  }

  assignDealer(): void {
    if (!this.selectedDealerId || this.selectedDealerId === '00000000-0000-0000-0000-000000000000') {
      this.toast.error('Please search and select a dealer first.');
      return;
    }
    this.isAssigning = true;
    this.stationsApi.assignDealerToStation(this.assignStationId, this.selectedDealerId).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: () => {
        this.toast.success('Dealer assigned successfully!');
        this.isAssigning = false;
        this.showAssignModal = false;
        this.loadStations();
      },
      error: (err) => {
        this.toast.error(err?.error?.message || 'Failed to assign dealer.');
        this.isAssigning = false;
      },
    });
  }

  removeDealer(station: DisplayStation): void {
    if (!confirm(`Remove dealer from "${station.name}"?`)) return;
    this.stationsApi.removeDealerFromStation(station.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('Dealer removed.'); this.loadStations(); },
      error: () => this.toast.error('Failed to remove dealer.'),
    });
  }

  // ═══ Dealer Requests ═══
  loadDealerRequests(): void {
    const raw = localStorage.getItem('epcl_dealer_requests');
    this.dealerRequests = raw ? JSON.parse(raw).filter((r: DealerRequest) => r.status === 'Pending') : [];
  }

  dismissRequest(req: DealerRequest): void {
    const all = JSON.parse(localStorage.getItem('epcl_dealer_requests') || '[]');
    const idx = all.findIndex((r: any) => r.id === req.id);
    if (idx >= 0) all[idx].status = 'Dismissed';
    localStorage.setItem('epcl_dealer_requests', JSON.stringify(all));
    this.dealerRequests = this.dealerRequests.filter(r => r.id !== req.id);
    this.toast.success('Request dismissed.');
  }

  assignFromRequest(req: DealerRequest): void {
    this.selectedDealerId = req.dealerUserId;
    this.dealerSearchTerm = req.dealerEmail;
    // Open assign modal with first unassigned station pre-selected
    const unassigned = this.stations.find(s =>
      !s.dealerUserId || s.dealerUserId === '00000000-0000-0000-0000-000000000000'
    );
    if (unassigned) {
      this.assignStationId = unassigned.id;
    } else if (this.stations.length > 0) {
      this.assignStationId = this.stations[0].id;
    }
    this.showAssignModal = true;
  }

  get totalPages(): number { return Math.ceil(this.totalCount / this.pageSize); }
  get pageNumbers(): number[] {
    const pages: number[] = [];
    const start = Math.max(1, this.page - 2);
    const end = Math.min(this.totalPages, this.page + 2);
    for (let i = start; i <= end; i++) pages.push(i);
    return pages;
  }

  onPageChange(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.page = p;
    this.loadStations();
  }

  getPumpStatusClass(status: string): string {
    return status === 'Active' ? 'st-active' : 'st-maint';
  }
}
