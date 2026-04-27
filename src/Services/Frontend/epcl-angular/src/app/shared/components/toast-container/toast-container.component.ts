import { Component } from '@angular/core';
import { animate, style, transition, trigger } from '@angular/animations';
import { ToastService, Toast } from '../../services/toast.service';

@Component({
  selector: 'epcl-toast-container',
  template: `
    <div class="toast-container">
      <div
        *ngFor="let toast of toastService.activeToasts$ | async; trackBy: trackById"
        class="toast toast-{{ toast.type }}"
        [@toastAnimation]
      >
        <div class="toast-icon">
          <ng-container [ngSwitch]="toast.type">
            <span *ngSwitchCase="'success'">✓</span>
            <span *ngSwitchCase="'error'">✕</span>
            <span *ngSwitchCase="'warning'">⚠</span>
            <span *ngSwitchCase="'info'">ℹ</span>
          </ng-container>
        </div>
        <div class="toast-message">{{ toast.message }}</div>
        <button class="toast-close" (click)="toastService.remove(toast.id)">×</button>
      </div>
    </div>
  `,
  styles: [
    `
      .toast-container {
        position: fixed;
        top: 20px;
        right: 20px;
        z-index: 10000;
        display: flex;
        flex-direction: column;
        gap: 8px;
        max-width: 420px;
      }

      .toast {
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 14px 18px;
        border-radius: var(--radius-md);
        backdrop-filter: blur(20px);
        -webkit-backdrop-filter: blur(20px);
        border: 1px solid rgba(71, 85, 105, 0.3);
        box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
        font-size: 14px;
        color: var(--text-primary);
      }

      .toast-success {
        background: rgba(16, 185, 129, 0.12);
        border-color: rgba(16, 185, 129, 0.3);
      }

      .toast-error {
        background: rgba(239, 68, 68, 0.12);
        border-color: rgba(239, 68, 68, 0.3);
      }

      .toast-warning {
        background: rgba(245, 158, 11, 0.12);
        border-color: rgba(245, 158, 11, 0.3);
      }

      .toast-info {
        background: rgba(59, 130, 246, 0.12);
        border-color: rgba(59, 130, 246, 0.3);
      }

      .toast-icon {
        width: 28px;
        height: 28px;
        border-radius: 50%;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 14px;
        font-weight: 700;
        flex-shrink: 0;
      }

      .toast-success .toast-icon {
        background: rgba(16, 185, 129, 0.2);
        color: #10B981;
      }
      .toast-error .toast-icon {
        background: rgba(239, 68, 68, 0.2);
        color: #EF4444;
      }
      .toast-warning .toast-icon {
        background: rgba(245, 158, 11, 0.2);
        color: #F59E0B;
      }
      .toast-info .toast-icon {
        background: rgba(59, 130, 246, 0.2);
        color: #3B82F6;
      }

      .toast-message {
        flex: 1;
        line-height: 1.4;
      }

      .toast-close {
        background: none;
        border: none;
        color: var(--text-muted);
        font-size: 18px;
        cursor: pointer;
        padding: 0 4px;
        transition: color 0.15s ease;
        flex-shrink: 0;

        &:hover {
          color: var(--text-primary);
        }
      }
    `,
  ],
  animations: [
    trigger('toastAnimation', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateX(80px)' }),
        animate('300ms cubic-bezier(0.34, 1.56, 0.64, 1)', style({ opacity: 1, transform: 'translateX(0)' })),
      ]),
      transition(':leave', [
        animate('200ms ease-in', style({ opacity: 0, transform: 'translateX(80px)' })),
      ]),
    ]),
  ],
})
export class ToastContainerComponent {
  constructor(public toastService: ToastService) {}

  trackById(_index: number, toast: Toast): string {
    return toast.id;
  }
}
