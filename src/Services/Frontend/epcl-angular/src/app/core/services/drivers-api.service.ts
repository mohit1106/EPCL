import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DriverDto {
  id: string;
  driverCode: string;
  fullName: string;
  phone: string;
  licenseNumber: string;
  vehicleNumber: string;
  isAvailable: boolean;
  currentRequestId?: string;
  createdAt: string;
}

export interface CreateDriverDto {
  fullName: string;
  phone: string;
  licenseNumber: string;
  vehicleNumber: string;
}

export interface UpdateDriverDto {
  fullName?: string;
  phone?: string;
  licenseNumber?: string;
  vehicleNumber?: string;
}

@Injectable({ providedIn: 'root' })
export class DriversApiService {
  private readonly base = '/gateway/drivers';

  constructor(private http: HttpClient) {}

  getAll(): Observable<DriverDto[]> {
    return this.http.get<DriverDto[]>(this.base);
  }

  getAvailable(): Observable<DriverDto[]> {
    return this.http.get<DriverDto[]>(`${this.base}/available`);
  }

  getById(id: string): Observable<DriverDto> {
    return this.http.get<DriverDto>(`${this.base}/${id}`);
  }

  create(dto: CreateDriverDto): Observable<DriverDto> {
    return this.http.post<DriverDto>(this.base, dto);
  }

  update(id: string, dto: UpdateDriverDto): Observable<DriverDto> {
    return this.http.put<DriverDto>(`${this.base}/${id}`, dto);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  release(id: string): Observable<DriverDto> {
    return this.http.put<DriverDto>(`${this.base}/${id}/release`, {});
  }
}
