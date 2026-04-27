import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { UsersApiService, UserListDto, UserFilters } from '../../../../core/services/users-api.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { FraudApiService } from '../../../../core/services/fraud-api.service';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';

interface DisplayUser {
  id: string;
  initials: string;
  name: string;
  uid: string;
  roleClass: string;
  role: string;
  statusClass: string;
  status: string;
  lastTelemetry: string;
  rawDto: UserListDto;
}

@Component({
  selector: 'app-admin-users',
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.scss'],
})
export class AdminUsersComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  users: DisplayUser[] = [];
  rawUsers: UserListDto[] = [];
  totalCount = 0;
  totalIdentities = 842;
  page = 1;
  pageSize = 20;
  isLoading = true;
  filters: UserFilters = {};
  selectedUser: UserListDto | null = null;
  searchTerm = '';
  actionMenuOpenForId: string | null = null;

  roles = ['Customer', 'Dealer', 'Admin', 'SuperAdmin'];

  kpis = [
    { label: 'ACTIVE USERS', value: '4,281', trendClass: 'up', trend: '↗ +12%' },
    { label: 'NEW REGISTRATIONS', value: '142', trendClass: 'up', trend: '↗ +5%' },
    { label: 'FLAGGED ACCOUNTS', value: '8', trendClass: 'down', trend: '↘ -2%' }
  ];
  securityPulse = '98.5%';
  nextAudit = '24H, 12M';

  systemLogs: any[] = [];

  // Chart configuration for Role Distribution
  public pieChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { position: 'right', labels: { color: '#94a3b8', font: { family: "'JetBrains Mono', monospace", size: 11 } } },
      tooltip: { backgroundColor: 'rgba(15, 23, 42, 0.9)', titleColor: '#fff', bodyColor: '#cbd5e1' }
    }
  };
  public pieChartData: ChartData<'doughnut', number[], string | string[]> = {
    labels: ['Customer', 'Dealer', 'Admin', 'Logistics'],
    datasets: [{
      data: [0, 0, 0, 0],
      backgroundColor: ['#3b82f6', '#f59e0b', '#6366f1', '#10b981'],
      borderWidth: 0,
      hoverOffset: 4
    }]
  };
  public pieChartType: ChartType = 'doughnut';

  constructor(
    private usersApi: UsersApiService, 
    private toast: ToastService,
    private fraudApi: FraudApiService
  ) {}

  ngOnInit(): void { 
    this.loadUsers(); 
    this.loadSystemLogs();
  }
  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  loadUsers(): void {
    this.isLoading = true;
    this.usersApi.getUsers(this.page, this.pageSize, this.filters).pipe(takeUntil(this.destroy$)).subscribe({
      next: (result) => {
        this.rawUsers = result.items;
        this.users = result.items.map(u => ({
          id: u.id,
          initials: u.fullName.split(' ').map(n => n.charAt(0)).slice(0, 2).join(''),
          name: u.fullName,
          uid: u.id.substring(0, 8),
          roleClass: this.getRoleBadgeClass(u.role),
          role: u.role || 'Unknown',
          statusClass: !u.isActive ? 'locked' : (u.isEmailVerified ? 'online' : 'offline'),
          status: !u.isActive ? 'LOCKED' : (u.isEmailVerified ? 'ACTIVE' : 'PENDING'),
          lastTelemetry: u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleTimeString() : 'N/A',
          rawDto: u
        }));
        this.totalCount = result.totalCount;
        this.totalIdentities = result.totalCount;
        this.kpis[0].value = result.totalCount.toString();
        this.updateRoleDistribution(result.items);
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; },
    });
  }

  onSearch(): void {
    this.filters.search = this.searchTerm;
    this.page = 1;
    this.loadUsers();
  }

  onRoleFilter(role: string): void {
    this.filters.role = role || undefined;
    this.page = 1;
    this.loadUsers();
  }

  onPageChange(page: number): void { this.page = page; this.loadUsers(); }

  viewUser(user: UserListDto): void { this.selectedUser = user; }
  closeDetail(): void { this.selectedUser = null; }

  changeRole(userId: string, newRole: string): void {
    if (!confirm(`Change this user's role to ${newRole}?`)) return;
    this.usersApi.updateUserRole(userId, newRole).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('Role updated.'); this.loadUsers(); this.closeDetail(); },
      error: () => this.toast.error('Failed to update role.'),
    });
  }

  lockUser(userId: string): void {
    const reason = prompt('Enter lock reason:');
    if (!reason) return;
    this.usersApi.lockUser(userId, reason).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('User locked.'); this.loadUsers(); this.closeDetail(); },
      error: () => this.toast.error('Failed to lock user.'),
    });
  }

  unlockUser(userId: string): void {
    this.usersApi.unlockUser(userId).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('User unlocked.'); this.loadUsers(); this.closeDetail(); },
      error: () => this.toast.error('Failed to unlock user.'),
    });
  }

  deactivateUser(userId: string): void {
    if (!confirm('Deactivate this user? They will no longer be able to log in.')) return;
    this.usersApi.softDeleteUser(userId).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('User deactivated.'); this.loadUsers(); this.closeDetail(); },
      error: () => this.toast.error('Failed to deactivate user.'),
    });
  }

  getRoleBadgeClass(role: string): string {
    switch (role?.toLowerCase()) {
      case 'superadmin': case 'admin': return 'badge-admin';
      case 'dealer': return 'badge-dealer';
      case 'customer': return 'badge-customer';
      default: return '';
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    // Close action menu when clicking outside
    if (this.actionMenuOpenForId) {
      this.actionMenuOpenForId = null;
    }
  }

  toggleActionMenu(userId: string, event: Event): void {
    event.stopPropagation();
    if (this.actionMenuOpenForId === userId) {
      this.actionMenuOpenForId = null;
    } else {
      this.actionMenuOpenForId = userId;
    }
  }

  closeActionMenu(): void {
    this.actionMenuOpenForId = null;
  }

  private loadSystemLogs(): void {
    // Fetch latest fraud alerts to use as system security logs
    this.fraudApi.getAlerts(1, 5).pipe(takeUntil(this.destroy$)).subscribe({
      next: (result) => {
        this.systemLogs = result.items.map(a => ({
          icon: a.severity === 'High' ? 'alert-triangle' : 'shield',
          text: `${a.ruleTriggered}: ${a.description}`,
          time: new Date(a.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
          meta: a.severity === 'High' ? 'Risk' : 'Security'
        }));
      }
    });
  }

  private updateRoleDistribution(users: UserListDto[]): void {
    const roles = { Customer: 0, Dealer: 0, Admin: 0, Logistics: 0 };
    users.forEach(u => {
      const r = u.role || 'Customer';
      if (r === 'SuperAdmin') roles.Admin++;
      else if (roles[r as keyof typeof roles] !== undefined) roles[r as keyof typeof roles]++;
      else roles.Customer++;
    });
    
    this.pieChartData.datasets[0].data = [
      roles.Customer || 1, 
      roles.Dealer || 1, 
      roles.Admin || 1, 
      roles.Logistics || 0
    ];
    // Trigger chart update
    this.pieChartData = { ...this.pieChartData };
  }
}
