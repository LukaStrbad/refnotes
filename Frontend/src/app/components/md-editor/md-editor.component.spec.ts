import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MdEditorComponent } from './md-editor.component';
import { TranslateFakeLoader, TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';

describe('MdEditorComponent', () => {
  let component: MdEditorComponent;
  let fixture: ComponentFixture<MdEditorComponent>;
  let editor: HTMLTextAreaElement;

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

    editor = fixture.nativeElement.querySelector('.editor-textarea') as HTMLTextAreaElement;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should handle Tab key press in onEditorKeydown', () => {
    const event = new KeyboardEvent('keydown', { key: 'Tab' });
    editor.dispatchEvent(event);
    expect(component.value()).toBe('    ');
  });

  it('should set isMobile to true on window resize below 640px', () => {
    window.innerWidth = 600;
    window.dispatchEvent(new Event('resize'));
    expect(component.isMobile).toBeTrue();
  });

  it('should set isMobile to false on window resize above 640px', () => {
    window.innerWidth = 800;
    window.dispatchEvent(new Event('resize'));
    expect(component.isMobile).toBeFalse();
  });

  it('should update previewContent when value changes', () => {
    const markdown = '# Title';
    component.value.set(markdown);
    fixture.detectChanges();
    expect(component.previewContent).toContain('<h1>Title</h1>');
  });

  it('should update editorLines when editor is scrolled', () => {
    const markdown = '# Title\n\nContent';
    component.value.set(markdown);
    editor.scrollTop = 100;
    editor.dispatchEvent(new Event('scroll'));
    fixture.detectChanges();
    expect(component.previewContent).toContain('<h1>Title</h1>');
    expect(component.editorLines?.length).toBeGreaterThan(0);
  });
});
