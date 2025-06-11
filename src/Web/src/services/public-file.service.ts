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

  async getUrlHash(filePath: string, groupId?: number): Promise<string> {
    const params = generateHttpParams({
      filePath,
      groupId,
    });

    return await firstValueFrom(
      this.http.get(`${apiUrl}/getUrlHash`, { responseType: 'text', params })
    );
  }

  async createPublicFile(filePath: string, groupId?: number): Promise<string> {
    const params = generateHttpParams({
      filePath,
      groupId,
    });

    return await firstValueFrom(
      this.http.post(`${apiUrl}/createPublicFile`, {}, { responseType: 'text', params })
    );
  }

  async deletePublicFile(filePath: string, groupId?: number): Promise<void> {
    const params = generateHttpParams({
      filePath,
      groupId,
    });

    await firstValueFrom(
      this.http.delete(`${apiUrl}/deletePublicFile`, { params })
    );
  }
}
