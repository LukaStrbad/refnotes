import {Component, ElementRef, EventEmitter, Input, Output, ViewChild} from '@angular/core';
import {FormsModule, ReactiveFormsModule} from "@angular/forms";
import {NgClass} from "@angular/common";
import {ByteSizePipe} from "../../../pipes/byte-size.pipe";

type ModalType = 'file' | 'folder';

@Component({
  selector: 'app-create-new-modal',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    NgClass,
    ByteSizePipe
  ],
  templateUrl: './create-new-modal.component.html',
  styleUrl: './create-new-modal.component.scss'
})
export class CreateNewModalComponent {
  @Input("modalType")
  modalType: ModalType = 'file';
  @Output("onCreate")
  onCreate = new EventEmitter<string>();
  @Output("onUpload")
  onUpload = new EventEmitter<FileList>();
  @Input("uploadProgress")
  uploadProgress: { [key: string]: number | null } = {};

  @ViewChild("modal")
  modal!: ElementRef<HTMLDialogElement>;

  selectedFiles: FileList | null = null;

  constructor() {
    // const dataTransfer = new DataTransfer();
    // const file = new File([''], 'test.txt');
    // dataTransfer.items.add(file);
    // this.selectedFiles = dataTransfer.files;
  }

  get selectedFilesArray(): File[] {
    if (this.selectedFiles) {
      return Array.from(this.selectedFiles);
    }

    return [];
  }

  newName = '';
  isDragOver = false;

  onCreateClick() {
    if (this.onCreate) {
      this.onCreate.emit(this.newName);
    }
  }

  show() {
    this.newName = '';
    this.modal.nativeElement.showModal();
  }

  close() {
    this.modal.nativeElement.close();
  }

  onFilesSelected(event: Event) {
    if (event.target instanceof HTMLInputElement) {
      const files = event.target.files;

      if (files && files.length > 0) {
        this.selectedFiles = files;
      }
    }
  }

  async onFileDrop(event: DragEvent) {
    event.preventDefault();
    const files = event.dataTransfer?.files;

    if (files && files.length > 0) {
      this.selectedFiles = files;
    }
  }

  removeSelectedFile(file: File) {
    if (this.selectedFiles) {
      const list = new DataTransfer();
      Array.from(this.selectedFiles).forEach(f => {
        if (f !== file) {
          list.items.add(f);
        }
      });

      if (list.files.length === 0) {
        this.selectedFiles = null;
      } else {
        this.selectedFiles = list.files;
      }
    }
  }

  onFileDragOver(event: DragEvent) {
    event.preventDefault();
    this.isDragOver = true;
  }

  onFileDragLeave() {
    this.isDragOver = false;
  }

  onUploadClick() {
    console.log('Upload clicked');
    if (this.selectedFiles) {
      console.log('Emitting upload event');
      this.onUpload.emit(this.selectedFiles);
    }
  }
}
