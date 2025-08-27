import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FavoritesComponent } from './favorites.component';
import { FavoriteService } from '../../services/favorite.service';
import { TranslateFakeLoader, TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { createDirectoryFavoriteDetails, createFileFavoriteDetails } from '../../tests/favorite-utils';
import { LoadingState } from '../../model/loading-state';
import { ActivatedRoute } from '@angular/router';

describe('FavoritesComponent', () => {
  let component: FavoritesComponent;
  let fixture: ComponentFixture<FavoritesComponent>;
  let favoriteService: jasmine.SpyObj<FavoriteService>;
  let nativeElement: HTMLElement;

  beforeEach(async () => {
    favoriteService = jasmine.createSpyObj<FavoriteService>('FavoriteService', ['getFavoriteFiles', 'getFavoriteDirectories', 'unfavoriteFile', 'unfavoriteDirectory']);
    favoriteService.getFavoriteFiles.and.resolveTo([]);
    favoriteService.getFavoriteDirectories.and.resolveTo([]);

    await TestBed.configureTestingModule({
      imports: [
        FavoritesComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
      providers: [
        { provide: FavoriteService, useValue: favoriteService },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: {} } } },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(FavoritesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    nativeElement = fixture.nativeElement as HTMLElement;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display file favorites', async () => {
    const fileFavorites = [
      createFileFavoriteDetails('test1.md'),
      createFileFavoriteDetails('test2.md', { id: 123, name: 'Group 1' }),
    ];
    favoriteService.getFavoriteFiles.and.resolveTo(fileFavorites);

    component.ngOnInit();
    await fixture.whenStable();
    fixture.detectChanges();

    const fileItems = nativeElement.querySelectorAll('[data-test="favorites.file-item"]');
    expect(component.loadingState).toBe(LoadingState.Loaded);
    expect(component.favoriteCount).toBe(2);
    expect(fileItems.length).toBe(2);
    expect(fileItems[0].textContent).toContain('test1.md');
    expect(fileItems[1].textContent).toContain('test2.md');
  });

  it('should display directory favorites', async () => {
    const directoryFavorites = [
      createDirectoryFavoriteDetails('/path/to/dir1'),
      createDirectoryFavoriteDetails('/path/to/dir2', { id: 456, name: 'Group 2' }),
    ];
    favoriteService.getFavoriteDirectories.and.resolveTo(directoryFavorites);

    component.ngOnInit();
    await fixture.whenStable();
    fixture.detectChanges();

    const directoryItems = nativeElement.querySelectorAll('[data-test="favorites.directory-item"]');
    expect(component.loadingState).toBe(LoadingState.Loaded);
    expect(component.favoriteCount).toBe(2);
    expect(directoryItems.length).toBe(2);
    expect(directoryItems[0].textContent).toContain('dir1');
    expect(directoryItems[1].textContent).toContain('dir2');
  });

  it('should call unfavoriteFile on removeFavorite', async () => {
    const favorite = createFileFavoriteDetails('test-file.md');
    favoriteService.getFavoriteFiles.and.resolveTo([favorite]);

    component.ngOnInit();
    await fixture.whenStable();
    fixture.detectChanges();

    component.onRemoveFileFavorite(favorite);
    await fixture.whenStable();

    expect(favoriteService.unfavoriteFile).toHaveBeenCalledWith(favorite.fileInfo.path, favorite.group?.id);
    expect(component.fileFavorites.length).toBe(0);
    expect(component.favoriteCount).toBe(0);
  });

  it('should call unfavoriteDirectory on removeFavorite', async () => {
    const favorite = createDirectoryFavoriteDetails('/path/to/dir');
    favoriteService.getFavoriteDirectories.and.resolveTo([favorite]);

    component.ngOnInit();
    await fixture.whenStable();
    fixture.detectChanges();

    component.onRemoveDirectoryFavorite(favorite);
    await fixture.whenStable();

    expect(favoriteService.unfavoriteDirectory).toHaveBeenCalledWith(favorite.path, favorite.group?.id);
    expect(component.directoryFavorites.length).toBe(0);
    expect(component.favoriteCount).toBe(0);
  });
});
