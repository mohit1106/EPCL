import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil, interval, startWith } from 'rxjs';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { SalesApiService, ShiftDto, ShiftSummaryDto } from '../../../../core/services/sales-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-shift',
  templateUrl: './shift.component.html',
  styleUrls: ['./shift.component.scss'],
})
export class ShiftComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  activeShift: ShiftDto | null = null;
  summary: ShiftSummaryDto | null = null;
  isLoading = true;
  isStarting = false;
  isEnding = false;

  operatorId = '';
  shiftStarted = '';

  shiftKpis: { label: string; value: string; unit: string; trend: string; trendClass: string }[] = [];

  shiftLogs: { id: string; initials: string; operator: string; period: string; revenue: string; cashPct: number; status: string }[] = [];

  discrepancy = 'No discrepancies detected during this session.';

  briefings = [
    { icon: '✓', title: 'Pre-Shift Checklist', desc: 'All equipment verified and operational.', type: 'success' },
    { icon: '⚠', title: 'Safety Protocol', desc: 'Fire extinguishers checked — valid until Dec 2025.', type: 'warn' },
    { icon: 'ℹ', title: 'System Update', desc: 'Price update scheduled at 06:00 UTC.', type: 'info' },
  ];

  constructor(
    private store: Store,
    private salesApi: SalesApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.store.select(selectUser).pipe(takeUntil(this.destroy$)).subscribe(user => {
      if (user) { this.operatorId = user.id?.substring(0, 8) || 'OP-001'; }
    });
    this.loadShift();
    interval(30000).pipe(startWith(0), takeUntil(this.destroy$)).subscribe(() => {
      if (this.activeShift) { this.loadSummary(); }
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  private loadShift(): void {
    this.isLoading = true;
    this.salesApi.getActiveShift().pipe(takeUntil(this.destroy$)).subscribe({
      next: (shift) => {
        this.activeShift = shift;
        this.shiftStarted = shift ? new Date(shift.startedAt).toLocaleTimeString() : 'No active shift';
        this.isLoading = false;
        if (shift) { this.loadSummary(); }
      },
      error: () => { this.isLoading = false; },
    });
  }

  private loadSummary(): void {
    this.salesApi.getShiftSummary().pipe(takeUntil(this.destroy$)).subscribe({
      next: (s) => {
        this.summary = s;
        this.shiftKpis = [
          { label: 'TRANSACTIONS', value: String(s.totalTransactions), unit: '', trend: 'Active session', trendClass: 'up' },
          { label: 'LITRES DISPENSED', value: s.totalLitresSold.toFixed(1), unit: 'L', trend: 'Running total', trendClass: '' },
          { label: 'REVENUE', value: `₹${s.totalRevenue.toLocaleString()}`, unit: '', trend: 'Gross amount', trendClass: 'up' },
          { label: 'ELAPSED', value: this.getElapsedTime(), unit: '', trend: 'Session time', trendClass: '' },
        ];
      },
    });
  }

  startShift(): void {
    this.isStarting = true;
    this.salesApi.startShift().pipe(takeUntil(this.destroy$)).subscribe({
      next: (shift) => {
        this.activeShift = shift;
        this.shiftStarted = new Date(shift.startedAt).toLocaleTimeString();
        this.toast.success('Shift started.');
        this.isStarting = false;
        this.loadSummary();
      },
      error: () => { this.toast.error('Failed to start shift.'); this.isStarting = false; },
    });
  }

  endShift(): void {
    if (!confirm('Are you sure you want to end the current shift?')) return;
    this.isEnding = true;
    this.salesApi.endShift().pipe(takeUntil(this.destroy$)).subscribe({
      next: (shift) => {
        this.shiftLogs.unshift({
          id: shift.id.substring(0, 8),
          initials: this.operatorId.substring(0, 2).toUpperCase(),
          operator: 'Current Operator',
          period: `${new Date(shift.startedAt).toLocaleTimeString()} — ${new Date(shift.endedAt!).toLocaleTimeString()}`,
          revenue: `₹${shift.totalRevenue.toLocaleString()}`,
          cashPct: 60,
          status: 'Completed',
        });
        this.activeShift = null;
        this.shiftStarted = 'No active shift';
        this.toast.success(`Shift ended. Total: ₹${shift.totalRevenue.toLocaleString()}`);
        this.isEnding = false;
        this.shiftKpis = [];
      },
      error: () => { this.toast.error('Failed to end shift.'); this.isEnding = false; },
    });
  }

  getElapsedTime(): string {
    if (!this.activeShift) return '00:00:00';
    const start = new Date(this.activeShift.startedAt).getTime();
    const diff = Math.floor((Date.now() - start) / 1000);
    const h = Math.floor(diff / 3600);
    const m = Math.floor((diff % 3600) / 60);
    const s = diff % 60;
    return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'completed': return 'status-success';
      case 'active': return 'status-active';
      case 'voided': return 'status-flagged';
      default: return 'status-default';
    }
  }
}
