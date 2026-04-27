import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { AuditApiService, AuditLogDto, AuditLogFilters } from '../../../../core/services/audit-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

/** UI-mapped audit log entry for the template */
interface MappedAuditEntry {
  date: string;
  time: string;
  initials: string;
  admin: string;
  actionClass: string;
  action: string;
  resource: string;
  rawDto: AuditLogDto;
  // Detail view fields (populated when entry is selected)
  title?: string;
  id?: string;
  detail?: string;
  currentState?: string;
  modifiedState?: string;
  sourceIp?: string;
  userAgent?: string;
}

@Component({
  selector: 'app-admin-audit',
  templateUrl: './audit.component.html',
  styleUrls: ['./audit.component.scss'],
})
export class AdminAuditComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  logs: MappedAuditEntry[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 50;
  isLoading = true;
  filters: AuditLogFilters = {};
  selectedEntry: MappedAuditEntry | null = null;
  parsedOldValues: Record<string, unknown> | null = null;
  parsedNewValues: Record<string, unknown> | null = null;
  isExporting = false;

  totalEvents = '1.2M';
  integrityScore = '99.9%';
  timeWindows = ['1H', '24H', '7D', '30D', 'ALL'];
  activeWindow = '24H';
  totalEntries = 12450;

  entityTypes = ['User', 'Station', 'Tank', 'Transaction', 'FuelPrice', 'FraudAlert', 'Shift'];
  operations = ['Create', 'Update', 'Delete', 'Login', 'Logout', 'Lock', 'Unlock'];

  constructor(private auditApi: AuditApiService, private toast: ToastService) {}

  ngOnInit(): void { this.loadLogs(); }
  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  loadLogs(): void {
    this.isLoading = true;
    this.auditApi.getLogs(this.page, this.pageSize, this.filters).pipe(takeUntil(this.destroy$)).subscribe({
      next: (result) => {
        this.logs = result.items.map((l: AuditLogDto): MappedAuditEntry => ({
          date: new Date(l.timestamp).toISOString().split('T')[0],
          time: new Date(l.timestamp).toLocaleTimeString(),
          initials: l.changedByUserId ? l.changedByUserId.substring(0, 2).toUpperCase() : 'SY',
          admin: l.changedByUserName || l.changedByUserId || 'SYSTEM_NODE',
          actionClass: this.getActionClass(l.operation),
          action: l.operation,
          resource: l.entityType,
          rawDto: l
        }));
        this.totalCount = result.totalCount;
        this.totalEntries = result.totalCount;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; },
    });
  }

  onPageChange(page: number): void { this.page = page; this.loadLogs(); }
  onFilterChange(filters: AuditLogFilters): void { this.filters = filters; this.page = 1; this.loadLogs(); }

  viewDetails(log: MappedAuditEntry): void {
    const raw = log.rawDto;
    this.selectedEntry = {
      ...log,
      title: `${raw.operation} on ${raw.entityType}`,
      id: raw.entityId,
      detail: `Operator ${raw.changedByUserName || raw.changedByUserId} modified ${raw.entityType} properties at ${new Date(raw.timestamp).toLocaleString()}`,
      currentState: raw.oldValues || '{}',
      modifiedState: raw.newValues || '{}',
      sourceIp: '10.2.44.109',
      userAgent: 'Mozilla/5.0 EPCL-Client/2.4'
    };
    this.parsedOldValues = raw.oldValues ? JSON.parse(raw.oldValues) : null;
    this.parsedNewValues = raw.newValues ? JSON.parse(raw.newValues) : null;
  }

  closeDetails(): void { this.selectedEntry = null; }

  getDiffKeys(): string[] {
    const keys = new Set<string>();
    if (this.parsedOldValues) Object.keys(this.parsedOldValues).forEach(k => keys.add(k));
    if (this.parsedNewValues) Object.keys(this.parsedNewValues).forEach(k => keys.add(k));
    return Array.from(keys);
  }

  isChanged(key: string): boolean {
    const old = this.parsedOldValues ? (this.parsedOldValues as Record<string, unknown>)[key] : undefined;
    const nw = this.parsedNewValues ? (this.parsedNewValues as Record<string, unknown>)[key] : undefined;
    return JSON.stringify(old) !== JSON.stringify(nw);
  }

  exportLogs(): void {
    this.isExporting = true;
    this.auditApi.exportLogs(this.filters).pipe(takeUntil(this.destroy$)).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `audit-logs-${new Date().toISOString().split('T')[0]}.csv`;
        a.click();
        URL.revokeObjectURL(url);
        this.isExporting = false;
      },
      error: () => { this.toast.error('Export failed.'); this.isExporting = false; },
    });
  }

  getActionClass(action: string): string {
    switch (action.toLowerCase()) {
      case 'create': case 'login': return 'action-create';
      case 'update': return 'action-update';
      case 'delete': case 'lock': return 'action-delete';
      default: return 'action-default';
    }
  }
}
