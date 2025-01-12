import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

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
import {Observable, of, Subscriber} from 'rxjs';
import {
  HttpResponse,
  provideHttpClient,
} from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Directory } from '../../model/directory';

describe('BrowserComponent', () => {
  let component: BrowserComponent;
  let fixture: ComponentFixture<BrowserComponent>;
  let browserService: jasmine.SpyObj<BrowserService>;

  beforeEach(async () => {
    browserService = jasmine.createSpyObj('BrowserService', [
      'listCached',
      'addTextFile',
      'addDirectory',
      'deleteFile',
      'deleteDirectory',
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
      files: ['test.txt'],
      directories: [],
    }
    const networkDirectory: Directory = {
      name: '/',
      files: ['test.txt', 'test2.txt'],
      directories: ['dir'],
    }

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
      files: ['test.txt'],
      directories: ['dir'],
    };
    browserService.listCached.and.returnValue(of(mockDirectory));
    await component.refreshRoute();
    fixture.detectChanges();

    const folderTrs = fixture.nativeElement.querySelectorAll('tr[data-test="folder-tr"]');
    const fileTrs = fixture.nativeElement.querySelectorAll('tr[data-test="file-tr"]');
    expect(folderTrs.length).toBe(1);
    expect(fileTrs.length).toBe(1);

    expect(component.currentFolder).toEqual(mockDirectory);
  });

  it('should create a new file', async () => {
    await component.createNewFile('test.txt');
    fixture.detectChanges();

    const fileTrs = fixture.nativeElement.querySelectorAll('tr[data-test="file-tr"]');

    expect(fileTrs.length).toBe(1);
    expect(component.currentFolder!.files).toContain('test.txt');
    expect(browserService.addTextFile).toHaveBeenCalledWith(
      '/',
      'test.txt',
      '',
    );
  });

  it('should create a new folder', async () => {
    await component.createNewFolder('newFolder');
    fixture.detectChanges();

    const folderTrs = fixture.nativeElement.querySelectorAll('tr[data-test="folder-tr"]');

    expect(folderTrs.length).toBe(1);
    expect(component.currentFolder!.directories).toContain('newFolder');
    expect(browserService.addDirectory).toHaveBeenCalledWith('/newFolder');
  });

  it('should delete a file', async () => {
    browserService.listCached.and.returnValue(
      of({ name: '/', files: ['test.txt'], directories: [] }),
    );
    component.ngOnInit();
    await component.loadingPromise;
    expect(component.currentFolder?.files).toContain('test.txt');

    browserService.deleteFile.and.callFake((directoryPath, name) => {
      browserService.listCached.and.returnValue(
        of({ name: '/', files: [], directories: [] }),
      );
      return Promise.resolve(Object);
    });

    await component.deleteFile('test.txt');
    fixture.detectChanges();

    const fileTrs = fixture.nativeElement.querySelectorAll('tr[data-test="file-tr"]');

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

    browserService.deleteDirectory.and.callFake((path) => {
      browserService.listCached.and.returnValue(
        of({ name: '/', files: [], directories: [] }),
      );
      return Promise.resolve(Object);
    });

    await component.deleteFolder('testFolder');
    fixture.detectChanges();

    const folderTrs = fixture.nativeElement.querySelectorAll('tr[data-test="folder-tr"]');

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

    const breadcrumb = fixture.nativeElement.querySelector('a[data-test="breadcrumb-item"][href="/browser/newFolder"]');

    expect(breadcrumb).toBeTruthy();
    expect(component.currentPath).toBe('/newFolder');
  });

  it('should upload files', async () => {
    const mockFile = new File([''], 'test.txt');
    const fileList = {
      0: mockFile,
      length: 1,
      item: (index: number) => mockFile,
    } as FileList;
    browserService.addFile.and.returnValue(
      of(new HttpResponse<Object>({ status: 200 })),
    );
    spyOn(component.fileModal, 'close');
    await component.onFilesUpload(fileList);
    expect(component.fileModal.close).toHaveBeenCalled();
  });

  it('should open edit for a file', async () => {
    const router = TestBed.inject(Router);
    spyOn(router, 'navigate');
    await component.openEdit('test.txt');
    expect(router.navigate).toHaveBeenCalledWith(['/editor'], {
      queryParams: { directory: '/', file: 'test.txt' },
    });
  });
});
