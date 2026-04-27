import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface StationDto {
  id: string;
  name: string;
  code: string;
  address: string;
  city: string;
  state: string;
  latitude: number;
  longitude: number;
  contactPhone: string;
  dealerUserId: string;
  is24x7: boolean;
  hasCng: boolean;
  isActive: boolean;
  operatingHoursStart: string;
  operatingHoursEnd: string;
  createdAt: string;
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
    return this.http.put<void>(`${this.base}/${id}`, { isActive: false });
  }

  getFuelTypes(): Observable<FuelTypeDto[]> {
    return this.http.get<FuelTypeDto[]>(`${this.base}/fuel-types`);
  }
}
