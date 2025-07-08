import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FavoriteFileItemComponent } from './favorite-file-item.component';

describe('FavoriteFileItemComponent', () => {
  let component: FavoriteFileItemComponent;
  let fixture: ComponentFixture<FavoriteFileItemComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FavoriteFileItemComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FavoriteFileItemComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
