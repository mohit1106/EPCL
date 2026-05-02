import { Component, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { selectUser } from '../../../../store/auth/auth.selectors';
import { UsersApiService, AdminSummaryDto } from '../../../../core/services/users-api.service';
import { HelpRequestsApiService, HelpRequestDto } from '../../../../core/services/help-requests-api.service';
import { ToastService } from '../../../../shared/services/toast.service';

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
  admins: AdminSummaryDto[] = [];
  isLoadingAdmins = true;

  categories = [
    'Station Assignment', 'Technical Support', 'Billing Issue',
    'Pump Malfunction', 'Inventory Problem', 'Account Issue', 'Other',
  ];

  myRequests: HelpRequestDto[] = [];
  isLoadingRequests = true;

  // Reply form
  replyText: { [reqId: string]: string } = {};
  expandedIds = new Set<string>();

  constructor(
    private store: Store,
    private usersApi: UsersApiService,
    private helpApi: HelpRequestsApiService,
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
    this.usersApi.getAdmins().pipe(
      takeUntil(this.destroy$),
      catchError(() => of([]))
    ).subscribe(admins => {
      this.admins = admins;
      if (this.admins.length > 0 && !this.selectedAdminId) {
        this.selectedAdminId = this.admins[0].id;
      }
      this.isLoadingAdmins = false;
    });
  }

  loadMyRequests(): void {
    this.isLoadingRequests = true;
    this.helpApi.getAll().pipe(
      takeUntil(this.destroy$),
      catchError(() => of([]))
    ).subscribe(requests => {
      this.myRequests = requests;
      this.isLoadingRequests = false;
    });
  }

  submitRequest(): void {
    if (!this.message.trim()) { this.toast.error('Please describe your issue or request.'); return; }
    if (!this.selectedAdminId) { this.toast.error('Please select an admin to contact.'); return; }
    this.isSubmitting = true;

    const admin = this.admins.find(a => a.id === this.selectedAdminId);

    this.helpApi.create({
      targetAdminId: this.selectedAdminId,
      targetAdminName: admin?.fullName || 'Admin',
      category: this.category,
      message: this.message.trim(),
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: (created) => {
        this.myRequests.unshift(created);
        this.message = '';
        this.isSubmitting = false;
        this.toast.success(`Request sent to ${created.targetAdminName}!`);
      },
      error: (err) => {
        this.toast.error(err?.error?.message || err?.error || 'Failed to send request.');
        this.isSubmitting = false;
      },
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
        this.toast.success('Reply sent!');
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
