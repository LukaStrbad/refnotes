import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';
import { SearchOptions } from '../model/search-options';

import { SearchService } from './search.service';

const apiUrl = environment.apiUrl + '/search';

describe('SearchService', () => {
  let service: SearchService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), SearchService],
    });
    service = TestBed.inject(SearchService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should send POST and map results', async () => {
    const options: SearchOptions = { searchTerm: 'foo', page: 0, pageSize: 10 };
    const mockResponse = [
      { path: '/foo.txt', tags: ['tag1'], modified: '2025-05-05T12:00:00Z', foundByFullText: false },
    ];
    const promise = service.searchFiles(options);
    const req = httpMock.expectOne(apiUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(options);
    req.flush(mockResponse);
    const result = await promise;
    expect(result.length).toBe(1);
    expect(result[0].path).toBe('/foo.txt');
    expect(result[0].tags).toEqual(['tag1']);
    expect(result[0].foundByFullText).toBe(false);
    expect(result[0].modified instanceof Date).toBeTrue();
    expect(result[0].modified.toISOString()).toBe('2025-05-05T12:00:00.000Z');
  });

  it('should handle empty result', async () => {
    const options: SearchOptions = { searchTerm: '', page: 0, pageSize: 10 };
    const promise = service.searchFiles(options);
    const req = httpMock.expectOne(apiUrl);
    req.flush([]);
    const result = await promise;
    expect(result).toEqual([]);
  });
});
