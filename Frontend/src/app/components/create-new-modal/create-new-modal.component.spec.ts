import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateNewModalComponent } from './create-new-modal.component';
import {
  TranslateFakeLoader,
  TranslateLoader,
  TranslateModule,
  TranslateService,
} from '@ngx-translate/core';
import {By} from "@angular/platform-browser";

describe('CreateNewModalComponent', () => {
  let component: CreateNewModalComponent;
  let fixture: ComponentFixture<CreateNewModalComponent>;
  let modal: HTMLDialogElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        CreateNewModalComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
      providers: [TranslateService],
    }).compileComponents();

    fixture = TestBed.createComponent(CreateNewModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    modal = fixture.nativeElement.querySelector('.modal') as HTMLDialogElement;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should emit onCreate event with newName when onCreateClick is called', () => {
    let newName = '';
    component.create.subscribe(name => (newName = name));
    component.newName = 'New File';
    component.onCreateClick();

    expect(newName).toBe('New File');
  });

  it('should show the modal and reset newName when show is called', () => {
    component.newName = 'Old Name';
    component.show();

    expect(modal.open).toBeTrue();
    expect(component.newName).toBe('');
  });

  it('should close the modal and reset selectedFiles when close is called', () => {
    component.show();
    component.selectedFiles = new DataTransfer().files;
    component.close();

    expect(component.selectedFiles).toBeNull();
    expect(modal.open).toBeFalse();
  });

  it('should update selectedFiles when files are selected', () => {
    const input = document.createElement('input');
    input.type = 'file';
    const dataTransfer = new DataTransfer();
    dataTransfer.items.add(new File([''], 'test.txt'));
    dataTransfer.items.add(new File([''], 'test2.txt'));
    input.files = dataTransfer.files;

    const event = new Event('change');
    Object.defineProperty(event, 'target', { value: input });

    component.onFilesSelected(event);
    fixture.detectChanges();

    const fileElements = fixture.debugElement.queryAll(By.css('[data-test="file-info"]'));

    expect(component.selectedFiles).toEqual(dataTransfer.files);
    expect(fileElements.length).toBe(2);
    expect(fileElements[0].nativeElement.textContent).toContain('test.txt');
    expect(fileElements[1].nativeElement.textContent).toContain('test2.txt');
  });

  it('should update selectedFiles when files are dropped', () => {
    const dataTransfer = new DataTransfer();
    const file = new File([''], 'test.txt');
    dataTransfer.items.add(file);

    const event = new DragEvent('drop', { dataTransfer });

    component.onFileDrop(event);
    fixture.detectChanges();

    const fileElements = fixture.debugElement.queryAll(By.css('[data-test="file-info"]'));

    expect(component.selectedFiles).toEqual(dataTransfer.files);
    expect(fileElements.length).toBe(1);
  });

  it('should remove the specified file from selectedFiles', () => {
    const dataTransfer = new DataTransfer();
    const file1 = new File([''], 'test1.txt');
    const file2 = new File([''], 'test2.txt');
    dataTransfer.items.add(file1);
    dataTransfer.items.add(file2);
    component.selectedFiles = dataTransfer.files;

    fixture.detectChanges();

    let fileElements = fixture.debugElement.queryAll(By.css('[data-test="file-info"]'));
    const fileElement1 = fileElements[0];
    const button = fileElement1.query(By.css('[data-test="remove-file-button"]')).nativeElement;
    if (button instanceof HTMLButtonElement) {
      button.click();
    }

    fixture.detectChanges();

    fileElements = fixture.debugElement.queryAll(By.css('[data-test="file-info"]'));

    expect(fileElements.length).toBe(1);
    expect(fileElements[0].nativeElement.textContent).toContain('test2.txt');
  });

  it('should set background color when dragover event is triggered', () => {
    const target = fixture.debugElement.query(By.css('[data-test="drag-and-drop-target"]')).nativeElement as HTMLElement;
    const bgColor = window.getComputedStyle(target).backgroundColor;
    target.dispatchEvent(new DragEvent('dragover'));
    fixture.detectChanges();

    expect(component.isDragOver).toBeTrue();
    expect(window.getComputedStyle(target).backgroundColor).not.toEqual(bgColor);

    target.dispatchEvent(new DragEvent('dragleave'));
    fixture.detectChanges();

    expect(component.isDragOver).toBeFalse();
    expect(window.getComputedStyle(target).backgroundColor).toEqual(bgColor);
  });

  it('should emit onUpload event with selectedFiles when onUploadClick is called', () => {
    const emitSpy = spyOn(component.upload, 'emit');

    const input = document.createElement('input');
    input.type = 'file';
    const dataTransfer = new DataTransfer();
    dataTransfer.items.add(new File([''], 'test.txt'));
    input.files = dataTransfer.files;

    const event = new Event('change');
    Object.defineProperty(event, 'target', { value: input });

    component.onFilesSelected(event);
    fixture.detectChanges();

    const uploadButton = fixture.debugElement.query(By.css('[data-test="upload-button"]')).nativeElement;
    uploadButton.click();

    expect(emitSpy).toHaveBeenCalledWith(dataTransfer.files);
  });
});
