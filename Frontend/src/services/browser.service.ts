import { HttpClient, HttpEvent } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom, merge, Observable, of, tap } from 'rxjs';
import { Directory } from '../model/directory';
import { environment } from '../environments/environment';
import { LRUCache } from 'lru-cache';

const apiUrl = environment.apiUrl + '/browser';

@Injectable({
  providedIn: 'root',
})
export class BrowserService {
  private listCache = new LRUCache<string, Directory>({ max: 100 });

  constructor(private http: HttpClient) {}

  listCached(path: string = '/'): Observable<Directory> {
    const cached = this.listCache.get(path);

    const network = this.http
      .get<Directory>(`${apiUrl}/list?path=${path}`)
      .pipe(tap((value) => this.listCache.set(path, value)));

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
}
