import { Component, OnInit } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { Store } from '@ngrx/store';
import { Observable, filter, map } from 'rxjs';
import { selectUser, selectIsAuthenticated, selectUserRole } from '../../store/auth/auth.selectors';
import { logout } from '../../store/auth/auth.actions';
import { UserDto } from '../../core/services/auth-api.service';
import { SignalRService } from '../../core/services/signalr.service';

export interface NavItem {
  label: string;
  icon: string; // SVG path for Heroicons
  route: string;
  roles: string[];
  badge?: Observable<number>;
}

// Heroicon SVG paths (outline, 24x24)
const ICONS = {
  dashboard: 'M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6',
  wallet: 'M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z',
  fuel: 'M17.657 18.657A8 8 0 016.343 7.343S7 9 9 10c0-2 .5-5 2.986-7C14 5 16.09 5.777 17.656 7.343A7.975 7.975 0 0120 13a7.975 7.975 0 01-2.343 5.657z',
  transactions: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01',
  stations: 'M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z M15 11a3 3 0 11-6 0 3 3 0 016 0z',
  loyalty: 'M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z',
  vehicles: 'M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h1m8-1a1 1 0 01-1 1H9m4-1V8a1 1 0 011-1h2.586a1 1 0 01.707.293l3.414 3.414a1 1 0 01.293.707V16a1 1 0 01-1 1h-1m-6-1a1 1 0 001 1h1M5 17a2 2 0 104 0m-4 0a2 2 0 114 0m6 0a2 2 0 104 0m-4 0a2 2 0 114 0',
  referral: 'M12 8v13m0-13V6a2 2 0 112 2h-2zm0 0V5.5A2.5 2.5 0 109.5 8H12zm-7 4h14M5 12a2 2 0 110-4h14a2 2 0 110 4M5 12v7a2 2 0 002 2h10a2 2 0 002-2v-7',
  newSale: 'M12 9v3m0 0v3m0-3h3m-3 0H9m12 0a9 9 0 11-18 0 9 9 0 0118 0z',
  inventory: 'M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4',
  shifts: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z',
  replenishment: 'M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4',
  reports: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z',
  users: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z',
  prices: 'M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z',
  fraud: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z',
  audit: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2',
  health: 'M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z',
  documents: 'M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z',
  aiChat: 'M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z',
  contactAdmin: 'M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z',
  helpRequests: 'M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z',
};

@Component({
  selector: 'epcl-app-shell',
  templateUrl: './app-shell.component.html',
  styleUrls: ['./app-shell.component.scss'],
})
export class AppShellComponent implements OnInit {
  isCollapsed = false;
  user$: Observable<UserDto | null>;
  isAuthenticated$: Observable<boolean>;
  userRole$: Observable<string | null>;
  connectionStatus$: Observable<string>;
  currentRoute = '';

  navItems: NavItem[] = [
    // Customer
    { label: 'Dashboard', icon: ICONS.dashboard, route: '/customer/dashboard', roles: ['Customer'] },
    { label: 'Wallet', icon: ICONS.wallet, route: '/customer/wallet', roles: ['Customer'] },
    { label: 'Fuel Prices', icon: ICONS.fuel, route: '/customer/prices', roles: ['Customer'] },
    { label: 'Transactions', icon: ICONS.transactions, route: '/customer/transactions', roles: ['Customer'] },
    { label: 'Stations', icon: ICONS.stations, route: '/customer/stations', roles: ['Customer'] },
    { label: 'Loyalty', icon: ICONS.loyalty, route: '/customer/loyalty', roles: ['Customer'] },
    { label: 'Vehicles', icon: ICONS.vehicles, route: '/customer/vehicles', roles: ['Customer'] },
    { label: 'Referral Hub', icon: ICONS.referral, route: '/customer/referral', roles: ['Customer'] },

    // Dealer
    { label: 'Dashboard', icon: ICONS.dashboard, route: '/dealer/dashboard', roles: ['Dealer'] },
    { label: 'New Sale', icon: ICONS.newSale, route: '/dealer/sales/new', roles: ['Dealer'] },
    { label: 'Transactions', icon: ICONS.transactions, route: '/dealer/transactions', roles: ['Dealer'] },
    { label: 'Inventory', icon: ICONS.inventory, route: '/dealer/inventory', roles: ['Dealer'] },
    { label: 'Shifts', icon: ICONS.shifts, route: '/dealer/shift', roles: ['Dealer'] },
    { label: 'Replenishment', icon: ICONS.replenishment, route: '/dealer/replenishment', roles: ['Dealer'] },
    { label: 'Reports', icon: ICONS.reports, route: '/dealer/reports', roles: ['Dealer'] },
    { label: 'Contact Admin', icon: ICONS.contactAdmin, route: '/dealer/contact-admin', roles: ['Dealer'] },

    // Admin
    { label: 'Dashboard', icon: ICONS.dashboard, route: '/admin/dashboard', roles: ['Admin', 'SuperAdmin'] },
    { label: 'Users', icon: ICONS.users, route: '/admin/users', roles: ['Admin', 'SuperAdmin'] },
    { label: 'Stations', icon: ICONS.stations, route: '/admin/stations', roles: ['Admin', 'SuperAdmin'] },
    { label: 'Prices', icon: ICONS.prices, route: '/admin/prices', roles: ['Admin', 'SuperAdmin'] },
    { label: 'Fraud Intel', icon: ICONS.fraud, route: '/admin/fraud', roles: ['Admin', 'SuperAdmin'] },
    { label: 'Audit Logs', icon: ICONS.audit, route: '/admin/audit', roles: ['Admin', 'SuperAdmin'] },
    { label: 'Reports', icon: ICONS.reports, route: '/admin/reports', roles: ['Admin', 'SuperAdmin'] },
    { label: 'Documents', icon: ICONS.documents, route: '/admin/documents', roles: ['Admin', 'SuperAdmin'] },
    { label: 'System Health', icon: ICONS.health, route: '/admin/system-health', roles: ['Admin', 'SuperAdmin'] },
    { label: 'Help Requests', icon: ICONS.helpRequests, route: '/admin/help-requests', roles: ['Admin', 'SuperAdmin'] },
    { label: 'Replenishment', icon: ICONS.replenishment, route: '/admin/replenishment-requests', roles: ['Admin', 'SuperAdmin'] },
    { label: 'Drivers', icon: ICONS.vehicles, route: '/admin/drivers', roles: ['Admin', 'SuperAdmin'] },
  ];

  filteredNavItems: NavItem[] = [];

  constructor(
    private store: Store,
    private router: Router,
    public signalR: SignalRService
  ) {
    this.user$ = this.store.select(selectUser);
    this.isAuthenticated$ = this.store.select(selectIsAuthenticated);
    this.userRole$ = this.store.select(selectUserRole);
    this.connectionStatus$ = this.signalR.connectionStatus$;
  }

  ngOnInit(): void {
    this.router.events
      .pipe(
        filter((e) => e instanceof NavigationEnd),
        map((e) => (e as NavigationEnd).urlAfterRedirects)
      )
      .subscribe((url) => (this.currentRoute = url));

    this.userRole$.subscribe((role) => {
      this.filteredNavItems = this.navItems.filter(
        (item) => role && item.roles.includes(role)
      );
    });
  }

  toggleSidebar(): void {
    this.isCollapsed = !this.isCollapsed;
  }

  onLogout(): void {
    this.store.dispatch(logout());
  }

  isActive(route: string): boolean {
    return this.currentRoute.startsWith(route);
  }
}
