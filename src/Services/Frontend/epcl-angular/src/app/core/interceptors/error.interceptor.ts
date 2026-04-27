import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse,
} from '@angular/common/http';
import { Observable, throwError, catchError } from 'rxjs';
import { ToastService } from '../../shared/services/toast.service';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(private toast: ToastService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        let message = 'An unexpected error occurred.';

        switch (error.status) {
          case 0:
            message = 'Unable to connect to the server. Please check your internet connection.';
            break;
          case 400:
            message = this.extractValidationErrors(error) || 'Invalid request. Please check your input.';
            break;
          case 401:
            // Handled by JwtInterceptor — don't show toast
            return throwError(() => error);
          case 403:
            message = 'You do not have permission to perform this action.';
            break;
          case 404:
            message = 'The requested resource was not found.';
            break;
          case 409:
            message = error.error?.message || 'A conflict occurred. The resource may already exist.';
            break;
          case 423:
            message = error.error?.message || 'Your account is locked. Please try again later.';
            break;
          case 429:
            message = 'Too many requests. Please wait a moment and try again.';
            break;
          case 500:
            message = 'A server error occurred. Please try again later.';
            break;
          case 503:
            message = 'Service temporarily unavailable. Please try again in a few minutes.';
            break;
          default:
            message = error.error?.message || `Error ${error.status}: ${error.statusText}`;
        }

        this.toast.error(message);
        return throwError(() => error);
      })
    );
  }

  private extractValidationErrors(error: HttpErrorResponse): string | null {
    if (error.error?.errors) {
      const errors = error.error.errors;
      if (typeof errors === 'object') {
        const messages: string[] = [];
        for (const field of Object.keys(errors)) {
          const fieldErrors = errors[field];
          if (Array.isArray(fieldErrors)) {
            messages.push(...fieldErrors);
          }
        }
        return messages.length > 0 ? messages.join('. ') : null;
      }
    }
    return error.error?.message || null;
  }
}
