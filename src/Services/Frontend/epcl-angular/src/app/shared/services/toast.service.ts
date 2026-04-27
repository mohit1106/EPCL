import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  id: string;
  type: ToastType;
  message: string;
  duration: number;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private toastsSubject = new BehaviorSubject<Toast[]>([]);
  readonly activeToasts$ = this.toastsSubject.asObservable();

  success(message: string, duration = 4000): void {
    this.add('success', message, duration);
  }

  error(message: string, duration = 6000): void {
    this.add('error', message, duration);
  }

  warning(message: string, duration = 5000): void {
    this.add('warning', message, duration);
  }

  info(message: string, duration = 4000): void {
    this.add('info', message, duration);
  }

  remove(id: string): void {
    this.toastsSubject.next(this.toastsSubject.value.filter((t) => t.id !== id));
  }

  private add(type: ToastType, message: string, duration: number): void {
    const id = this.generateId();
    const current = this.toastsSubject.value;

    // Max 5 toasts at a time
    const updated = current.length >= 5 ? [...current.slice(1), { id, type, message, duration }] : [...current, { id, type, message, duration }];

    this.toastsSubject.next(updated);
    setTimeout(() => this.remove(id), duration);
  }

  private generateId(): string {
    return Math.random().toString(36).substring(2, 11);
  }
}
