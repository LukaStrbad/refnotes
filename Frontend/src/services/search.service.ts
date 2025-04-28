import { Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { HttpClient } from '@angular/common/http';
import { SearchOptions } from '../model/search-options';
import { firstValueFrom } from 'rxjs';
import { FileSearchResult } from '../model/file-search-result';

const apiUrl = environment.apiUrl + '/search';

@Injectable({
  providedIn: 'root'
})
export class SearchService {

  constructor(private http: HttpClient) { }

  async searchFiles(options: SearchOptions) {
    const values = await firstValueFrom(
      this.http.post<FileSearchResult[]>(
        `${apiUrl}`, options, { responseType: 'json' }
      )
    );

    return values.map((file) => {
      return {
        ...file,
        modified: new Date(file.modified),
      };
    });
  }
}
