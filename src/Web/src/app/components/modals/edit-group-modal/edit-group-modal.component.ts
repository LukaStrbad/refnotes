import { Component, ElementRef, EventEmitter, Output, ViewChild } from '@angular/core';
import { GroupDto, UpdateGroupDto } from '../../../../model/user-group';
import { FormsModule } from '@angular/forms';
import { TranslateDirective } from '@ngx-translate/core';
import { TestTagDirective } from '../../../../directives/test-tag.directive';

@Component({
  selector: 'app-edit-group-modal',
  imports: [FormsModule, TranslateDirective, TestTagDirective],
  templateUrl: './edit-group-modal.component.html',
  styleUrl: './edit-group-modal.component.css'
})
export class EditGroupModalComponent {
  @ViewChild('modal')
  modal!: ElementRef<HTMLDialogElement>;

  @Output()
  edit = new EventEmitter<[number, UpdateGroupDto]>();

  groupName = '';
  private group?: GroupDto;

  show(group: GroupDto) {
    this.group = group;
    this.groupName = group.name;
    this.modal.nativeElement.showModal();
  }

  hide() {
    this.modal.nativeElement.close();
  }

  async editGroup() {
    if (this.groupName.trim() === '' || !this.group) {
      return;
    }

    this.edit.emit([this.group.id, {
      name: this.groupName,
    }]);
    this.hide();
  }
}
