import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ChatRequest {
  message: string;
  sessionId?: string;
}

export interface ChatResponse {
  answer: string;
  sessionId: string;
  tableData: Record<string, unknown>[] | null;
  columnNames: string[] | null;
  hasChartData: boolean;
  chartType: 'bar' | 'line' | 'pie' | null;
  rowsReturned: number;
  generatedSql: string | null;
}

export interface SuggestedQuestions {
  questions: string[];
}

export interface ConversationMessage {
  id: string;
  userId: string;
  sessionId: string;
  role: 'user' | 'assistant';
  content: string;
  generatedSql?: string;
  rowsReturned?: number;
  executionMs?: number;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class AiApiService {
  private readonly base = '/gateway/ai';

  constructor(private http: HttpClient) {}

  chat(request: ChatRequest): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(`${this.base}/chat`, request);
  }

  getSuggestions(): Observable<SuggestedQuestions> {
    return this.http.get<SuggestedQuestions>(`${this.base}/suggestions`);
  }

  getHistory(sessionId?: string): Observable<ConversationMessage[]> {
    const params = sessionId ? `?sessionId=${sessionId}` : '';
    return this.http.get<ConversationMessage[]>(`${this.base}/history${params}`);
  }

  clearHistory(sessionId?: string): Observable<void> {
    const params = sessionId ? `?sessionId=${sessionId}` : '';
    return this.http.delete<void>(`${this.base}/history${params}`);
  }
}
