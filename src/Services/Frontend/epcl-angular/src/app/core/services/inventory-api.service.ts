import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaginatedResult } from './stations-api.service';

export interface TankDto {
  id: string;
  stationId: string;
  fuelTypeId: string;
  fuelTypeName: string;
  capacityLitres: number;
  currentStockLitres: number;
  reservedLitres: number;
  minThresholdLitres: number;
  criticalThresholdLitres: number;
  status: string;
  lastReplenishedAt: string;
  updatedAt: string;
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
  tankId: string;
  quantityLitres: number;
  tankerNumber: string;
  invoiceNumber: string;
  notes?: string;
}

export interface ReplenishmentRequestDto {
  tankId: string;
  requestedQuantityLitres: number;
  urgencyLevel: string;
  notes?: string;
}

export interface ReplenishmentStatusDto {
  id: string;
  tankId: string;
  fuelTypeName: string;
  requestedQuantityLitres: number;
  urgencyLevel: string;
  status: string;
  notes: string;
  createdAt: string;
  reviewedAt?: string;
  reviewedByUserId?: string;
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

  recordStockLoading(loading: StockLoadingDto): Observable<void> {
    return this.http.post<void>(`${this.base}/stock-loading`, loading);
  }

  createReplenishmentRequest(request: ReplenishmentRequestDto): Observable<ReplenishmentStatusDto> {
    return this.http.post<ReplenishmentStatusDto>(`${this.base}/replenishment-requests`, request);
  }

  getReplenishmentRequests(stationId: string): Observable<ReplenishmentStatusDto[]> {
    return this.http.get<ReplenishmentStatusDto[]>(`${this.base}/replenishment-requests`, {
      params: new HttpParams().set('stationId', stationId),
    });
  }

  getStockHistory(tankId: string, days = 30): Observable<StockHistoryPoint[]> {
    return this.http.get<StockHistoryPoint[]>(`${this.base}/stock-history/${tankId}`, {
      params: new HttpParams().set('days', days),
    });
  }
}
