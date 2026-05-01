import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { WalletPaymentRequestDto } from './sales-api.service';

export interface WalletBalanceDto {
  balance: number;
  lastUpdated: string;
}

export interface WalletTransactionDto {
  id: string;
  type: string;
  amount: number;
  description: string;
  referenceId?: string;
  createdAt: string;
}

export interface CreateOrderResponse {
  orderId: string;
  amount: number;
  currency: string;
  keyId: string;
}

export interface VerifyPaymentRequest {
  orderId: string;
  paymentId: string;
  signature: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentsApiService {
  private readonly base = '/gateway/payments';

  constructor(private http: HttpClient) {}

  getWalletBalance(): Observable<WalletBalanceDto> {
    return this.http.get<WalletBalanceDto>(`${this.base}/wallet/balance`);
  }

  getWalletHistory(page = 1, pageSize = 20): Observable<WalletTransactionDto[]> {
    return this.http.get<WalletTransactionDto[]>(`${this.base}/wallet/history`, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize),
    });
  }

  createWalletOrder(amount: number): Observable<CreateOrderResponse> {
    return this.http.post<CreateOrderResponse>(`${this.base}/wallet/create-order`, { amount });
  }

  verifyPayment(request: VerifyPaymentRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.base}/wallet/verify`, request);
  }

  // Wallet Payment Requests (customer approval flow)
  getPendingPaymentRequests(): Observable<WalletPaymentRequestDto[]> {
    return this.http.get<WalletPaymentRequestDto[]>(`${this.base}/wallet/pending-requests`);
  }

  approvePaymentRequest(requestId: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.base}/wallet/approve/${requestId}`, {});
  }

  rejectPaymentRequest(requestId: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.base}/wallet/reject/${requestId}`, {});
  }
}
