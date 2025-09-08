import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BrowserComponent } from './browser.component';
import {
  TranslateFakeLoader,
  TranslateLoader,
  TranslateModule,
} from '@ngx-translate/core';
import { BrowserService } from '../../services/browser.service';
import { LoggerService } from '../../services/logger.service';
import { AuthService } from '../../services/auth.service';
import { ActivatedRoute, provideRouter, Router } from '@angular/router';
import { Observable, of, Subscriber } from 'rxjs';
import { HttpResponse, provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Directory } from '../../model/directory';
import { FileService } from '../../services/file.service';
import { File } from '../../model/file';
import { BrowserFavoriteService } from '../../services/components/browser-favorite.service';
import { signal } from '@angular/core';

function createFile(name: string): File {
  return { name: name, path: `/${name}`, tags: [], size: 0, created: new Date(), modified: new Date() };
}

function setupTestBed(groupId?: string) {
  const browserService = jasmine.createSpyObj<BrowserService>('BrowserService', [
    'listCached',
    'addDirectory',
    'deleteDirectory',
  ]);
  const fileService = jasmine.createSpyObj<FileService>('FileService', [
    'addTextFile',
    'deleteFile',
    'addFile',
  ]);
  const authService = jasmine.createSpyObj<AuthService>('AuthService', [], ['user']);
  const storage: Record<string, string> = {};
  const favorite = jasmine.createSpyObj<BrowserFavoriteService>('BrowserFavoriteService', ['setGroupId', 'favoriteFile', 'removeLocalFileFavorite', 'unfavoriteFile', 'favoriteDirectory', 'removeLocalDirectoryFavorite', 'unfavoriteDirectory'], {
    fileFavorites: signal([]),
    directoryFavorites: signal([]),
  });

  // All tests here sometimes fail because AuthService cannot decode a token, probably because of test parallelization
  // This mocks localStorage to avoid the issue
  spyOn(localStorage, 'getItem').and.callFake(
    (key: string) => storage[key] ?? null,
  );
  spyOn(localStorage, 'setItem').and.callFake(
    (key: string, value: string) => {
      storage[key] = value;
    },
  );

  browserService.listCached.and.returnValue(
    of({ name: '/', files: [], sharedFiles: [], directories: [] }),
  );

  const imports = [
    BrowserComponent,
    TranslateModule.forRoot({
      loader: {
        provide: TranslateLoader,
        useClass: TranslateFakeLoader,
      },
    }),
  ];

  const providers: unknown[] = [
    provideRouter([
      {
        path: '**',
        component: BrowserComponent,
      },
    ]),
    provideHttpClient(),
    provideHttpClientTesting(),
    LoggerService,
    { provide: AuthService, useValue: authService },
    { provide: BrowserService, useValue: browserService },
    { provide: FileService, useValue: fileService },
    { provide: BrowserFavoriteService, useValue: favorite },
  ];

  // Add ActivatedRoute mock only if groupId is provided
  if (groupId) {
    const mockActivatedRoute = {
      snapshot: {
        params: { groupId },
        paramMap: { get: (key: string) => key === 'groupId' ? groupId : null },
        queryParams: { get: () => null },
      }
    };
    providers.push({ provide: ActivatedRoute, useValue: mockActivatedRoute });
  }

  return { browserService, fileService, authService, storage, favorite, providers, imports };
}

describe('BrowserComponent', () => {
  let component: BrowserComponent;
  let fixture: ComponentFixture<BrowserComponent>;
  let browserService: jasmine.SpyObj<BrowserService>;
  let fileService: jasmine.SpyObj<FileService>;
  let favorite: jasmine.SpyObj<BrowserFavoriteService>;

  beforeEach(async () => {
    const setup = setupTestBed();
    browserService = setup.browserService;
    fileService = setup.fileService;
    favorite = setup.favorite;

    await TestBed.configureTestingModule({
      imports: setup.imports,
      providers: setup.providers,
    }).compileComponents();

    fixture = TestBed.createComponent(BrowserComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    await component.loadingPromise;
    expect(component.currentFolder).toBeTruthy();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should refresh routes when refreshRoute is called', async () => {
    const cachedDirectory: Directory = {
      name: '/',
      files: [createFile('test.txt')],
      sharedFiles: [],
      directories: [],
    };
    const networkDirectory: Directory = {
      name: '/',
      files: [createFile('test.txt'), createFile('test2.txt')],
      sharedFiles: [],
      directories: ['dir'],
    };

    let sub: Subscriber<Directory> = null!;
    const observable = new Observable<Directory>((subscriber) => {
      sub = subscriber;
      subscriber.next(cachedDirectory);
    });

    browserService.listCached.and.returnValue(observable);

    const refreshPromise = component.refreshRoute();
    await component.refreshRouteInnerPromise;

    expect(component.currentFolder()).toBe(cachedDirectory);

    sub.next(networkDirectory);
    sub.complete();

    await refreshPromise;
    expect(component.currentFolder()).toBe(networkDirectory);
  });

  it('should list cached directory on refresh route', async () => {
    const mockDirectory: Directory = {
      name: '/',
      files: [createFile('test.txt')],
      sharedFiles: [],
      directories: ['dir'],
    };
    browserService.listCached.and.returnValue(of(mockDirectory));
    await component.refreshRoute();
    fixture.detectChanges();

    const folderTrs = fixture.nativeElement.querySelectorAll(
      'tr[data-test="folder-tr"]',
    );
    const fileTrs = fixture.nativeElement.querySelectorAll(
      'tr[data-test="file-tr"]',
    );
    expect(folderTrs.length).toBe(1);
    expect(fileTrs.length).toBe(1);

    expect(component.currentFolder()).toEqual(mockDirectory);
  });

  it('should create a new file', async () => {
    fixture.detectChanges();
    browserService.listCached.and.returnValue(
      of({ name: '/', files: [createFile('test.txt')], sharedFiles: [], directories: [] }),
    );
    await component.createNewFile('test.txt');
    fixture.detectChanges();

    const fileTrs = fixture.nativeElement.querySelectorAll(
      'tr[data-test="file-tr"]',
    );

    expect(fileTrs.length).toBe(1);
    expect(component.files()).toContain(jasmine.objectContaining({ name: 'test.txt' }));
    expect(fileService.addTextFile).toHaveBeenCalledWith('/', 'test.txt', '', undefined);
  });

  it('should create a new folder', async () => {
    fixture.detectChanges();
    browserService.listCached.and.returnValue(
      of({ name: '/', files: [], sharedFiles: [], directories: ['newFolder'] }),
    );
    await component.createNewFolder('newFolder');
    fixture.detectChanges();

    const folderTrs = fixture.nativeElement.querySelectorAll(
      'tr[data-test="folder-tr"]',
    );

    expect(folderTrs.length).toBe(1);
    expect(component.directories()).toContain(jasmine.objectContaining({ name: 'newFolder' }));
    expect(browserService.addDirectory).toHaveBeenCalledWith('/newFolder', undefined);
  });

  it('should delete a file', async () => {
    browserService.listCached.and.returnValue(
      of({ name: '/', files: [createFile('test.txt')], sharedFiles: [], directories: [] }),
    );
    component.ngOnInit();
    await component.loadingPromise;
    expect(component.files()).toContain(jasmine.objectContaining({ name: 'test.txt' }));

    fileService.deleteFile.and.callFake(() => {
      browserService.listCached.and.returnValue(
        of({ name: '/', files: [], sharedFiles: [], directories: [] }),
      );
      return Promise.resolve(Object);
    });

    await component.deleteFile(createFile('test.txt'));
    fixture.detectChanges();

    const fileTrs = fixture.nativeElement.querySelectorAll(
      'tr[data-test="file-tr"]',
    );

    expect(fileTrs.length).toBe(0);
    expect(component.files()).not.toContain(jasmine.objectContaining({ name: 'test.txt' }));
    expect(favorite.removeLocalFileFavorite).toHaveBeenCalledWith(jasmine.objectContaining({ name: 'test.txt' }));
  });

  it('should delete a folder', async () => {
    browserService.listCached.and.returnValue(
      of({ name: '/', files: [], sharedFiles: [], directories: ['testFolder'] }),
    );
    component.ngOnInit();
    await component.loadingPromise;
    expect(component.directories()).toContain(jasmine.objectContaining({ name: 'testFolder' }));

    browserService.deleteDirectory.and.callFake(() => {
      browserService.listCached.and.returnValue(
        of({ name: '/', files: [], sharedFiles: [], directories: [] }),
      );
      return Promise.resolve(Object);
    });

    await component.deleteFolder('testFolder');
    fixture.detectChanges();

    const folderTrs = fixture.nativeElement.querySelectorAll(
      'tr[data-test="folder-tr"]',
    );

    expect(folderTrs.length).toBe(0);
    expect(component.directories()).not.toContain(jasmine.objectContaining({ name: 'testFolder' }));
    expect(favorite.removeLocalDirectoryFavorite).toHaveBeenCalledWith('/testFolder');
  });

  it('should navigate to folder', async () => {
    browserService.listCached.and.returnValue(
      of({ name: '/newFolder', files: [], sharedFiles: [], directories: [] }),
    );
    await component.navigateToFolder('/newFolder');
    await component.loadingPromise;
    fixture.detectChanges();

    const breadcrumb = fixture.nativeElement.querySelector(
      'a[data-test="breadcrumb-item"][href="/browser/newFolder"]',
    );

    expect(breadcrumb).toBeTruthy();
    expect(component.currentPath).toBe('/newFolder');
  });

  it('should upload files', async () => {
    const mockFile = new File([''], 'test.txt');
    const fileList = {
      0: mockFile,
      length: 1,
      item: () => mockFile,
    } as FileList;
    fileService.addFile.and.returnValue(
      of(new HttpResponse<object>({ status: 200 })),
    );
    spyOn(component.fileModal, 'close');
    await component.onFilesUpload(fileList);
    expect(component.fileModal.close).toHaveBeenCalled();
  });

  it('should show moving files', async () => {
    component.currentFolder.set({
      name: '/',
      files: [createFile('test.txt')],
      sharedFiles: [],
      directories: [],
    });
    fixture.detectChanges();

    const checkboxes = fixture.nativeElement.querySelectorAll(
      'input[data-test="browser.file-to-move-checkbox"]',
    ) as HTMLInputElement[];

    expect(checkboxes.length).toBe(1);
    expect(checkboxes[0].checked).toBe(false);
    checkboxes[0].click();

    fixture.detectChanges();

    const filesToMove = fixture.nativeElement.querySelectorAll(
      '[data-test="browser.file-to-move"]',
    ) as HTMLElement[];

    expect(filesToMove.length).toBe(1);
    expect(filesToMove[0].textContent).toEqual('/test.txt');
  });
});

describe('BrowserComponent with groupId', () => {
  let component: BrowserComponent;
  let fixture: ComponentFixture<BrowserComponent>;
  let browserService: jasmine.SpyObj<BrowserService>;
  let favorite: jasmine.SpyObj<BrowserFavoriteService>;

  beforeEach(async () => {
    const setup = setupTestBed('1'); // Pass groupId to setup function
    browserService = setup.browserService;
    favorite = setup.favorite;

    await TestBed.configureTestingModule({
      imports: setup.imports,
      providers: setup.providers,
    }).compileComponents();

    fixture = TestBed.createComponent(BrowserComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    await component.loadingPromise;
  });

  it('should set groupId from route parameters and update linkBasePath', () => {
    expect(component.groupId).toBe(1);
    expect(component.linkBasePath).toBe('/groups/1');
    expect(favorite.setGroupId).toHaveBeenCalledWith(1);
  });

  it('should navigate to folder with group path when groupId is set', async () => {
    const router = TestBed.inject(Router);
    const navigateSpy = spyOn(router, 'navigate');

    await component.navigateToFolder('/test-folder');

    expect(navigateSpy).toHaveBeenCalledWith(['/groups/1/browser/test-folder'], { queryParams: {} });
  });

  it('should call browserService.listCached with groupId when refreshing route', async () => {
    const mockDirectory: Directory = {
      name: '/',
      files: [],
      sharedFiles: [],
      directories: []
    };
    browserService.listCached.and.returnValue(of(mockDirectory));

    await component.refreshRoute();

    expect(browserService.listCached).toHaveBeenCalledWith('/', 1);
  });

  it('should call browserService.addDirectory with groupId when creating new folder', async () => {
    component.currentFolder.set({
      name: '/',
      files: [],
      sharedFiles: [],
      directories: []
    });

    await component.createNewFolder('testFolder');

    expect(browserService.addDirectory).toHaveBeenCalledWith('/testFolder', 1);
  });

  it('should call browserService.deleteDirectory with groupId when deleting folder', async () => {
    await component.deleteFolder('testFolder');

    expect(browserService.deleteDirectory).toHaveBeenCalledWith('/testFolder', 1);
  });

  it('should call browserService.listCached with groupId in nested folder paths', async () => {
    // Mock router URL to simulate being in a nested folder
    const router = TestBed.inject(Router);
    spyOnProperty(router, 'url', 'get').and.returnValue('/groups/1/browser/parent/child');

    const mockDirectory: Directory = {
      name: '/parent/child',
      files: [],
      sharedFiles: [],
      directories: []
    };
    browserService.listCached.and.returnValue(of(mockDirectory));

    await component.refreshRoute();

    expect(browserService.listCached).toHaveBeenCalledWith('/parent/child', 1);
  });

  it('should call browserService.addDirectory with groupId in nested folder paths', async () => {
    component.currentFolder.set({
      name: '/parent',
      files: [],
      sharedFiles: [],
      directories: []
    });
    component.currentPath = '/parent';

    await component.createNewFolder('newChild');

    expect(browserService.addDirectory).toHaveBeenCalledWith('/parent/newChild', 1);
  });

  it('should call browserService.deleteDirectory with groupId in nested folder paths', async () => {
    component.currentPath = '/parent';

    await component.deleteFolder('childToDelete');

    expect(browserService.deleteDirectory).toHaveBeenCalledWith('/parent/childToDelete', 1);
  });
});
