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
import { User } from '../../model/user';

function createFile(name: string): File {
  return { name, tags: [], size: 0, created: new Date(), modified: new Date() };
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
    of({ name: '/', files: [], directories: [] }),
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
  ];

  // Add ActivatedRoute mock only if groupId is provided
  if (groupId) {
    const mockActivatedRoute = {
      snapshot: {
        params: { groupId },
        paramMap: { get: (key: string) => key === 'groupId' ? groupId : null }
      }
    };
    providers.push({ provide: ActivatedRoute, useValue: mockActivatedRoute });
  }

  return { browserService, fileService, authService, storage, providers, imports };
}

describe('BrowserComponent', () => {
  let component: BrowserComponent;
  let fixture: ComponentFixture<BrowserComponent>;
  let browserService: jasmine.SpyObj<BrowserService>;
  let fileService: jasmine.SpyObj<FileService>;
  let authService: jasmine.SpyObj<AuthService>;
  beforeEach(async () => {
    const setup = setupTestBed();
    browserService = setup.browserService;
    fileService = setup.fileService;
    authService = setup.authService;

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

  it('should navigate to login if user is not authenticated', () => {
    const userProperty = Object.getOwnPropertyDescriptor(authService, 'user') as { get: jasmine.Spy<(this: jasmine.SpyObj<AuthService>) => User | null> };
    userProperty.get.and.returnValue(null);
    const router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.rejectWith(true);
    component.ngOnInit();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should refresh routes when refreshRoute is called', async () => {
    const cachedDirectory: Directory = {
      name: '/',
      files: [createFile('test.txt')],
      directories: [],
    };
    const networkDirectory: Directory = {
      name: '/',
      files: [createFile('test.txt'), createFile('test2.txt')],
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

    expect(component.currentFolder).toBe(cachedDirectory);

    sub.next(networkDirectory);
    sub.complete();

    await refreshPromise;
    expect(component.currentFolder).toBe(networkDirectory);
  });

  it('should list cached directory on refresh route', async () => {
    const mockDirectory: Directory = {
      name: '/',
      files: [createFile('test.txt')],
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

    expect(component.currentFolder).toEqual(mockDirectory);
  });

  it('should create a new file', async () => {
    fixture.detectChanges();
    browserService.listCached.and.returnValue(
      of({ name: '/', files: [createFile('test.txt')], directories: [] }),
    );
    await component.createNewFile('test.txt');
    fixture.detectChanges();

    const fileTrs = fixture.nativeElement.querySelectorAll(
      'tr[data-test="file-tr"]',
    );

    expect(fileTrs.length).toBe(1);
    const fileNames = component.currentFolder!.files.map((f) => f.name);
    expect(fileNames).toContain('test.txt');
    expect(fileService.addTextFile).toHaveBeenCalledWith('/', 'test.txt', '', undefined);
  });

  it('should create a new folder', async () => {
    fixture.detectChanges();
    browserService.listCached.and.returnValue(
      of({ name: '/', files: [], directories: ['newFolder'] }),
    );
    await component.createNewFolder('newFolder');
    fixture.detectChanges();

    const folderTrs = fixture.nativeElement.querySelectorAll(
      'tr[data-test="folder-tr"]',
    );

    expect(folderTrs.length).toBe(1);
    expect(component.currentFolder!.directories).toContain('newFolder');
    expect(browserService.addDirectory).toHaveBeenCalledWith('/newFolder', undefined);
  });

  it('should delete a file', async () => {
    browserService.listCached.and.returnValue(
      of({ name: '/', files: [createFile('test.txt')], directories: [] }),
    );
    component.ngOnInit();
    await component.loadingPromise;
    expect(component.currentFolder?.files.map((f) => f.name)).toContain(
      'test.txt',
    );

    fileService.deleteFile.and.callFake(() => {
      browserService.listCached.and.returnValue(
        of({ name: '/', files: [], directories: [] }),
      );
      return Promise.resolve(Object);
    });

    await component.deleteFile(createFile('test.txt'));
    fixture.detectChanges();

    const fileTrs = fixture.nativeElement.querySelectorAll(
      'tr[data-test="file-tr"]',
    );

    expect(fileTrs.length).toBe(0);
    expect(component.currentFolder?.files).not.toContain('test.txt');
  });

  it('should delete a folder', async () => {
    browserService.listCached.and.returnValue(
      of({ name: '/', files: [], directories: ['testFolder'] }),
    );
    component.ngOnInit();
    await component.loadingPromise;
    expect(component.currentFolder?.directories).toContain('testFolder');

    browserService.deleteDirectory.and.callFake(() => {
      browserService.listCached.and.returnValue(
        of({ name: '/', files: [], directories: [] }),
      );
      return Promise.resolve(Object);
    });

    await component.deleteFolder('testFolder');
    fixture.detectChanges();

    const folderTrs = fixture.nativeElement.querySelectorAll(
      'tr[data-test="folder-tr"]',
    );

    expect(folderTrs.length).toBe(0);
    expect(component.currentFolder?.directories).not.toContain('testFolder');
  });

  it('should navigate to folder', async () => {
    browserService.listCached.and.returnValue(
      of({ name: '/newFolder', files: [], directories: [] }),
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
    component.currentFolder = {
      name: '/',
      files: [createFile('test.txt')],
      directories: [],
    };
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

  beforeEach(async () => {
    const setup = setupTestBed('1'); // Pass groupId to setup function
    browserService = setup.browserService;

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
  });

  it('should navigate to folder with group path when groupId is set', async () => {
    const router = TestBed.inject(Router);
    spyOn(router, 'navigateByUrl');

    await component.navigateToFolder('/test-folder');

    expect(router.navigateByUrl).toHaveBeenCalledWith('/groups/1/browser/test-folder');
  });

  it('should call browserService.listCached with groupId when refreshing route', async () => {
    const mockDirectory: Directory = {
      name: '/',
      files: [],
      directories: []
    };
    browserService.listCached.and.returnValue(of(mockDirectory));

    await component.refreshRoute();

    expect(browserService.listCached).toHaveBeenCalledWith('/', 1);
  });

  it('should call browserService.addDirectory with groupId when creating new folder', async () => {
    component.currentFolder = {
      name: '/',
      files: [],
      directories: []
    };

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
      directories: []
    };
    browserService.listCached.and.returnValue(of(mockDirectory));

    await component.refreshRoute();

    expect(browserService.listCached).toHaveBeenCalledWith('/parent/child', 1);
  });

  it('should call browserService.addDirectory with groupId in nested folder paths', async () => {
    component.currentFolder = {
      name: '/parent',
      files: [],
      directories: []
    };
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
