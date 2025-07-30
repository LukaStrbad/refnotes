import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MdEditorComponent } from './md-editor.component';
import {
  TranslateFakeLoader,
  TranslateLoader,
  TranslateModule,
  TranslateService,
} from '@ngx-translate/core';
import { SettingsService } from '../../../services/settings.service';
import { FileService } from '../../../services/file.service';
import { ActivatedRoute } from '@angular/router';

describe('MdEditorComponent', () => {
  let component: MdEditorComponent;
  let fixture: ComponentFixture<MdEditorComponent>;
  let fileService: jasmine.SpyObj<FileService>;

  const activatedRoute = {
    snapshot: {
      paramMap: {
        get: () => null,
      }
    }
  }

  beforeEach(async () => {
    fileService = jasmine.createSpyObj('FileService', ['getImage']);

    await TestBed.configureTestingModule({
      imports: [
        MdEditorComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
      providers: [
        TranslateService,
        { provide: FileService, useValue: fileService },
        { provide: ActivatedRoute, useValue: activatedRoute },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MdEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    // Ensure that the editor is visible
    const settings = TestBed.inject(SettingsService);
    settings.setMdEditorSettings({
      useWysiwyg: false,
      editorMode: 'SideBySide',
      showLineNumbers: true,
      wrapLines: false,
      experimentalFastRender: false,
    });
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should handle Tab key press in onEditorKeydown', () => {
    const event = new KeyboardEvent('keydown', { key: 'Tab' });
    const editor = component.editorElementRef.nativeElement;
    editor.dispatchEvent(event);
    expect(component.value()).toBe('    ');
  });

  it('should set isMobile to true on window resize below 640px', () => {
    spyOnProperty(window, 'innerWidth').and.returnValue(600);
    window.dispatchEvent(new Event('resize'));
    expect(component.isMobile).toBeTrue();
  });

  it('should set isMobile to false on window resize above 640px', () => {
    spyOnProperty(window, 'innerWidth').and.returnValue(800);
    window.dispatchEvent(new Event('resize'));
    expect(component.isMobile).toBeFalse();
  });

  it('should update previewContent when value changes', () => {
    const markdown = '# Title';
    component.value.set(markdown);
    fixture.detectChanges();
    const preview = component.previewContentElement.nativeElement;
    expect(preview.innerHTML).toContain('<h1>Title</h1>');
  });

  it("should update preview only when it's visible", () => {
    const markdown = '# Title';
    const editorOnlyEl = fixture.nativeElement.querySelector(
      'a[data-test="editorMode-editorOnly"]',
    ) as HTMLAnchorElement;
    editorOnlyEl.click();
    component.value.set(markdown);
    fixture.detectChanges();
    // Empty content when preview is not visible
    let preview = component.previewContentElement.nativeElement;
    expect(preview.checkVisibility()).toBeFalsy();
    expect(preview.innerHTML).toBe('');

    // Content should update when preview becomes visible
    const previewOnlyEl = fixture.nativeElement.querySelector(
      'a[data-test="editorMode-sideBySide"]',
    ) as HTMLAnchorElement;
    previewOnlyEl.click();
    fixture.detectChanges();
    preview = component.previewContentElement.nativeElement;
    expect(preview.innerHTML).toContain('<h1>Title</h1>');
  });

  it('should update editorLines when editor is scrolled', async () => {
    const markdown = '# Title\n\nContent';
    component.value.set(markdown);
    fixture.detectChanges();
    const editor = component.editorElementRef.nativeElement;
    editor.scrollTop = 100;
    editor.dispatchEvent(new Event('scroll'));
    fixture.detectChanges();
    const preview = component.previewContentElement.nativeElement;
    expect(preview.innerHTML).toContain('<h1>Title</h1>');
    expect(component.editorLines?.length).toBeGreaterThan(0);
  });

  it('should render image in preview (http)', () => {
    const markdown = '![image](https://example.com/image.jpg "Image Title")';
    component.value.set(markdown);
    fixture.detectChanges();
    const preview = component.previewContentElement.nativeElement;
    expect(preview.innerHTML).toContain(
      '<img src="https://example.com/image.jpg" alt="image" title="Image Title">',
    );
  });

  it('should update image in preview (relative path)', async () => {
    const buffer = new Uint8Array([0, 1, 2, 3]).buffer;
    fileService.getImage.and.resolveTo(buffer);
    const markdown = '![image](./image.jpg "Image Title")';
    component.value.set(markdown);
    fixture.detectChanges();
    await fixture.whenStable();

    const preview = component.previewContentElement.nativeElement;
    const img = preview.querySelector('img') as HTMLImageElement;
    expect(img).toBeTruthy();
    expect(img.src).toContain('blob:');
    expect(img.alt).toBe('image');
    expect(img.title).toBe('Image Title');
  });
});
