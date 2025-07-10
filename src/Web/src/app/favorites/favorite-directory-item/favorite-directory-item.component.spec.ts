import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FavoriteDirectoryItemComponent } from './favorite-directory-item.component';
import { TranslateFakeLoader, TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { ActivatedRoute } from '@angular/router';
import { DirectoryFavoriteDetails } from '../../../model/directory-favorite-details';
import { click } from '../../../tests/click-utils';
import { createDirectoryFavoriteDetails } from '../../../tests/favorite-utils';

describe('FavoriteDirectoryItemComponent', () => {
  let component: FavoriteDirectoryItemComponent;
  let fixture: ComponentFixture<FavoriteDirectoryItemComponent>;
  let nativeElement: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        FavoriteDirectoryItemComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: {} } },
        }
      ]
    })
      .compileComponents();
  });

  function createFixture(favorite: DirectoryFavoriteDetails) {
    fixture = TestBed.createComponent(FavoriteDirectoryItemComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('favorite', favorite);
    fixture.detectChanges();

    nativeElement = fixture.nativeElement as HTMLElement;
  }

  it('should create', () => {
    createFixture(createDirectoryFavoriteDetails('/test-directory'));
    expect(component).toBeTruthy();
  });

  it('should send removeFavorite event on button click', () => {
    const favorite = createDirectoryFavoriteDetails('/test-directory');
    createFixture(favorite);

    spyOn(component.removeFavorite, 'emit');

    const removeButton = nativeElement.querySelector('[data-test="favorite-directory.button.remove"]') as HTMLButtonElement;
    click(removeButton);

    expect(component.removeFavorite.emit).toHaveBeenCalledWith(favorite);
  });
});
