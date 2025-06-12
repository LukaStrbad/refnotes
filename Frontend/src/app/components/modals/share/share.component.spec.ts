import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShareModalComponent } from './share.component';

describe('ShareComponent', () => {
  let component: ShareModalComponent;
  let fixture: ComponentFixture<ShareModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShareModalComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShareModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
