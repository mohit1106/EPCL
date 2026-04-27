import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil, interval, startWith, switchMap } from 'rxjs';
import { HealthApiService, ServiceHealthDto } from '../../../../core/services/health-api.service';

@Component({
  selector: 'app-system-health',
  templateUrl: './system-health.component.html',
  styleUrls: ['./system-health.component.scss'],
})
export class SystemHealthComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  services: ServiceHealthDto[] = [];
  isLoading = true;
  clusterStatus = 'ACTIVE';
  unhealthyCount = 0;
  
  kpis = [
    { label: 'GLOBAL LATENCY', value: '42', unit: 'ms', topRight: 'AVG', icon: 'zap', color: '#10b981' },
    { label: 'API ERROR RATE', value: '0.01', unit: '%', topRight: '24H', icon: 'alert-triangle', color: '#f59e0b' }
  ];
  
  queueBars = [20, 35, 40, 60, 55, 75, 65, 80, 45, 30];
  
  systemEvents = [
    { level: 'INFO', levelClass: 'info', time: '10:45 AM', message: 'Node Alpha resynced with primary database' },
    { level: 'WARN', levelClass: 'warn', time: '10:32 AM', message: 'High memory usage detected on Auth Service' },
    { level: 'ERROR', levelClass: 'error', time: '09:12 AM', message: 'Payment gateway timeout for Gateway-B' }
  ];

  regions = [
    { name: 'US-EAST-1', latency: '24ms', load: 45 },
    { name: 'ME-SOUTH-1', latency: '42ms', load: 82 },
    { name: 'AP-SOUTHEAST-1', latency: '18ms', load: 30 }
  ];

  constructor(private healthApi: HealthApiService) {}

  ngOnInit(): void {
    // Poll every 30 seconds
    interval(30000).pipe(
      startWith(0),
      takeUntil(this.destroy$),
      switchMap(() => this.healthApi.checkAllServices())
    ).subscribe({
      next: (services) => {
        this.services = services;
        this.unhealthyCount = services.filter(s => s.status !== 'Healthy').length;
        this.clusterStatus = this.unhealthyCount > 0 ? 'DEGRADED' : 'ACTIVE';
        
        // Update generic KPI
        this.kpis[0].value = (services.reduce((acc, s) => acc + s.responseTimeMs, 0) / (services.length || 1)).toFixed(0);
        this.kpis[1].value = this.unhealthyCount.toString();

        this.isLoading = false;
      },
      error: () => { this.isLoading = false; this.clusterStatus = 'OFFLINE'; },
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  getStatusColor(status: string): string {
    return status === 'Healthy' ? '#10b981' : '#ef4444';
  }

  getStatusIcon(status: string): string {
    return status === 'Healthy' ? 'check-circle' : 'alert-circle';
  }

  getResponseTimeColor(ms: number): string {
    if (ms < 100) return '#10b981';
    if (ms < 500) return '#f59e0b';
    return '#ef4444';
  }

  refreshNow(): void {
    this.isLoading = true;
    this.healthApi.checkAllServices().pipe(takeUntil(this.destroy$)).subscribe({
      next: (services) => {
        this.services = services;
        this.unhealthyCount = services.filter(s => s.status !== 'Healthy').length;
        this.clusterStatus = this.unhealthyCount > 0 ? 'DEGRADED' : 'ACTIVE';
        this.kpis[0].value = (services.reduce((acc, s) => acc + s.responseTimeMs, 0) / (services.length || 1)).toFixed(0);
        this.kpis[1].value = this.unhealthyCount.toString();
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; this.clusterStatus = 'OFFLINE'; },
    });
  }
}
