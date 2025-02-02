import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { firstValueFrom, merge, Observable, of, tap } from 'rxjs';

const apiUrl = environment.apiUrl + '/tag';

@Injectable({
  providedIn: 'root',
})
export class TagService {
  private listCache: string[] | null = null;

  constructor(private http: HttpClient) {}

  listAllCached(): Observable<string[]> {
    const network = this.http.get<string[]>(`${apiUrl}/listAllTags`).pipe(
      tap((value) => {
        this.listCache = value;
      }),
    );

    return this.listCache ? merge(of(this.listCache), network) : network;
  }

  async listFileTags(directoryPath: string, name: string) {
    return await firstValueFrom(
      this.http.get<string[]>(
        `${apiUrl}/listFileTags?directoryPath=${directoryPath}&name=${name}`,
      ),
    );
  }

  async addFileTag(directoryPath: string, name: string, tag: string) {
    await firstValueFrom(
      this.http.post(
        `${apiUrl}/addFileTag?directoryPath=${directoryPath}&name=${name}&tag=${tag}`,
        {},
      ),
    );
  }

  async removeFileTag(directoryPath: string, name: string, tag: string) {
    await firstValueFrom(
      this.http.delete(
        `${apiUrl}/removeFileTag?directoryPath=${directoryPath}&name=${name}&tag=${tag}`,
        {},
      ),
    );
  }
}
