import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil, of, interval } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { StationsApiService, StationDto } from '../../../../core/services/stations-api.service';
import { HelpRequestsApiService, HelpRequestDto } from '../../../../core/services/help-requests-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-admin-help-requests',
  templateUrl: './help-requests.component.html',
  styleUrls: ['./help-requests.component.scss'],
})
export class AdminHelpRequestsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  adminId = '';
  adminName = '';

  requests: HelpRequestDto[] = [];
  filteredRequests: HelpRequestDto[] = [];
  filterStatus = 'All';
  filterCategory = 'All';

  pendingCount = 0;
  resolvedCount = 0;
  dismissedCount = 0;

  // Assignment modal
  showAssignModal = false;
  currentRequest: HelpRequestDto | null = null;
  unassignedStations: StationDto[] = [];
  selectedStationId = '';
  isAssigning = false;

  // Reply
  replyText: { [reqId: string]: string } = {};
  expandedIds = new Set<string>();

  categories = ['All', 'Station Assignment', 'Technical Support', 'Billing Issue', 'Pump Malfunction', 'Inventory Problem', 'Account Issue', 'Other'];

  constructor(
    private store: Store,
    private stationsApi: StationsApiService,
    private helpApi: HelpRequestsApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.store.select(selectUser).pipe(takeUntil(this.destroy$)).subscribe(u => {
      if (u) {
        this.adminId = u.id;
        this.adminName = u.fullName;
        this.loadRequests();
      }
    });
    this.loadUnassignedStations();

    // Poll for new requests every 15 seconds
    interval(15000).pipe(takeUntil(this.destroy$)).subscribe(() => this.loadRequests());
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  loadRequests(): void {
    this.helpApi.getAll().pipe(
      takeUntil(this.destroy$),
      catchError(() => of([]))
    ).subscribe(requests => {
      this.requests = requests;
      this.pendingCount = this.requests.filter(r => r.status === 'Pending').length;
      this.resolvedCount = this.requests.filter(r => r.status === 'Resolved').length;
      this.dismissedCount = this.requests.filter(r => r.status === 'Dismissed').length;
      this.applyFilters();
    });
  }

  applyFilters(): void {
    this.filteredRequests = this.requests.filter(r => {
      if (this.filterStatus !== 'All' && r.status !== this.filterStatus) return false;
      if (this.filterCategory !== 'All' && r.category !== this.filterCategory) return false;
      return true;
    });
  }

  loadUnassignedStations(): void {
    this.stationsApi.getUnassignedStations().pipe(takeUntil(this.destroy$), catchError(() => of([]))).subscribe(stations => {
      this.unassignedStations = stations;
    });
  }

  resolveRequest(req: HelpRequestDto): void {
    this.helpApi.updateStatus(req.id, 'Resolved').pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('Request marked as resolved.'); this.loadRequests(); },
      error: () => this.toast.error('Failed to update status.'),
    });
  }

  dismissRequest(req: HelpRequestDto): void {
    this.helpApi.updateStatus(req.id, 'Dismissed').pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('Request dismissed.'); this.loadRequests(); },
      error: () => this.toast.error('Failed to update status.'),
    });
  }

  reopenRequest(req: HelpRequestDto): void {
    this.helpApi.updateStatus(req.id, 'Pending').pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('Request reopened.'); this.loadRequests(); },
      error: () => this.toast.error('Failed to update status.'),
    });
  }

  sendReply(req: HelpRequestDto): void {
    const text = (this.replyText[req.id] || '').trim();
    if (!text) return;

    this.helpApi.addReply(req.id, text).pipe(takeUntil(this.destroy$)).subscribe({
      next: (reply) => {
        req.replies = req.replies || [];
        req.replies.push(reply);
        this.replyText[req.id] = '';
        this.toast.success('Reply sent to dealer!');
      },
      error: () => this.toast.error('Failed to send reply.'),
    });
  }

  toggleExpand(req: HelpRequestDto): void {
    if (this.expandedIds.has(req.id)) {
      this.expandedIds.delete(req.id);
    } else {
      this.expandedIds.add(req.id);
    }
  }

  isExpanded(req: HelpRequestDto): boolean {
    return this.expandedIds.has(req.id);
  }

  openAssignStation(req: HelpRequestDto): void {
    this.currentRequest = req;
    this.selectedStationId = '';
    this.showAssignModal = true;
  }

  closeAssignModal(): void { this.showAssignModal = false; this.currentRequest = null; }

  assignStation(): void {
    if (!this.selectedStationId || !this.currentRequest) {
      this.toast.error('Please select a station.');
      return;
    }
    this.isAssigning = true;
    this.stationsApi.assignDealerToStation(this.selectedStationId, this.currentRequest.dealerUserId).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: () => {
        this.toast.success(`Station assigned to ${this.currentRequest!.dealerName || 'dealer'} successfully!`);
        // Auto-reply about assignment
        this.helpApi.addReply(this.currentRequest!.id, 'A station has been assigned to you. Please refresh your dashboard.').pipe(
          takeUntil(this.destroy$)
        ).subscribe();
        // Mark as resolved
        this.helpApi.updateStatus(this.currentRequest!.id, 'Resolved').pipe(
          takeUntil(this.destroy$)
        ).subscribe(() => this.loadRequests());

        this.isAssigning = false;
        this.showAssignModal = false;
        this.loadUnassignedStations();
      },
      error: (err) => {
        this.toast.error(err?.error?.message || 'Failed to assign station.');
        this.isAssigning = false;
      },
    });
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Pending': return 'st-pending';
      case 'Resolved': return 'st-resolved';
      case 'Dismissed': return 'st-dismissed';
      default: return 'st-default';
    }
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleString('en-IN', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }
}
