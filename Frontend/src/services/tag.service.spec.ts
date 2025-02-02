import { TestBed } from '@angular/core/testing';

import { TagService } from './tag.service';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { environment } from '../environments/environment';

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
