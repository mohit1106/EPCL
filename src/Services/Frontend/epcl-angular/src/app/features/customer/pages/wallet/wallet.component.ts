import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { PaymentsApiService, WalletBalanceDto, WalletTransactionDto } from '../../../../core/services/payments-api.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { environment } from '../../../../../environments/environment';

declare let Razorpay: any;

@Component({
  selector: 'app-wallet',
  templateUrl: './wallet.component.html',
  styleUrls: ['./wallet.component.scss'],
})
export class WalletComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  balance: WalletBalanceDto = { balance: 0, lastUpdated: '' };
  transactions: WalletTransactionDto[] = [];
  isLoading = true;
  addMoneyAmount = 1;
  isProcessing = false;
  isLiveMode = true; // Always true with live keys
  environment = environment;

  autoTopUp = true;
  autoTopUpAmount = 5000;
  autoTopUpThreshold = 1000;
  
  avgSpend = 12400;
  avgSpendTrend = 4.2;

  paymentMethods = [
    { icon: 'bank', name: 'HDFC Bank', detail: '•••• 4212', active: true },
    { icon: 'card', name: 'Corporate Card', detail: '•••• 8821', active: false },
    { icon: 'upi', name: 'UPI AutoPay', detail: 'eleven@okhdfc', active: false },
  ];

  recentTransactions: { date: string; desc: string; subDesc: string; id: string; method: string; amount: number; status: string }[] = [];

  constructor(private paymentsApi: PaymentsApiService, private toast: ToastService) {}

  ngOnInit(): void { this.loadWalletData(); }
  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  private loadWalletData(): void {
    this.isLoading = true;
    this.paymentsApi.getWalletBalance().pipe(takeUntil(this.destroy$)).subscribe({
      next: (b) => { this.balance = b; this.isLoading = false; },
      error: () => { this.isLoading = false; },
    });
    this.paymentsApi.getWalletHistory().pipe(takeUntil(this.destroy$)).subscribe({
      next: (txns) => { 
        this.transactions = txns; 
        this.recentTransactions = txns.map(t => ({
          date: new Date(t.createdAt).toLocaleDateString(),
          desc: t.type === 'Credit' ? 'Wallet Top-up' : 'Fuel Purchase',
          subDesc: t.description || (t.type === 'Credit' ? 'Auto-recharge' : 'Station 082'),
          id: t.referenceId || t.id.substring(0, 8),
          method: t.type === 'Credit' ? 'Bank Transfer' : 'Wallet Deduction',
          amount: t.type === 'Credit' ? t.amount : -t.amount,
          status: 'Settled'
        }));
      },
    });
  }

  toggleAutoTopUp(): void {
    this.autoTopUp = !this.autoTopUp;
  }

  addMoney(): void {
    if (this.addMoneyAmount < 1) {
      this.toast.error('Minimum amount is ₹1');
      return;
    }

    this.isProcessing = true;
    this.paymentsApi.createWalletOrder(this.addMoneyAmount).pipe(takeUntil(this.destroy$)).subscribe({
      next: (order) => {
        const options = {
          key: environment.razorpayKeyId,
          amount: order.amount,
          currency: order.currency,
          name: 'EPCL — Eleven Petroleum',
          description: 'Wallet Top-up',
          order_id: order.orderId,
          handler: (response: { razorpay_order_id: string; razorpay_payment_id: string; razorpay_signature: string }) => {
            this.verifyPayment(response.razorpay_order_id, response.razorpay_payment_id, response.razorpay_signature);
          },
          modal: {
            ondismiss: () => { this.isProcessing = false; },
          },
          theme: { color: '#6366f1' },
        };

        const rzp = new Razorpay(options);
        rzp.on('payment.failed', (response: { error: { description: string } }) => {
          this.toast.error(`Payment failed: ${response.error.description}`);
          this.isProcessing = false;
        });
        rzp.open();
      },
      error: () => {
        this.toast.error('Failed to create payment order.');
        this.isProcessing = false;
      },
    });
  }

  private verifyPayment(orderId: string, paymentId: string, signature: string): void {
    this.paymentsApi.verifyPayment({ orderId, paymentId, signature }).pipe(takeUntil(this.destroy$)).subscribe({
      next: (result) => {
        if (result.success) {
          this.balance.balance = result.newBalance;
          this.toast.success(`₹${this.addMoneyAmount} added to your wallet!`);
          this.loadWalletData(); // Refresh history
        }
        this.isProcessing = false;
      },
      error: () => {
        this.toast.error('Payment verification failed.');
        this.isProcessing = false;
      },
    });
  }
}
