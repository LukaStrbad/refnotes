import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FileIconComponent } from './file-icon.component';

describe('FileIconComponent', () => {
  let component: FileIconComponent;
  let fixture: ComponentFixture<FileIconComponent>;
  let nativeElement: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FileIconComponent]
    })
      .compileComponents();
  });

  function createFixture(fileName: string) {
    fixture = TestBed.createComponent(FileIconComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('fileName', fileName);
    fixture.detectChanges();

    nativeElement = fixture.nativeElement as HTMLElement;
  }

  it('should set markdown file type', () => {
    createFixture('test-file.md');

    const icon = nativeElement.querySelector('i') as HTMLElement;

    expect(component.fileType()).toBe('markdown');
    expect(icon.classList).toContain('bi-filetype-md');
  });

  it('should set text file type', () => {
    createFixture('test-file.txt');

    const icon = nativeElement.querySelector('i') as HTMLElement;

    expect(component.fileType()).toBe('text');
    expect(icon.classList).toContain('bi-file-text');
  });

  it('should set image file type', () => {
    createFixture('test-image.png');

    const icon = nativeElement.querySelector('i') as HTMLElement;

    expect(component.fileType()).toBe('image');
    expect(icon.classList).toContain('bi-file-image');
  });

  it('should set unknown file type', () => {
    createFixture('test-file.unknown');

    const icon = nativeElement.querySelector('i') as HTMLElement;

    expect(component.fileType()).toBe('unknown');
    expect(icon.classList).toContain('bi-file-earmark');
  });
});
