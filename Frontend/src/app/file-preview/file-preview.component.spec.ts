import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FilePreviewComponent } from './file-preview.component';
import { TranslateFakeLoader, TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { ActivatedRoute } from '@angular/router';
import { FileService } from '../../services/file.service';
import { TagService } from '../../services/tag.service';
import { SettingsService } from '../../services/settings.service';
import { By } from '@angular/platform-browser';
import { mockActivatedRoute } from '../../tests/route-utils';

describe('FilePreviewComponent', () => {
  let fixture: ComponentFixture<FilePreviewComponent>;
  let fileService: jasmine.SpyObj<FileService>;
  let tagService: jasmine.SpyObj<TagService>;

  const createComponent = () => {
    fixture = TestBed.createComponent(FilePreviewComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    return component;
  }

  beforeEach(async () => {
    fileService = jasmine.createSpyObj('FileService', ['getFile', 'getImage', 'getFileInfo']);
    tagService = jasmine.createSpyObj('TagService', ['listFileTags']);

    await TestBed.configureTestingModule({
      imports: [
        FilePreviewComponent,
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
          useValue: { snapshot: { paramMap: { get: () => '/test' } } },
        },
        { provide: FileService, useValue: fileService },
        { provide: TagService, useValue: tagService },
      ],
    }).compileComponents();

    // Ensure that the editor is visible
    const settings = TestBed.inject(SettingsService);
    settings.setMdEditorSettings({
      editorMode: 'SideBySide',
      showLineNumbers: true,
      wrapLines: false,
      experimentalFastRender: false,
    });

    tagService.listFileTags.and.resolveTo(['tag1', 'tag2']);
    fileService.getFileInfo.and.resolveTo({
      name: 'test.md',
      size: 1234,
      modified: new Date(),
      created: new Date(),
      tags: ['tag1', 'tag2'],
    });
  });

  it('should create', () => {
    const component = createComponent();

    expect(component).toBeTruthy();
  });

  it('should render image in markdown preview (http)', async () => {
    const markdown = '![image](https://example.com/image.jpg "Image Title")';
    const markdownArray = new TextEncoder().encode(markdown);

    mockActivatedRoute('test', 'test.md');
    fileService.getFile.and.resolveTo(markdownArray.buffer as ArrayBuffer);

    const component = createComponent();
    await fixture.whenStable();
    fixture.detectChanges();

    const previewElement = fixture.debugElement.query(By.css('[data-test="preview.content"]')).nativeElement as HTMLElement;

    expect(component.fileType).toBe('markdown');
    expect(previewElement.innerHTML).toContain(
      '<img src="https://example.com/image.jpg" alt="image" title="Image Title">',
    );
  });

  it('should update image in markdown preview (relative path)', async () => {
    const buffer = new Uint8Array([0, 1, 2, 3]).buffer;
    fileService.getImage.and.resolveTo(buffer);
    const markdown = '![image](./image.jpg "Image Title")';
    const markdownArray = new TextEncoder().encode(markdown);

    mockActivatedRoute('test', 'test.md');
    fileService.getFile.and.resolveTo(markdownArray.buffer as ArrayBuffer);

    createComponent();
    await fixture.whenStable();
    fixture.detectChanges();

    const previewElement = fixture.debugElement.query(By.css('[data-test="preview.content"]')).nativeElement as HTMLElement;
    const img = previewElement.querySelector('img') as HTMLImageElement;
    expect(img).toBeTruthy();
    expect(img.src).toContain('blob:');
    expect(img.alt).toBe('image');
    expect(img.title).toBe('Image Title');
  });

  it('should show image from file', async () => {
    const buffer = new Uint8Array([0, 1, 2, 3]).buffer;
    fileService.getImage.and.resolveTo(buffer);

    mockActivatedRoute('test', 'image.jpg');

    createComponent();
    await fixture.whenStable();
    fixture.detectChanges();

    const previewElement = fixture.debugElement.query(By.css('img[data-test="preview.image"]')).nativeElement as HTMLImageElement;
    expect(previewElement).toBeTruthy();
    expect(previewElement.checkVisibility()).toBeTrue();
    expect(previewElement.src).toContain('blob:');
    expect(previewElement.alt).toBe('image.jpg');
  });
});
