import { Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { generateHttpParams } from '../utils/http-utils';
import { DirectoryFavoriteDetails } from '../model/directory-favorite-details';
import { FileFavoriteDetails } from '../model/file-favorite-details';

const apiUrl = environment.apiUrl + '/favorite';

@Injectable({
  providedIn: 'root'
})
export class FavoriteService {

  constructor(
    private http: HttpClient,
  ) { }

  favoriteFile(filePath: string, groupId?: number): Promise<void> {
    const params = generateHttpParams({
      filePath: filePath,
      groupId: groupId,
    });

    return firstValueFrom(this.http.post<void>(`${apiUrl}/favoriteFile`, null, { params }));
  }

  unfavoriteFile(filePath: string, groupId?: number): Promise<void> {
    const params = generateHttpParams({
      filePath: filePath,
      groupId: groupId,
    });

    return firstValueFrom(this.http.post<void>(`${apiUrl}/unfavoriteFile`, null, { params }));
  }

  getFavoriteFiles(): Promise<FileFavoriteDetails[]> {
    return firstValueFrom(this.http.get<FileFavoriteDetails[]>(`${apiUrl}/getFavoriteFiles`));
  }

  favoriteDirectory(directoryPath: string, groupId?: number): Promise<void> {
    const params = generateHttpParams({
      directoryPath: directoryPath,
      groupId: groupId,
    });

    return firstValueFrom(this.http.post<void>(`${apiUrl}/favoriteDirectory`, null, { params }));
  }

  unfavoriteDirectory(directoryPath: string, groupId?: number): Promise<void> {
    const params = generateHttpParams({
      directoryPath: directoryPath,
      groupId: groupId,
    });

    return firstValueFrom(this.http.post<void>(`${apiUrl}/unfavoriteDirectory`, null, { params }));
  }

  getFavoriteDirectories(): Promise<DirectoryFavoriteDetails[]> {
    return firstValueFrom(this.http.get<DirectoryFavoriteDetails[]>(`${apiUrl}/getFavoriteDirectories`));
  }
}
