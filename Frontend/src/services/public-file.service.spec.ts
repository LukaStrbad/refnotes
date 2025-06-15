import { TestBed } from '@angular/core/testing';

import { PublicFileService } from './public-file.service';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../environments/environment';

const apiUrl = environment.apiUrl + '/publicFile';

describe('PublicFileService', () => {
  let service: PublicFileService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(PublicFileService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  afterEach(() => {
    // Verify that no unmatched requests are outstanding
    httpMock.verify();
  });

  it('should get URL hash', async () => {
    const filePath = '/path/to/file.txt';
    const groupId = 1;
    const urlHash = 'url-hash';
    const expectedUrl = `${environment.frontendUrl}/file/public/${urlHash}`;

    const promise = service.getUrl(filePath, groupId);

    const req = httpMock.expectOne(
      `${apiUrl}/getUrlHash?filePath=${filePath}&groupId=${groupId}`
    );
    req.flush(urlHash);

    expect(req.request.method).toBe('GET');
    expect(await promise).toBe(expectedUrl);
  });

  it('should create public file', async () => {
    const filePath = '/path/to/file.txt';
    const groupId = 1;
    const urlHash = 'url-hash';
    const expectedUrl = `${environment.frontendUrl}/file/public/${urlHash}`;

    const promise = service.createPublicFile(filePath, groupId);

    const req = httpMock.expectOne(
      `${apiUrl}/create?filePath=${filePath}&groupId=${groupId}`
    );
    req.flush(urlHash);

    expect(req.request.method).toBe('POST');
    expect(await promise).toBe(expectedUrl);
  });

  it('should delete public file', async () => {
    const filePath = '/path/to/file.txt';
    const groupId = 1;

    const promise = service.deletePublicFile(filePath, groupId);

    const req = httpMock.expectOne(
      `${apiUrl}/delete?filePath=${filePath}&groupId=${groupId}`
    );
    expect(req.request.method).toBe('DELETE');
    req.flush({});

    await promise;
  });
});
