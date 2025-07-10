import { TestBed } from '@angular/core/testing';

import { FavoriteService } from './favorite.service';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';
import { DirectoryFavoriteDetails } from '../model/directory-favorite-details';
import { FileFavoriteDetails } from '../model/file-favorite-details';
import { createDirectoryFavoriteDetails, createFileFavoriteDetails } from '../tests/favorite-utils';

const apiUrl = environment.apiUrl + '/favorite';

describe('FavoriteService', () => {
  let service: FavoriteService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        FavoriteService,
      ],
    });
    service = TestBed.inject(FavoriteService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should favorite a file without group', async () => {
    const promise = service.favoriteFile('/test/file.txt');

    const req = httpMock.expectOne(`${apiUrl}/favoriteFile?filePath=/test/file.txt`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
    req.flush({});

    await promise;
  });

  it('should favorite a file with group', async () => {
    const promise = service.favoriteFile('/test/file.txt', 123);

    const req = httpMock.expectOne(`${apiUrl}/favoriteFile?filePath=/test/file.txt&groupId=123`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
    req.flush({});

    await promise;
  });

  it('should unfavorite a file without group', async () => {
    const promise = service.unfavoriteFile('/test/file.txt');

    const req = httpMock.expectOne(`${apiUrl}/unfavoriteFile?filePath=/test/file.txt`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
    req.flush({});

    await promise;
  });

  it('should unfavorite a file with group', async () => {
    const promise = service.unfavoriteFile('/test/file.txt', 456);

    const req = httpMock.expectOne(`${apiUrl}/unfavoriteFile?filePath=/test/file.txt&groupId=456`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
    req.flush({});

    await promise;
  });

  it('should get favorite files', async () => {
    const mockFavoriteFiles: FileFavoriteDetails[] = [
      createFileFavoriteDetails('file1.txt', { id: 1, name: 'Group 1' }),
      createFileFavoriteDetails('file2.txt', { id: 2, name: 'Group 2' }),
    ];

    const promise = service.getFavoriteFiles();

    const req = httpMock.expectOne(`${apiUrl}/getFavoriteFiles`);
    expect(req.request.method).toBe('GET');
    req.flush(mockFavoriteFiles);

    const result = await promise;
    expect(result).toEqual(mockFavoriteFiles);
  });

  it('should favorite a directory without group', async () => {
    const promise = service.favoriteDirectory('/test/directory');

    const req = httpMock.expectOne(`${apiUrl}/favoriteDirectory?directoryPath=/test/directory`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
    req.flush({});

    await promise;
  });

  it('should favorite a directory with group', async () => {
    const promise = service.favoriteDirectory('/test/directory', 789);

    const req = httpMock.expectOne(`${apiUrl}/favoriteDirectory?directoryPath=/test/directory&groupId=789`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
    req.flush({});

    await promise;
  });

  it('should unfavorite a directory without group', async () => {
    const promise = service.unfavoriteDirectory('/test/directory');

    const req = httpMock.expectOne(`${apiUrl}/unfavoriteDirectory?directoryPath=/test/directory`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
    req.flush({});

    await promise;
  });

  it('should unfavorite a directory with group', async () => {
    const promise = service.unfavoriteDirectory('/test/directory', 101);

    const req = httpMock.expectOne(`${apiUrl}/unfavoriteDirectory?directoryPath=/test/directory&groupId=101`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
    req.flush({});

    await promise;
  });

  it('should get favorite directories', async () => {
    const mockFavoriteDirectories: DirectoryFavoriteDetails[] = [
      createDirectoryFavoriteDetails('/test/dir1', { id: 1, name: 'Group 1' }),
      createDirectoryFavoriteDetails('/test/dir2', { id: 2, name: 'Group 2' }),
    ];

    const promise = service.getFavoriteDirectories();

    const req = httpMock.expectOne(`${apiUrl}/getFavoriteDirectories`);
    expect(req.request.method).toBe('GET');
    req.flush(mockFavoriteDirectories);

    const result = await promise;
    expect(result).toEqual(mockFavoriteDirectories);
  });
});
