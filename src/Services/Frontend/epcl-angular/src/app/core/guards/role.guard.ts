import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router, UrlTree } from '@angular/router';
import { Store } from '@ngrx/store';
import { Observable, map, take } from 'rxjs';
import { selectUserRole } from '../../store/auth/auth.selectors';

@Injectable({ providedIn: 'root' })
export class RoleGuard implements CanActivate {
  constructor(
    private store: Store,
    private router: Router
  ) {}

  canActivate(route: ActivatedRouteSnapshot): Observable<boolean | UrlTree> {
    const expectedRoles: string[] = route.data['roles'] || [];

    return this.store.select(selectUserRole).pipe(
      take(1),
      map((role) => {
        if (role && expectedRoles.includes(role)) return true;
        return this.router.createUrlTree(['/unauthorized']);
      })
    );
  }
}
