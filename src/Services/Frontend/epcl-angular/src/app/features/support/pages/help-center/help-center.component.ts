import { Component } from '@angular/core';
@Component({ selector: 'app-help-center', templateUrl: './help-center.component.html', styleUrls: ['./help-center.component.scss'] })
export class HelpCenterComponent {
  domains = [
    { icon: 'lock', name: 'Account', desc: 'Manage terminal credentials, authentication protocols, and access levels.', linkColor: '' },
    { icon: 'clipboard-list', name: 'Billing', desc: 'Invoice reconciliation, automated payment processing, and ledger history.', linkColor: '' },
    { icon: 'fuel', name: 'Station Issues', desc: 'Hardware diagnostics, pump sensor failures, and infrastructure monitoring.', linkColor: 'purple' },
    { icon: 'trending-up', name: 'Loyalty', desc: 'Privilege tier management, rewards redemption, and partner benefits.', linkColor: 'gold' },
  ];
  trending = [
    { ref: 'Q4-REF-012', title: 'How to redeem rewards?', desc: 'Step-by-step terminal authentication for digital vouchers and fuel discounts.' },
    { ref: 'Q4-REF-089', title: 'What is the EPCL Privilege tier?', desc: 'Overview of elite membership benefits, high-volume rebates, and priority dispatch.' },
  ];
  featured = { icon: 'cpu', title: 'Updating Terminal Firmware', desc: 'Critical safety protocols for over-the-air system patches.' };
  agentsActive = 12;
}
