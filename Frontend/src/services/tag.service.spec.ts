import { TestBed } from '@angular/core/testing';

import { TagService } from './tag.service';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { environment } from '../environments/environment';
import { firstValueFrom, lastValueFrom } from 'rxjs';

const apiUrl = environment.apiUrl + '/tag';

describe('TagService', () => {
  let service: TagService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), TagService],
    });
    service = TestBed.inject(TagService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should list tags with cache', async () => {
    const mockResponse = ['tag1', 'tag2'];

    const listPromise = firstValueFrom(service.listAllCached());

    const req = httpMock.expectOne(`${apiUrl}/listAllTags`);
    req.flush(mockResponse);
    await listPromise;

    const observable = service.listAllCached();

    // First value should be cached
    const firstValue = firstValueFrom(observable);
    httpMock.expectNone(`${apiUrl}/listTags`);
    expect(await firstValue).toEqual(mockResponse);

    const newResponse = ['tag1', 'tag2', 'tag3'];

    // Second value should be a new value
    const lastValue = lastValueFrom(observable);
    const req2 = httpMock.expectOne(`${apiUrl}/listAllTags`);
    req2.flush(newResponse);
    expect(await lastValue).toEqual(newResponse);
  });

  it('should list file tags', async () => {
    const mockResponse = ['tag1', 'tag2'];
    const promise = service.listFileTags('/', 'test.txt');

    const req = httpMock.expectOne(
      `${apiUrl}/listFileTags?directoryPath=/&name=test.txt`,
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);

    expect(await promise).toEqual(mockResponse);
  });

  it('should add a tag to a file', async () => {
    const mockResponse = {};
    const promise = service.addFileTag('/', 'test.txt', 'tag');

    const req = httpMock.expectOne(
      `${apiUrl}/addFileTag?directoryPath=/&name=test.txt&tag=tag`,
    );
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
    await promise;
  });

  it('should remove a tag from a file', async () => {
    const mockResponse = {};
    const promise = service.removeFileTag('/', 'test.txt', 'tag');

    const req = httpMock.expectOne(
      `${apiUrl}/removeFileTag?directoryPath=/&name=test.txt&tag=tag`,
    );
    expect(req.request.method).toBe('DELETE');
    req.flush(mockResponse);
    await promise;
  });
});
