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
import { provideRouter, Router } from '@angular/router';
import { Observable, of, Subscriber } from 'rxjs';
import { HttpResponse, provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Directory } from '../../model/directory';
import { FileService } from '../../services/file.service';
import { File } from '../../model/file';

function createFile(name: string): File {
  return { name, tags: [], size: 0, created: new Date(), modified: new Date() };
}

describe('BrowserComponent', () => {
  let component: BrowserComponent;
  let fixture: ComponentFixture<BrowserComponent>;
  let browserService: jasmine.SpyObj<BrowserService>;
  let fileService: jasmine.SpyObj<FileService>;
  const storage: Record<string, string> = {};

  beforeEach(async () => {
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

    browserService = jasmine.createSpyObj('BrowserService', [
      'listCached',
      'addDirectory',
      'deleteDirectory',
    ]);
    fileService = jasmine.createSpyObj('FileService', [
      'addTextFile',
      'deleteFile',
      'addFile',
    ]);

    browserService.listCached.and.returnValue(
      of({ name: '/', files: [], directories: [] }),
    );

    await TestBed.configureTestingModule({
      imports: [
        BrowserComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
      providers: [
        provideRouter([
          {
            path: '**',
            component: BrowserComponent,
          },
        ]),
        provideHttpClient(),
        provideHttpClientTesting(),
        LoggerService,
        AuthService,
        { provide: BrowserService, useValue: browserService },
        { provide: FileService, useValue: fileService },
      ],
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
    const authService = TestBed.inject(AuthService);
    authService.user = null;
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
    expect(fileService.addTextFile).toHaveBeenCalledWith('/', 'test.txt', '');
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
    expect(browserService.addDirectory).toHaveBeenCalledWith('/newFolder');
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

  it('should open edit for a file', async () => {
    const router = TestBed.inject(Router);
    spyOn(router, 'navigate');
    await component.openEdit(createFile('test.txt'));
    expect(router.navigate).toHaveBeenCalledWith(['/editor'], {
      queryParams: { directory: '/', file: 'test.txt' },
    });
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
