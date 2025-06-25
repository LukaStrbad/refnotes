import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { firstValueFrom, merge, Observable, of, tap } from 'rxjs';
import { generateHttpParams } from '../utils/http-utils';

const apiUrl = environment.apiUrl + '/tag';

@Injectable({
  providedIn: 'root',
})
export class TagService {
  private listCache: string[] | null = null;
  private listGroupCache: Record<number, string[]> = {};

  constructor(private http: HttpClient) { }

  listAllCached(): Observable<string[]> {
    const network = this.http.get<string[]>(`${apiUrl}/listAllTags`).pipe(
      tap((value) => {
        this.listCache = value;
      }),
    );

    return this.listCache ? merge(of(this.listCache), network) : network;
  }

  listAllGroupCached(groupId: number): Observable<string[]> {
    const params = generateHttpParams({
      groupId: groupId,
    });

    const network = this.http.get<string[]>(`${apiUrl}/listAllGroupTags`, { params }).pipe(
      tap((value) => {
        this.listGroupCache[groupId] = value;
      }),
    );

    const cached = this.listGroupCache[groupId];
    return cached ? merge(of(cached), network) : network;
  }

  async listFileTags(directoryPath: string, name: string, groupId?: number) {
    const params = generateHttpParams({
      directoryPath: directoryPath,
      name: name,
      groupId: groupId,
    });

    return await firstValueFrom(
      this.http.get<string[]>(
        `${apiUrl}/listFileTags`,
        { params },
      ),
    );
  }

  async addFileTag(directoryPath: string, name: string, tag: string, groupId?: number) {
    const params = generateHttpParams({
      directoryPath: directoryPath,
      name: name,
      tag: tag,
      groupId: groupId,
    });

    await firstValueFrom(
      this.http.post(
        `${apiUrl}/addFileTag`,
        null,
        { params },
      ),
    );
  }

  async removeFileTag(directoryPath: string, name: string, tag: string, groupId?: number) {
    const params = generateHttpParams({
      directoryPath: directoryPath,
      name: name,
      tag: tag,
      groupId: groupId,
    });

    await firstValueFrom(
      this.http.delete(
        `${apiUrl}/removeFileTag`,
        { params },
      ),
    );
  }
}
