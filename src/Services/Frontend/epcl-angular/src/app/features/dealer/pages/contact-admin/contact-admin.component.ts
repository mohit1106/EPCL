import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { UsersApiService, UserListDto } from '../../../../core/services/users-api.service';
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
  selector: 'app-dealer-contact-admin',
  templateUrl: './contact-admin.component.html',
  styleUrls: ['./contact-admin.component.scss'],
})
export class DealerContactAdminComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  userId = '';
  userEmail = '';
  userName = '';

  // Form
  category = 'Station Assignment';
  message = '';
  selectedAdminId = '';
  isSubmitting = false;

  // Admin list
  admins: UserListDto[] = [];
  isLoadingAdmins = true;

  categories = [
    'Station Assignment', 'Technical Support', 'Billing Issue',
    'Pump Malfunction', 'Inventory Problem', 'Account Issue', 'Other',
  ];

  myRequests: HelpRequest[] = [];

  // Reply form
  replyText: { [reqId: string]: string } = {};

  constructor(
    private store: Store,
    private usersApi: UsersApiService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.store.select(selectUser).pipe(takeUntil(this.destroy$)).subscribe(u => {
      if (u) {
        this.userId = u.id;
        this.userEmail = u.email;
        this.userName = u.fullName;
        this.loadMyRequests();
      }
    });
    this.loadAdmins();
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  private loadAdmins(): void {
    this.isLoadingAdmins = true;
    this.usersApi.getUsers(1, 50, { role: 'Admin' }).pipe(
      takeUntil(this.destroy$),
      catchError(() => of({ items: [], totalCount: 0, page: 1, pageSize: 50, totalPages: 0 }))
    ).subscribe(result => {
      this.admins = result.items;
      if (this.admins.length > 0 && !this.selectedAdminId) {
        this.selectedAdminId = this.admins[0].id;
      }
      this.isLoadingAdmins = false;
    });
  }

  loadMyRequests(): void {
    const raw = localStorage.getItem('epcl_dealer_requests');
    if (raw) {
      const all: HelpRequest[] = JSON.parse(raw);
      this.myRequests = all
        .filter(r => r.dealerUserId === this.userId)
        .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
    }
  }

  submitRequest(): void {
    if (!this.message.trim()) { this.toast.error('Please describe your issue or request.'); return; }
    if (!this.selectedAdminId) { this.toast.error('Please select an admin to contact.'); return; }
    this.isSubmitting = true;

    const admin = this.admins.find(a => a.id === this.selectedAdminId);

    const request: HelpRequest = {
      id: crypto.randomUUID(),
      dealerUserId: this.userId,
      dealerEmail: this.userEmail,
      dealerName: this.userName,
      targetAdminId: this.selectedAdminId,
      targetAdminName: admin?.fullName || 'Admin',
      message: this.message.trim(),
      category: this.category,
      status: 'Pending',
      createdAt: new Date().toISOString(),
      replies: [],
    };

    const all = JSON.parse(localStorage.getItem('epcl_dealer_requests') || '[]');
    all.push(request);
    localStorage.setItem('epcl_dealer_requests', JSON.stringify(all));

    this.myRequests.unshift(request);
    this.message = '';
    this.isSubmitting = false;
    this.toast.success(`Request sent to ${request.targetAdminName}!`);
  }

  sendReply(req: HelpRequest): void {
    const text = (this.replyText[req.id] || '').trim();
    if (!text) return;

    const reply: HelpReply = { from: 'dealer', fromName: this.userName, message: text, createdAt: new Date().toISOString() };
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
    this.toast.success('Reply sent!');
  }

  toggleExpand(req: HelpRequest): void { req.expanded = !req.expanded; }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Pending': return 'st-pending';
      case 'Resolved': return 'st-resolved';
      case 'Dismissed': return 'st-dismissed';
      default: return 'st-default';
    }
  }

  copyUserId(): void {
    navigator.clipboard.writeText(this.userId).then(() => this.toast.success('User ID copied!'));
  }

  getTimeAgo(dateStr: string): string {
    const diff = Date.now() - new Date(dateStr).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return 'Just now';
    if (mins < 60) return `${mins}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return `${hrs}h ago`;
    return `${Math.floor(hrs / 24)}d ago`;
  }
}
