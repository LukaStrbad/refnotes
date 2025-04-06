import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AskModalComponent } from './ask-modal.component';

describe('AskModalComponent', () => {
  let component: AskModalComponent;
  let fixture: ComponentFixture<AskModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AskModalComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AskModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
