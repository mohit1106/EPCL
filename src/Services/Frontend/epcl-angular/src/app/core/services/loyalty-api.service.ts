import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface LoyaltyBalanceDto {
  points: number;
  tier: string;
  lifetimePoints: number;
  nextTier: string;
  pointsToNextTier: number;
}

export interface LoyaltyHistoryDto {
  id: string;
  type: string;
  points: number;
  description: string;
  transactionId?: string;
  createdAt: string;
}

export interface ReferralCodeDto {
  code: string;
  totalReferrals: number;
  totalPointsEarned: number;
  createdAt: string;
}

export interface ReferralLeaderDto {
  rank: number;
  customerName: string;
  totalReferrals: number;
  pointsEarned: number;
}

@Injectable({ providedIn: 'root' })
export class LoyaltyApiService {
  private readonly base = '/gateway/loyalty';

  constructor(private http: HttpClient) {}

  getBalance(): Observable<LoyaltyBalanceDto> {
    return this.http.get<LoyaltyBalanceDto>(`${this.base}/balance`);
  }

  getHistory(): Observable<LoyaltyHistoryDto[]> {
    return this.http.get<LoyaltyHistoryDto[]>(`${this.base}/history`);
  }

  redeemPoints(points: number): Observable<{ success: boolean; newBalance: number; redeemedValue: number }> {
    return this.http.post<{ success: boolean; newBalance: number; redeemedValue: number }>(`${this.base}/redeem`, { points });
  }

  getMyReferralCode(): Observable<ReferralCodeDto> {
    return this.http.get<ReferralCodeDto>(`${this.base}/referral/my-code`);
  }

  getLeaderboard(): Observable<ReferralLeaderDto[]> {
    return this.http.get<ReferralLeaderDto[]>(`${this.base}/referral/leaderboard`);
  }
}
