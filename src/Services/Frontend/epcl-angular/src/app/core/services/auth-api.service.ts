import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  phoneNumber: string;
  password: string;
  confirmPassword: string;
  role: string;
  referralCode?: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresIn: number;
  user: UserDto;
}

export interface UserDto {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  role: string;
  isActive: boolean;
  isEmailVerified: boolean;
  profilePictureUrl?: string;
  authProvider: string;
  createdAt: string;
  profile?: UserProfileDto;
}

export interface UserProfileDto {
  city?: string;
  state?: string;
  pinCode?: string;
  stationId?: string;
  preferredLanguage?: string;
}

export interface RegisterResponse {
  userId: string;
  message: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  otp: string;
  newPassword: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly baseUrl = '/gateway/auth';

  constructor(private http: HttpClient) {}

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.baseUrl}/login`, request);
  }

  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${this.baseUrl}/register`, request);
  }

  googleLogin(idToken: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.baseUrl}/google-login`, { idToken });
  }

  refreshToken(): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.baseUrl}/refresh`, {});
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/logout`, {});
  }

  getCurrentUser(): Observable<UserDto> {
    return this.http.get<UserDto>('/gateway/users/me');
  }

  forgotPassword(request: ForgotPasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/forgot-password`, request);
  }

  resetPassword(request: ResetPasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/reset-password`, request);
  }

  changePassword(request: ChangePasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/change-password`, request);
  }

  verifyOtp(email: string, otp: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/verify-otp`, { email, otp });
  }
}
