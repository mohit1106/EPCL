import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaginatedResult } from './stations-api.service';

export interface FraudAlertDto {
  id: string;
  transactionId: string;
  stationId: string;
  stationName: string;
  ruleTriggered: string;
  severity: string;
  status: string;
  description: string;
  createdAt: string;
  reviewedAt?: string;
  reviewedByUserId?: string;
  dismissReason?: string;
}

export interface FraudAlertFilters {
  status?: string;
  severity?: string;
  stationId?: string;
  dateFrom?: string;
  dateTo?: string;
}

@Injectable({ providedIn: 'root' })
export class FraudApiService {
  private readonly base = '/gateway/fraud';

  constructor(private http: HttpClient) {}

  getAlerts(page = 1, pageSize = 20, filters?: FraudAlertFilters): Observable<PaginatedResult<FraudAlertDto>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (filters?.status) params = params.set('status', filters.status);
    if (filters?.severity) params = params.set('severity', filters.severity);
    if (filters?.stationId) params = params.set('stationId', filters.stationId);
    if (filters?.dateFrom) params = params.set('dateFrom', filters.dateFrom);
    if (filters?.dateTo) params = params.set('dateTo', filters.dateTo);
    return this.http.get<PaginatedResult<FraudAlertDto>>(`${this.base}/alerts`, { params });
  }

  dismissAlert(id: string, reason: string): Observable<void> {
    return this.http.put<void>(`${this.base}/alerts/${id}/dismiss`, { reason });
  }

  investigateAlert(id: string): Observable<void> {
    return this.http.put<void>(`${this.base}/alerts/${id}/investigate`, {});
  }

  escalateAlert(id: string): Observable<void> {
    return this.http.put<void>(`${this.base}/alerts/${id}/escalate`, {});
  }

  bulkDismiss(alertIds: string[], reason: string): Observable<void> {
    return this.http.post<void>(`${this.base}/alerts/bulk-dismiss`, { alertIds, reason });
  }
}
