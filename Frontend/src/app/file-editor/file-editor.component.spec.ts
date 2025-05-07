import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FileEditorComponent } from './file-editor.component';
import {
  TranslateFakeLoader,
  TranslateLoader,
  TranslateModule,
  TranslateService,
} from '@ngx-translate/core';
import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FileService } from '../../services/file.service';
import { TagService } from '../../services/tag.service';
import { mockActivatedRoute } from '../../tests/route-utils';
import { Location } from '@angular/common';

@Component({
  selector: 'app-md-editor',
  template: '',
})
class MdEditorStubComponent { }

describe('FileEditorComponent', () => {
  let component: FileEditorComponent;
  let fixture: ComponentFixture<FileEditorComponent>;
  let fileService: jasmine.SpyObj<FileService>;
  let tagService: jasmine.SpyObj<TagService>;

  beforeEach(async () => {
    fileService = jasmine.createSpyObj('FileService', [
      'getFile',
      'saveTextFile',
      'moveFile',
    ]);
    tagService = jasmine.createSpyObj('TagService', [
      'listFileTags',
      'addFileTag',
      'removeFileTag',
    ]);

    await TestBed.configureTestingModule({
      imports: [
        FileEditorComponent,
        MdEditorStubComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
      providers: [
        TranslateService,
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: {} } },
        },
        { provide: FileService, useValue: fileService },
        { provide: TagService, useValue: tagService },
      ],
    }).compileComponents();

    mockActivatedRoute('/test', 'test.txt');
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
    );
    expect(component.tags).not.toContain('tag1');
    expect(component.tags).toContain('tag2');
  });

  it('should rename file', async () => {
    fileService.moveFile.and.resolveTo();
    fileService.getFile.and.resolveTo(new ArrayBuffer(0));
    tagService.listFileTags.and.resolveTo([]);
    const location = TestBed.inject(Location);
    let newPath = '';
    spyOn(location, 'replaceState').and.callFake((path) => (newPath = path));

    fixture = TestBed.createComponent(FileEditorComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();

    await component.renameFile(['test.txt', 'new-name.txt']);

    expect(fileService.moveFile).toHaveBeenCalledWith(
      "/test/test.txt",
      "/test/new-name.txt",
    );
    expect(newPath).toContain('file=new-name.txt');
  });

  it('should rename file in root directory', async () => {
    fileService.moveFile.and.resolveTo();
    fileService.getFile.and.resolveTo(new ArrayBuffer(0));
    tagService.listFileTags.and.resolveTo([]);

    mockActivatedRoute('/', 'test.txt');

    fixture = TestBed.createComponent(FileEditorComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();

    await component.renameFile(['test.txt', 'new-name.txt']);

    expect(fileService.moveFile).toHaveBeenCalledWith(
      "/test.txt",
      "/new-name.txt",
    );
  });
});
