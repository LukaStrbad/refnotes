import { Component, OnInit } from '@angular/core';
import { FavoriteService } from '../../services/favorite.service';
import { FileFavoriteDetails } from '../../model/file-favorite-details';
import { DirectoryFavoriteDetails } from '../../model/directory-favorite-details';
import { NotificationService } from '../../services/notification.service';
import { TranslateDirective, TranslateService } from '@ngx-translate/core';
import { getTranslation } from '../../utils/translation-utils';
import { LoadingState } from '../../model/loading-state';
import { LoggerService } from '../../services/logger.service';
import { FavoriteFileItemComponent } from "./favorite-file-item/favorite-file-item.component";
import { FavoriteDirectoryItemComponent } from "./favorite-directory-item/favorite-directory-item.component";
import { TestTagDirective } from '../../directives/test-tag.directive';

@Component({
  selector: 'app-favorites',
  imports: [TranslateDirective, FavoriteFileItemComponent, FavoriteDirectoryItemComponent, TestTagDirective],
  templateUrl: './favorites.component.html',
  styleUrl: './favorites.component.css'
})
export class FavoritesComponent implements OnInit {
  fileFavorites: FileFavoriteDetails[] = [];
  directoryFavorites: DirectoryFavoriteDetails[] = [];
  favoriteCount = 0;
  loadingState = LoadingState.Loading;

  LoadingState = LoadingState;

  constructor(
    private favoriteService: FavoriteService,
    private translate: TranslateService,
    private notificationService: NotificationService,
    private log: LoggerService,
  ) { }

  ngOnInit(): void {
    (async () => {
      const fileFavoritesPromise = this.favoriteService.getFavoriteFiles();
      const directoryFavoritesPromise = this.favoriteService.getFavoriteDirectories();
      const combinedPromise = Promise.all([fileFavoritesPromise, directoryFavoritesPromise]);

      try {
        await this.notificationService.awaitAndNotifyError(combinedPromise, {
          default: await getTranslation(this.translate, 'favorites.error.loading'),
        });
      } catch (error) {
        this.loadingState = LoadingState.Error;
        this.log.error('Error loading favorites', error);
        return;
      }

      this.fileFavorites = await fileFavoritesPromise;
      this.directoryFavorites = await directoryFavoritesPromise;
      this.favoriteCount = this.fileFavorites.length + this.directoryFavorites.length;
      this.loadingState = LoadingState.Loaded;
    })();
  }

  async onRemoveFileFavorite(favorite: FileFavoriteDetails): Promise<void> {
    try {
      await this.favoriteService.unfavoriteFile(favorite.fileInfo.path, favorite.group?.id);
      this.fileFavorites = this.fileFavorites.filter(f => f !== favorite);
      this.favoriteCount = this.fileFavorites.length + this.directoryFavorites.length;

      const successMessage = await getTranslation(this.translate, 'favorites.success.removed');
      this.notificationService.success(successMessage);
    } catch (error) {
      this.log.error('Error removing file favorite', error);
      const errorMessage = await getTranslation(this.translate, 'favorites.error.removing');
      this.notificationService.error(errorMessage);
    }
  }

  async onRemoveDirectoryFavorite(favorite: DirectoryFavoriteDetails): Promise<void> {
    try {
      await this.favoriteService.unfavoriteDirectory(favorite.path, favorite.group?.id);
      this.directoryFavorites = this.directoryFavorites.filter(f => f !== favorite);
      this.favoriteCount = this.fileFavorites.length + this.directoryFavorites.length;

      const successMessage = await getTranslation(this.translate, 'favorites.success.removed');
      this.notificationService.success(successMessage);
    } catch (error) {
      this.log.error('Error removing directory favorite', error);
      const errorMessage = await getTranslation(this.translate, 'favorites.error.removing');
      this.notificationService.error(errorMessage);
    }
  }

}
