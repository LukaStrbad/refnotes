import { Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { HttpClient } from '@angular/common/http';
import { generateHttpParams } from '../utils/http-utils';
import { firstValueFrom } from 'rxjs';

const apiUrl = environment.apiUrl + '/publicFile';

@Injectable({
  providedIn: 'root'
})
export class PublicFileService {

  constructor(private http: HttpClient) { }

  async getUrl(filePath: string, groupId?: number): Promise<string|null> {
    const params = generateHttpParams({
      filePath,
      groupId,
    });

    const urlHash = await firstValueFrom(
      this.http.get(`${apiUrl}/getUrlHash`, { responseType: 'text', params })
    );

    if (!urlHash) {
      return null; // If no hash is returned, the file is not public
    }

    return this.createUrlFromHash(urlHash);
  }

  async createPublicFile(filePath: string, groupId?: number): Promise<string|null> {
    const params = generateHttpParams({
      filePath,
      groupId,
    });

    const urlHash = await firstValueFrom(
      this.http.post(`${apiUrl}/create`, {}, { responseType: 'text', params })
    );

    if (!urlHash) {
      return null; // If no hash is returned, the file could not be made public
    }

    return this.createUrlFromHash(urlHash);
  }

  async deletePublicFile(filePath: string, groupId?: number): Promise<void> {
    const params = generateHttpParams({
      filePath,
      groupId,
    });

    await firstValueFrom(
      this.http.delete(`${apiUrl}/delete`, { params })
    );
  }

  private createUrlFromHash(urlHash: string): string {
    const origin = window.location.origin;
    return `${origin}/file/public/${urlHash}`;
  }
}
