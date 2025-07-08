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

const apiUrl = environment.apiUrl + '/favorite';

function createFileDetails(name: string, groupId?: number): FileFavoriteDetails {
  return {
    fileInfo: {
      name,
      path: `/${name}`,
      size: 1024,
      tags: [],
      created: new Date(),
      modified: new Date(),
    },
    groupId,
    favoriteDate: new Date(),
  };
}

function createDirectoryDetails(path: string, groupId?: number): DirectoryFavoriteDetails {
  return {
    path: path,
    groupId,
    favoriteDate: new Date(),
  };
}

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
      createFileDetails('file1.txt', 1),
      createFileDetails('file2.txt', 2),
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
      createDirectoryDetails('/test/dir1', 1),
      createDirectoryDetails('/test/dir2', 2),
    ];

    const promise = service.getFavoriteDirectories();

    const req = httpMock.expectOne(`${apiUrl}/getFavoriteDirectories`);
    expect(req.request.method).toBe('GET');
    req.flush(mockFavoriteDirectories);

    const result = await promise;
    expect(result).toEqual(mockFavoriteDirectories);
  });
});
