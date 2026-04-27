import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { FraudApiService, FraudAlertDto, FraudAlertFilters } from '../../../../core/services/fraud-api.service';
import { SignalRService } from '../../../../core/services/signalr.service';
import { ToastService } from '../../../../shared/services/toast.service';

interface DisplayAnomaly {
  id: string;
  timestamp: string;
  station: string;
  stationStatus: string;
  nodeId: string;
  flagIcon: string;
  flagType: string;
  severityClass: string;
  severity: string;
  rawDto: FraudAlertDto;
}

@Component({
  selector: 'app-admin-fraud',
  templateUrl: './fraud.component.html',
  styleUrls: ['./fraud.component.scss'],
})
export class AdminFraudComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  anomalies: DisplayAnomaly[] = [];
  rawAlerts: FraudAlertDto[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 20;
  isLoading = true;
  filters: FraudAlertFilters = { status: 'Open' };
  selectedAlert: FraudAlertDto | null = null;

  // Bulk selection
  selectedIds: Set<string> = new Set();
  showDismissModal = false;
  dismissReason = '';

  kpis = [
    { label: 'CRITICAL FLAGS', value: '14', color: '#ef4444', trendClass: 'up', trend: '↑ 2' },
    { label: 'NETWORK RISK', value: 'High', color: '#f59e0b', sub: 'Monitoring 4 Clusters' },
    { label: 'LOSS PREVENTION', value: '42.5', unit: 'kL', color: '#10b981', trendClass: 'up', trend: '↑' }
  ];
  totalAlerts = 142;
  aiPattern = 'Multiple micro-transactions detected from Node ST-0042 matching known skimming footprint. Volume anomaly: 4.2%.';
  aiAction = 'Isolate Node ST-0042 from payment network and cross-reference with pump hardware logs.';

  constructor(
    private fraudApi: FraudApiService,
    private signalR: SignalRService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.loadAlerts();
    // Live fraud alerts via SignalR
    this.signalR.newFraudAlert$.pipe(takeUntil(this.destroy$)).subscribe(alert => {
      const mapped: FraudAlertDto = {
        id: alert.id,
        transactionId: alert.transactionId,
        stationId: alert.stationId,
        stationName: `Station ${alert.stationId.substring(0, 4)}`,
        ruleTriggered: alert.ruleTriggered,
        severity: alert.severity,
        status: 'Open',
        description: alert.description,
        createdAt: alert.createdAt,
      };
      this.rawAlerts.unshift(mapped);
      this.totalCount++;
      this.loadAlerts(); // Refresh display anomalies
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  loadAlerts(): void {
    this.isLoading = true;
    this.fraudApi.getAlerts(this.page, this.pageSize, this.filters).pipe(takeUntil(this.destroy$)).subscribe({
      next: (result) => {
        this.rawAlerts = result.items;
        this.anomalies = result.items.map(a => ({
          id: a.id,
          timestamp: new Date(a.createdAt).toLocaleTimeString(),
          station: `Station ${a.stationId.substring(0,4)}`,
          stationStatus: 'up',
          nodeId: a.stationId.substring(0,8),
          flagIcon: a.severity === 'High' ? 'alert-triangle' : 'alert-circle',
          flagType: a.ruleTriggered || 'Anomaly',
          severityClass: this.getSeverityClass(a.severity),
          severity: a.severity,
          rawDto: a
        }));
        this.totalCount = result.totalCount;
        this.totalAlerts = result.totalCount;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; },
    });
  }

  onPageChange(page: number): void { this.page = page; this.loadAlerts(); }
  onFilterChange(filters: FraudAlertFilters): void { this.filters = filters; this.page = 1; this.loadAlerts(); }

  viewAlert(alert: DisplayAnomaly): void { this.selectedAlert = alert.rawDto; }
  closeDetail(): void { this.selectedAlert = null; }

  dismissAlert(id: string, reason: string): void {
    this.fraudApi.dismissAlert(id, reason).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('Alert dismissed.'); this.loadAlerts(); this.closeDetail(); },
      error: () => this.toast.error('Failed to dismiss alert.'),
    });
  }

  investigateAlert(id: string): void {
    this.fraudApi.investigateAlert(id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('Alert marked for investigation.'); this.loadAlerts(); this.closeDetail(); },
      error: () => this.toast.error('Failed to update alert.'),
    });
  }

  escalateAlert(id: string): void {
    this.fraudApi.escalateAlert(id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('Alert escalated.'); this.loadAlerts(); this.closeDetail(); },
      error: () => this.toast.error('Failed to escalate alert.'),
    });
  }

  toggleSelection(id: string): void {
    if (this.selectedIds.has(id)) this.selectedIds.delete(id);
    else this.selectedIds.add(id);
  }

  bulkDismiss(): void {
    if (this.selectedIds.size === 0 || !this.dismissReason) return;
    this.fraudApi.bulkDismiss(Array.from(this.selectedIds), this.dismissReason).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toast.success(`${this.selectedIds.size} alerts dismissed.`);
        this.selectedIds.clear();
        this.dismissReason = '';
        this.showDismissModal = false;
        this.loadAlerts();
      },
      error: () => this.toast.error('Bulk dismiss failed.'),
    });
  }

  getSeverityClass(severity: string): string {
    switch (severity?.toLowerCase()) {
      case 'high': return 'severity-high';
      case 'medium': return 'severity-medium';
      case 'low': return 'severity-low';
      default: return '';
    }
  }
}
