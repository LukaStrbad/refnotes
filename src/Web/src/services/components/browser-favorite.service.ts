import { Injectable } from '@angular/core';
import { FavoriteService } from '../favorite.service';
import { FileFavoriteDetails } from '../../model/file-favorite-details';
import { DirectoryFavoriteDetails } from '../../model/directory-favorite-details';
import { LoggerService } from '../logger.service';
import { File } from '../../model/file';
import { LRUCache } from 'lru-cache';

@Injectable({
  providedIn: 'root'
})
export class BrowserFavoriteService {
  private fileFavorites: FileFavoriteDetails[] | null = null;
  private directoryFavorites: DirectoryFavoriteDetails[] | null = null;
  private readonly favoriteFileCache = new LRUCache<File, boolean>({
    max: 100,
    ttl: 60000, // 1 minute
  });
  private readonly favoriteDirectoryCache = new LRUCache<string, boolean>({
    max: 100,
    ttl: 60000, // 1 minute
  });

  private groupId?: number;

  constructor(
    private favoriteService: FavoriteService,
    private logger: LoggerService,
  ) {
    Promise.all([
      this.favoriteService.getFavoriteFiles(),
      this.favoriteService.getFavoriteDirectories(),
    ]).then(([fileFavorites, directoryFavorites]) => {
      this.logger.info('Loaded favorites', { fileFavorites, directoryFavorites });
      this.fileFavorites = fileFavorites;
      this.directoryFavorites = directoryFavorites;
    }).catch((error) => {
      this.logger.error('Error loading favorites', error);
    });
  }

  setGroupId(groupId: number) {
    this.groupId = groupId;
  }

  async favoriteFile(file: File) {
    await this.favoriteService.favoriteFile(file.path, this.groupId);

    // Add to local favorites
    this.fileFavorites?.push({
      fileInfo: file,
      groupId: this.groupId,
      favoriteDate: new Date(),
    });
  }

  async unfavoriteFile(file: File) {
    await this.favoriteService.unfavoriteFile(file.path, this.groupId);

    // Remove from local favorites
    const index = this.fileFavorites?.findIndex(fav => fav.fileInfo.path === file.path && fav.groupId === this.groupId);
    if (index !== -1 && index !== undefined) {
      this.fileFavorites?.splice(index, 1);
    }
  }

  async favoriteDirectory(path: string) {
    await this.favoriteService.favoriteDirectory(path, this.groupId);

    // Add to local favorites
    this.directoryFavorites?.push({
      directoryPath: path,
      groupId: this.groupId,
      favoriteDate: new Date(),
    });
  }

  async unfavoriteDirectory(path: string) {
    await this.favoriteService.unfavoriteDirectory(path, this.groupId);

    // Remove from local favorites
    const index = this.directoryFavorites?.findIndex(fav => fav.directoryPath === path && fav.groupId === this.groupId);
    if (index !== -1 && index !== undefined) {
      this.directoryFavorites?.splice(index, 1);
    }
  }

  isFavoriteFile(file: File): boolean {
    if (this.fileFavorites === null) {
      return false;
    }
    // Check cache first
    let isFavorite = this.favoriteFileCache.get(file);
    // console.log(`Checking favorite for ${file.path}: ${isFavorite}`)
    if (isFavorite !== undefined) {
      return isFavorite;
    }

    isFavorite = this.fileFavorites.some(fav => fav.fileInfo.path === file.path && fav.groupId === this.groupId);
    this.favoriteFileCache.set(file, isFavorite);
    // console.log(`File favorite cache updated for ${file.path}: ${isFavorite}`);
    return isFavorite;
  }

  isFavoriteDirectory(path: string): boolean {
    if (this.directoryFavorites === null) {
      return false;
    }

    // Check cache first
    let isFavorite = this.favoriteDirectoryCache.get(path);
    if (isFavorite !== undefined) {
      return isFavorite;
    }

    isFavorite = this.directoryFavorites.some(fav => fav.directoryPath === path && fav.groupId === this.groupId);
    this.favoriteDirectoryCache.set(path, isFavorite);
    return isFavorite;
  }
}
