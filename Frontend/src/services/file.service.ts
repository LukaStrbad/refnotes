import { Injectable } from '@angular/core';
import { firstValueFrom, Observable } from 'rxjs';
import { HttpClient, HttpEvent } from '@angular/common/http';
import { environment } from '../environments/environment';
import { FileInfo } from '../model/file';
import { mapFileDates } from '../utils/date-utils';
import { generateHttpParams } from '../utils/http-utils';

const apiUrl = environment.apiUrl + '/file';

@Injectable({
  providedIn: 'root',
})
export class FileService {
  constructor(private http: HttpClient) { }

  addFile(directoryPath: string, file: File, groupId?: number): Observable<HttpEvent<object>> {
    const formData = new FormData();
    formData.append('file', file);

    const params = generateHttpParams({
      directoryPath: directoryPath,
      groupId: groupId,
    });

    return this.http.post(
      `${apiUrl}/addFile`,
      formData,
      {
        reportProgress: true,
        observe: 'events',
        params,
      },
    );
  }

  async addTextFile(directoryPath: string, name: string, content: string, groupId?: number) {
    const params = generateHttpParams({
      directoryPath: directoryPath,
      name: name,
      groupId: groupId,
    });

    return firstValueFrom(
      this.http.post(
        `${apiUrl}/addTextFile`,
        content,
        { params },
      ),
    );
  }

  async moveFile(oldFilePath: string, newFilePath: string, groupId?: number) {
    const params = generateHttpParams({
      oldName: oldFilePath,
      newName: newFilePath,
      groupId: groupId,
    });

    return firstValueFrom(
      this.http.post(
        `${apiUrl}/moveFile`,
        null,
        { params },
      ),
    );
  }

  async deleteFile(directoryPath: string, name: string, groupId?: number) {
    const params = generateHttpParams({
      directoryPath: directoryPath,
      name: name,
      groupId: groupId,
    });

    return firstValueFrom(
      this.http.delete(
        `${apiUrl}/deleteFile`,
        { params },
      ),
    );
  }

  async getFile(directoryPath: string, name: string, groupId?: number): Promise<ArrayBuffer> {
    const params = generateHttpParams({
      directoryPath: directoryPath,
      name: name,
      groupId: groupId,
    });

    return await firstValueFrom(
      this.http.get(
        `${apiUrl}/getFile`,
        { params, responseType: 'arraybuffer' },
      ),
    );
  }

  async getImage(
    directoryPath: string,
    name: string,
    groupId?: number,
  ): Promise<ArrayBuffer | null> {
    const params = generateHttpParams({
      directoryPath: directoryPath,
      name: name,
      groupId: groupId,
    });

    const result = await firstValueFrom(
      this.http.get(
        `${apiUrl}/getImage`,
        { params, responseType: 'arraybuffer' },
      ),
    );

    if (result.byteLength === 0) {
      return null;
    }

    return result;
  }

  async saveTextFile(directoryPath: string, name: string, content: string, groupId?: number) {
    const params = generateHttpParams({
      directoryPath: directoryPath,
      name: name,
      groupId: groupId,
    });

    await firstValueFrom(
      this.http.post(
        `${apiUrl}/saveTextFile`,
        content,
        { params },
      ),
    );
  }

  async getFileInfo(path: string, groupId?: number): Promise<FileInfo> {
    const params = generateHttpParams({
      path: path,
      groupId: groupId,
    });

    const fileInfo = await firstValueFrom(
      this.http.get<FileInfo>(`${apiUrl}/getFileInfo`, { params }),
    );
    return mapFileDates(fileInfo);
  }

  downloadFile(path: string, groupId?: number) {
    let requestUrl = `${apiUrl}/downloadFile?path=${encodeURIComponent(path)}`;
    if (groupId) {
      requestUrl += `&groupId=${groupId}`;
    }

    const a = document.createElement('a');
    a.href = requestUrl;
    a.setAttribute('download', '');
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
  }
}
