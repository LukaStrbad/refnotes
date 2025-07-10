import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FavoriteFileItemComponent } from './favorite-file-item.component';
import { TranslateFakeLoader, TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { ActivatedRoute } from '@angular/router';
import { FileFavoriteDetails } from '../../../model/file-favorite-details';
import { click } from '../../../tests/click-utils';

describe('FavoriteFileItemComponent', () => {
  let component: FavoriteFileItemComponent;
  let fixture: ComponentFixture<FavoriteFileItemComponent>;
  let nativeElement: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        FavoriteFileItemComponent,
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
        },
      ],
    })
      .compileComponents();
  });

  function createFavoriteDetails(name: string): FileFavoriteDetails {
    return {
      fileInfo: {
        name: name,
        path: `/${name}`,
        size: 1234,
        tags: [],
        modified: new Date(),
        created: new Date(),
      },
      favoriteDate: new Date(),
    };
  };

  function createFixture(favorite: FileFavoriteDetails) {
    fixture = TestBed.createComponent(FavoriteFileItemComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('favorite', favorite);
    fixture.detectChanges();

    nativeElement = fixture.nativeElement as HTMLElement;
  }

  it('should create', () => {
    createFixture(createFavoriteDetails('test-file.md'));
    expect(component).toBeTruthy();
  });

  it('should diplay edit button for editable filex', () => {
    const favorite = createFavoriteDetails('test-file.md');
    createFixture(favorite);

    const editButton = nativeElement.querySelector('[data-test="favorite-file.button.edit"]');

    expect(editButton).toBeTruthy();
  });

  it('should not display edit button for non-editable files', () => {
    // Images are not editable
    const favorite = createFavoriteDetails('test-file.png');
    createFixture(favorite);

    const editButton = nativeElement.querySelector('[data-test="favorite-file.button.edit"]');

    expect(editButton).toBeFalsy();
  });

  it('should send removeFavorite event when remove button is clicked', () => {
    const favorite = createFavoriteDetails('test-file.md');
    createFixture(favorite);

    const emitSpy = spyOn(component.removeFavorite, 'emit');

    const removeButton = nativeElement.querySelector('[data-test="favorite-file.button.remove"]') as HTMLButtonElement;
    click(removeButton);

    expect(emitSpy).toHaveBeenCalledWith(favorite);
  });
});
