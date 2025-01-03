import { HttpClient, HttpEvent } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom, merge, Observable, of, tap } from 'rxjs';
import { Directory } from '../model/directory';
import { environment } from '../environments/environment';
import { LRUCache } from 'lru-cache';

const apiUrl = environment.apiUrl + '/browser';

@Injectable({
  providedIn: 'root'
})
export class BrowserService {
  private listCache = new LRUCache<string, Directory>({ max: 100 });

  constructor(
    private http: HttpClient
  ) {
  }

  listCached(path: string = '/'): Observable<Directory> {
    const cached = this.listCache.get(path);

    const network = this.http.get<Directory>(`${apiUrl}/list?path=${path}`).pipe(
      tap(value => this.listCache.set(path, value))
    );

    return cached ? merge(of(cached), network) : network;
  }

  addFile(directoryPath: string, file: File): Observable<HttpEvent<Object>> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post(`${apiUrl}/addFile?directoryPath=${directoryPath}`, formData, {
      reportProgress: true,
      observe: "events"
    });
  }

  async addTextFile(directoryPath: string, name: string, content: string) {
    return firstValueFrom(this.http.post(`${apiUrl}/addTextFile?directoryPath=${directoryPath}&name=${name}`, content));
  }

  async deleteFile(directoryPath: string, name: string) {
    return firstValueFrom(this.http.delete(`${apiUrl}/deleteFile?directoryPath=${directoryPath}&name=${name}`));
  }

  async addDirectory(path: string) {
    return firstValueFrom(this.http.post(`${apiUrl}/addDirectory?path=${path}`, {}));
  }

  async deleteDirectory(path: string) {
    return firstValueFrom(this.http.delete(`${apiUrl}/deleteDirectory?path=${path}`));
  }

  async getFile(directoryPath: string, name: string) {
    return await firstValueFrom(this.http.get(
      `${apiUrl}/getFile?directoryPath=${directoryPath}&name=${name}`, { responseType: 'arraybuffer' })
    );
  }

  async saveTextfile(directoryPath: string, name: string, content: string) {
    await firstValueFrom(this.http.post(`${apiUrl}/saveTextFile?directoryPath=${directoryPath}&name=${name}`, content));
  }
}
