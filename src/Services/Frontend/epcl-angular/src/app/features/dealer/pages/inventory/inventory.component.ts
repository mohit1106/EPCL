import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil } from 'rxjs';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { InventoryApiService, TankDto } from '../../../../core/services/inventory-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

interface DisplayTank {
  id: string;
  name: string;
  tag: string;
  status: string;
  statusClass: string;
  isCng: boolean;
  current: number;
  capacity: number;
  unit: string;
  pct: number;
  color: string;
}

@Component({
  selector: 'app-inventory',
  templateUrl: './inventory.component.html',
  styleUrls: ['./inventory.component.scss'],
})
export class InventoryComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private stationId = '';

  totalCapacity = 0;
  currentLoadout = 0;

  tanks: DisplayTank[] = [];
  tankOptions: string[] = [];

  replenishForm = {
    fuelType: 'MS - Petrol (92 RON)',
    tank: '',
    volume: 0,
    bowserId: '',
  };

  loadingLog = [
    { time: '10:45 AM', op: 'STOCK_IN', qty: '+5,000 L', target: 'TANK-A', status: 'VERIFIED' },
    { time: '08:12 AM', op: 'STOCK_IN', qty: '+1,200 KG', target: 'TANK-C', status: 'VERIFIED' },
    { time: 'Yesterday', op: 'ADJUSTMENT', qty: '-12 L', target: 'TANK-B', status: 'FLAGGED' },
  ];

  isLoading = true;

  constructor(
    private store: Store,
    private inventoryApi: InventoryApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.store.select(selectUser).pipe(takeUntil(this.destroy$)).subscribe(user => {
      if (user) {
        this.stationId = user.profile?.stationId || '';
        this.loadInventory();
      }
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  loadInventory(): void {
    if (!this.stationId) return;
    this.isLoading = true;
    this.inventoryApi.getTanks(this.stationId).pipe(takeUntil(this.destroy$)).subscribe({
      next: (tanks) => {
        this.totalCapacity = 0;
        this.currentLoadout = 0;
        this.tanks = tanks.map((t, i) => {
          this.totalCapacity += t.capacityLitres;
          this.currentLoadout += t.currentStockLitres;
          const isCng = t.fuelTypeName.includes('CNG');
          const pct = Math.round((t.currentStockLitres / t.capacityLitres) * 100);
          
          let statusStr = 'OPTIMAL';
          let statusClass = 'optimal';
          let color = '#10b981';

          if (pct < 15) { statusStr = 'CRITICAL'; statusClass = 'low'; color = '#ef4444'; }
          else if (pct < 30) { statusStr = 'WARNING'; statusClass = 'warn'; color = '#f59e0b'; }

          if (t.status !== 'Active') {
            statusStr = 'OFFLINE';
            statusClass = '';
            color = '#6b7280';
          }

          return {
            id: t.id,
            name: `TANK-${String.fromCharCode(65 + i)}`,
            tag: t.fuelTypeName,
            status: statusStr,
            statusClass,
            isCng,
            current: t.currentStockLitres,
            capacity: t.capacityLitres,
            unit: isCng ? 'KG' : 'L',
            pct,
            color,
          };
        });

        this.tankOptions = this.tanks.map(t => t.name);
        if (this.tankOptions.length > 0) {
          this.replenishForm.tank = this.tankOptions[0];
        }

        this.isLoading = false;
      },
      error: () => { this.isLoading = false; },
    });
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'verified': return 'status-success';
      case 'flagged': return 'status-flagged';
      case 'pending': return 'status-pending';
      default: return 'status-default';
    }
  }
}
