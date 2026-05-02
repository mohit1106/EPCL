import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaginatedResult } from './stations-api.service';

export interface TankDto {
  id: string;
  stationId: string;
  fuelTypeId: string;
  tankSerialNumber: string;
  capacityLitres: number;
  currentStockLitres: number;
  reservedLitres: number;
  availableStock: number;
  minThresholdLitres: number;
  status: string;
  lastReplenishedAt: string;
  lastDipReadingAt: string;
  createdAt: string;
}

export interface DipReadingDto {
  tankId: string;
  measuredLitres: number;
  notes?: string;
}

export interface DipReadingResultDto {
  id: string;
  systemLitres: number;
  measuredLitres: number;
  variancePercentage: number;
  isFraudFlagged: boolean;
  recordedAt: string;
}

export interface StockLoadingDto {
  id: string;
  tankId: string;
  quantityLoadedLitres: number;
  loadedByUserId: string;
  tankerNumber: string;
  invoiceNumber: string;
  supplierName?: string;
  stockBefore: number;
  stockAfter: number;
  timestamp: string;
  notes?: string;
}

export interface StockLoadingRequest {
  tankId: string;
  quantityLitres: number;
  tankerNumber: string;
  invoiceNumber: string;
  notes?: string;
}

export interface ReplenishmentCreateDto {
  stationId: string;
  tankId: string;
  requestedQuantityLitres: number;
  urgencyLevel: string;
  notes?: string;
  targetPumpName?: string;
  fuelTypeName?: string;
  priority?: string;
  requestedWindow?: string;
}

export interface ReplenishmentStatusDto {
  id: string;
  stationId: string;
  tankId: string;
  requestedByUserId: string;
  requestedQuantityLitres: number;
  urgencyLevel: string;
  status: string;
  requestedAt: string;
  reviewedByUserId?: string;
  reviewedAt?: string;
  rejectionReason?: string;
  notes?: string;
  // Extended fields
  orderNumber: string;
  targetPumpName?: string;
  fuelTypeName?: string;
  priority: string;
  requestedWindow?: string;
  // Driver
  assignedDriverId?: string;
  assignedDriverName?: string;
  assignedDriverPhone?: string;
  assignedDriverCode?: string;
  // Verification
  dealerVerifiedAt?: string;
  dealerVerifiedDriverCode?: string;
}

export interface StockHistoryPoint {
  date: string;
  stockLitres: number;
}

@Injectable({ providedIn: 'root' })
export class InventoryApiService {
  private readonly base = '/gateway/inventory';

  constructor(private http: HttpClient) {}

  getTanks(stationId: string): Observable<TankDto[]> {
    return this.http.get<TankDto[]>(`${this.base}/stations/${stationId}/tanks`);
  }

  recordDipReading(reading: DipReadingDto): Observable<DipReadingResultDto> {
    return this.http.post<DipReadingResultDto>(`${this.base}/tanks/${reading.tankId}/dip-reading`, reading);
  }

  recordStockLoading(loading: StockLoadingRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/stock-loading`, loading);
  }

  getStockLoadingHistory(tankId: string, page = 1, pageSize = 20): Observable<StockLoadingDto[]> {
    return this.http.get<StockLoadingDto[]>(`${this.base}/stock-loading/${tankId}`, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize),
    });
  }

  // ── Replenishment ────────────────────────────────────────────────

  createReplenishmentRequest(request: ReplenishmentCreateDto): Observable<ReplenishmentStatusDto> {
    return this.http.post<ReplenishmentStatusDto>(`${this.base}/replenishment-requests`, request);
  }

  getReplenishmentRequests(stationId: string): Observable<{ items: ReplenishmentStatusDto[] }> {
    return this.http.get<{ items: ReplenishmentStatusDto[] }>(`${this.base}/replenishment-requests/station/${stationId}`);
  }

  getAllReplenishmentRequests(page = 1, pageSize = 50, status?: string): Observable<{ items: ReplenishmentStatusDto[] }> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    return this.http.get<{ items: ReplenishmentStatusDto[] }>(`${this.base}/replenishment-requests`, { params });
  }

  approveReplenishment(id: string, notes?: string): Observable<any> {
    return this.http.put(`${this.base}/replenishment-requests/${id}/approve`, { notes });
  }

  rejectReplenishment(id: string, reason: string): Observable<any> {
    return this.http.put(`${this.base}/replenishment-requests/${id}/reject`, { reason });
  }

  assignDriver(id: string, driverId: string, driverName: string, driverPhone: string, driverCode: string): Observable<any> {
    return this.http.put(`${this.base}/replenishment-requests/${id}/assign-driver`, {
      driverId, driverName, driverPhone, driverCode
    });
  }

  updateReplenishmentStatus(id: string, status: string): Observable<any> {
    return this.http.put(`${this.base}/replenishment-requests/${id}/update-status`, { status });
  }

  verifyOffloading(id: string, orderNumber: string, driverCode: string): Observable<any> {
    return this.http.put(`${this.base}/replenishment-requests/${id}/verify-offloading`, { orderNumber, driverCode });
  }

  completeReplenishment(id: string): Observable<any> {
    return this.http.put(`${this.base}/replenishment-requests/${id}/complete`, {});
  }

  getStockHistory(tankId: string, days = 30): Observable<StockHistoryPoint[]> {
    return this.http.get<StockHistoryPoint[]>(`${this.base}/stock-history/${tankId}`, {
      params: new HttpParams().set('days', days),
    });
  }
}
