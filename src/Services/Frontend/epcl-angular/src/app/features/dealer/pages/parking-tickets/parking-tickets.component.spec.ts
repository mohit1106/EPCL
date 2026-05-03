import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ParkingTicketsComponent } from './parking-tickets.component';

describe('ParkingTicketsComponent', () => {
  let component: ParkingTicketsComponent;
  let fixture: ComponentFixture<ParkingTicketsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ParkingTicketsComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(ParkingTicketsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
