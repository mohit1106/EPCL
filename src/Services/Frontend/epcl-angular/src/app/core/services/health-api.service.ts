import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, of, catchError, map } from 'rxjs';

export interface ServiceHealthDto {
  name: string;
  status: 'Healthy' | 'Unhealthy' | 'Unknown';
  responseTimeMs: number;
}

@Injectable({ providedIn: 'root' })
export class HealthApiService {
  private readonly services = [
    { name: 'Gateway', port: 5000 },
    { name: 'Identity', port: 5217 },
    { name: 'Stations', port: 5143 },
    { name: 'Inventory', port: 5134 },
    { name: 'Sales', port: 5167 },
    { name: 'Fraud Detection', port: 5237 },
    { name: 'Notifications', port: 5037 },
    { name: 'Reporting', port: 5062 },
    { name: 'Audit', port: 5268 },
    { name: 'Loyalty', port: 5192 },
  ];

  constructor(private http: HttpClient) {}

  checkAllServices(): Observable<ServiceHealthDto[]> {
    const checks = this.services.map((svc) => {
      const url = `http://localhost:${svc.port}/health`;
      const start = performance.now();
      return this.http.get(url, { responseType: 'text' }).pipe(
        map(() => ({
          name: svc.name,
          status: 'Healthy' as const,
          responseTimeMs: Math.round(performance.now() - start),
        })),
        catchError(() =>
          of({
            name: svc.name,
            status: 'Unhealthy' as const,
            responseTimeMs: Math.round(performance.now() - start),
          })
        )
      );
    });
    return forkJoin(checks);
  }
}
