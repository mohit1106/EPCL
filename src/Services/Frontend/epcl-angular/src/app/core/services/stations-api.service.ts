import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface StationDto {
  id: string;
  stationName: string;
  stationCode: string;
  addressLine1: string;
  city: string;
  state: string;
  pinCode?: string;
  latitude: number;
  longitude: number;
  dealerUserId?: string;
  is24x7: boolean;
  isActive: boolean;
  operatingHoursStart: string;
  operatingHoursEnd: string;
  createdAt: string;
  distanceKm?: number;
  // Backend sometimes sends these aliases (mapped differently)
  name: string;
  code: string;
  address: string;
  hasCng?: boolean;
  contactPhone?: string;
}

export interface FuelTypeDto {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
}

export interface NearbyStationDto extends StationDto {
  distanceKm: number;
  stockStatus: string;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class StationsApiService {
  private readonly base = '/gateway/stations';

  constructor(private http: HttpClient) {}

  getStations(page = 1, pageSize = 20, filters?: { isActive?: boolean; city?: string }): Observable<PaginatedResult<StationDto>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (filters?.isActive !== undefined) params = params.set('isActive', filters.isActive);
    if (filters?.city) params = params.set('city', filters.city);
    return this.http.get<PaginatedResult<StationDto>>(this.base, { params });
  }

  getStationById(id: string): Observable<StationDto> {
    return this.http.get<StationDto>(`${this.base}/${id}`);
  }

  getNearbyStations(lat: number, lng: number, radiusKm = 10, filters?: { hasCng?: boolean; is24x7?: boolean }): Observable<NearbyStationDto[]> {
    let params = new HttpParams().set('lat', lat).set('lng', lng).set('radiusKm', radiusKm);
    if (filters?.hasCng) params = params.set('hasCng', true);
    if (filters?.is24x7) params = params.set('is24x7', true);
    return this.http.get<NearbyStationDto[]>(`${this.base}/nearby`, { params });
  }

  createStation(station: Partial<StationDto>): Observable<StationDto> {
    return this.http.post<StationDto>(this.base, station);
  }

  updateStation(id: string, station: Partial<StationDto>): Observable<StationDto> {
    return this.http.put<StationDto>(`${this.base}/${id}`, station);
  }

  deactivateStation(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  getFuelTypes(): Observable<FuelTypeDto[]> {
    return this.http.get<FuelTypeDto[]>(`${this.base}/fuel-types`);
  }

  assignDealerToStation(stationId: string, dealerUserId: string): Observable<StationDto> {
    return this.http.put<StationDto>(`${this.base}/${stationId}/dealer`, { dealerUserId });
  }

  removeDealerFromStation(stationId: string): Observable<StationDto> {
    return this.http.put<StationDto>(`${this.base}/${stationId}/dealer`, { dealerUserId: '00000000-0000-0000-0000-000000000000' });
  }

  getUnassignedStations(): Observable<StationDto[]> {
    return this.getStations(1, 100).pipe(
      map(res => res.items.filter(s => !s.dealerUserId || s.dealerUserId === '00000000-0000-0000-0000-000000000000'))
    );
  }

  /** Find the station assigned to the current dealer by matching dealerUserId */
  getMyStation(dealerUserId: string): Observable<StationDto | null> {
    return this.getStations(1, 100).pipe(
      map(res => {
        const match = res.items.find(s =>
          s.dealerUserId === dealerUserId
        );
        return match || null;
      })
    );
  }

  // ── Parking ──────────────────────────────────────────
  getParkingSlots(stationId: string): Observable<ParkingSlotDto[]> {
    return this.http.get<ParkingSlotDto[]>(`/gateway/sales/parking/stations/${stationId}/slots`);
  }

  getParkingPricing(): Observable<Record<string, Record<string, number>>> {
    return this.http.get<Record<string, Record<string, number>>>(`/gateway/sales/parking/pricing`);
  }

  bookParking(stationId: string, slotType: string, durationHours: number): Observable<any> {
    return this.http.post('/gateway/sales/parking/book', { stationId, slotType, durationHours });
  }

  confirmParkingPayment(orderId: string, paymentId: string, signature: string): Observable<any> {
    return this.http.post('/gateway/sales/parking/confirm', { orderId, paymentId, signature });
  }

  getMyParkingBookings(page = 1, pageSize = 10): Observable<ParkingBookingDto[]> {
    return this.http.get<ParkingBookingDto[]>(`/gateway/sales/parking/my-bookings`, {
      params: { page: page.toString(), pageSize: pageSize.toString() }
    });
  }
}

export interface ParkingSlotDto {
  id: string;
  stationId: string;
  slotType: string;
  slotNumber: string;
  isAvailable: boolean;
}

export interface ParkingBookingDto {
  id: string;
  parkingSlotId: string;
  stationId: string;
  customerId: string;
  slotType: string;
  durationHours: number;
  amount: number;
  status: string;
  razorpayOrderId?: string;
  razorpayPaymentId?: string;
  bookedAt: string;
  expiresAt: string;
}
