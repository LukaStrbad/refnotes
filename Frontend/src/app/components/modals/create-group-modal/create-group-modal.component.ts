import { Component, ElementRef, EventEmitter, Output, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateDirective } from '@ngx-translate/core';
import { TestTagDirective } from '../../../../directives/test-tag.directive';

@Component({
  selector: 'app-create-group-modal',
  imports: [TranslateDirective, FormsModule, TestTagDirective],
  templateUrl: './create-group-modal.component.html',
  styleUrl: './create-group-modal.component.css',
})
export class CreateGroupModalComponent {
  @ViewChild('modal')
  modal!: ElementRef<HTMLDialogElement>;

  @Output()
  create = new EventEmitter<string>();

  groupName = '';

  show() {
    this.groupName = '';
    this.modal.nativeElement.showModal();
  }

  hide() {
    this.modal.nativeElement.close();
  }

  createGroup() {
    if (this.groupName.trim() === '') {
      return;
    }

    this.create.emit(this.groupName);
    this.hide();
  }
}
