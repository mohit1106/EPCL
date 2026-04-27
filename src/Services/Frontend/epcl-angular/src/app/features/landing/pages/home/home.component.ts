import { Component, AfterViewInit, OnDestroy, ViewChild, ElementRef, Renderer2 } from '@angular/core';
import { Router } from '@angular/router';

declare var THREE: any;
declare var Chart: any;

@Component({
  selector: 'app-landing-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
})
export class HomeComponent implements AfterViewInit, OnDestroy {
  @ViewChild('heroCanvas') heroCanvasRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('heroMiniChart') heroMiniChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('networkCanvas') networkCanvasRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('aiMiniChart') aiMiniChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('dashSalesChart') dashSalesChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('dashStationChart') dashStationChartRef!: ElementRef<HTMLCanvasElement>;

  mobileMenuOpen = false;
  private observer?: IntersectionObserver;
  private animationFrames: number[] = [];

  // Counter targets for stats ticker
  counters = [
    { target: 847, current: 0, suffix: '+', label: 'Active Stations', decimal: false },
    { target: 2.4, current: 0, suffix: 'M', label: 'Daily Transactions', decimal: true },
    { target: 99.9, current: 0, suffix: '%', label: 'Uptime SLA', decimal: true },
    { target: 12, current: 0, suffix: 'ms', label: 'Avg Response Time', decimal: false },
  ];

  features = [
    { icon: 'M13 10V3L4 14h7v7l9-11h-7z', title: 'Real-Time Sales', desc: 'Capture every transaction with live pump-level telemetry. Instant receipt generation and payment processing.' },
    { icon: 'M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4', title: 'Inventory Intelligence', desc: 'Tank-level monitoring with AI-powered consumption forecasting. Automated replenishment triggers.' },
    { icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z', title: 'Fraud Detection', desc: '14 rule-based and ML-powered fraud detection algorithms scanning every transaction in real-time.' },
    { icon: 'M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z', title: 'Digital Payments', desc: 'Razorpay-powered wallet with auto-recharge. UPI, Card, and Net Banking for seamless transactions.' },
    { icon: 'M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z', title: 'Loyalty Engine', desc: 'Tiered rewards program with Silver, Gold, and Platinum tiers. Referral tracking and points on every purchase.' },
    { icon: 'M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z', title: 'AI Insights', desc: 'Ask questions in natural language. Gemini AI generates SQL, queries your data, and returns formatted answers.' },
  ];

  aiFeatures = [
    { icon: 'M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z', title: 'Natural Language Queries', desc: 'Type questions like "What was last week\'s revenue?" and get instant results.' },
    { icon: 'M11 3.055A9.001 9.001 0 1020.945 13H11V3.055z', title: 'Auto-Generated Charts', desc: 'AI suggests the best chart type and generates visuals from your data.' },
    { icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z', title: 'SQL Safety Guard', desc: 'Only SELECT queries pass. All mutations are blocked at the engine level.' },
    { icon: 'M15 12a3 3 0 11-6 0 3 3 0 016 0z', title: 'Role-Based Scoping', desc: 'Dealers can only query their own station data. Admins see everything.' },
  ];

  roles = [
    { letter: 'A', class: 'admin', title: 'Administrator', desc: 'Full platform oversight with analytics, user management, and fraud monitoring.', features: ['Enterprise analytics dashboard', 'Fraud detection & alerts', 'User & station management', 'AI-powered queries'] },
    { letter: 'D', class: 'dealer', title: 'Dealer', desc: 'Station-level operations: sales, inventory, shifts, and local reporting.', features: ['Fuel sale recording', 'Tank level monitoring', 'Shift open/close management', 'Replenishment requests'] },
    { letter: 'C', class: 'customer', title: 'Customer', desc: 'Digital wallet, loyalty rewards, transaction history, and station finder.', features: ['Digital wallet & payments', 'Loyalty points & tier rewards', 'Fuel price tracker', 'Referral program'] },
  ];

  constructor(private router: Router, private renderer: Renderer2) {}

  ngAfterViewInit(): void {
    this.loadExternalScripts().then(() => {
      this.initHero3D();
      this.initHeroMiniChart();
      this.initNetwork3D();
      this.initAiMiniChart();
      this.initDashboardCharts();
    });
    this.initRevealObserver();
    this.animateCounters();
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
    this.animationFrames.forEach(id => cancelAnimationFrame(id));
  }

  navigateTo(route: string): void {
    this.router.navigate([route]);
  }

  scrollTo(sectionId: string): void {
    const el = document.getElementById(sectionId);
    if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen = !this.mobileMenuOpen;
  }

  private async loadExternalScripts(): Promise<void> {
    const scripts = [
      'https://cdnjs.cloudflare.com/ajax/libs/three.js/r128/three.min.js',
      'https://cdn.jsdelivr.net/npm/three@0.128.0/examples/js/loaders/GLTFLoader.js',
      'https://cdn.jsdelivr.net/npm/three@0.128.0/examples/js/controls/OrbitControls.js',
      'https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js',
    ];

    for (const src of scripts) {
      if (document.querySelector(`script[src="${src}"]`)) continue;
      await new Promise<void>((resolve, reject) => {
        const script = this.renderer.createElement('script');
        script.src = src;
        script.onload = () => resolve();
        script.onerror = () => resolve(); // Don't block on failures
        document.head.appendChild(script);
      });
    }
  }

  private initRevealObserver(): void {
    if (typeof IntersectionObserver === 'undefined') return;
    this.observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add('visible');
          }
        });
      },
      { threshold: 0.1, rootMargin: '0px 0px -40px 0px' }
    );
    setTimeout(() => {
      document.querySelectorAll('.reveal').forEach((el) => this.observer?.observe(el));
    }, 100);
  }

  private animateCounters(): void {
    setTimeout(() => {
      this.counters.forEach((counter) => {
        const duration = 2000;
        const start = performance.now();
        const animate = (now: number) => {
          const progress = Math.min((now - start) / duration, 1);
          const eased = 1 - Math.pow(1 - progress, 3);
          counter.current = counter.decimal
            ? Math.round(counter.target * eased * 10) / 10
            : Math.round(counter.target * eased);
          if (progress < 1) {
            requestAnimationFrame(animate);
          }
        };
        requestAnimationFrame(animate);
      });
    }, 800);
  }

  private initHero3D(): void {
    if (typeof THREE === 'undefined') return;
    const canvas = this.heroCanvasRef?.nativeElement;
    if (!canvas) return;

    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(45, canvas.clientWidth / canvas.clientHeight, 0.1, 1000);
    const renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true });
    renderer.setSize(canvas.clientWidth, canvas.clientHeight);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.setClearColor(0x000000, 0);

    scene.add(new THREE.AmbientLight(0xffffff, 0.6));
    const dl = new THREE.DirectionalLight(0x1E40AF, 0.8);
    dl.position.set(5, 5, 5);
    scene.add(dl);

    // Create a stylized fuel pump-like abstract shape
    const group = new THREE.Group();
    const bodyGeo = new THREE.BoxGeometry(1.2, 2, 0.8);
    const bodyMat = new THREE.MeshPhongMaterial({ color: 0x1E40AF, shininess: 80 });
    const body = new THREE.Mesh(bodyGeo, bodyMat);
    group.add(body);

    const topGeo = new THREE.CylinderGeometry(0.3, 0.3, 0.4, 16);
    const topMat = new THREE.MeshPhongMaterial({ color: 0x3B82F6, shininess: 100 });
    const top = new THREE.Mesh(topGeo, topMat);
    top.position.y = 1.2;
    group.add(top);

    const baseGeo = new THREE.BoxGeometry(1.6, 0.2, 1.0);
    const baseMat = new THREE.MeshPhongMaterial({ color: 0x0F172A });
    const base = new THREE.Mesh(baseGeo, baseMat);
    base.position.y = -1.1;
    group.add(base);

    // Orbiting data particles
    const particleGeo = new THREE.SphereGeometry(0.06, 8, 8);
    const particles: any[] = [];
    for (let i = 0; i < 20; i++) {
      const mat = new THREE.MeshBasicMaterial({
        color: [0x3B82F6, 0xF59E0B, 0x10B981][i % 3],
        transparent: true,
        opacity: 0.8,
      });
      const p = new THREE.Mesh(particleGeo, mat);
      group.add(p);
      particles.push(p);
    }

    scene.add(group);
    camera.position.set(3, 1, 4);
    camera.lookAt(0, 0, 0);

    const animate = () => {
      const id = requestAnimationFrame(animate);
      this.animationFrames.push(id);
      const time = Date.now() * 0.001;
      group.rotation.y = time * 0.3;

      particles.forEach((p, i) => {
        const angle = time * 0.5 + (i * Math.PI * 2) / particles.length;
        const radius = 1.5 + Math.sin(time + i) * 0.3;
        p.position.x = Math.cos(angle) * radius;
        p.position.z = Math.sin(angle) * radius;
        p.position.y = Math.sin(time * 0.8 + i * 0.5) * 1.2;
      });

      renderer.render(scene, camera);
    };
    animate();

    window.addEventListener('resize', () => {
      if (!canvas.clientWidth) return;
      camera.aspect = canvas.clientWidth / canvas.clientHeight;
      camera.updateProjectionMatrix();
      renderer.setSize(canvas.clientWidth, canvas.clientHeight);
    });
  }

  private initHeroMiniChart(): void {
    if (typeof Chart === 'undefined') return;
    const ctx = this.heroMiniChartRef?.nativeElement?.getContext('2d');
    if (!ctx) return;

    const dieselGrad = ctx.createLinearGradient(0, 0, 0, 140);
    dieselGrad.addColorStop(0, 'rgba(30, 64, 175, 0.25)');
    dieselGrad.addColorStop(1, 'rgba(30, 64, 175, 0.02)');
    const petrolGrad = ctx.createLinearGradient(0, 0, 0, 140);
    petrolGrad.addColorStop(0, 'rgba(245, 158, 11, 0.25)');
    petrolGrad.addColorStop(1, 'rgba(245, 158, 11, 0.02)');

    new Chart(ctx, {
      type: 'line',
      data: {
        labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
        datasets: [
          { label: 'Diesel', data: [420, 380, 445, 490, 520, 480, 510, 540, 505, 560, 590, 620], borderColor: '#1E40AF', backgroundColor: dieselGrad, borderWidth: 2, fill: true, tension: 0.4, pointRadius: 0 },
          { label: 'Petrol', data: [310, 340, 365, 390, 410, 385, 450, 470, 430, 485, 510, 530], borderColor: '#F59E0B', backgroundColor: petrolGrad, borderWidth: 2, fill: true, tension: 0.4, pointRadius: 0 },
        ],
      },
      options: {
        responsive: true, maintainAspectRatio: false,
        interaction: { intersect: false, mode: 'index' },
        plugins: {
          legend: { display: true, position: 'top', labels: { font: { size: 10, family: 'Inter' }, boxWidth: 10, padding: 8 } },
          tooltip: { backgroundColor: 'rgba(15,23,42,0.9)', titleFont: { family: 'Inter', size: 11 }, bodyFont: { family: 'Inter', size: 11 }, cornerRadius: 8, padding: 8 },
        },
        scales: {
          x: { grid: { display: false }, ticks: { font: { size: 9 }, maxRotation: 0 }, border: { display: false } },
          y: { grid: { color: 'rgba(0,0,0,0.04)' }, ticks: { font: { size: 9 } }, border: { display: false } },
        },
      },
    });
  }

  private initNetwork3D(): void {
    if (typeof THREE === 'undefined') return;
    const canvas = this.networkCanvasRef?.nativeElement;
    if (!canvas) return;

    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(60, canvas.clientWidth / canvas.clientHeight, 0.1, 1000);
    const renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true });
    renderer.setSize(canvas.clientWidth, canvas.clientHeight);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));

    scene.add(new THREE.AmbientLight(0xffffff, 0.4));
    const dl = new THREE.DirectionalLight(0x3B82F6, 1);
    dl.position.set(5, 5, 5);
    scene.add(dl);

    const stations: { mesh: any; isHub: boolean }[] = [];
    const stationCount = 60;

    for (let i = 0; i < stationCount; i++) {
      const isHub = i < 5;
      const size = isHub ? 0.25 : 0.08 + Math.random() * 0.08;
      const color = isHub ? 0xF59E0B : (Math.random() > 0.3 ? 0x3B82F6 : 0x10B981);
      const geo = new THREE.SphereGeometry(size, 12, 12);
      const mat = new THREE.MeshPhongMaterial({ color, emissive: color, emissiveIntensity: isHub ? 0.6 : 0.3 });
      const mesh = new THREE.Mesh(geo, mat);
      mesh.position.set((Math.random() - 0.5) * 8, (Math.random() - 0.5) * 6, (Math.random() - 0.5) * 4);
      scene.add(mesh);
      stations.push({ mesh, isHub });
    }

    const connMat = new THREE.LineBasicMaterial({ color: 0x3B82F6, transparent: true, opacity: 0.12 });
    const connections: { from: number; to: number }[] = [];
    for (let i = 0; i < stations.length; i++) {
      const nearest = stations
        .map((s, j) => ({ j, dist: stations[i].mesh.position.distanceTo(s.mesh.position) }))
        .filter((s) => s.j !== i)
        .sort((a, b) => a.dist - b.dist)
        .slice(0, stations[i].isHub ? 8 : 3);

      nearest.forEach((n) => {
        const geo = new THREE.BufferGeometry().setFromPoints([stations[i].mesh.position, stations[n.j].mesh.position]);
        const line = new THREE.Line(geo, connMat);
        scene.add(line);
        connections.push({ from: i, to: n.j });
      });
    }

    const pulseGeo = new THREE.SphereGeometry(0.04, 8, 8);
    const pulses: { mesh: any; from: number; to: number; t: number }[] = [];
    for (let i = 0; i < 15; i++) {
      const pulseMat = new THREE.MeshBasicMaterial({ color: 0xFCD34D });
      const pulse = new THREE.Mesh(pulseGeo, pulseMat);
      scene.add(pulse);
      const conn = connections[Math.floor(Math.random() * connections.length)];
      pulses.push({ mesh: pulse, from: conn.from, to: conn.to, t: Math.random() });
    }

    camera.position.z = 8;

    const animate = () => {
      const id = requestAnimationFrame(animate);
      this.animationFrames.push(id);
      const time = Date.now() * 0.001;
      scene.rotation.y += 0.001;

      stations.forEach((s, i) => {
        if (s.isHub) s.mesh.scale.setScalar(1 + 0.15 * Math.sin(time * 1.5 + i));
      });

      pulses.forEach((p) => {
        p.t += 0.008;
        if (p.t > 1) {
          p.t = 0;
          const conn = connections[Math.floor(Math.random() * connections.length)];
          p.from = conn.from;
          p.to = conn.to;
        }
        const from = stations[p.from].mesh.position;
        const to = stations[p.to].mesh.position;
        p.mesh.position.lerpVectors(from, to, p.t);
        (p.mesh.material as any).opacity = Math.sin(p.t * Math.PI);
      });

      renderer.render(scene, camera);
    };
    animate();

    window.addEventListener('resize', () => {
      if (!canvas.clientWidth) return;
      camera.aspect = canvas.clientWidth / canvas.clientHeight;
      camera.updateProjectionMatrix();
      renderer.setSize(canvas.clientWidth, canvas.clientHeight);
    });
  }

  private initAiMiniChart(): void {
    if (typeof Chart === 'undefined') return;
    const ctx = this.aiMiniChartRef?.nativeElement?.getContext('2d');
    if (!ctx) return;

    new Chart(ctx, {
      type: 'bar',
      data: {
        labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
        datasets: [
          {
            label: 'Revenue (₹L)',
            data: [45, 52, 48, 61, 55, 67, 58],
            backgroundColor: 'rgba(30, 64, 175, 0.7)',
            borderRadius: 4,
            barThickness: 12,
          },
        ],
      },
      options: {
        responsive: true, maintainAspectRatio: false,
        plugins: { legend: { display: false }, tooltip: { backgroundColor: 'rgba(15,23,42,0.9)', cornerRadius: 8, padding: 8 } },
        scales: {
          x: { grid: { display: false }, ticks: { font: { size: 10 } }, border: { display: false } },
          y: { grid: { color: 'rgba(0,0,0,0.06)' }, ticks: { font: { size: 10 } }, border: { display: false } },
        },
      },
    });
  }

  private initDashboardCharts(): void {
    if (typeof Chart === 'undefined') return;

    // Sales chart
    const salesCtx = this.dashSalesChartRef?.nativeElement?.getContext('2d');
    if (salesCtx) {
      const grad = salesCtx.createLinearGradient(0, 0, 0, 220);
      grad.addColorStop(0, 'rgba(30, 64, 175, 0.2)');
      grad.addColorStop(1, 'rgba(30, 64, 175, 0.01)');

      new Chart(salesCtx, {
        type: 'line',
        data: {
          labels: ['6am', '8am', '10am', '12pm', '2pm', '4pm', '6pm', '8pm'],
          datasets: [{
            label: 'Revenue (₹L)',
            data: [12, 28, 45, 62, 55, 70, 48, 35],
            borderColor: '#1E40AF', backgroundColor: grad,
            borderWidth: 2, fill: true, tension: 0.4, pointRadius: 3, pointBackgroundColor: '#1E40AF',
          }],
        },
        options: {
          responsive: true, maintainAspectRatio: false,
          plugins: { legend: { display: false }, tooltip: { backgroundColor: 'rgba(15,23,42,0.9)', cornerRadius: 8 } },
          scales: {
            x: { grid: { display: false }, ticks: { color: '#64748B', font: { size: 11 } } },
            y: { grid: { color: '#E2E8F0' }, ticks: { color: '#64748B', font: { size: 11 } } },
          },
        },
      });
    }

    // Station chart (donut)
    const stationCtx = this.dashStationChartRef?.nativeElement?.getContext('2d');
    if (stationCtx) {
      new Chart(stationCtx, {
        type: 'doughnut',
        data: {
          labels: ['Online', 'Maintenance', 'Offline'],
          datasets: [{
            data: [412, 28, 7],
            backgroundColor: ['#10B981', '#F59E0B', '#EF4444'],
            borderWidth: 0, hoverOffset: 6,
          }],
        },
        options: {
          responsive: true, maintainAspectRatio: false,
          cutout: '65%',
          plugins: {
            legend: { position: 'bottom', labels: { color: '#64748B', font: { size: 11 }, boxWidth: 12, padding: 12 } },
          },
        },
      });
    }
  }
}
