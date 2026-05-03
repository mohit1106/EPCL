import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { UsersApiService, UserListDto, UserFilters } from '../../../../core/services/users-api.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { Chart, registerables, ChartData, ChartType } from 'chart.js';

Chart.register(...registerables);

interface DisplayUser {
  id: string;
  initials: string;
  name: string;
  email: string;
  phone: string;
  uid: string;
  roleClass: string;
  role: string;
  statusClass: string;
  status: string;
  lastLogin: string;
  createdAt: string;
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
  page = 1;
  pageSize = 15;
  isLoading = true;
  filters: UserFilters = {};
  searchTerm = '';
  actionMenuOpenForId: string | null = null;

  // Selected user detail
  selectedUser: UserListDto | null = null;

  // Role change modal
  showRoleModal = false;
  roleChangeUserId = '';
  roleChangeUserName = '';
  newRole = 'Customer';
  roles = ['Customer', 'Dealer', 'Admin', 'SuperAdmin'];

  // Create user modal
  showCreateModal = false;
  isCreating = false;
  createForm = {
    fullName: '',
    email: '',
    phoneNumber: '',
    password: '',
    confirmPassword: '',
    role: 'Customer',
  };

  // KPIs (computed from data)
  activeCount = 0;
  lockedCount = 0;
  pendingCount = 0;

  // Role filter for select
  selectedRole = '';
  selectedStatus = '';

  // Chart — Role Distribution
  public pieChartOptions: any = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'bottom',
        labels: {
          color: '#475569',
          font: { family: "'Inter', sans-serif", size: 12, weight: 500 },
          padding: 16,
          usePointStyle: true,
          pointStyle: 'circle',
        },
      },
      tooltip: {
        backgroundColor: '#FFFFFF',
        titleColor: '#1E293B',
        bodyColor: '#475569',
        borderColor: '#E2E8F0',
        borderWidth: 1,
        padding: 12,
        cornerRadius: 8,
        titleFont: { family: "'Inter', sans-serif", weight: 'bold' as const },
        bodyFont: { family: "'Inter', sans-serif" },
      },
    },
    cutout: '68%',
  };

  public pieChartData: ChartData<'doughnut', number[], string | string[]> = {
    labels: ['Customer', 'Dealer', 'Admin', 'SuperAdmin'],
    datasets: [{
      data: [0, 0, 0, 0],
      backgroundColor: ['#3B82F6', '#F59E0B', '#8B5CF6', '#10B981'],
      borderWidth: 0,
      hoverOffset: 6,
    }],
  };
  public pieChartType: ChartType = 'doughnut';

  constructor(
    private usersApi: UsersApiService,
    private toast: ToastService,
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.usersApi.getUsers(this.page, this.pageSize, this.filters).pipe(takeUntil(this.destroy$)).subscribe({
      next: (result) => {
        this.rawUsers = result.items;
        this.users = result.items.map(u => ({
          id: u.id,
          initials: u.fullName.split(' ').map(n => n.charAt(0)).slice(0, 2).join('').toUpperCase(),
          name: u.fullName,
          email: u.email,
          phone: u.phoneNumber || '—',
          uid: u.id.substring(0, 8).toUpperCase(),
          roleClass: this.getRoleClass(u.role),
          role: u.role || 'Unknown',
          statusClass: !u.isActive ? 'status-locked' : (u.isEmailVerified ? 'status-active' : 'status-pending'),
          status: !u.isActive ? 'Locked' : (u.isEmailVerified ? 'Active' : 'Pending'),
          lastLogin: u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' }) + ' ' + new Date(u.lastLoginAt).toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit' }) : 'Never',
          createdAt: new Date(u.createdAt).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' }),
          rawDto: u,
        }));
        this.totalCount = result.totalCount;

        // Compute KPIs
        this.activeCount = result.items.filter(u => u.isActive && u.isEmailVerified).length;
        this.lockedCount = result.items.filter(u => !u.isActive).length;
        this.pendingCount = result.items.filter(u => u.isActive && !u.isEmailVerified).length;
        this.updateRoleDistribution(result.items);
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.toast.error('Failed to load users.');
      },
    });
  }

  // ═══ Search & Filters ═══
  onSearch(): void {
    this.filters.search = this.searchTerm.trim() || undefined;
    this.page = 1;
    this.loadUsers();
  }

  onRoleFilter(role: string): void {
    this.selectedRole = role;
    this.filters.role = role || undefined;
    this.page = 1;
    this.loadUsers();
  }

  onStatusFilter(status: string): void {
    this.selectedStatus = status;
    if (status === 'active') {
      this.filters.isActive = true;
    } else if (status === 'locked') {
      this.filters.isActive = false;
    } else {
      this.filters.isActive = undefined;
    }
    this.page = 1;
    this.loadUsers();
  }

  // ═══ Pagination ═══
  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }

  get pageNumbers(): number[] {
    const pages: number[] = [];
    const start = Math.max(1, this.page - 2);
    const end = Math.min(this.totalPages, this.page + 2);
    for (let i = start; i <= end; i++) pages.push(i);
    return pages;
  }

  onPageChange(p: number): void {
    if (p < 1 || p > this.totalPages || p === this.page) return;
    this.page = p;
    this.loadUsers();
  }

  // ═══ User Detail ═══
  viewUser(user: UserListDto): void {
    this.selectedUser = user;
  }

  closeDetail(): void {
    this.selectedUser = null;
  }

  // ═══ Create User ═══
  openCreateModal(): void {
    this.showCreateModal = true;
    this.createForm = { fullName: '', email: '', phoneNumber: '', password: '', confirmPassword: '', role: 'Customer' };
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
  }

  submitCreateUser(): void {
    if (!this.createForm.fullName.trim() || !this.createForm.email.trim() || !this.createForm.password) {
      this.toast.error('Please fill all required fields.');
      return;
    }
    if (this.createForm.password !== this.createForm.confirmPassword) {
      this.toast.error('Passwords do not match.');
      return;
    }
    if (this.createForm.password.length < 8) {
      this.toast.error('Password must be at least 8 characters.');
      return;
    }
    this.isCreating = true;
    this.usersApi.createUser(this.createForm).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toast.success('User created successfully!');
        this.isCreating = false;
        this.showCreateModal = false;
        this.loadUsers();
      },
      error: (err) => {
        this.toast.error(err?.error?.message || 'Failed to create user.');
        this.isCreating = false;
      },
    });
  }

  // ═══ Role Change ═══
  openRoleModal(user: DisplayUser): void {
    this.roleChangeUserId = user.id;
    this.roleChangeUserName = user.name;
    this.newRole = user.role;
    this.showRoleModal = true;
    this.closeActionMenu();
  }

  closeRoleModal(): void {
    this.showRoleModal = false;
  }

  submitRoleChange(): void {
    if (!this.newRole) return;
    this.usersApi.updateUserRole(this.roleChangeUserId, this.newRole).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toast.success(`Role changed to ${this.newRole}.`);
        this.showRoleModal = false;
        this.loadUsers();
      },
      error: () => this.toast.error('Failed to update role.'),
    });
  }

  // ═══ User Actions ═══
  lockUser(userId: string): void {
    const reason = prompt('Enter lock reason:');
    if (!reason) return;
    this.closeActionMenu();
    this.usersApi.lockUser(userId, reason).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('User locked.'); this.loadUsers(); },
      error: () => this.toast.error('Failed to lock user.'),
    });
  }

  unlockUser(userId: string): void {
    this.closeActionMenu();
    this.usersApi.unlockUser(userId).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('User unlocked.'); this.loadUsers(); },
      error: () => this.toast.error('Failed to unlock user.'),
    });
  }

  deactivateUser(userId: string): void {
    if (!confirm('Deactivate this user? They will no longer be able to log in.')) return;
    this.closeActionMenu();
    this.usersApi.softDeleteUser(userId).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.toast.success('User deactivated.'); this.loadUsers(); },
      error: () => this.toast.error('Failed to deactivate user.'),
    });
  }

  // ═══ Action Menu ═══
  @HostListener('document:click')
  onDocumentClick(): void {
    if (this.actionMenuOpenForId) this.actionMenuOpenForId = null;
  }

  toggleActionMenu(userId: string, event: Event): void {
    event.stopPropagation();
    this.actionMenuOpenForId = this.actionMenuOpenForId === userId ? null : userId;
  }

  closeActionMenu(): void {
    this.actionMenuOpenForId = null;
  }

  // ═══ Helpers ═══
  getRoleClass(role: string): string {
    switch (role?.toLowerCase()) {
      case 'superadmin': return 'role-superadmin';
      case 'admin': return 'role-admin';
      case 'dealer': return 'role-dealer';
      case 'customer': return 'role-customer';
      default: return 'role-customer';
    }
  }

  getInitials(fullName: string): string {
    return fullName.split(' ').map(n => n.charAt(0)).slice(0, 2).join('').toUpperCase();
  }

  get chartLegendItems(): { label: string; color: string; value: number }[] {
    const labels = this.pieChartData.labels as string[] || [];
    const colors = this.pieChartData.datasets[0].backgroundColor as string[] || [];
    const data = this.pieChartData.datasets[0].data;
    return labels.map((label, i) => ({
      label,
      color: colors[i] || '#94A3B8',
      value: data[i] || 0,
    }));
  }

  private updateRoleDistribution(users: UserListDto[]): void {
    const roles = { Customer: 0, Dealer: 0, Admin: 0, SuperAdmin: 0 };
    users.forEach(u => {
      const r = u.role || 'Customer';
      if (roles[r as keyof typeof roles] !== undefined) {
        roles[r as keyof typeof roles]++;
      } else {
        roles.Customer++;
      }
    });

    this.pieChartData = {
      ...this.pieChartData,
      datasets: [{
        ...this.pieChartData.datasets[0],
        data: [roles.Customer || 0, roles.Dealer || 0, roles.Admin || 0, roles.SuperAdmin || 0],
      }],
    };
  }
}
