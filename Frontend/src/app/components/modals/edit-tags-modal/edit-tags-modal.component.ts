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
  selector: 'app-edit-tags-modal',
  imports: [TranslateDirective, TranslatePipe, FormsModule, TestTagDirective],
  templateUrl: './edit-tags-modal.component.html',
  styleUrl: './edit-tags-modal.component.css',
})
export class EditTagsModalComponent {
  @ViewChild('modal')
  modal!: ElementRef<HTMLDialogElement>;

  /**
   * Emits when a tag is added.
   * The event payload is a tuple with the file name and the tag.
   */
  @Output('onAdd')
  onAdd = new EventEmitter<[string, string]>();

  /**
   * Emits when a tag is removed.
   * The event payload is a tuple with the file name and the tag.
   */
  @Output('onRemove')
  onRemove = new EventEmitter<[string, string]>();

  fileName = '';
  tags: string[] = [];
  newTag: string = '';

  show(fileName: string, tags: string[]) {
    this.fileName = fileName;
    // Copy the tags array to avoid modifying the original array.
    this.tags = [...tags];
    this.modal.nativeElement.showModal();
  }

  hide() {
    this.modal.nativeElement.close();
  }

  addTag() {
    if (this.newTag.trim() === '') {
      return;
    }

    this.tags.push(this.newTag);
    this.onAdd.emit([this.fileName, this.newTag]);
    this.newTag = '';
  }

  removeTag(tag: string) {
    this.tags = this.tags.filter((t) => t !== tag);
    this.onRemove.emit([this.fileName, tag]);
  }
}
