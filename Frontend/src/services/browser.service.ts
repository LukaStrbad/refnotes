import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom, map, merge, Observable, of, tap } from 'rxjs';
import { Directory } from '../model/directory';
import { environment } from '../environments/environment';
import { LRUCache } from 'lru-cache';
import { File } from '../model/file';

const apiUrl = environment.apiUrl + '/browser';

@Injectable({
  providedIn: 'root',
})
export class BrowserService {
  private listCache = new LRUCache<string, Directory>({ max: 100 });

  constructor(private http: HttpClient) { }

  listCached(path = '/'): Observable<Directory> {
    const cached = this.listCache.get(path);

    const network = this.http
      .get<Directory>(`${apiUrl}/list?path=${path}`)
      .pipe(tap((value) => {
        value.files = value.files.map(this.mapFileDates);
        this.listCache.set(path, value);
      }));

    return cached ? merge(of(cached), network) : network;
  }

  async addDirectory(path: string) {
    return firstValueFrom(
      this.http.post(`${apiUrl}/addDirectory?path=${path}`, {}),
    );
  }

  async deleteDirectory(path: string) {
    return firstValueFrom(
      this.http.delete(`${apiUrl}/deleteDirectory?path=${path}`),
    );
  }

  private mapFileDates(file: File): File {
    // + 'Z' is added to make the date interpreted as UTC
    // without this, the date is interpreted as local time
    file.modified = new Date(file.modified + 'Z');
    file.created = new Date(file.created + 'Z');
    return file;
  }
}
