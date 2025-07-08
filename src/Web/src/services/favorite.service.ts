import { Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom, merge, Observable, of, tap } from 'rxjs';
import { generateHttpParams } from '../utils/http-utils';
import { DirectoryFavoriteDetails } from '../model/directory-favorite-details';
import { FileFavoriteDetails } from '../model/file-favorite-details';
import { SimpleCache } from './utils/simple-cache';

const apiUrl = environment.apiUrl + '/favorite';

@Injectable({
  providedIn: 'root'
})
export class FavoriteService {
  private readonly fileFavoriteCache = new SimpleCache<FileFavoriteDetails[]>(60000);
  private readonly directoryFavoriteCache = new SimpleCache<DirectoryFavoriteDetails[]>(60000);

  constructor(
    private http: HttpClient,
  ) { }

  async favoriteFile(filePath: string, groupId?: number): Promise<void> {
    const params = generateHttpParams({
      filePath: filePath,
      groupId: groupId,
    });

    await firstValueFrom(this.http.post<void>(`${apiUrl}/favoriteFile`, null, { params }));
    // Invalidate the cache after favoriting a file
    this.fileFavoriteCache.clear();
  }

  async unfavoriteFile(filePath: string, groupId?: number): Promise<void> {
    const params = generateHttpParams({
      filePath: filePath,
      groupId: groupId,
    });

    await firstValueFrom(this.http.post<void>(`${apiUrl}/unfavoriteFile`, null, { params }));
    // Invalidate the cache after unfavoriting a file
    this.fileFavoriteCache.clear();
  }

  async getFavoriteFiles(): Promise<FileFavoriteDetails[]> {
    return await this.fileFavoriteCache.getOrProvide(() => firstValueFrom(this.http.get<FileFavoriteDetails[]>(`${apiUrl}/getFavoriteFiles`)));
  }

  clearFileFavoritesCache(): void {
    this.fileFavoriteCache.clear();
  }

  async favoriteDirectory(directoryPath: string, groupId?: number): Promise<void> {
    const params = generateHttpParams({
      directoryPath: directoryPath,
      groupId: groupId,
    });

    await firstValueFrom(this.http.post<void>(`${apiUrl}/favoriteDirectory`, null, { params }));
    // Invalidate the cache after favoriting a directory
    this.directoryFavoriteCache.clear();
  }

  async unfavoriteDirectory(directoryPath: string, groupId?: number): Promise<void> {
    const params = generateHttpParams({
      directoryPath: directoryPath,
      groupId: groupId,
    });

    await firstValueFrom(this.http.post<void>(`${apiUrl}/unfavoriteDirectory`, null, { params }));
    // Invalidate the cache after unfavoriting a directory
    this.directoryFavoriteCache.clear();
  }

  async getFavoriteDirectories(): Promise<DirectoryFavoriteDetails[]> {
    return await this.directoryFavoriteCache.getOrProvide(() => firstValueFrom(this.http.get<DirectoryFavoriteDetails[]>(`${apiUrl}/getFavoriteDirectories`)));
  }

  clearDirectoryFavoritesCache(): void {
    this.directoryFavoriteCache.clear();
  }
}
