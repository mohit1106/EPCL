import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil, of, interval } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { StationsApiService, StationDto } from '../../../../core/services/stations-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

interface HelpReply {
  from: string;
  fromName: string;
  message: string;
  createdAt: string;
}

interface HelpRequest {
  id: string;
  dealerUserId: string;
  dealerEmail: string;
  dealerName: string;
  targetAdminId: string;
  targetAdminName: string;
  message: string;
  category: string;
  status: string;
  createdAt: string;
  replies: HelpReply[];
  expanded?: boolean;
}

@Component({
  selector: 'app-admin-help-requests',
  templateUrl: './help-requests.component.html',
  styleUrls: ['./help-requests.component.scss'],
})
export class AdminHelpRequestsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  adminId = '';
  adminName = '';

  requests: HelpRequest[] = [];
  filteredRequests: HelpRequest[] = [];
  filterStatus = 'All';
  filterCategory = 'All';

  pendingCount = 0;
  resolvedCount = 0;
  dismissedCount = 0;

  // Assignment modal
  showAssignModal = false;
  currentRequest: HelpRequest | null = null;
  unassignedStations: StationDto[] = [];
  selectedStationId = '';
  isAssigning = false;

  // Reply
  replyText: { [reqId: string]: string } = {};

  categories = ['All', 'Station Assignment', 'Technical Support', 'Billing Issue', 'Pump Malfunction', 'Inventory Problem', 'Account Issue', 'Other'];

  constructor(
    private store: Store,
    private stationsApi: StationsApiService,
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
    const raw = localStorage.getItem('epcl_dealer_requests');
    const all: HelpRequest[] = raw ? JSON.parse(raw) : [];
    // Show only requests targeted to THIS admin (or older ones without targetAdminId for backward compat)
    this.requests = all
      .filter(r => !r.targetAdminId || r.targetAdminId === this.adminId)
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
    this.pendingCount = this.requests.filter(r => r.status === 'Pending').length;
    this.resolvedCount = this.requests.filter(r => r.status === 'Resolved').length;
    this.dismissedCount = this.requests.filter(r => r.status === 'Dismissed').length;
    this.applyFilters();
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

  resolveRequest(req: HelpRequest): void {
    this.updateRequestStatus(req, 'Resolved');
    this.toast.success('Request marked as resolved.');
  }

  dismissRequest(req: HelpRequest): void {
    this.updateRequestStatus(req, 'Dismissed');
    this.toast.success('Request dismissed.');
  }

  reopenRequest(req: HelpRequest): void {
    this.updateRequestStatus(req, 'Pending');
    this.toast.success('Request reopened.');
  }

  private updateRequestStatus(req: HelpRequest, status: string): void {
    const all: HelpRequest[] = JSON.parse(localStorage.getItem('epcl_dealer_requests') || '[]');
    const idx = all.findIndex(r => r.id === req.id);
    if (idx >= 0) all[idx].status = status;
    localStorage.setItem('epcl_dealer_requests', JSON.stringify(all));
    this.loadRequests();
  }

  // Reply to a request
  sendReply(req: HelpRequest): void {
    const text = (this.replyText[req.id] || '').trim();
    if (!text) return;

    const reply: HelpReply = { from: 'admin', fromName: this.adminName, message: text, createdAt: new Date().toISOString() };
    const all: HelpRequest[] = JSON.parse(localStorage.getItem('epcl_dealer_requests') || '[]');
    const idx = all.findIndex(r => r.id === req.id);
    if (idx >= 0) {
      if (!all[idx].replies) all[idx].replies = [];
      all[idx].replies.push(reply);
      localStorage.setItem('epcl_dealer_requests', JSON.stringify(all));
    }
    req.replies = req.replies || [];
    req.replies.push(reply);
    this.replyText[req.id] = '';
    this.toast.success('Reply sent to dealer!');
  }

  toggleExpand(req: HelpRequest): void { req.expanded = !req.expanded; }

  openAssignStation(req: HelpRequest): void {
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
        const reply: HelpReply = {
          from: 'admin', fromName: this.adminName,
          message: `A station has been assigned to you. Please refresh your dashboard to see the assigned station.`,
          createdAt: new Date().toISOString()
        };
        const all: HelpRequest[] = JSON.parse(localStorage.getItem('epcl_dealer_requests') || '[]');
        const idx = all.findIndex(r => r.id === this.currentRequest!.id);
        if (idx >= 0) {
          if (!all[idx].replies) all[idx].replies = [];
          all[idx].replies.push(reply);
          all[idx].status = 'Resolved';
          localStorage.setItem('epcl_dealer_requests', JSON.stringify(all));
        }
        this.isAssigning = false;
        this.showAssignModal = false;
        this.loadUnassignedStations();
        this.loadRequests();
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
