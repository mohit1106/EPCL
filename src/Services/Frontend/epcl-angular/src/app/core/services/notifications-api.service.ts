import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface NotificationPrefsDto {
  emailEnabled: boolean;
  smsEnabled: boolean;
  pushEnabled: boolean;
  priceAlerts: boolean;
  transactionReceipts: boolean;
  securityAlerts: boolean;
  loyaltyRewards: boolean;
}

export interface PriceAlertDto {
  id: string;
  fuelTypeId: string;
  fuelTypeName: string;
  threshold: number;
  channel: string;
  isActive: boolean;
  createdAt: string;
}

export interface InAppNotificationDto {
  id: string;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationsApiService {
  private readonly base = '/gateway/notifications';

  constructor(private http: HttpClient) {}

  getPreferences(): Observable<NotificationPrefsDto> {
    return this.http.get<NotificationPrefsDto>(`${this.base}/preferences`);
  }

  updatePreferences(prefs: Partial<NotificationPrefsDto>): Observable<void> {
    return this.http.put<void>(`${this.base}/preferences`, prefs);
  }

  getInAppNotifications(): Observable<InAppNotificationDto[]> {
    return this.http.get<InAppNotificationDto[]>(`${this.base}/in-app`);
  }

  markAsRead(id: string): Observable<void> {
    return this.http.put<void>(`${this.base}/in-app/${id}/read`, {});
  }

  getPriceAlerts(): Observable<PriceAlertDto[]> {
    return this.http.get<PriceAlertDto[]>(`${this.base}/price-alerts`);
  }

  createPriceAlert(alert: { fuelTypeId: string; threshold: number; channel: string }): Observable<PriceAlertDto> {
    return this.http.post<PriceAlertDto>(`${this.base}/price-alerts`, alert);
  }

  deletePriceAlert(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/price-alerts/${id}`);
  }
}
