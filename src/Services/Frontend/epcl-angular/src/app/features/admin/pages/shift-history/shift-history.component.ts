import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil, catchError, of, forkJoin } from 'rxjs';
import { SalesApiService, ShiftDto } from '../../../../core/services/sales-api.service';
import { StationsApiService, StationDto } from '../../../../core/services/stations-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

interface DisplayShift {
  id: string;
  dealerUserId: string;
  stationId: string;
  stationName: string;
  startedAt: string;
  endedAt?: string;
  duration: string;
  totalTransactions: number;
  totalLitresSold: number;
  totalRevenue: number;
  discrepancyFlagged: boolean;
  status: string;
}

@Component({
  selector: 'app-admin-shift-history',
  templateUrl: './shift-history.component.html',
  styleUrls: ['./shift-history.component.scss'],
})
export class AdminShiftHistoryComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  shifts: DisplayShift[] = [];
  stationMap = new Map<string, string>(); // stationId → name
  stations: StationDto[] = [];

  isLoading = true;
  totalCount = 0;
  page = 1;
  pageSize = 25;

  // Filters
  filterStationId = '';

  // Stats
  totalShifts = 0;
  totalRevenue = 0;
  totalLitres = 0;
  avgDuration = '';
  flaggedCount = 0;

  constructor(
    private salesApi: SalesApiService,
    private stationsApi: StationsApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.loadStations();
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  private loadStations(): void {
    this.stationsApi.getStations(1, 100).pipe(
      takeUntil(this.destroy$),
      catchError(() => of({ items: [] as StationDto[], totalCount: 0, page: 1, pageSize: 100, totalPages: 0 }))
    ).subscribe(result => {
      this.stations = result.items;
      this.stationMap.clear();
      result.items.forEach(s => {
        this.stationMap.set(s.id, s.stationName || s.name || s.stationCode || s.code || s.id.substring(0, 8));
      });
      this.loadShifts();
    });
  }

  loadShifts(): void {
    this.isLoading = true;
    const stationId = this.filterStationId || undefined;
    this.salesApi.getAllShifts(this.page, this.pageSize, stationId).pipe(
      takeUntil(this.destroy$),
      catchError(() => of({ items: [] as ShiftDto[], totalCount: 0, page: 1, pageSize: 25, totalPages: 0 }))
    ).subscribe(result => {
      this.totalCount = result.totalCount;
      this.shifts = result.items.map(s => ({
        id: s.id,
        dealerUserId: s.dealerUserId,
        stationId: s.stationId,
        stationName: this.stationMap.get(s.stationId) || s.stationId.substring(0, 8),
        startedAt: s.startedAt,
        endedAt: s.endedAt,
        duration: s.endedAt ? this.calcDuration(s.startedAt, s.endedAt) : 'Active',
        totalTransactions: s.totalTransactions,
        totalLitresSold: s.totalLitresSold,
        totalRevenue: s.totalRevenue,
        discrepancyFlagged: s.discrepancyFlagged,
        status: !s.endedAt ? 'Active' : s.discrepancyFlagged ? 'Flagged' : 'Completed',
      }));

      // Stats
      this.totalShifts = this.totalCount;
      this.totalRevenue = this.shifts.reduce((sum, s) => sum + s.totalRevenue, 0);
      this.totalLitres = this.shifts.reduce((sum, s) => sum + s.totalLitresSold, 0);
      this.flaggedCount = this.shifts.filter(s => s.discrepancyFlagged).length;
      const durations = this.shifts.filter(s => s.endedAt).map(s =>
        (new Date(s.endedAt!).getTime() - new Date(s.startedAt).getTime()) / 60000
      );
      const avgMin = durations.length > 0 ? Math.round(durations.reduce((a, b) => a + b, 0) / durations.length) : 0;
      this.avgDuration = avgMin > 60 ? `${Math.floor(avgMin / 60)}h ${avgMin % 60}m` : `${avgMin}m`;

      this.isLoading = false;
    });
  }

  onFilterChange(): void {
    this.page = 1;
    this.loadShifts();
  }

  private calcDuration(start: string, end: string): string {
    const diff = Math.floor((new Date(end).getTime() - new Date(start).getTime()) / 1000);
    const h = Math.floor(diff / 3600);
    const m = Math.floor((diff % 3600) / 60);
    return h > 0 ? `${h}h ${m}m` : `${m}m`;
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'completed': return 'st-success';
      case 'active': return 'st-active';
      case 'flagged': return 'st-danger';
      default: return 'st-default';
    }
  }

  exportCSV(): void {
    if (this.shifts.length === 0) { this.toast.error('No shift data to export.'); return; }
    const headers = ['Shift ID', 'Station', 'Dealer ID', 'Started', 'Ended', 'Duration', 'Transactions', 'Litres', 'Revenue', 'Status'];
    const rows = this.shifts.map(s => [
      s.id.substring(0, 8),
      s.stationName,
      s.dealerUserId.substring(0, 8),
      new Date(s.startedAt).toLocaleString(),
      s.endedAt ? new Date(s.endedAt).toLocaleString() : 'Active',
      s.duration,
      s.totalTransactions,
      s.totalLitresSold.toFixed(1),
      s.totalRevenue.toFixed(2),
      s.status,
    ]);
    const csv = [headers.join(','), ...rows.map(r => r.join(','))].join('\n');
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `admin-shift-history-${new Date().toISOString().split('T')[0]}.csv`;
    a.click();
    URL.revokeObjectURL(url);
    this.toast.success('CSV exported successfully.');
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
    this.loadShifts();
  }
}
