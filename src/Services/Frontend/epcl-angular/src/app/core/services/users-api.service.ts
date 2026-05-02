import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UserDto } from './auth-api.service';
import { PaginatedResult } from './stations-api.service';

export interface UserListDto extends UserDto {
  lastLoginAt?: string;
  failedLoginAttempts: number;
  lockoutEnd?: string;
}

export interface UserFilters {
  role?: string;
  isActive?: boolean;
  search?: string;
}

export interface UpdateProfileDto {
  fullName?: string;
  phoneNumber?: string;
  city?: string;
  state?: string;
  preferredLanguage?: string;
}

@Injectable({ providedIn: 'root' })
export class UsersApiService {
  private readonly base = '/gateway/users';

  constructor(private http: HttpClient) {}

  getUsers(page = 1, pageSize = 20, filters?: UserFilters): Observable<PaginatedResult<UserListDto>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (filters?.role) params = params.set('role', filters.role);
    if (filters?.isActive !== undefined) params = params.set('isActive', filters.isActive);
    if (filters?.search) params = params.set('search', filters.search);
    return this.http.get<PaginatedResult<UserListDto>>(this.base, { params });
  }

  /** Get admin users list — accessible by ALL authenticated users (including dealers). */
  getAdmins(): Observable<AdminSummaryDto[]> {
    return this.http.get<AdminSummaryDto[]>(`${this.base}/admins`);
  }

  getUserById(id: string): Observable<UserListDto> {
    return this.http.get<UserListDto>(`${this.base}/${id}`);
  }

  getCurrentUser(): Observable<UserDto> {
    return this.http.get<UserDto>(`${this.base}/me`);
  }

  updateProfile(profile: UpdateProfileDto): Observable<UserDto> {
    return this.http.put<UserDto>(`${this.base}/me`, profile);
  }

  updateUserRole(userId: string, role: string): Observable<void> {
    return this.http.put<void>(`${this.base}/${userId}/role`, { role });
  }

  lockUser(userId: string, reason: string): Observable<void> {
    return this.http.put<void>(`${this.base}/${userId}/lock`, { reason });
  }

  unlockUser(userId: string): Observable<void> {
    return this.http.put<void>(`${this.base}/${userId}/unlock`, {});
  }

  softDeleteUser(userId: string): Observable<void> {
    return this.http.put<void>(`${this.base}/${userId}`, { isActive: false });
  }
}

export interface AdminSummaryDto {
  id: string;
  fullName: string;
  email: string;
}
