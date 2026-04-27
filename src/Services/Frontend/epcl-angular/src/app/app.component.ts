import { Component, OnInit } from '@angular/core';
import { Store } from '@ngrx/store';
import { restoreSession } from './store/auth/auth.actions';

@Component({
  selector: 'app-root',
  template: `
    <epcl-app-shell></epcl-app-shell>
    <epcl-toast-container></epcl-toast-container>
  `,
  styles: [],
})
export class AppComponent implements OnInit {
  constructor(private store: Store) {}

  ngOnInit(): void {
    // Restore session from localStorage on app init
    this.store.dispatch(restoreSession());
  }
}
