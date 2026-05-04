import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map, catchError, of } from 'rxjs';

export interface AdminKpiDto {
  totalStations: number;
  totalTransactionsToday: number;
  totalRevenueToday: number;
  totalLitresToday: number;
  fraudAlertsToday: number;
  activeDealers: number;
}

export interface DealerKpiDto {
  stationId: string;
  transactionsToday: number;
  revenueToday: number;
  litresToday: number;
  transactionsThisMonth: number;
  revenueThisMonth: number;
}

// Raw backend response — DailySalesSummaryDto
export interface RawSalesSummaryItem {
  id: string;
  stationId: string;
  fuelTypeId: string;
  date: string;
  totalTransactions: number;
  totalLitresSold: number;
  totalRevenue: number;
}

// Transformed for frontend charts/tables
export interface SalesSummaryDto {
  data: { label: string; value: number; litres: number }[];
  byFuelType: { fuelTypeId: string; litres: number; revenue: number; transactions: number }[];
  totalRevenue: number;
  totalLitres: number;
  totalTransactions: number;
}

export interface GeneratedReportDto {
  id: string;
  reportType: string;
  status: string;
  fileUrl?: string;
  createdAt: string;
  completedAt?: string;
}

export interface ScheduledReportDto {
  id: string;
  reportType: string;
  schedule: string;
  recipients: string;
  isActive: boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class ReportsApiService {
  private readonly base = '/gateway/reports';

  constructor(private http: HttpClient) {}

  getAdminKpi(): Observable<AdminKpiDto> {
    return this.http.get<AdminKpiDto>(`${this.base}/kpi/admin`).pipe(
      catchError(err => {
        console.error('getAdminKpi failed', err);
        return of({
          totalStations: 0, totalTransactionsToday: 0, totalRevenueToday: 0,
          totalLitresToday: 0, fraudAlertsToday: 0, activeDealers: 0
        });
      })
    );
  }

  getDealerKpi(stationId: string): Observable<DealerKpiDto> {
    return this.http.get<DealerKpiDto>(`${this.base}/kpi/dealer/${stationId}`).pipe(
      catchError(err => {
        console.error('getDealerKpi failed', err);
        return of({
          stationId, transactionsToday: 0, revenueToday: 0,
          litresToday: 0, transactionsThisMonth: 0, revenueThisMonth: 0,
        });
      })
    );
  }

  getSalesSummary(params: { stationId?: string; dateFrom?: string; dateTo?: string }): Observable<SalesSummaryDto> {
    let httpParams = new HttpParams();
    if (params.stationId) httpParams = httpParams.set('stationId', params.stationId);
    if (params.dateFrom) httpParams = httpParams.set('dateFrom', params.dateFrom);
    if (params.dateTo) httpParams = httpParams.set('dateTo', params.dateTo);

    return this.http.get<RawSalesSummaryItem[]>(`${this.base}/sales-summary`, { params: httpParams }).pipe(
      map(response => {
        const items = Array.isArray(response) ? response : [];

        let totalRevenue = 0;
        let totalLitres = 0;
        let totalTransactions = 0;

        items.forEach(item => {
          totalRevenue += item.totalRevenue || 0;
          totalLitres += item.totalLitresSold || 0;
          totalTransactions += item.totalTransactions || 0;
        });

        // Group by date for daily chart
        const byDate = new Map<string, { litres: number; revenue: number }>();
        items.forEach(item => {
          const dateKey = item.date || 'unknown';
          const existing = byDate.get(dateKey) || { litres: 0, revenue: 0 };
          existing.litres += item.totalLitresSold || 0;
          existing.revenue += item.totalRevenue || 0;
          byDate.set(dateKey, existing);
        });

        const data = Array.from(byDate.entries())
          .sort((a, b) => a[0].localeCompare(b[0]))
          .map(([date, vals]) => ({
            label: this.formatDateLabel(date),
            value: vals.revenue,
            litres: vals.litres,
          }));

        // Group by fuelTypeId for fuel grade breakdown
        const byFuelMap = new Map<string, { litres: number; revenue: number; transactions: number }>();
        items.forEach(item => {
          const fuelId = item.fuelTypeId || 'unknown';
          const existing = byFuelMap.get(fuelId) || { litres: 0, revenue: 0, transactions: 0 };
          existing.litres += item.totalLitresSold || 0;
          existing.revenue += item.totalRevenue || 0;
          existing.transactions += item.totalTransactions || 0;
          byFuelMap.set(fuelId, existing);
        });

        const byFuelType = Array.from(byFuelMap.entries())
          .map(([fuelTypeId, vals]) => ({ fuelTypeId, ...vals }))
          .sort((a, b) => b.litres - a.litres);

        return { data, byFuelType, totalRevenue, totalLitres, totalTransactions };
      }),
      catchError(err => {
        console.error('getSalesSummary failed:', err);
        return of({ data: [], byFuelType: [], totalRevenue: 0, totalLitres: 0, totalTransactions: 0 });
      })
    );
  }

  // ═══ Exports — fixed to match backend ExportReportRequest DTO ═══
  exportPdf(reportType: string, filters: Record<string, string>): Observable<GeneratedReportDto> {
    const body = {
      reportType,
      dateFrom: filters['dateFrom'] || null,
      dateTo: filters['dateTo'] || null,
      stationId: filters['stationId'] || null,
    };
    return this.http.post<GeneratedReportDto>(`${this.base}/export/pdf`, body);
  }

  exportExcel(reportType: string, filters: Record<string, string>): Observable<GeneratedReportDto> {
    const body = {
      reportType,
      dateFrom: filters['dateFrom'] || null,
      dateTo: filters['dateTo'] || null,
      stationId: filters['stationId'] || null,
    };
    return this.http.post<GeneratedReportDto>(`${this.base}/export/excel`, body);
  }

  getExportStatus(reportId: string): Observable<GeneratedReportDto> {
    return this.http.get<GeneratedReportDto>(`${this.base}/exports/${reportId}/status`);
  }

  downloadExport(reportId: string): Observable<Blob> {
    return this.http.get(`${this.base}/exports/${reportId}/download`, { responseType: 'blob' });
  }

  getScheduledReports(): Observable<ScheduledReportDto[]> {
    return this.http.get<ScheduledReportDto[]>(`${this.base}/schedule`);
  }

  createScheduledReport(report: Partial<ScheduledReportDto>): Observable<ScheduledReportDto> {
    return this.http.post<ScheduledReportDto>(`${this.base}/schedule`, report);
  }

  deleteScheduledReport(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/schedule/${id}`);
  }

  getStockPredictions(stationId?: string): Observable<StockPredictionDto[]> {
    let params = new HttpParams();
    if (stationId) params = params.set('stationId', stationId);
    return this.http.get<StockPredictionDto[]>(`${this.base}/stock-predictions`, { params });
  }

  GetAtRiskStockPredictions(daysThreshold: number = 7, stationId?: string): Observable<StockPredictionDto[]> {
    let params = new HttpParams().set('daysThreshold', daysThreshold.toString());
    if (stationId) params = params.set('stationId', stationId);
    return this.http.get<StockPredictionDto[]>(`${this.base}/stock-predictions/at-risk`, { params });
  }

  // ═══ Helpers ═══
  private formatDateLabel(dateStr: string): string {
    try {
      const d = new Date(dateStr);
      if (isNaN(d.getTime())) return dateStr;
      return d.toLocaleDateString('en-IN', { day: '2-digit', month: 'short' });
    } catch {
      return dateStr;
    }
  }
}

export interface StockPredictionDto {
  id: string;
  tankId: string;
  stationId: string;
  fuelTypeId: string;
  currentStockLitres: number;
  avgDailyConsumptionL: number;
  predictedEmptyAt: string | null;
  daysUntilEmpty: number | null;
  alertSentAt: string | null;
  calculatedAt: string;
  dataPointsUsed: number;
}
