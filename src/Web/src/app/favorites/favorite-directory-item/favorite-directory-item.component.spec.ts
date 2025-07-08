import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FavoriteDirectoryItemComponent } from './favorite-directory-item.component';

describe('FavoriteDirectoryItemComponent', () => {
  let component: FavoriteDirectoryItemComponent;
  let fixture: ComponentFixture<FavoriteDirectoryItemComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FavoriteDirectoryItemComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FavoriteDirectoryItemComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
