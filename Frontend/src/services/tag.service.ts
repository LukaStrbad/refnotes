import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { firstValueFrom } from 'rxjs';

const apiUrl = environment.apiUrl + '/tag';

@Injectable({
  providedIn: 'root',
})
export class TagService {
  constructor(private http: HttpClient) {}

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
