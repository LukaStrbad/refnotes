import { Injectable } from '@angular/core';
import { firstValueFrom, Observable } from 'rxjs';
import { HttpClient, HttpEvent } from '@angular/common/http';
import { environment } from '../environments/environment';

const apiUrl = environment.apiUrl + '/file';

@Injectable({
  providedIn: 'root',
})
export class FileService {
  constructor(private http: HttpClient) {}

  addFile(directoryPath: string, file: File): Observable<HttpEvent<Object>> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post(
      `${apiUrl}/addFile?directoryPath=${directoryPath}`,
      formData,
      {
        reportProgress: true,
        observe: 'events',
      },
    );
  }

  async addTextFile(directoryPath: string, name: string, content: string) {
    return firstValueFrom(
      this.http.post(
        `${apiUrl}/addTextFile?directoryPath=${directoryPath}&name=${name}`,
        content,
      ),
    );
  }

  async moveFile(oldFilePath: string, newFilePath: string) {
    return firstValueFrom(
      this.http.post(
        `${apiUrl}/moveFile?oldName=${oldFilePath}&newName=${newFilePath}`,
        {},
      ),
    );
  }

  async deleteFile(directoryPath: string, name: string) {
    return firstValueFrom(
      this.http.delete(
        `${apiUrl}/deleteFile?directoryPath=${directoryPath}&name=${name}`,
      ),
    );
  }

  async getFile(directoryPath: string, name: string) {
    return await firstValueFrom(
      this.http.get(
        `${apiUrl}/getFile?directoryPath=${directoryPath}&name=${name}`,
        { responseType: 'arraybuffer' },
      ),
    );
  }

  async getImage(
    directoryPath: string,
    name: string,
  ): Promise<ArrayBuffer | null> {
    const result = await firstValueFrom(
      this.http.get(
        `${apiUrl}/getImage?directoryPath=${directoryPath}&name=${name}`,
        { responseType: 'arraybuffer' },
      ),
    );

    if (result.byteLength === 0) {
      return null;
    }

    return result;
  }

  async saveTextFile(directoryPath: string, name: string, content: string) {
    await firstValueFrom(
      this.http.post(
        `${apiUrl}/saveTextFile?directoryPath=${directoryPath}&name=${name}`,
        content,
      ),
    );
  }
}
