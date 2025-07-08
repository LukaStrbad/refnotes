import { TestBed, fakeAsync, tick } from '@angular/core/testing';

import { BrowserFavoriteService } from './browser-favorite.service';
import { FavoriteService } from '../favorite.service';
import { FileFavoriteDetails } from '../../model/file-favorite-details';
import { DirectoryFavoriteDetails } from '../../model/directory-favorite-details';

describe('BrowserFavoriteService', () => {
  let service: BrowserFavoriteService;
  let favoriteService: jasmine.SpyObj<FavoriteService>;

  beforeEach(() => {
    favoriteService = jasmine.createSpyObj('FavoriteService', ['getFavoriteFiles', 'getFavoriteDirectories', 'favoriteFile', 'unfavoriteFile', 'favoriteDirectory', 'unfavoriteDirectory', 'clearFileFavoritesCache', 'clearDirectoryFavoritesCache']);

    TestBed.configureTestingModule({
      providers: [
        { provide: FavoriteService, useValue: favoriteService },
      ]
    });
    favoriteService.getFavoriteFiles.and.resolveTo([]);
    favoriteService.getFavoriteDirectories.and.resolveTo([]);
  });

  it('should be created', () => {
    service = TestBed.inject(BrowserFavoriteService);
    expect(service).toBeTruthy();
  });

  it('should initialize with values from FavoriteService', fakeAsync(() => {
    const mockFileFavorites: FileFavoriteDetails[] = [{ fileInfo: { name: 'file1.txt', path: '/file1.txt', tags: [], size: 1024, created: new Date(), modified: new Date() }, groupId: 1, favoriteDate: new Date() }];
    const mockDirectoryFavorites: DirectoryFavoriteDetails[] = [{ path: '/', groupId: 1, favoriteDate: new Date() }];

    favoriteService.getFavoriteFiles.and.resolveTo(mockFileFavorites);
    favoriteService.getFavoriteDirectories.and.resolveTo(mockDirectoryFavorites);

    service = TestBed.inject(BrowserFavoriteService);
    tick();

    expect(service.fileFavorites()).toEqual(mockFileFavorites);
    expect(service.directoryFavorites()).toEqual(mockDirectoryFavorites);
  }));

  it('should set groupId', () => {
    service = TestBed.inject(BrowserFavoriteService);
    service.setGroupId(1);
    expect(service['groupId']).toBe(1);
  });

  it('should favorite a file', fakeAsync(async () => {
    service = TestBed.inject(BrowserFavoriteService);
    tick();
    const mockFile = { name: 'file1.txt', path: '/file1.txt', tags: [], size: 1024, created: new Date(), modified: new Date() };
    service.setGroupId(1);

    await service.favoriteFile(mockFile);

    expect(favoriteService.favoriteFile).toHaveBeenCalledWith(mockFile.path, 1);
    expect(service.fileFavorites()).toContain(jasmine.objectContaining({
      fileInfo: mockFile,
      groupId: 1
    }));
  }));

  it('should unfavorite a file', fakeAsync(async () => {
    service = TestBed.inject(BrowserFavoriteService);
    tick();
    const mockFile = { name: 'file1.txt', path: '/file1.txt', tags: [], size: 1024, created: new Date(), modified: new Date() };
    service.setGroupId(1);

    await service.favoriteFile(mockFile);
    await service.unfavoriteFile(mockFile);

    expect(favoriteService.unfavoriteFile).toHaveBeenCalledWith(mockFile.path, 1);
    expect(service.fileFavorites()).not.toContain(jasmine.objectContaining({
      fileInfo: mockFile,
      groupId: 1
    }));
  }));

  it('should favorite a directory', fakeAsync(async () => {
    service = TestBed.inject(BrowserFavoriteService);
    tick();
    const mockDirectory = { path: '/testDir', groupId: 1, favoriteDate: new Date() };
    service.setGroupId(1);

    await service.favoriteDirectory(mockDirectory.path);

    expect(favoriteService.favoriteDirectory).toHaveBeenCalledWith(mockDirectory.path, 1);
    expect(service.directoryFavorites()).toContain(jasmine.objectContaining({
      path: mockDirectory.path,
      groupId: 1
    }));
  }));

  it('should unfavorite a directory', fakeAsync(async () => {
    service = TestBed.inject(BrowserFavoriteService);
    tick();
    const mockDirectory = { path: '/testDir', groupId: 1, favoriteDate: new Date() };
    service.setGroupId(1);

    await service.favoriteDirectory(mockDirectory.path);
    await service.unfavoriteDirectory(mockDirectory.path);

    expect(favoriteService.unfavoriteDirectory).toHaveBeenCalledWith(mockDirectory.path, 1);
    expect(service.directoryFavorites()).not.toContain(jasmine.objectContaining({
      path: mockDirectory.path,
      groupId: 1
    }));
  }));
});
