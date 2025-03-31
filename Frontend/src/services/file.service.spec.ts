import { TestBed } from '@angular/core/testing';

import { FileService } from './file.service';
import { lastValueFrom } from 'rxjs';
import { HttpResponse, provideHttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';

const apiUrl = environment.apiUrl + '/file';

describe('FileService', () => {
  let service: FileService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [],
      providers: [provideHttpClient(), provideHttpClientTesting(), FileService],
    });
    service = TestBed.inject(FileService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
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

  it('should move a file', async () => {
    const mockResponse = {};
    const promise = service.moveFile('/oldPath/test.txt', '/newPath/test.txt');

    const req = httpMock.expectOne(
      `${apiUrl}/moveFile?oldFilePath=/oldPath/test.txt&newFilePath=/newPath/test.txt`,
    );
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
    expect(await promise).toEqual(mockResponse);
  })

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

  it("should get null if image doesn't exist", async () => {
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
    const promise = service.saveTextFile('/', 'test.txt', 'content');

    const req = httpMock.expectOne(
      `${apiUrl}/saveTextFile?directoryPath=/&name=test.txt`,
    );
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
    await promise;
  });
});
