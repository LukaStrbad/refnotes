import { TestBed } from '@angular/core/testing';

import { BrowserService } from './browser.service';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { Directory } from '../model/directory';
import { environment } from '../environments/environment';
import { firstValueFrom, lastValueFrom } from 'rxjs';
import { File } from '../model/file';

const apiUrl = environment.apiUrl + '/browser';

function createFile(name: string): File {
  return { name, path: `/${name}`, tags: [], size: 0, created: new Date(), modified: new Date() };
}

describe('BrowserService', () => {
  let service: BrowserService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        BrowserService,
      ],
    });
    service = TestBed.inject(BrowserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should list directory with cache', async () => {
    const mockDirectory: Directory = {
      name: '/',
      files: [createFile('test.txt')],
      directories: [],
    };

    const listPromise = firstValueFrom(service.listCached('/'));

    const req = httpMock.expectOne(`${apiUrl}/list?path=/`);
    req.flush(mockDirectory);
    await listPromise;

    const observable = service.listCached('/');

    // First value should be cached
    const firstValue = firstValueFrom(observable);
    httpMock.expectNone(`${apiUrl}/list?path=/`);
    expect(await firstValue).toEqual(mockDirectory);

    const newDirectory: Directory = {
      name: '/',
      files: [createFile('test.txt'), createFile('test2.txt')],
      directories: ['dir'],
    };

    // Second value should be a new value
    const lastValue = lastValueFrom(observable);
    const req2 = httpMock.expectOne(`${apiUrl}/list?path=/`);
    req2.flush(newDirectory);
    expect(await lastValue).toEqual(newDirectory);
  });

  it('should add a directory', async () => {
    const mockResponse = {};
    const promise = service.addDirectory('/');

    const req = httpMock.expectOne(`${apiUrl}/addDirectory?path=/`);
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
    expect(await promise).toEqual(mockResponse);
  });

  it('should delete a directory', async () => {
    const mockResponse = {};
    const promise = service.deleteDirectory('/');

    const req = httpMock.expectOne(`${apiUrl}/deleteDirectory?path=/`);
    expect(req.request.method).toBe('DELETE');
    req.flush(mockResponse);
    expect(await promise).toEqual(mockResponse);
  });
});
