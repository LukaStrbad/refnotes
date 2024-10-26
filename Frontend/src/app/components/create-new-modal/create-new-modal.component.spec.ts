import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateNewModalComponent } from './create-new-modal.component';

describe('CreateNewModalComponent', () => {
  let component: CreateNewModalComponent;
  let fixture: ComponentFixture<CreateNewModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateNewModalComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CreateNewModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
