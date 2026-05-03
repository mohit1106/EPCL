import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PendingOffloadsComponent } from './pending-offloads.component';

describe('PendingOffloadsComponent', () => {
  let component: PendingOffloadsComponent;
  let fixture: ComponentFixture<PendingOffloadsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [PendingOffloadsComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(PendingOffloadsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
