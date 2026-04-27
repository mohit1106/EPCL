import { Injectable, NgZone } from '@angular/core';
import { Subject, BehaviorSubject } from 'rxjs';
import { HubConnection, HubConnectionBuilder, LogLevel, HubConnectionState } from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { ToastService } from '../../shared/services/toast.service';

export interface FraudAlertDto {
  id: string;
  transactionId: string;
  stationId: string;
  ruleTriggered: string;
  severity: string;
  description: string;
  createdAt: string;
}

export interface StockCriticalDto {
  tankId: string;
  stationId: string;
  fuelType: string;
  currentStockLitres: number;
  capacityLitres: number;
  percentage: number;
}

export interface PriceUpdateDto {
  fuelTypeId: string;
  fuelTypeName: string;
  newPrice: number;
  effectiveFrom: string;
}

export interface ReplenishmentDto {
  requestId: string;
  stationId: string;
  tankId: string;
  fuelType: string;
  requestedQuantity: number;
  urgencyLevel: string;
}

/**
 * SignalR real-time communication service.
 *
 * Architecture:
 * - AdminHub: connected by Admin/SuperAdmin users → receives fraud alerts, stock criticals, replenishment requests
 * - DealerHub: connected by Dealer users → receives stock criticals, price updates
 *
 * The hubs are hosted by ReportingService on port 5007.
 * In development, Angular proxy routes /hubs/* → localhost:5007 (bypasses Ocelot since Ocelot cannot proxy WebSockets).
 * In production, nginx/reverse proxy must route /hubs/* to ReportingService directly.
 *
 * Reconnection: exponential backoff at 1s, 2s, 4s, 8s, 15s, 30s intervals.
 */
@Injectable({ providedIn: 'root' })
export class SignalRService {
  private adminHub: HubConnection | null = null;
  private dealerHub: HubConnection | null = null;

  /** Emits new fraud alerts received from AdminHub */
  readonly newFraudAlert$ = new Subject<FraudAlertDto>();
  /** Emits stock critical events from AdminHub or DealerHub */
  readonly stockLevelCritical$ = new Subject<StockCriticalDto>();
  /** Emits fuel price update events from DealerHub */
  readonly priceUpdated$ = new Subject<PriceUpdateDto>();
  /** Emits replenishment request events from AdminHub */
  readonly replenishmentRequested$ = new Subject<ReplenishmentDto>();
  /** Current connection status — drives the UI status indicator in the sidebar */
  readonly connectionStatus$ = new BehaviorSubject<'connected' | 'disconnected' | 'reconnecting'>('disconnected');

  constructor(
    private toast: ToastService,
    private ngZone: NgZone
  ) {}

  /**
   * Connect to the AdminHub. Called after login when user role is Admin or SuperAdmin.
   * Uses JWT access_token passed as query parameter for hub authentication.
   */
  async connectAdmin(token: string): Promise<void> {
    if (this.adminHub?.state === HubConnectionState.Connected) return;

    // Tear down any stale connection
    if (this.adminHub) {
      await this.adminHub.stop().catch(() => {});
      this.adminHub = null;
    }

    const hubUrl = `${environment.signalrUrl}/hubs/admin`;

    this.adminHub = new HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => token })
      .withAutomaticReconnect([1000, 2000, 4000, 8000, 15000, 30000])
      .configureLogging(environment.production ? LogLevel.Error : LogLevel.Warning)
      .build();

    // --- Event handlers (run inside NgZone for change detection) ---

    this.adminHub.on('NewFraudAlert', (alert: FraudAlertDto) => {
      this.ngZone.run(() => {
        this.newFraudAlert$.next(alert);
        this.toast.warning(`New ${alert.severity} fraud alert: ${alert.ruleTriggered}`);
      });
    });

    this.adminHub.on('StockLevelCritical', (data: StockCriticalDto) => {
      this.ngZone.run(() => {
        this.stockLevelCritical$.next(data);
        this.toast.warning(`Critical stock: ${data.fuelType} at ${data.percentage}%`);
      });
    });

    this.adminHub.on('ReplenishmentRequested', (data: ReplenishmentDto) => {
      this.ngZone.run(() => {
        this.replenishmentRequested$.next(data);
        this.toast.info(`New replenishment request: ${data.fuelType}`);
      });
    });

    this.adminHub.on('FuelPriceUpdated', (data: PriceUpdateDto) => {
      this.ngZone.run(() => {
        this.priceUpdated$.next(data);
      });
    });

    // --- Connection lifecycle ---

    this.adminHub.onreconnecting((error) => {
      this.ngZone.run(() => {
        this.connectionStatus$.next('reconnecting');
        if (error) {
          this.toast.info('Real-time connection lost. Reconnecting...');
        }
      });
    });

    this.adminHub.onreconnected(() => {
      this.ngZone.run(() => {
        this.connectionStatus$.next('connected');
        this.toast.success('Real-time connection restored.');
      });
    });

    this.adminHub.onclose(() => {
      this.ngZone.run(() => this.connectionStatus$.next('disconnected'));
    });

    // --- Start connection ---
    await this.startConnection(this.adminHub, 'AdminHub');
  }

  /**
   * Connect to the DealerHub. Called after login when user role is Dealer.
   * The hub automatically groups the dealer by their StationId claim.
   */
  async connectDealer(token: string): Promise<void> {
    if (this.dealerHub?.state === HubConnectionState.Connected) return;

    if (this.dealerHub) {
      await this.dealerHub.stop().catch(() => {});
      this.dealerHub = null;
    }

    const hubUrl = `${environment.signalrUrl}/hubs/dealer`;

    this.dealerHub = new HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => token })
      .withAutomaticReconnect([1000, 2000, 4000, 8000, 15000, 30000])
      .configureLogging(environment.production ? LogLevel.Error : LogLevel.Warning)
      .build();

    this.dealerHub.on('StockLevelCritical', (data: StockCriticalDto) => {
      this.ngZone.run(() => {
        this.stockLevelCritical$.next(data);
        this.toast.warning(`Critical stock: ${data.fuelType} at ${data.percentage}%`);
      });
    });

    this.dealerHub.on('FuelPriceUpdated', (data: PriceUpdateDto) => {
      this.ngZone.run(() => {
        this.priceUpdated$.next(data);
        this.toast.info(`Price updated: ${data.fuelTypeName} → ₹${data.newPrice}/L`);
      });
    });

    this.dealerHub.onreconnecting((error) => {
      this.ngZone.run(() => {
        this.connectionStatus$.next('reconnecting');
        if (error) {
          this.toast.info('Real-time connection lost. Reconnecting...');
        }
      });
    });

    this.dealerHub.onreconnected(() => {
      this.ngZone.run(() => {
        this.connectionStatus$.next('connected');
        this.toast.success('Real-time connection restored.');
      });
    });

    this.dealerHub.onclose(() => {
      this.ngZone.run(() => this.connectionStatus$.next('disconnected'));
    });

    await this.startConnection(this.dealerHub, 'DealerHub');
  }

  /** Gracefully disconnect all hubs. Called on logout. */
  disconnect(): void {
    this.adminHub?.stop().catch(() => {});
    this.dealerHub?.stop().catch(() => {});
    this.adminHub = null;
    this.dealerHub = null;
    this.connectionStatus$.next('disconnected');
  }

  /**
   * Starts a hub connection with error handling.
   * On failure, sets status to disconnected — the automatic reconnect
   * will handle retry since withAutomaticReconnect is configured.
   */
  private async startConnection(hub: HubConnection, hubName: string): Promise<void> {
    try {
      await hub.start();
      this.connectionStatus$.next('connected');
    } catch {
      // Connection failed — withAutomaticReconnect handles retries.
      // No console.error — the toast and status indicator inform the user.
      this.connectionStatus$.next('disconnected');
    }
  }
}
