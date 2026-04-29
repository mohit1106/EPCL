import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { of, catchError, map, switchMap, tap } from 'rxjs';
import * as AuthActions from './auth.actions';
import { AuthApiService } from '../../core/services/auth-api.service';
import { SignalRService } from '../../core/services/signalr.service';
import { ToastService } from '../../shared/services/toast.service';

@Injectable()
export class AuthEffects {
  constructor(
    private actions$: Actions,
    private authApi: AuthApiService,
    private signalR: SignalRService,
    private toast: ToastService,
    private router: Router
  ) {}

  login$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.login),
      switchMap(({ request }) =>
        this.authApi.login(request).pipe(
          map((response) => AuthActions.loginSuccess({ response })),
          catchError((err) =>
            of(AuthActions.loginFailure({ error: err.error?.message || 'Login failed' }))
          )
        )
      )
    )
  );

  googleLogin$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.googleLogin),
      switchMap(({ idToken }) =>
        this.authApi.googleLogin(idToken).pipe(
          map((response) => AuthActions.loginSuccess({ response })),
          catchError((err) =>
            of(AuthActions.loginFailure({ error: err.error?.message || 'Google login failed' }))
          )
        )
      )
    )
  );

  loginSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(AuthActions.loginSuccess),
        tap(({ response }) => {
          localStorage.setItem('epcl_access_token', response.accessToken);
          this.toast.success(`Welcome back, ${response.user.fullName}!`);

          // Connect SignalR based on role
          const token = response.accessToken;
          if (response.user.role === 'Admin' || response.user.role === 'SuperAdmin') {
            this.signalR.connectAdmin(token);
          } else if (response.user.role === 'Dealer') {
            this.signalR.connectDealer(token);
          }

          // Navigate to role-appropriate dashboard
          switch (response.user.role) {
            case 'Admin':
            case 'SuperAdmin':
              this.router.navigate(['/admin/dashboard']);
              break;
            case 'Dealer':
              this.router.navigate(['/dealer/dashboard']);
              break;
            case 'Customer':
              this.router.navigate(['/customer/dashboard']);
              break;
            default:
              this.router.navigate(['/']);
          }
        })
      ),
    { dispatch: false }
  );

  register$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.register),
      switchMap(({ request }) =>
        this.authApi.register(request).pipe(
          map((response) => AuthActions.registerSuccess({ message: response.message })),
          catchError((err) => {
            let errorMsg = 'Registration failed';
            if (err.error?.message) {
              errorMsg = err.error.message;
            } else if (err.error?.errors) {
              // ASP.NET validation errors: { errors: { FieldName: ["msg1", "msg2"] } }
              const allErrors = Object.values(err.error.errors).flat();
              errorMsg = (allErrors as string[]).join('. ');
            } else if (err.error?.title) {
              errorMsg = err.error.title;
            } else if (typeof err.error === 'string') {
              errorMsg = err.error;
            }
            return of(AuthActions.registerFailure({ error: errorMsg }));
          })
        )
      )
    )
  );

  registerSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(AuthActions.registerSuccess),
        tap(({ message }) => {
          this.toast.success(message);
          this.router.navigate(['/auth/login']);
        })
      ),
    { dispatch: false }
  );

  logout$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.logout),
      switchMap(() =>
        this.authApi.logout().pipe(
          catchError(() => of(void 0)) // Don't fail on logout API errors
        )
      ),
      tap(() => {
        localStorage.removeItem('epcl_access_token');
        this.signalR.disconnect();
        this.router.navigate(['/auth/login']);
      }),
      map(() => AuthActions.logoutComplete())
    )
  );

  restoreSession$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.restoreSession),
      switchMap(() => {
        const token = localStorage.getItem('epcl_access_token');
        if (!token) {
          return of(AuthActions.restoreSessionFailure());
        }

        return this.authApi.getCurrentUser().pipe(
          map((user) => AuthActions.restoreSessionSuccess({ user, token })),
          catchError(() => {
            localStorage.removeItem('epcl_access_token');
            return of(AuthActions.restoreSessionFailure());
          })
        );
      })
    )
  );

  restoreSessionSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(AuthActions.restoreSessionSuccess),
        tap(({ user, token }) => {
          if (user.role === 'Admin' || user.role === 'SuperAdmin') {
            this.signalR.connectAdmin(token);
          } else if (user.role === 'Dealer') {
            this.signalR.connectDealer(token);
          }
        })
      ),
    { dispatch: false }
  );
}
