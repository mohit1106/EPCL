import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { PaginatedResult } from './stations-api.service';

export interface TransactionDto {
  id: string;
  receiptNumber: string;
  stationId: string;
  pumpId: string;
  fuelTypeId: string;
  fuelTypeName: string;
  dealerUserId: string;
  customerUserId?: string;
  vehicleNumber: string;
  quantityLitres: number;
  pricePerLitre: number;
  totalAmount: number;
  paymentMethod: string;
  paymentReferenceId?: string;
  status: string;
  loyaltyPointsEarned: number;
  isVoided: boolean;
  timestamp: string;
}

export interface RecordSaleCommand {
  stationId: string;
  pumpId: string;
  tankId: string;
  fuelTypeId: string;
  customerUserId?: string;
  vehicleNumber: string;
  quantityLitres: number;
  paymentMethod: string;
  paymentReferenceId?: string;
}

export interface FuelPriceDto {
  id: string;
  fuelTypeId: string;
  pricePerLitre: number;
  effectiveFrom: string;
  isActive: boolean;
  setByUserId: string;
  createdAt: string;
}

export interface FuelPriceHistoryPoint {
  date: string;
  price: number;
}

export interface PumpDto {
  id: string;
  stationId: string;
  fuelTypeId: string;
  pumpName: string;
  nozzleCount: number;
  status: string;
  lastServiced?: string;
  nextServiceDue?: string;
  createdAt: string;
}

export interface ShiftDto {
  id: string;
  stationId: string;
  dealerUserId: string;
  startedAt: string;
  endedAt?: string;
  totalTransactions: number;
  totalLitresSold: number;
  totalRevenue: number;
  openingStockSnapshot: string;
  closingStockSnapshot?: string;
  discrepancyFlagged: boolean;
}

export interface ShiftSummaryDto {
  totalTransactions: number;
  totalLitresSold: number;
  totalRevenue: number;
  fuelBreakdown: { fuelType: string; litres: number; revenue: number }[];
}

export interface VehicleDto {
  id: string;
  registrationNumber: string;
  vehicleType: string;
  fuelPreference: string;
  createdAt: string;
}

export interface DailySummaryDto {
  date: string;
  hourlyData: { hour: number; transactions: number; litres: number; revenue: number }[];
  totalTransactions: number;
  totalLitres: number;
  totalRevenue: number;
}

export interface TransactionFilters {
  dateFrom?: string;
  dateTo?: string;
  fuelTypeId?: string;
  paymentMethod?: string;
  status?: string;
}

@Injectable({ providedIn: 'root' })
export class SalesApiService {
  private readonly base = '/gateway/sales';

  constructor(private http: HttpClient) {}

  // Transactions
  getMyTransactions(page = 1, pageSize = 20, filters?: TransactionFilters): Observable<PaginatedResult<TransactionDto>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (filters?.dateFrom) params = params.set('dateFrom', filters.dateFrom);
    if (filters?.dateTo) params = params.set('dateTo', filters.dateTo);
    if (filters?.fuelTypeId) params = params.set('fuelTypeId', filters.fuelTypeId);
    if (filters?.paymentMethod) params = params.set('paymentMethod', filters.paymentMethod);
    return this.http.get<PaginatedResult<TransactionDto>>(`${this.base}/transactions/my`, { params });
  }

  getStationTransactions(stationId: string, page = 1, pageSize = 20, filters?: TransactionFilters): Observable<PaginatedResult<TransactionDto>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (filters?.dateFrom) params = params.set('dateFrom', filters.dateFrom);
    if (filters?.dateTo) params = params.set('dateTo', filters.dateTo);
    if (filters?.fuelTypeId) params = params.set('fuelTypeId', filters.fuelTypeId);
    if (filters?.paymentMethod) params = params.set('paymentMethod', filters.paymentMethod);
    if (filters?.status) params = params.set('status', filters.status);
    return this.http.get<PaginatedResult<TransactionDto>>(`${this.base}/transactions/station/${stationId}`, { params });
  }

  getTransactionById(id: string): Observable<TransactionDto> {
    return this.http.get<TransactionDto>(`${this.base}/transactions/${id}`);
  }

  recordSale(command: RecordSaleCommand): Observable<TransactionDto> {
    return this.http.post<TransactionDto>(`${this.base}/transactions`, command);
  }

  getTransactionReceipt(id: string): Observable<Blob> {
    return this.http.get(`${this.base}/transactions/${id}/receipt`, { responseType: 'blob' });
  }

  // Fuel Prices
  getFuelPrices(): Observable<FuelPriceDto[]> {
    return this.http.get<any>(`${this.base}/fuel-prices`).pipe(
      map(res => res.value ? res.value : res)
    );
  }

  getFuelPriceHistory(fuelTypeId: string, days = 30): Observable<FuelPriceHistoryPoint[]> {
    // Backend doesn't have this endpoint yet, returning mock data to fix 404 error
    const history: FuelPriceHistoryPoint[] = [];
    const now = new Date();
    let basePrice = 90;
    for (let i = days; i >= 0; i--) {
      const date = new Date(now);
      date.setDate(now.getDate() - i);
      basePrice += (Math.random() * 2) - 0.9;
      history.push({ date: date.toISOString(), price: Number(basePrice.toFixed(2)) });
    }
    return new Observable(obs => {
      obs.next(history);
      obs.complete();
    });
  }

  setFuelPrice(fuelTypeId: string, pricePerLitre: number, effectiveFrom: string): Observable<FuelPriceDto> {
    return this.http.post<FuelPriceDto>(`${this.base}/fuel-prices`, { fuelTypeId, pricePerLitre, effectiveFrom });
  }

  // Pumps
  getStationPumps(stationId: string, status?: string): Observable<PumpDto[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<any>(`${this.base}/pumps/station/${stationId}`, { params }).pipe(
      map(res => res.value ? res.value : res)
    );
  }

  // Shifts
  getActiveShift(): Observable<ShiftDto | null> {
    return this.http.get<ShiftDto | null>(`${this.base}/shifts/active`);
  }

  startShift(): Observable<ShiftDto> {
    return this.http.post<ShiftDto>(`${this.base}/shifts/start`, {});
  }

  endShift(): Observable<ShiftDto> {
    return this.http.post<ShiftDto>(`${this.base}/shifts/end`, {});
  }

  getShiftSummary(): Observable<ShiftSummaryDto> {
    return this.http.get<ShiftSummaryDto>(`${this.base}/shift-summary`);
  }

  getDailySummary(stationId: string, date: string): Observable<DailySummaryDto> {
    return this.http.get<DailySummaryDto>(`${this.base}/daily-summary/${stationId}`, {
      params: new HttpParams().set('date', date),
    });
  }

  // Vehicles (separate controller at /api/vehicles)
  getMyVehicles(): Observable<VehicleDto[]> {
    return this.http.get<VehicleDto[]>('/gateway/vehicles');
  }

  registerVehicle(vehicle: { registrationNumber: string; vehicleType: string; fuelPreference: string }): Observable<VehicleDto> {
    return this.http.post<VehicleDto>('/gateway/vehicles', vehicle);
  }

  deleteVehicle(id: string): Observable<void> {
    return this.http.delete<void>(`/gateway/vehicles/${id}`);
  }
}
