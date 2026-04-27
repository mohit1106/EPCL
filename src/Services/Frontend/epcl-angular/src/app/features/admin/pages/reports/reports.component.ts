import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { ReportsApiService, AdminKpiDto, SalesSummaryDto, GeneratedReportDto, ScheduledReportDto } from '../../../../core/services/reports-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-admin-reports',
  templateUrl: './reports.component.html',
  styleUrls: ['./reports.component.scss'],
})
export class AdminReportsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  kpi: AdminKpiDto | null = null;
  salesSummary: SalesSummaryDto | null = null;
  scheduledReports: ScheduledReportDto[] = [];
  isLoading = true;
  isExporting = false;
  selectedPeriod = 'today';

  // Report builder
  reportType = 'daily-sales';
  reportStationId = '';
  reportDateFrom = new Date().toISOString().split('T')[0];
  reportDateTo = new Date().toISOString().split('T')[0];

  startDate = '2023-11-01';
  endDate = '2023-11-07';
  clusters = ['NORTH_HUB', 'EAST_DEPOT'];
  fuelTypes = [
    { name: 'Petrol 95', color: '#10b981', selected: true },
    { name: 'Petrol 98', color: '#f59e0b', selected: true },
    { name: 'Diesel', color: '#6366f1', selected: false }
  ];
  operatorCohort = 'ALL FLEET OPERATORS';
  
  queryId = 'REQ-09A1F4';
  results = [
    { timestamp: '11-07 14:22', stationId: 'ST-0041', product: 'P95', productClass: 'badge-success', volume: 1450.4, yield: '+2.1%', status: 'COMPLETED' },
    { timestamp: '11-07 14:18', stationId: 'ST-0092', product: 'DSL', productClass: 'badge-primary', volume: 8200.0, yield: '-0.4%', status: 'COMPLETED' }
  ];
  totalPages = 24;
  totalRecords = 1428;
  complexity = 4;
  estRows = '1.4M';
  velocityBars = [20, 35, 40, 60, 55, 75, 65, 80, 45, 30, 25, 50];
  networkIntegrity = '99.9%';

  reportTypes = [
    { value: 'daily-sales', label: 'Daily Sales Report' },
    { value: 'station-performance', label: 'Station Performance' },
    { value: 'inventory-summary', label: 'Inventory Summary' },
    { value: 'fraud-summary', label: 'Fraud Analysis' },
    { value: 'loyalty-summary', label: 'Loyalty Program Report' },
  ];

  constructor(private reportsApi: ReportsApiService, private toast: ToastService) {}

  ngOnInit(): void { this.loadReports(); }
  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  private loadReports(): void {
    this.isLoading = true;
    this.reportsApi.getAdminKpi().pipe(takeUntil(this.destroy$)).subscribe({
      next: (kpi) => { this.kpi = kpi; this.isLoading = false; },
      error: () => { this.isLoading = false; },
    });
    this.loadSalesSummary();
    this.reportsApi.getScheduledReports().pipe(takeUntil(this.destroy$)).subscribe({
      next: (s) => { this.scheduledReports = s; },
    });
  }

  loadSalesSummary(): void {
    const now = new Date();
    let dateFrom = now.toISOString().split('T')[0];
    if (this.selectedPeriod === 'week') { const d = new Date(now); d.setDate(d.getDate() - 7); dateFrom = d.toISOString().split('T')[0]; }
    else if (this.selectedPeriod === 'month') { const d = new Date(now); d.setMonth(d.getMonth() - 1); dateFrom = d.toISOString().split('T')[0]; }
    this.reportsApi.getSalesSummary({ dateFrom, dateTo: now.toISOString().split('T')[0], groupBy: 'fuelType' })
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: (s) => { this.salesSummary = s; } });
  }

  onPeriodChange(period: string): void { this.selectedPeriod = period; this.loadSalesSummary(); }

  exportPdf(): void {
    this.isExporting = true;
    const filters: Record<string, string> = { dateFrom: this.reportDateFrom, dateTo: this.reportDateTo };
    if (this.reportStationId) filters['stationId'] = this.reportStationId;
    this.reportsApi.exportPdf(this.reportType, filters).pipe(takeUntil(this.destroy$)).subscribe({
      next: (report) => { this.pollAndDownload(report.id); },
      error: () => { this.toast.error('Export failed.'); this.isExporting = false; },
    });
  }

  exportExcel(): void {
    this.isExporting = true;
    const filters: Record<string, string> = { dateFrom: this.reportDateFrom, dateTo: this.reportDateTo };
    if (this.reportStationId) filters['stationId'] = this.reportStationId;
    this.reportsApi.exportExcel(this.reportType, filters).pipe(takeUntil(this.destroy$)).subscribe({
      next: (report) => { this.pollAndDownload(report.id); },
      error: () => { this.toast.error('Export failed.'); this.isExporting = false; },
    });
  }

  private pollAndDownload(reportId: string): void {
    const poll = setInterval(() => {
      this.reportsApi.getExportStatus(reportId).pipe(takeUntil(this.destroy$)).subscribe({
        next: (status) => {
          if (status.status === 'Completed') {
            clearInterval(poll);
            this.isExporting = false;
            this.reportsApi.downloadExport(reportId).pipe(takeUntil(this.destroy$)).subscribe({
              next: (blob) => {
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `report-${reportId}`;
                a.click();
                URL.revokeObjectURL(url);
                this.toast.success('Report downloaded.');
              },
            });
          } else if (status.status === 'Failed') {
            clearInterval(poll);
            this.isExporting = false;
            this.toast.error('Report generation failed.');
          }
        },
      });
    }, 2000);
  }

  deleteScheduledReport(id: string): void {
    if (!confirm('Delete this scheduled report?')) return;
    this.reportsApi.deleteScheduledReport(id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('Scheduled report deleted.'); this.loadReports(); },
    });
  }

  getStatusClass(status: string): string {
    return status === 'COMPLETED' ? 'status-success' : 'status-pending';
  }
}
