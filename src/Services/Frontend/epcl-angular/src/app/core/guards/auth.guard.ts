import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { Store } from '@ngrx/store';
import { Observable, map, take } from 'rxjs';
import { selectIsAuthenticated } from '../../store/auth/auth.selectors';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
  constructor(
    private store: Store,
    private router: Router
  ) {}

  canActivate(): Observable<boolean | UrlTree> {
    return this.store.select(selectIsAuthenticated).pipe(
      take(1),
      map((isAuth) => {
        if (isAuth) return true;
        return this.router.createUrlTree(['/auth/login']);
      })
    );
  }
}
