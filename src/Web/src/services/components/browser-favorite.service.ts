import { Injectable, signal } from '@angular/core';
import { FavoriteService } from '../favorite.service';
import { FileFavoriteDetails } from '../../model/file-favorite-details';
import { DirectoryFavoriteDetails } from '../../model/directory-favorite-details';
import { LoggerService } from '../logger.service';
import { File } from '../../model/file';

@Injectable({
  providedIn: 'root'
})
export class BrowserFavoriteService {
  private readonly _fileFavorites = signal<FileFavoriteDetails[]>([]);
  private readonly _directoryFavorites = signal<DirectoryFavoriteDetails[]>([]);

  readonly fileFavorites = this._fileFavorites.asReadonly();
  readonly directoryFavorites = this._directoryFavorites.asReadonly();

  private groupId?: number;

  constructor(
    private favoriteService: FavoriteService,
    private logger: LoggerService,
  ) {
    Promise.all([
      this.favoriteService.getFavoriteFiles(),
      this.favoriteService.getFavoriteDirectories(),
    ]).then(([fileFavorites, directoryFavorites]) => {
      this._fileFavorites.set(fileFavorites);
      this._directoryFavorites.set(directoryFavorites);
    }).catch((error) => {
      this.logger.error('Error loading favorites', error);
    });
  }

  setGroupId(groupId: number) {
    this.groupId = groupId;
  }

  /**
   * Favorites a file in the current group.
   * @param file File to favorite
   */
  async favoriteFile(file: File) {
    await this.favoriteService.favoriteFile(file.path, this.groupId);

    // Add to local favorites
    this._fileFavorites.update(favorites => [
      ...favorites,
      {
        fileInfo: file,
        groupId: this.groupId,
        favoriteDate: new Date(),
      }
    ]);
  }

  /**
   * Removes a file from favorites in the current group.
   * This just removes it locally, not from the server.
   * @param file File to remove from favorites
   */
  removeLocalFileFavorite(file: File) {
    this._fileFavorites.update(favorites => favorites.filter(fav => fav.fileInfo.path !== file.path || fav.groupId !== this.groupId));
    this.favoriteService.clearFileFavoritesCache();
  }

  /**
   * Unfavorites a file in the current group.
   * This will remove it from the server and also from local favorites.
   * @param file File to unfavorite
   */
  async unfavoriteFile(file: File) {
    await this.favoriteService.unfavoriteFile(file.path, this.groupId);

    // Remove from local favorites
    this.removeLocalFileFavorite(file);
  }

  /**
   * Favorites a directory in the current group.
   * @param path Path of the directory to favorite
   */
  async favoriteDirectory(path: string) {
    await this.favoriteService.favoriteDirectory(path, this.groupId);

    // Add to local favorites
    this._directoryFavorites.update(favorites => [
      ...favorites,
      {
        path: path,
        groupId: this.groupId,
        favoriteDate: new Date(),
      }
    ]);
  }

  /**
   * Removes a directory from favorites in the current group.
   * This just removes it locally, not from the server.
   * @param path Path of the directory to remove from favorites
   */
  removeLocalDirectoryFavorite(path: string) {
    this._directoryFavorites.update(favorites => favorites.filter(fav => fav.path !== path || fav.groupId !== this.groupId));
    this.favoriteService.clearDirectoryFavoritesCache();
  }

  /**
   * Unfavorites a directory in the current group.
   * This will remove it from the server and also from local favorites.
   * @param path Path of the directory to unfavorite
   */
  async unfavoriteDirectory(path: string) {
    await this.favoriteService.unfavoriteDirectory(path, this.groupId);

    // Remove from local favorites
    this.removeLocalDirectoryFavorite(path);
  }
}
