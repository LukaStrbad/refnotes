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
  tags: TagWithStatus[] = [];
  newTag: string = '';

  show(fileName: string, tags: string[]) {
    this.fileName = fileName;
    // Copy the tags array to avoid modifying the original array.
    this.tags = tags.map((tag) => ({ name: tag, deleted: false }));
    this.modal.nativeElement.showModal();
  }

  hide() {
    this.modal.nativeElement.close();
  }

  restoreTag(tag: string) {
    const tagIndex = this.tags.findIndex((t) => t.name === tag);
    if (tagIndex === -1) {
      return;
    }

    this.tags[tagIndex].deleted = false;
    this.onAdd.emit([this.fileName, tag]);
  }

  addTag() {
    if (this.newTag.trim() === '') {
      return;
    }

    const existingTag = this.tags.find((t) => t.name === this.newTag);
    if (existingTag) {
      existingTag.deleted = false;
    } else {
      this.tags.push({ name: this.newTag, deleted: false });
    }
    this.onAdd.emit([this.fileName, this.newTag]);
    this.newTag = '';
  }

  removeTag(tag: string) {
    const tagIndex = this.tags.findIndex((t) => t.name === tag);
    if (tagIndex === -1) {
      return;
    }

    this.tags[tagIndex].deleted = true;
    this.onRemove.emit([this.fileName, tag]);
  }
}

interface TagWithStatus {
  name: string;
  deleted: boolean;
}
