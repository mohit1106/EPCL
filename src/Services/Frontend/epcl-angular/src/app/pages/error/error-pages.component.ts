import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-not-found',
  template: `
    <div class="error-page">
      <div class="error-left">
        <div class="error-code-wrap">
          <span class="error-code">404</span>
          <span class="signal-tag mono">SIGNAL_LOST</span>
          <span class="error-tag mono">E-404-UNREACHABLE</span>
        </div>
      </div>
      <div class="error-right">
        <h1 class="error-title">Intelligence Signal Lost</h1>
        <p class="error-sub mono">OPERATIONAL NODE NOT FOUND</p>
        <p class="error-desc">The requested intelligence stream is currently unavailable. This may be due to a decommissioned data node or a temporary synchronization lapse in the PetroCore network.</p>
        <a class="btn-return" routerLink="/"><span>Return to Command Center</span> <span class="arrow">→</span></a>
        <div class="system-status"><span class="status-icon">⊡</span> <span>System Status: <strong>Active</strong></span></div>
      </div>
    </div>
    <div class="error-alert">
      <span class="alert-icon">📊</span>
      <div><strong>Data Integrity Alert</strong><p>Verification of upstream telemetry failed. Attempting automated reroute through secondary logic gates.</p></div>
    </div>
    <div class="error-footer mono">
      <span><span class="dot-green">●</span> SYSTEM LATENCY: 14MS</span>
      <span>● NODE ID: OB-9912-X</span>
      <span>© 2024 EPCL INTELLIGENCE SERVICES | SECURITY PROTOCOL: ENCRYPTED</span>
    </div>
  `,
  styles: [`
    :host { display: block; min-height: 100vh; background: var(--bg-primary); padding: 0; }
    .error-page { display: grid; grid-template-columns: 1fr 1fr; min-height: 70vh; align-items: center; padding: 60px; gap: 40px; }
    .error-left { position: relative; }
    .error-code-wrap { position: relative; }
    .error-code { font-size: 220px; font-weight: 900; background: linear-gradient(180deg, rgba(99,102,241,0.4), rgba(99,102,241,0.05)); -webkit-background-clip: text; -webkit-text-fill-color: transparent; background-clip: text; line-height: 1; letter-spacing: -8px; }
    .signal-tag { position: absolute; top: 30%; right: 0; font-size: 10px; background: rgba(30,30,35,0.8); padding: 4px 10px; color: var(--text-muted); }
    .error-tag { position: absolute; bottom: 20%; left: 10%; font-size: 10px; background: rgba(239,68,68,0.2); color: var(--color-danger); padding: 4px 10px; }
    .error-title { font-size: 42px; font-weight: 700; line-height: 1.1; margin-bottom: 8px; }
    .error-sub { font-size: 12px; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.15em; margin-bottom: 20px; }
    .error-desc { font-size: 14px; color: var(--text-muted); line-height: 1.7; margin-bottom: 32px; max-width: 400px; }
    .btn-return { display: inline-flex; align-items: center; gap: 12px; padding: 16px 32px; background: var(--color-primary); color: white; text-decoration: none; border-radius: var(--radius-md); font-size: 14px; font-weight: 700; transition: filter 0.2s; }
    .btn-return:hover { filter: brightness(1.15); }
    .arrow { font-size: 18px; }
    .system-status { margin-top: 24px; font-size: 13px; color: var(--text-muted); display: flex; align-items: center; gap: 8px; }
    .status-icon { font-size: 16px; }
    .error-alert { position: fixed; bottom: 80px; right: 40px; background: var(--bg-surface); border: 1px solid var(--border-card); border-radius: var(--radius-md); padding: 16px 20px; display: flex; gap: 12px; max-width: 320px; }
    .alert-icon { font-size: 24px; flex-shrink: 0; }
    .error-alert strong { font-size: 13px; display: block; margin-bottom: 4px; }
    .error-alert p { font-size: 11px; color: var(--text-muted); line-height: 1.5; margin: 0; }
    .error-footer { position: fixed; bottom: 0; left: 0; right: 0; display: flex; justify-content: space-between; padding: 16px 60px; font-size: 10px; color: var(--text-muted); border-top: 1px solid rgba(70,69,84,0.1); }
    .dot-green { color: var(--color-success); }
    .mono { font-family: 'JetBrains Mono', monospace; }
    @media (max-width: 768px) { .error-page { grid-template-columns: 1fr; padding: 32px; } .error-code { font-size: 120px; } }
  `],
})
export class NotFoundComponent {}

@Component({
  selector: 'app-unauthorized',
  template: `
    <div class="error-page">
      <div class="error-left">
        <div class="error-code-wrap">
          <span class="error-code">403</span>
          <span class="error-tag mono">ACCESS_DENIED</span>
        </div>
      </div>
      <div class="error-right">
        <h1 class="error-title">Clearance Required</h1>
        <p class="error-sub mono">INSUFFICIENT ACCESS PRIVILEGES</p>
        <p class="error-desc">Your current credential matrix does not authorize access to this intelligence sector. Contact your system administrator to request elevated privileges.</p>
        <a class="btn-return" routerLink="/"><span>Return to Command Center</span> <span class="arrow">→</span></a>
      </div>
    </div>
  `,
  styles: [`
    :host { display: block; min-height: 100vh; background: var(--bg-primary); }
    .error-page { display: grid; grid-template-columns: 1fr 1fr; min-height: 80vh; align-items: center; padding: 60px; gap: 40px; }
    .error-left { position: relative; }
    .error-code-wrap { position: relative; }
    .error-code { font-size: 220px; font-weight: 900; background: linear-gradient(180deg, rgba(239,68,68,0.4), rgba(239,68,68,0.05)); -webkit-background-clip: text; -webkit-text-fill-color: transparent; background-clip: text; line-height: 1; letter-spacing: -8px; }
    .error-tag { position: absolute; bottom: 20%; left: 10%; font-size: 10px; background: rgba(239,68,68,0.2); color: var(--color-danger); padding: 4px 10px; }
    .error-title { font-size: 42px; font-weight: 700; line-height: 1.1; margin-bottom: 8px; }
    .error-sub { font-size: 12px; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.15em; margin-bottom: 20px; }
    .error-desc { font-size: 14px; color: var(--text-muted); line-height: 1.7; margin-bottom: 32px; max-width: 400px; }
    .btn-return { display: inline-flex; align-items: center; gap: 12px; padding: 16px 32px; background: var(--color-danger); color: white; text-decoration: none; border-radius: 8px; font-size: 14px; font-weight: 700; }
    .arrow { font-size: 18px; }
    .mono { font-family: 'JetBrains Mono', monospace; }
    @media (max-width: 768px) { .error-page { grid-template-columns: 1fr; padding: 32px; } .error-code { font-size: 120px; } }
  `],
})
export class UnauthorizedComponent {}
