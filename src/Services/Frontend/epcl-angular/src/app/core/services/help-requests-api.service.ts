import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface HelpRequestReplyDto {
  id: string;
  fromRole: string;
  fromName: string;
  fromUserId: string;
  message: string;
  createdAt: string;
}

export interface HelpRequestDto {
  id: string;
  dealerUserId: string;
  dealerEmail: string;
  dealerName: string;
  targetAdminId: string;
  targetAdminName: string;
  category: string;
  message: string;
  status: string;
  createdAt: string;
  resolvedAt: string | null;
  replies: HelpRequestReplyDto[];
}

export interface CreateHelpRequestDto {
  targetAdminId: string;
  targetAdminName: string;
  category: string;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class HelpRequestsApiService {
  private readonly base = '/gateway/help-requests';

  constructor(private http: HttpClient) {}

  getAll(status?: string): Observable<HelpRequestDto[]> {
    let params = new HttpParams();
    if (status && status !== 'All') params = params.set('status', status);
    return this.http.get<HelpRequestDto[]>(this.base, { params });
  }

  getById(id: string): Observable<HelpRequestDto> {
    return this.http.get<HelpRequestDto>(`${this.base}/${id}`);
  }

  create(dto: CreateHelpRequestDto): Observable<HelpRequestDto> {
    return this.http.post<HelpRequestDto>(this.base, dto);
  }

  updateStatus(id: string, status: string): Observable<HelpRequestDto> {
    return this.http.put<HelpRequestDto>(`${this.base}/${id}/status`, { status });
  }

  addReply(id: string, message: string): Observable<HelpRequestReplyDto> {
    return this.http.post<HelpRequestReplyDto>(`${this.base}/${id}/replies`, { message });
  }
}
