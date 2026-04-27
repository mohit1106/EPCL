import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil } from 'rxjs';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { InventoryApiService, TankDto, ReplenishmentStatusDto } from '../../../../core/services/inventory-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-replenishment',
  templateUrl: './replenishment.component.html',
  styleUrls: ['./replenishment.component.scss'],
})
export class ReplenishmentComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private stationId = '';

  // Form
  requestId = 'REQ-' + Math.random().toString(36).substring(2, 8).toUpperCase();
  fuelGrades = ['Petrol 95', 'Diesel', 'CNG', 'Premium Petrol'];
  selectedGrade = 'Petrol 95';
  requestVolume = 20000;
  targetTank = 'TANK-A (Primary)';
  requestWindow = 'Next 24 hours';
  priority = 'STANDARD';

  // Active Deployment
  deploymentStatus = 'PENDING';
  eta = '~3h 45m';
  driver = { name: 'Rahul Sharma' };

  deploymentSteps = [
    { label: 'Request Approved', detail: 'Authorized by Station Manager', done: true, active: false, date: '' },
    { label: 'Tanker Assigned', detail: 'Vehicle allocated from fleet', done: false, active: true, date: '' },
    { label: 'In Transit', detail: 'En route to station', done: false, active: false, date: '' },
    { label: 'Offloading', detail: 'Fuel being transferred', done: false, active: false, date: '' },
    { label: 'Complete', detail: 'Stock updated in system', done: false, active: false, date: '' },
  ];

  // Budget Stats
  budgetStats = [
    { label: 'MONTHLY PROCUREMENT', value: '₹2.4M', trend: '+8.2%', trendClass: 'up' },
    { label: 'AVG DELIVERY TIME', value: '4.2 hrs', trend: '-0.5h', trendClass: 'up' },
    { label: 'FULFILLMENT RATE', value: '98.1%', trend: '+1.2%', trendClass: 'up' },
  ];

  // History
  history: { id: string; grade: string; volume: string; status: string; completion: string }[] = [];

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
        this.loadData();
      }
    });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  private loadData(): void {
    if (!this.stationId) { this.isLoading = false; return; }
    this.isLoading = true;
    this.inventoryApi.getReplenishmentRequests(this.stationId).pipe(takeUntil(this.destroy$)).subscribe({
      next: (reqs) => {
        this.history = reqs.map(r => ({
          id: r.id.substring(0, 8),
          grade: r.fuelTypeName,
          volume: `${r.requestedQuantityLitres.toLocaleString()} L`,
          status: r.status,
          completion: r.reviewedAt ? new Date(r.reviewedAt).toLocaleDateString() : '—',
        }));
        // Update deployment status from latest active request
        const active = reqs.find(r => r.status === 'Approved' || r.status === 'Pending');
        if (active) {
          this.deploymentStatus = active.status === 'Approved' ? 'IN-TRANSIT' : 'PENDING';
        }
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; },
    });
  }

  submitRequest(): void {
    if (this.requestVolume < 1000) {
      this.toast.error('Minimum request volume is 1,000 litres.');
      return;
    }
    // Find the matching tank ID for selectedGrade
    this.inventoryApi.getTanks(this.stationId).pipe(takeUntil(this.destroy$)).subscribe({
      next: (tanks) => {
        const tank = tanks.find(t => t.fuelTypeName.toLowerCase().includes(this.selectedGrade.toLowerCase()));
        if (!tank) {
          this.toast.error('No matching tank found for the selected fuel grade.');
          return;
        }
        this.inventoryApi.createReplenishmentRequest({
          tankId: tank.id,
          requestedQuantityLitres: this.requestVolume,
          urgencyLevel: this.priority === 'URGENT' ? 'Critical' : 'Normal',
          notes: `Grade: ${this.selectedGrade}, Window: ${this.requestWindow}`,
        }).pipe(takeUntil(this.destroy$)).subscribe({
          next: () => {
            this.toast.success('Replenishment request submitted.');
            this.requestId = 'REQ-' + Math.random().toString(36).substring(2, 8).toUpperCase();
            this.loadData();
          },
          error: () => this.toast.error('Failed to submit request.'),
        });
      },
    });
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'approved': case 'completed': return 'status-success';
      case 'pending': return 'status-pending';
      case 'rejected': return 'status-flagged';
      default: return 'status-default';
    }
  }
}
