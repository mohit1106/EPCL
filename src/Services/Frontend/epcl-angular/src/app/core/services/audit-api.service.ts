import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaginatedResult } from './stations-api.service';

export interface AuditLogDto {
  id: string;
  entityType: string;
  entityId: string;
  operation: string;
  oldValues?: string;
  newValues?: string;
  changedByUserId: string;
  changedByUserName?: string;
  correlationId?: string;
  serviceName: string;
  timestamp: string;
}

export interface AuditLogFilters {
  entityType?: string;
  userId?: string;
  operation?: string;
  dateFrom?: string;
  dateTo?: string;
}

@Injectable({ providedIn: 'root' })
export class AuditApiService {
  private readonly base = '/gateway/audit';

  constructor(private http: HttpClient) {}

  getLogs(page = 1, pageSize = 50, filters?: AuditLogFilters): Observable<PaginatedResult<AuditLogDto>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (filters?.entityType) params = params.set('entityType', filters.entityType);
    if (filters?.userId) params = params.set('userId', filters.userId);
    if (filters?.operation) params = params.set('operation', filters.operation);
    if (filters?.dateFrom) params = params.set('dateFrom', filters.dateFrom);
    if (filters?.dateTo) params = params.set('dateTo', filters.dateTo);
    return this.http.get<PaginatedResult<AuditLogDto>>(`${this.base}/logs`, { params });
  }

  getLogById(id: string): Observable<AuditLogDto> {
    return this.http.get<AuditLogDto>(`${this.base}/logs/${id}`);
  }

  exportLogs(filters?: AuditLogFilters): Observable<Blob> {
    let params = new HttpParams();
    if (filters?.entityType) params = params.set('entityType', filters.entityType);
    if (filters?.dateFrom) params = params.set('dateFrom', filters.dateFrom);
    if (filters?.dateTo) params = params.set('dateTo', filters.dateTo);
    return this.http.post(`${this.base}/logs/export`, null, { params, responseType: 'blob' });
  }
}
