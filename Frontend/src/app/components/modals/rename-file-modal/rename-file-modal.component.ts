import {
  Component,
  ElementRef,
  EventEmitter,
  Output,
  ViewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateDirective, TranslatePipe } from '@ngx-translate/core';
import { TestTagDirective } from '../../../../directives/test-tag.directive';

@Component({
  selector: 'app-rename-file-modal',
  imports: [TranslateDirective, TranslatePipe, FormsModule, TestTagDirective],
  templateUrl: './rename-file-modal.component.html',
  styleUrl: './rename-file-modal.component.css',
})
export class RenameFileModalComponent {
  @ViewChild('modal')
  modal!: ElementRef<HTMLDialogElement>;

  /**
   * Emits when a file is renamed.
   * The event payload is a tuple with the old file name and the new file name.
   */
  @Output()
  onRename = new EventEmitter<[string, string]>();

  originalFileName = '';
  newFileName = '';

  show(fileName: string) {
    this.originalFileName = fileName;
    this.newFileName = fileName;
    this.modal.nativeElement.showModal();
  }

  hide() {
    this.modal.nativeElement.close();
  }

  saveChanges() {
    if (this.newFileName.trim() === '' || this.newFileName === this.originalFileName) {
      return;
    }

    this.onRename.emit([this.originalFileName, this.newFileName]);
    this.hide();
  }
}
