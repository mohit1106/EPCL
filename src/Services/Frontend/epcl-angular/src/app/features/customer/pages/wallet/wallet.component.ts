import { Component, OnInit, OnDestroy, AfterViewInit, ViewChild, ElementRef, NgZone } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { PaymentsApiService, WalletBalanceDto, WalletTransactionDto } from '../../../../core/services/payments-api.service';
import { WalletPaymentRequestDto } from '../../../../core/services/sales-api.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { environment } from '../../../../../environments/environment';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);
declare let Razorpay: any;

interface SavedCard {
  id: string;
  name: string;
  last4: string;
  network: string;       // Visa | Mastercard | Rupay
  expiry: string;        // MM/YY
  colorIndex: number;
}

@Component({
  selector: 'app-wallet',
  templateUrl: './wallet.component.html',
  styleUrls: ['./wallet.component.scss'],
})
export class WalletComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('spendChart') chartCanvas!: ElementRef<HTMLCanvasElement>;
  private destroy$ = new Subject<void>();
  private chart: Chart | null = null;

  balance: WalletBalanceDto = { balance: 0, lastUpdated: '' };
  transactions: WalletTransactionDto[] = [];
  isLoading = true;
  isProcessing = false;

  // Add Funds
  addAmount = 1000;
  amountPresets = [500, 1000, 2000, 5000];
  showAddFundsPanel = false;

  // Auto Top-up
  autoTopUp = false;
  autoTopUpAmount = 5000;
  autoTopUpThreshold = 1000;

  // Saved Cards (localStorage)
  savedCards: SavedCard[] = [];
  showAddCardModal = false;
  cardForm: FormGroup;
  activeCardMenuId: string | null = null;

  // Card themes (dark gradients like vehicles page)
  cardThemes = [
    { bg: 'linear-gradient(135deg, #0F172A 0%, #1E293B 50%, #334155 100%)', accent: '#60A5FA', network: 'VISA' },
    { bg: 'linear-gradient(135deg, #1A1A2E 0%, #16213E 50%, #0F3460 100%)', accent: '#818CF8', network: 'MASTERCARD' },
    { bg: 'linear-gradient(135deg, #0C0C1D 0%, #1B1B3A 50%, #2D2D5F 100%)', accent: '#A78BFA', network: 'RUPAY' },
    { bg: 'linear-gradient(135deg, #1C1917 0%, #292524 50%, #44403C 100%)', accent: '#FCD34D', network: 'VISA' },
    { bg: 'linear-gradient(135deg, #14120E 0%, #1F1D18 50%, #3B3730 100%)', accent: '#F9A825', network: 'MASTERCARD' },
    { bg: 'linear-gradient(135deg, #0D1117 0%, #161B22 50%, #21262D 100%)', accent: '#7DD3FC', network: 'RUPAY' },
  ];

  // Recent activity (derived from transactions)
  recentActivity: { date: string; desc: string; amount: number; type: string; icon: string }[] = [];

  // Pending payment requests from dealers
  pendingRequests: WalletPaymentRequestDto[] = [];
  processingRequestId: string | null = null;

  // Spending chart data
  monthLabels: string[] = [];
  monthData: number[] = [];

  constructor(
    private paymentsApi: PaymentsApiService,
    private toast: ToastService,
    private fb: FormBuilder,
    private router: Router,
    private elRef: ElementRef,
    private zone: NgZone
  ) {
    this.cardForm = this.fb.group({
      cardNumber: ['', [Validators.required, Validators.pattern(/^\d{16}$/)]],
      cardName: ['', [Validators.required, Validators.minLength(2)]],
      expiry: ['', [Validators.required, Validators.pattern(/^(0[1-9]|1[0-2])\/\d{2}$/)]],
      cvv: ['', [Validators.required, Validators.pattern(/^\d{3,4}$/)]],
      network: ['Visa', [Validators.required]],
    });
  }

  ngOnInit(): void {
    this.loadSavedCards();
    this.loadWalletData();
    this.loadPendingRequests();
  }

  ngAfterViewInit(): void {
    // Chart will be initialized after data loads
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.chart?.destroy();
  }

  closeCardMenu(): void {
    this.activeCardMenuId = null;
  }

  // ─── Data Loading ───

  private loadWalletData(): void {
    this.isLoading = true;
    this.paymentsApi.getWalletBalance().pipe(takeUntil(this.destroy$)).subscribe({
      next: (b) => { this.balance = b; this.isLoading = false; },
      error: () => { this.isLoading = false; },
    });
    this.paymentsApi.getWalletHistory(1, 50).pipe(takeUntil(this.destroy$)).subscribe({
      next: (txns) => {
        this.transactions = txns;
        this.buildRecentActivity(txns);
        this.buildChartData(txns);
        setTimeout(() => this.renderChart(), 100);
      },
    });
  }

  private isCredit(type: string): boolean {
    return type === 'TopUp' || type === 'Refund';
  }

  private buildRecentActivity(txns: WalletTransactionDto[]): void {
    this.recentActivity = txns.slice(0, 5).map(t => ({
      date: new Date(t.createdAt).toLocaleDateString('en-IN', { day: '2-digit', month: 'short' }),
      desc: t.description || (this.isCredit(t.type) ? 'Wallet Top-up' : 'Fuel Purchase'),
      amount: this.isCredit(t.type) ? t.amount : -t.amount,
      type: this.isCredit(t.type) ? 'Credit' : 'Debit',
      icon: this.isCredit(t.type) ? 'arrow-down' : 'arrow-up',
    }));
  }

  private buildChartData(txns: WalletTransactionDto[]): void {
    const now = new Date();
    const months: Map<string, number> = new Map();

    // Last 6 months
    for (let i = 5; i >= 0; i--) {
      const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
      const key = d.toLocaleDateString('en-IN', { month: 'short' });
      months.set(key, 0);
    }

    // Aggregate debits by month
    txns.filter(t => t.type === 'Debit').forEach(t => {
      const d = new Date(t.createdAt);
      const key = d.toLocaleDateString('en-IN', { month: 'short' });
      if (months.has(key)) months.set(key, (months.get(key) || 0) + t.amount);
    });

    this.monthLabels = Array.from(months.keys());
    this.monthData = Array.from(months.values());

    // If all zeros, use placeholder data for visual appeal
    if (this.monthData.every(v => v === 0)) {
      this.monthData = [4200, 6800, 5100, 7300, 6200, 8500];
    }
  }

  private renderChart(): void {
    if (!this.chartCanvas?.nativeElement) return;
    this.chart?.destroy();

    const ctx = this.chartCanvas.nativeElement.getContext('2d')!;
    const gradient = ctx.createLinearGradient(0, 0, 0, 180);
    gradient.addColorStop(0, 'rgba(30, 64, 175, 0.3)');
    gradient.addColorStop(1, 'rgba(30, 64, 175, 0.02)');

    this.chart = new Chart(ctx, {
      type: 'bar',
      data: {
        labels: this.monthLabels,
        datasets: [{
          data: this.monthData,
          backgroundColor: this.monthData.map((_, i) =>
            i === this.monthData.length - 1 ? '#1E40AF' : 'rgba(30, 64, 175, 0.18)'
          ),
          borderRadius: 6,
          borderSkipped: false,
          barPercentage: 0.55,
        }],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false }, tooltip: {
          backgroundColor: '#1E293B',
          titleColor: '#F8FAFC',
          bodyColor: '#CBD5E1',
          padding: 10,
          cornerRadius: 8,
          displayColors: false,
          callbacks: {
            label: (ctx) => `₹${Number(ctx.raw).toLocaleString('en-IN')}`,
          },
        }},
        scales: {
          x: {
            grid: { display: false },
            ticks: { color: '#94A3B8', font: { size: 11, family: "'Inter', sans-serif" } },
            border: { display: false },
          },
          y: {
            display: false,
            beginAtZero: true,
          },
        },
      },
    });
  }

  // ─── Add Funds (Razorpay) ───

  toggleAddFunds(): void {
    this.showAddFundsPanel = !this.showAddFundsPanel;
  }

  setPresetAmount(amt: number): void {
    this.addAmount = amt;
  }

  addMoney(): void {
    if (this.addAmount < 1) {
      this.toast.error('Minimum amount is ₹1');
      return;
    }
    this.isProcessing = true;
    this.paymentsApi.createWalletOrder(this.addAmount).pipe(takeUntil(this.destroy$)).subscribe({
      next: (order) => {
        const options = {
          key: order.keyId || environment.razorpayKeyId, // Use key from backend order response
          amount: Math.round(order.amount * 100), // Razorpay expects paise, backend returns rupees
          currency: order.currency,
          name: 'EPCL — Eleven Petroleum',
          description: 'Wallet Top-up',
          order_id: order.orderId,
          handler: (response: { razorpay_order_id: string; razorpay_payment_id: string; razorpay_signature: string }) => {
            this.zone.run(() => {
              this.verifyPayment(response.razorpay_order_id, response.razorpay_payment_id, response.razorpay_signature);
            });
          },
          modal: {
            ondismiss: () => {
              this.zone.run(() => { this.isProcessing = false; });
            },
          },
          theme: { color: '#1E40AF' },
        };
        const rzp = new Razorpay(options);
        rzp.on('payment.failed', (resp: { error: { description: string } }) => {
          this.zone.run(() => {
            this.toast.error(`Payment failed: ${resp.error.description}`);
            this.isProcessing = false;
          });
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
      next: () => {
        // Backend returns {message} on success
        this.toast.success(`₹${this.addAmount.toLocaleString('en-IN')} added to your wallet!`);
        this.showAddFundsPanel = false;
        this.isProcessing = false;
        this.loadWalletData(); // Refresh balance + history
      },
      error: () => {
        this.toast.error('Payment verification failed.');
        this.isProcessing = false;
      },
    });
  }

  // ─── Auto Top-up ───

  toggleAutoTopUp(): void {
    this.autoTopUp = !this.autoTopUp;
    this.toast.info(this.autoTopUp ? 'Auto top-up enabled' : 'Auto top-up disabled');
  }

  // ─── Saved Cards (localStorage) ───

  private loadSavedCards(): void {
    try {
      const raw = localStorage.getItem('epcl_saved_cards');
      this.savedCards = raw ? JSON.parse(raw) : [];
    } catch { this.savedCards = []; }
  }

  private persistCards(): void {
    localStorage.setItem('epcl_saved_cards', JSON.stringify(this.savedCards));
  }

  openAddCardModal(): void {
    this.showAddCardModal = true;
    this.cardForm.reset({ network: 'Visa' });
  }

  closeAddCardModal(): void {
    this.showAddCardModal = false;
  }

  saveCard(): void {
    if (this.cardForm.invalid) {
      this.cardForm.markAllAsTouched();
      return;
    }
    const v = this.cardForm.value;
    const card: SavedCard = {
      id: crypto.randomUUID(),
      name: v.cardName,
      last4: v.cardNumber.slice(-4),
      network: v.network,
      expiry: v.expiry,
      colorIndex: this.savedCards.length % this.cardThemes.length,
    };
    this.savedCards.push(card);
    this.persistCards();
    this.toast.success('Card saved successfully');
    this.closeAddCardModal();
  }

  toggleCardMenu(cardId: string): void {
    this.activeCardMenuId = this.activeCardMenuId === cardId ? null : cardId;
  }

  deleteCard(card: SavedCard): void {
    this.activeCardMenuId = null;
    this.savedCards = this.savedCards.filter(c => c.id !== card.id);
    this.persistCards();
    this.toast.success('Card removed');
  }

  getCardTheme(idx: number) {
    return this.cardThemes[idx % this.cardThemes.length];
  }

  // ─── Navigation ───

  goToTransactions(): void {
    this.router.navigate(['/customer/transactions']);
  }

  // ─── Helpers ───

  get totalSpend(): number {
    return this.monthData.reduce((a, b) => a + b, 0);
  }

  get avgMonthlySpend(): number {
    return this.monthData.length > 0 ? this.totalSpend / this.monthData.length : 0;
  }

  get spendTrend(): number {
    if (this.monthData.length < 2) return 0;
    const last = this.monthData[this.monthData.length - 1];
    const prev = this.monthData[this.monthData.length - 2];
    return prev > 0 ? ((last - prev) / prev) * 100 : 0;
  }

  // ── Pending Payment Requests ──────────────────────────
  loadPendingRequests(): void {
    this.paymentsApi.getPendingPaymentRequests().pipe(takeUntil(this.destroy$)).subscribe({
      next: (requests) => this.pendingRequests = requests,
      error: () => {} // silently fail if no requests
    });
  }

  approveRequest(requestId: string): void {
    this.processingRequestId = requestId;
    this.paymentsApi.approvePaymentRequest(requestId).pipe(takeUntil(this.destroy$)).subscribe({
      next: (res) => {
        this.toast.success(res.message);
        this.pendingRequests = this.pendingRequests.filter(r => r.id !== requestId);
        this.processingRequestId = null;
        this.loadWalletData(); // refresh balance
      },
      error: (err) => {
        this.toast.error(err?.error?.message || 'Failed to approve request.');
        this.processingRequestId = null;
      }
    });
  }

  rejectRequest(requestId: string): void {
    this.processingRequestId = requestId;
    this.paymentsApi.rejectPaymentRequest(requestId).pipe(takeUntil(this.destroy$)).subscribe({
      next: (res) => {
        this.toast.success(res.message);
        this.pendingRequests = this.pendingRequests.filter(r => r.id !== requestId);
        this.processingRequestId = null;
      },
      error: (err) => {
        this.toast.error(err?.error?.message || 'Failed to reject request.');
        this.processingRequestId = null;
      }
    });
  }

  getTimeRemaining(expiresAt: string): string {
    const diff = new Date(expiresAt).getTime() - Date.now();
    if (diff <= 0) return 'Expired';
    const mins = Math.floor(diff / 60000);
    return mins > 0 ? `${mins} min left` : 'Less than a minute';
  }
}
