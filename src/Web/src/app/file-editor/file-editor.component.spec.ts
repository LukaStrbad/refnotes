import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FileEditorComponent } from './file-editor.component';
import {
  TranslateFakeLoader,
  TranslateLoader,
  TranslateModule,
  TranslateService,
} from '@ngx-translate/core';
import { Component, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FileService } from '../../services/file.service';
import { TagService } from '../../services/tag.service';
import { mockActivatedRoute } from '../../tests/route-utils';
import { Location } from '@angular/common';
import { ShareService } from '../../services/components/modals/share.service';
import { FileSyncMessageType } from '../../model/file-sync-message';

@Component({
  selector: 'app-md-editor',
  template: '',
})
class MdEditorStubComponent { }

function setupTestBed() {
  const fileService = jasmine.createSpyObj<FileService>('FileService', [
    'getFile',
    'saveTextFile',
    'moveFile',
    'createFileSyncSocket',
  ]);
  const webSocket = jasmine.createSpyObj<WebSocket>('WebSocket', ['addEventListener', 'close']);
  fileService.createFileSyncSocket.and.returnValue(webSocket);
  const tagService = jasmine.createSpyObj<TagService>('TagService', [
    'listFileTags',
    'addFileTag',
    'removeFileTag',
  ]);
  const shareService = jasmine.createSpyObj<ShareService>('ShareService', ['setPublicState', 'loadPublicLink', 'setFilePath'], {
    fileName: signal(''),
    isPublic: signal(false),
    publicLink: signal<string | null>(null),
    userShareLink: signal<string | null>(null),
  });

  const imports = [
    FileEditorComponent,
    MdEditorStubComponent,
    TranslateModule.forRoot({
      loader: {
        provide: TranslateLoader,
        useClass: TranslateFakeLoader,
      },
    }),
  ];

  const providers = [
    TranslateService,
    {
      provide: ActivatedRoute,
      useValue: { snapshot: { paramMap: {}, url: [] } },
    },
    { provide: FileService, useValue: fileService },
    { provide: TagService, useValue: tagService },
    { provide: ShareService, useValue: shareService },
  ];

  return { fileService, tagService, providers, imports };
}

describe('FileEditorComponent', () => {
  let component: FileEditorComponent;
  let fixture: ComponentFixture<FileEditorComponent>;
  let fileService: jasmine.SpyObj<FileService>;
  let tagService: jasmine.SpyObj<TagService>;

  beforeEach(async () => {
    const setup = setupTestBed();
    fileService = setup.fileService;
    tagService = setup.tagService;

    await TestBed.configureTestingModule({
      imports: setup.imports,
      providers: setup.providers,
    }).compileComponents();

    mockActivatedRoute({
      path: '/test/test.txt',
    });
  });

  it('should create', () => {
    fileService.getFile.and.resolveTo(new ArrayBuffer(0));
    tagService.listFileTags.and.resolveTo([]);

    fixture = TestBed.createComponent(FileEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component).toBeTruthy();
  });

  it('should load file content', async () => {
    fileService.getFile.and.resolveTo(
      new TextEncoder().encode('test').buffer as ArrayBuffer,
    );
    tagService.listFileTags.and.resolveTo([]);

    fixture = TestBed.createComponent(FileEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    await fixture.whenStable();

    expect(component.content).toBe('test');
    expect(component.loading).toBeFalse();
  });

  it('should show loading skeleton', async () => {
    let resolve: ((value?: unknown) => void) | null = null;
    const waitPromise = new Promise((r) => {
      resolve = r;
    });
    fileService.getFile.and.callFake(async () => {
      await waitPromise;
      const buffer = new TextEncoder().encode('test');
      const arrayBuffer = buffer.buffer as ArrayBuffer;
      return arrayBuffer;
    });
    tagService.listFileTags.and.resolveTo([]);

    fixture = TestBed.createComponent(FileEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    await fixture.whenStable();

    let skeleton = fixture.nativeElement.querySelector('.skeleton');
    expect(skeleton).toBeTruthy();
    expect(component.content).toBe('');
    expect(component.loading).toBeTrue();

    resolve!();
    await waitPromise;
    await fixture.whenStable();
    fixture.detectChanges();

    skeleton = fixture.nativeElement.querySelector('.skeleton');
    expect(skeleton).toBeFalsy();
    expect(component.content).toBe('test');
    expect(component.loading).toBeFalse();
  });

  it('should save file content', async () => {
    fileService.getFile.and.resolveTo(new ArrayBuffer(0));
    tagService.listFileTags.and.resolveTo([]);

    fixture = TestBed.createComponent(FileEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    await fixture.whenStable();

    component.content = 'test';
    const saveButton = fixture.nativeElement.querySelector(
      '[data-test="save-button"]',
    );
    saveButton.click();

    expect(fileService.saveTextFile).toHaveBeenCalledWith(
      '/test',
      'test.txt',
      'test',
      undefined,
      jasmine.any(String),
    );
  });

  it('should add tag', async () => {
    fileService.getFile.and.resolveTo(new ArrayBuffer(0));
    tagService.listFileTags.and.resolveTo([]);

    fixture = TestBed.createComponent(FileEditorComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();

    await component.addTag(['test.txt', 'tag1']);
    expect(tagService.addFileTag).toHaveBeenCalledWith(
      '/test',
      'test.txt',
      'tag1',
      undefined,
    );
    expect(component.tags).toContain('tag1');

    // Test duplicate addition does not add tag again
    await component.addTag(['file.txt', 'tag1']);
    expect(component.tags.filter((t) => t === 'tag1').length).toBe(1);
  });

  it('should remove tag', async () => {
    fileService.getFile.and.resolveTo(new ArrayBuffer(0));
    tagService.listFileTags.and.resolveTo(['tag1', 'tag2']);

    fixture = TestBed.createComponent(FileEditorComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();

    await component.removeTag(['test.txt', 'tag1']);
    expect(tagService.removeFileTag).toHaveBeenCalledWith(
      '/test',
      'test.txt',
      'tag1',
      undefined,
    );
    expect(component.tags).not.toContain('tag1');
    expect(component.tags).toContain('tag2');
  });

  it('should rename file', async () => {
    fileService.moveFile.and.resolveTo();
    fileService.getFile.and.resolveTo(new ArrayBuffer(0));
    tagService.listFileTags.and.resolveTo([]);
    const location = TestBed.inject(Location);
    spyOn(location, 'replaceState');

    fixture = TestBed.createComponent(FileEditorComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();

    await component.renameFile(['test.txt', 'new-name.txt']);

    expect(fileService.moveFile).toHaveBeenCalledWith(
      "/test/test.txt",
      "/test/new-name.txt",
      undefined,
    );
    expect(location.replaceState).toHaveBeenCalledWith('/file/%2Ftest%2Fnew-name.txt/edit');
  });

  it('should rename file in root directory', async () => {
    fileService.moveFile.and.resolveTo();
    fileService.getFile.and.resolveTo(new ArrayBuffer(0));
    tagService.listFileTags.and.resolveTo([]);

    mockActivatedRoute(
      {
        path: '/test.txt',
      }
    );

    fixture = TestBed.createComponent(FileEditorComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();

    await component.renameFile(['test.txt', 'new-name.txt']);

    expect(fileService.moveFile).toHaveBeenCalledWith(
      "/test.txt",
      "/new-name.txt",
      undefined,
    );
  });

  it('should create file sync socket on init', async () => {
    tagService.listFileTags.and.resolveTo([]);
    mockActivatedRoute({
      path: '/test.txt',
    });

    const webSocket = jasmine.createSpyObj<WebSocket>('WebSocket', ['addEventListener', 'close']);
    fileService.createFileSyncSocket.and.returnValue(webSocket);

    fixture = TestBed.createComponent(FileEditorComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fileService.createFileSyncSocket).toHaveBeenCalledWith(
      '/test.txt',
      undefined,
    );
  });

  it('should send client ID on socket open', async () => {
    tagService.listFileTags.and.resolveTo([]);
    mockActivatedRoute({
      path: '/test.txt',
    });

    const webSocket = jasmine.createSpyObj<WebSocket>('WebSocket', ['addEventListener', 'send', 'close']);
    fileService.createFileSyncSocket.and.returnValue(webSocket);

    fixture = TestBed.createComponent(FileEditorComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();

    expect(webSocket.addEventListener).toHaveBeenCalledWith('open', jasmine.any(Function));
    const openCallback = webSocket.addEventListener.calls.all()
      .find(call => call.args[0] === 'open')?.args[1];
    if (typeof openCallback === 'function') {
      openCallback(new Event('open'));
    }

    expect(webSocket.send).toHaveBeenCalledWith(
      JSON.stringify({
        messageType: FileSyncMessageType.ClientId,
        clientId: component['clientId'],
      }),
    );
  });

  it('should handle file update message', async () => {
    tagService.listFileTags.and.resolveTo([]);
    mockActivatedRoute({
      path: '/test.txt',
    });

    const webSocket = jasmine.createSpyObj<WebSocket>('WebSocket', ['addEventListener', 'send', 'close']);
    fileService.createFileSyncSocket.and.returnValue(webSocket);

    fixture = TestBed.createComponent(FileEditorComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    // Clear getFile call to avoid conflicts
    fileService.getFile.calls.reset();

    expect(webSocket.addEventListener).toHaveBeenCalledWith('message', jasmine.any(Function));
    const messageCallback = webSocket.addEventListener.calls.all()
      .find(call => call.args[0] === 'message')?.args[1];
    if (typeof messageCallback === 'function') {
      const event = new MessageEvent('message', {
        data: JSON.stringify({
          messageType: FileSyncMessageType.UpdateTime,
          senderClientId: 'other-client',
        }),
      });
      messageCallback(event);
    }

    expect(fileService.getFile).toHaveBeenCalled();
  });

});

describe('FileEditorComponent with groupId', () => {
  let component: FileEditorComponent;
  let fixture: ComponentFixture<FileEditorComponent>;
  let fileService: jasmine.SpyObj<FileService>;
  let tagService: jasmine.SpyObj<TagService>;

  beforeEach(async () => {
    const setup = setupTestBed();
    fileService = setup.fileService;
    tagService = setup.tagService;

    await TestBed.configureTestingModule({
      imports: setup.imports,
      providers: setup.providers,
    }).compileComponents();

    mockActivatedRoute({
      path: '/test/test.txt',
      groupId: '123',
    });

    fileService.moveFile.and.resolveTo();
    fileService.getFile.and.resolveTo(new ArrayBuffer(0));
    tagService.listFileTags.and.resolveTo([]);
    const location = TestBed.inject(Location);
    spyOn(location, 'replaceState');

    fixture = TestBed.createComponent(FileEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should pass groupId to getFile on component creation', () => {
    expect(fileService.getFile).toHaveBeenCalledWith(
      '/test',
      'test.txt',
      123,
    );
  });

  it('should pass groupId to listFileTags on component creation', () => {
    expect(tagService.listFileTags).toHaveBeenCalledWith(
      '/test',
      'test.txt',
      123,
    );
  });

  it('should pass groupId to saveTextFile when saving content', async () => {
    await fixture.whenStable();

    component.content = 'test content';
    await component.saveContent();

    expect(fileService.saveTextFile).toHaveBeenCalledWith(
      '/test',
      'test.txt',
      'test content',
      123,
      jasmine.any(String),
    );
  });

  it('should pass groupId to addFileTag when adding tag', async () => {
    await component.addTag(['test.txt', 'new-tag']);

    expect(tagService.addFileTag).toHaveBeenCalledWith(
      '/test',
      'test.txt',
      'new-tag',
      123,
    );
  });

  it('should pass groupId to removeFileTag when removing tag', async () => {
    await component.removeTag(['test.txt', 'existing-tag']);

    expect(tagService.removeFileTag).toHaveBeenCalledWith(
      '/test',
      'test.txt',
      'existing-tag',
      123,
    );
  });

  it('should pass groupId to moveFile when renaming file', async () => {
    await component.renameFile(['test.txt', 'renamed.txt']);

    expect(fileService.moveFile).toHaveBeenCalledWith(
      '/test/test.txt',
      '/test/renamed.txt',
      123,
    );
  });

  it('should update URL with groupId when renaming file', async () => {
    const location = TestBed.inject(Location);

    await component.renameFile(['test.txt', 'renamed.txt']);

    expect(location.replaceState).toHaveBeenCalledWith('/groups/123/file/%2Ftest%2Frenamed.txt/edit');
  });
});
