import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { HttpErrorResponse } from '@angular/common/http';
import { Subject, takeUntil, interval, catchError, of, EMPTY } from 'rxjs';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { SalesApiService, ShiftDto } from '../../../../core/services/sales-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

interface ShiftLogEntry {
  id: string;
  dealerUserId: string;
  stationId: string;
  startedAt: string;
  endedAt?: string;
  totalTransactions: number;
  totalLitresSold: number;
  totalRevenue: number;
  discrepancyFlagged: boolean;
  duration: string;
  status: string;
}

interface ChecklistItem {
  icon: string;
  title: string;
  desc: string;
  type: 'success' | 'warn' | 'info';
  checked: boolean;
}

@Component({
  selector: 'app-shift',
  templateUrl: './shift.component.html',
  styleUrls: ['./shift.component.scss'],
})
export class ShiftComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  activeShift: ShiftDto | null = null;
  isLoading = true;
  isStarting = false;
  isEnding = false;

  operatorId = '';
  operatorName = '';
  stationId = '';
  elapsedDisplay = '00:00:00';
  private elapsedTimer: any;

  shiftKpis: { label: string; value: string; unit: string; icon: string }[] = [];
  shiftLogs: ShiftLogEntry[] = [];
  filteredLogs: ShiftLogEntry[] = [];

  // Filters
  filterDateFrom = '';
  filterDateTo = '';
  showFilters = false;

  // Discrepancy
  discrepancy = '';
  discrepancyClass = 'ok';

  // Checklists — must all be checked before starting shift
  briefings: ChecklistItem[] = [
    { icon: '✓', title: 'Pre-Shift Checklist', desc: 'Verify all pumps are calibrated and operational. Check nozzle seals and flow meters.', type: 'success', checked: false },
    { icon: '!', title: 'Safety Protocol', desc: 'Fire extinguishers checked and valid. Spill containment kits accessible.', type: 'warn', checked: false },
    { icon: 'i', title: 'System Update', desc: 'Fuel price updates applied. POS system synced with central server.', type: 'info', checked: false },
  ];

  allChecked = false;

  constructor(
    private store: Store,
    private salesApi: SalesApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.store.select(selectUser).pipe(takeUntil(this.destroy$)).subscribe(user => {
      if (user) {
        this.operatorId = user.id?.substring(0, 8).toUpperCase() || 'OP-001';
        this.operatorName = user.fullName || user.email || 'Operator';
        this.stationId = user.profile?.stationId || '';
        this.loadShift();
        this.loadHistory();
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next(); this.destroy$.complete();
    if (this.elapsedTimer) clearInterval(this.elapsedTimer);
  }

  private loadShift(): void {
    this.isLoading = true;
    this.salesApi.getActiveShift().pipe(
      takeUntil(this.destroy$),
      catchError((err: HttpErrorResponse) => {
        // 404 = no active shift — this is normal, not an error
        if (err.status === 404) {
          return of(null);
        }
        return of(null);
      })
    ).subscribe({
      next: (shift) => {
        this.activeShift = shift;
        this.isLoading = false;
        if (shift) {
          this.startElapsedTimer();
          this.buildKpisFromShift(shift);
          this.updateDiscrepancy(shift);
        } else {
          this.shiftKpis = [];
          this.elapsedDisplay = '00:00:00';
          this.discrepancy = 'No active shift. Start a shift to begin monitoring.';
          this.discrepancyClass = 'ok';
        }
      },
      error: () => { this.isLoading = false; },
    });
  }

  private buildKpisFromShift(shift: ShiftDto): void {
    this.shiftKpis = [
      { label: 'TRANSACTIONS', value: String(shift.totalTransactions || 0), unit: '', icon: 'receipt' },
      { label: 'LITRES DISPENSED', value: (shift.totalLitresSold || 0).toFixed(1), unit: 'L', icon: 'fuel' },
      { label: 'REVENUE', value: `₹${(shift.totalRevenue || 0).toLocaleString()}`, unit: '', icon: 'revenue' },
      { label: 'ELAPSED', value: this.elapsedDisplay, unit: '', icon: 'clock' },
    ];
  }

  loadHistory(): void {
    if (!this.stationId) return;
    this.salesApi.getShiftHistory(this.stationId, 1, 50).pipe(
      takeUntil(this.destroy$),
      catchError(() => of([]))
    ).subscribe(shifts => {
      this.shiftLogs = shifts
        .filter(s => s.endedAt) // only completed shifts
        .map(s => ({
          id: s.id,
          dealerUserId: s.dealerUserId,
          stationId: s.stationId,
          startedAt: s.startedAt,
          endedAt: s.endedAt,
          totalTransactions: s.totalTransactions,
          totalLitresSold: s.totalLitresSold,
          totalRevenue: s.totalRevenue,
          discrepancyFlagged: s.discrepancyFlagged,
          duration: this.calcDuration(s.startedAt, s.endedAt!),
          status: s.discrepancyFlagged ? 'Flagged' : 'Completed',
        }));
      this.applyFilters();
    });
  }

  startShift(): void {
    if (!this.stationId) { this.toast.error('No station assigned. Contact admin.'); return; }
    if (!this.allChecked) { this.toast.error('Please complete all operator briefing checks before starting a shift.'); return; }
    this.isStarting = true;
    this.salesApi.startShift(this.stationId).pipe(takeUntil(this.destroy$)).subscribe({
      next: (shift) => {
        this.activeShift = shift;
        this.toast.success('Shift started successfully.');
        this.isStarting = false;
        this.startElapsedTimer();
        this.buildKpisFromShift(shift);
        this.updateDiscrepancy(shift);
      },
      error: (err) => {
        this.toast.error(err?.error?.message || err?.error?.detail || 'Failed to start shift.');
        this.isStarting = false;
      },
    });
  }

  endShift(): void {
    if (!confirm('Are you sure you want to end the current shift?')) return;
    this.isEnding = true;
    this.salesApi.endShift().pipe(takeUntil(this.destroy$)).subscribe({
      next: (shift) => {
        this.activeShift = null;
        this.elapsedDisplay = '00:00:00';
        if (this.elapsedTimer) clearInterval(this.elapsedTimer);
        this.shiftKpis = [];
        this.toast.success(`Shift ended. Revenue: ₹${shift.totalRevenue.toLocaleString()}, Transactions: ${shift.totalTransactions}, Litres: ${shift.totalLitresSold.toFixed(1)}`);
        this.isEnding = false;
        this.discrepancy = 'Shift ended. No active monitoring.';
        this.discrepancyClass = 'ok';
        // Reset checklist for next shift
        this.briefings.forEach(b => b.checked = false);
        this.allChecked = false;
        this.loadHistory(); // refresh history with the new entry
      },
      error: (err) => {
        this.toast.error(err?.error?.message || err?.error?.detail || 'Failed to end shift.');
        this.isEnding = false;
      },
    });
  }

  // Timer
  private startElapsedTimer(): void {
    if (this.elapsedTimer) clearInterval(this.elapsedTimer);
    this.updateElapsed();
    this.elapsedTimer = setInterval(() => this.updateElapsed(), 1000);
  }

  private updateElapsed(): void {
    if (!this.activeShift) return;
    const start = new Date(this.activeShift.startedAt).getTime();
    const diff = Math.floor((Date.now() - start) / 1000);
    const h = Math.floor(diff / 3600);
    const m = Math.floor((diff % 3600) / 60);
    const s = diff % 60;
    this.elapsedDisplay = `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
    // Update KPI if present
    const kpi = this.shiftKpis.find(k => k.label === 'ELAPSED');
    if (kpi) kpi.value = this.elapsedDisplay;
  }

  private calcDuration(start: string, end: string): string {
    const diff = Math.floor((new Date(end).getTime() - new Date(start).getTime()) / 1000);
    const h = Math.floor(diff / 3600);
    const m = Math.floor((diff % 3600) / 60);
    return h > 0 ? `${h}h ${m}m` : `${m}m`;
  }

  private updateDiscrepancy(shift: ShiftDto): void {
    if (shift.discrepancyFlagged) {
      this.discrepancy = 'Discrepancy detected! Stock readings do not match expected values. Review required.';
      this.discrepancyClass = 'error';
    } else {
      this.discrepancy = 'No discrepancies detected during this session. All readings nominal.';
      this.discrepancyClass = 'ok';
    }
  }

  // Filters
  toggleFilters(): void { this.showFilters = !this.showFilters; }

  applyFilters(): void {
    let filtered = [...this.shiftLogs];
    if (this.filterDateFrom) {
      const from = new Date(this.filterDateFrom).getTime();
      filtered = filtered.filter(s => new Date(s.startedAt).getTime() >= from);
    }
    if (this.filterDateTo) {
      const to = new Date(this.filterDateTo).getTime() + 86400000;
      filtered = filtered.filter(s => new Date(s.startedAt).getTime() <= to);
    }
    this.filteredLogs = filtered;
  }

  clearFilters(): void {
    this.filterDateFrom = '';
    this.filterDateTo = '';
    this.applyFilters();
  }

  // CSV Export
  exportCSV(): void {
    if (this.filteredLogs.length === 0) { this.toast.error('No shift data to export.'); return; }
    const headers = ['Shift ID', 'Started', 'Ended', 'Duration', 'Transactions', 'Litres Sold', 'Revenue (INR)', 'Status'];
    const rows = this.filteredLogs.map(s => [
      s.id.substring(0, 8),
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
    a.download = `shift-history-${new Date().toISOString().split('T')[0]}.csv`;
    a.click();
    URL.revokeObjectURL(url);
    this.toast.success('CSV exported successfully.');
  }

  // Checklist
  toggleCheck(item: ChecklistItem): void {
    item.checked = !item.checked;
    this.allChecked = this.briefings.every(b => b.checked);
  }

  countChecked(): number {
    return this.briefings.filter(b => b.checked).length;
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'completed': return 'status-success';
      case 'active': return 'status-active';
      case 'flagged': return 'status-danger';
      default: return 'status-default';
    }
  }
}
