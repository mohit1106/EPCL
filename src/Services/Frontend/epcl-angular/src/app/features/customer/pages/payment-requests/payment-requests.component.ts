import { Component, OnInit, OnDestroy, NgZone } from '@angular/core';
import { Subject, takeUntil, interval } from 'rxjs';
import { PaymentsApiService, CreateOrderResponse } from '../../../../core/services/payments-api.service';
import { WalletPaymentRequestDto } from '../../../../core/services/sales-api.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { environment } from '../../../../../environments/environment';

declare let Razorpay: any;

@Component({
  selector: 'app-payment-requests',
  templateUrl: './payment-requests.component.html',
  styleUrls: ['./payment-requests.component.scss'],
})
export class PaymentRequestsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  requests: WalletPaymentRequestDto[] = [];
  isLoading = true;
  processingId: string | null = null;
  activeTab: 'pending' | 'history' = 'pending';

  constructor(
    private paymentsApi: PaymentsApiService,
    private toast: ToastService,
    private zone: NgZone
  ) {}

  ngOnInit(): void {
    this.loadRequests();
    // Auto-refresh every 10 seconds
    interval(10000).pipe(takeUntil(this.destroy$)).subscribe(() => this.loadRequests());
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadRequests(): void {
    this.paymentsApi.getAllPaymentRequests().pipe(takeUntil(this.destroy$)).subscribe({
      next: (r) => { this.requests = r; this.isLoading = false; },
      error: () => { this.isLoading = false; },
    });
  }

  get pendingRequests(): WalletPaymentRequestDto[] {
    return this.requests.filter(r => r.status === 'Pending');
  }

  get completedRequests(): WalletPaymentRequestDto[] {
    return this.requests.filter(r => r.status !== 'Pending');
  }

  getMethodIcon(method: string): string {
    switch (method) {
      case 'Wallet': return 'wallet';
      case 'UPI': return 'upi';
      case 'Card': return 'card';
      default: return 'wallet';
    }
  }

  getMethodLabel(method: string): string {
    switch (method) {
      case 'Wallet': return 'Pay from Wallet';
      case 'UPI': return 'Pay via UPI (Razorpay)';
      case 'Card': return 'Pay via Card (Razorpay)';
      default: return 'Pay';
    }
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Approved': return 'status-approved';
      case 'Rejected': return 'status-rejected';
      case 'Expired': return 'status-expired';
      default: return 'status-pending';
    }
  }

  getTimeRemaining(expiresAt: string): string {
    const diff = new Date(expiresAt).getTime() - Date.now();
    if (diff <= 0) return 'Expired';
    const mins = Math.floor(diff / 60000);
    return mins > 0 ? `${mins} min left` : 'Less than a minute';
  }

  /** Pay from wallet (existing approve flow) */
  approveFromWallet(requestId: string): void {
    this.processingId = requestId;
    this.paymentsApi.approvePaymentRequest(requestId).pipe(takeUntil(this.destroy$)).subscribe({
      next: (res) => {
        this.toast.success(res.message);
        this.processingId = null;
        this.loadRequests();
      },
      error: (err) => {
        this.toast.error(err?.error?.message || 'Failed to approve request.');
        this.processingId = null;
      },
    });
  }

  /** Pay via Razorpay (UPI/Bank/Card) */
  payViaRazorpay(request: WalletPaymentRequestDto): void {
    this.processingId = request.id;

    this.paymentsApi.createRequestPaymentOrder(request.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: (order: CreateOrderResponse) => {
        const options = {
          key: order.keyId || environment.razorpayKeyId,
          amount: Math.round(order.amount * 100),
          currency: order.currency,
          name: 'EPCL — Eleven Petroleum',
          description: `Fuel Payment — ${request.fuelTypeName || 'Fuel'} ${request.quantityLitres || ''}L`,
          order_id: order.orderId,
          handler: (response: { razorpay_order_id: string; razorpay_payment_id: string; razorpay_signature: string }) => {
            this.zone.run(() => {
              this.verifyRazorpayPayment(request.id, response.razorpay_order_id, response.razorpay_payment_id, response.razorpay_signature);
            });
          },
          modal: {
            ondismiss: () => {
              this.zone.run(() => { this.processingId = null; });
            },
          },
          theme: { color: '#1E40AF' },
        };
        const rzp = new Razorpay(options);
        rzp.on('payment.failed', (resp: { error: { description: string } }) => {
          this.zone.run(() => {
            this.toast.error(`Payment failed: ${resp.error.description}`);
            this.processingId = null;
          });
        });
        rzp.open();
      },
      error: () => {
        this.toast.error('Failed to create payment order.');
        this.processingId = null;
      },
    });
  }

  private verifyRazorpayPayment(requestId: string, orderId: string, paymentId: string, signature: string): void {
    this.paymentsApi.verifyRequestPayment(requestId, orderId, paymentId, signature).pipe(takeUntil(this.destroy$)).subscribe({
      next: (res) => {
        this.toast.success(res.message);
        this.processingId = null;
        this.loadRequests();
      },
      error: (err) => {
        this.toast.error(err?.error?.message || 'Payment verification failed.');
        this.processingId = null;
      },
    });
  }

  /** Reject a payment request */
  rejectRequest(requestId: string): void {
    this.processingId = requestId;
    this.paymentsApi.rejectPaymentRequest(requestId).pipe(takeUntil(this.destroy$)).subscribe({
      next: (res) => {
        this.toast.success(res.message);
        this.processingId = null;
        this.loadRequests();
      },
      error: (err) => {
        this.toast.error(err?.error?.message || 'Failed to reject request.');
        this.processingId = null;
      },
    });
  }

  /** Handle the primary action */
  handlePay(request: WalletPaymentRequestDto): void {
    if (request.paymentMethod === 'Wallet') {
      this.approveFromWallet(request.id);
    } else {
      this.payViaRazorpay(request);
    }
  }
}
