import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom, merge, Observable, of, tap } from 'rxjs';
import { Directory } from '../model/directory';
import { environment } from '../environments/environment';
import { LRUCache } from 'lru-cache';
import { mapFileDates } from '../utils/date-utils';
import { generateHttpParams } from '../utils/http-utils';

const apiUrl = environment.apiUrl + '/browser';

@Injectable({
  providedIn: 'root',
})
export class BrowserService {
  private listCache = new LRUCache<string, Directory>({ max: 100 });

  constructor(private http: HttpClient) { }

  listCached(path: string, groupId?: number): Observable<Directory> {
    const cached = this.listCache.get(path);

    const params = generateHttpParams({
      path: path,
      groupId: groupId,
    });

    const network = this.http
      .get<Directory>(`${apiUrl}/list`, { params })
      .pipe(tap((value) => {
        value.files = value.files.map(mapFileDates);
        this.listCache.set(path, value);
      }));

    return cached ? merge(of(cached), network) : network;
  }

  async addDirectory(path: string, groupId?: number) {
    const params = generateHttpParams({
      path: path,
      groupId: groupId,
    });

    return firstValueFrom(
      this.http.post(`${apiUrl}/addDirectory`, {}, { params }),
    );
  }

  async deleteDirectory(path: string, groupId?: number) {
    const params = generateHttpParams({
      path: path,
      groupId: groupId,
    });

    return firstValueFrom(
      this.http.delete(`${apiUrl}/deleteDirectory`, { params }),
    );
  }
}
