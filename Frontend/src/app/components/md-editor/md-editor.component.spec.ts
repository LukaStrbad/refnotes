import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MdEditorComponent } from './md-editor.component';
import { TranslateFakeLoader, TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import {SettingsService} from "../../../services/settings.service";

describe('MdEditorComponent', () => {
  let component: MdEditorComponent;
  let fixture: ComponentFixture<MdEditorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        MdEditorComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader
          }
        })
      ],
      providers: [
        TranslateService
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MdEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    // Ensure that the editor is visible
    const settings = TestBed.inject(SettingsService);
    settings.setMdEditorSettings({
      editorMode: 'SideBySide',
      showLineNumbers: true,
      wrapLines: false
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
    expect(component.previewContent).toContain('<h1>Title</h1>');
  });

  it("should update previewContent only when it's visible", () => {
    const markdown = '# Title';
    const editorOnlyEl = fixture.nativeElement.querySelector('a[data-test="editorMode-editorOnly"]') as HTMLAnchorElement;
    editorOnlyEl.click();
    component.value.set(markdown);
    fixture.detectChanges();
    // Empty content when preview is not visible
    expect(component.previewContent).toEqual('');

    // Content should update when preview becomes visible
    const previewOnlyEl = fixture.nativeElement.querySelector('a[data-test="editorMode-sideBySide"]') as HTMLAnchorElement;
    previewOnlyEl.click();
    fixture.detectChanges();
    expect(component.previewContent).toContain('<h1>Title</h1>');
  });

  it('should update editorLines when editor is scrolled', async () => {
    const markdown = '# Title\n\nContent';
    component.value.set(markdown);
    fixture.detectChanges();
    const editor = component.editorElementRef.nativeElement;
    editor.scrollTop = 100;
    editor.dispatchEvent(new Event('scroll'));
    fixture.detectChanges();
    expect(component.previewContent).toContain('<h1>Title</h1>');
    expect(component.editorLines?.length).toBeGreaterThan(0);
  });
});
