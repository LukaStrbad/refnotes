import { TestBed } from '@angular/core/testing';

import { BrowserService } from './browser.service';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { HttpResponse, provideHttpClient } from '@angular/common/http';
import { Directory } from '../model/directory';
import { environment } from '../environments/environment';
import { firstValueFrom, lastValueFrom, Observable, of } from 'rxjs';

const apiUrl = environment.apiUrl + '/browser';

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
      files: ['test.txt'],
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
      files: ['test.txt', 'test2.txt'],
      directories: ['dir'],
    };

    // Second value should be a new value
    const lastValue = lastValueFrom(observable);
    const req2 = httpMock.expectOne(`${apiUrl}/list?path=/`);
    req2.flush(newDirectory);
    expect(await lastValue).toEqual(newDirectory);
  });

  it('should add a file', async () => {
    const mockFile = new File([''], 'test.txt');

    const promise = lastValueFrom(service.addFile('/', mockFile));

    const req = httpMock.expectOne(`${apiUrl}/addFile?directoryPath=/`);
    expect(req.request.method).toBe('POST');
    req.flush({});

    const response = await promise;
    expect(response).toBeInstanceOf(HttpResponse);
  });

  it('should add a text file', async () => {
    const mockResponse = {};
    const promise = service.addTextFile('/', 'test.txt', 'content');

    const req = httpMock.expectOne(
      `${apiUrl}/addTextFile?directoryPath=/&name=test.txt`,
    );
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
    expect(await promise).toEqual(mockResponse);
  });

  it('should delete a file', async () => {
    const mockResponse = {};
    const promise = service.deleteFile('/', 'test.txt');

    const req = httpMock.expectOne(
      `${apiUrl}/deleteFile?directoryPath=/&name=test.txt`,
    );
    expect(req.request.method).toBe('DELETE');
    req.flush(mockResponse);
    expect(await promise).toEqual(mockResponse);
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

  it('should get a file', async () => {
    const mockResponse = new ArrayBuffer(8);
    const promise = service.getFile('/', 'test.txt');

    const req = httpMock.expectOne(
      `${apiUrl}/getFile?directoryPath=/&name=test.txt`,
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
    expect(await promise).toEqual(mockResponse);
  });

  it('should get an image', async () => {
    const mockResponse = new ArrayBuffer(8);
    const promise = service.getImage('/', 'test.txt');

    const req = httpMock.expectOne(
      `${apiUrl}/getImage?directoryPath=/&name=test.txt`,
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
    expect(await promise).toEqual(mockResponse);
  });

  it('should get null if image doesn\'t exist', async () => {
    const mockResponse = new ArrayBuffer(0);
    const promise = service.getImage('/', 'test.txt');

    const req = httpMock.expectOne(
      `${apiUrl}/getImage?directoryPath=/&name=test.txt`,
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
    expect(await promise).toEqual(null);
  });

  it('should save a text file', async () => {
    const mockResponse = {};
    const promise = service.saveTextfile('/', 'test.txt', 'content');

    const req = httpMock.expectOne(
      `${apiUrl}/saveTextFile?directoryPath=/&name=test.txt`,
    );
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
    await promise;
  });
});
