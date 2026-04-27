import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse,
} from '@angular/common/http';
import { Observable, throwError, catchError } from 'rxjs';
import { Store } from '@ngrx/store';
import { logout } from '../../store/auth/auth.actions';

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  private readonly skipUrls = [
    '/auth/login',
    '/auth/register',
    '/auth/google-login',
    '/auth/forgot-password',
    '/auth/reset-password',
    '/auth/verify-otp',
    '/auth/refresh',
  ];

  constructor(private store: Store) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    // Skip auth-related endpoints
    if (this.skipUrls.some(url => request.url.includes(url))) {
      return next.handle(request);
    }

    const token = localStorage.getItem('epcl_access_token');
    if (token) {
      request = this.addToken(request, token);
    }

    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        // On 401, just pass through — don't logout immediately.
        // The dashboard pages handle 401 gracefully with fallback mock data.
        // Only logout if we explicitly detect expired/invalid session.
        return throwError(() => error);
      })
    );
  }

  private addToken(request: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
    return request.clone({
      setHeaders: { Authorization: `Bearer ${token}` },
    });
  }
}
