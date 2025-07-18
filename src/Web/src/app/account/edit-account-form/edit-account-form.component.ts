import { Component, effect, inject, input, output } from '@angular/core';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { UserResponse } from '../../../model/user-response';
import { TranslateDirective } from '@ngx-translate/core';
import { EditUserRequest } from '../../../model/edit-user-request';
import { TestTagDirective } from '../../../directives/test-tag.directive';
import { AskModalService } from '../../../services/ask-modal.service';

@Component({
  selector: 'app-edit-account-form',
  imports: [TranslateDirective, FormsModule, ReactiveFormsModule, TestTagDirective],
  templateUrl: './edit-account-form.component.html',
  styleUrl: './edit-account-form.component.css'
})
export class EditAccountFormComponent {
  readonly accountInfo = input.required<UserResponse | undefined>();
  readonly saveChanges = output<EditUserRequest>();
  readonly cancelEdit = output<void>();

  readonly askModal = inject(AskModalService);

  readonly accountEditForm = new FormGroup({
    username: new FormControl('', [
      Validators.required,
      Validators.minLength(4)
    ]),
    name: new FormControl('', [Validators.required]),
    email: new FormControl('', [
      Validators.required,
      Validators.email
    ]),
  });

  constructor() {
    // Set form values every time accountInfo changes
    effect(() => {
      const accountInfo = this.accountInfo();
      if (accountInfo) {
        this.accountEditForm.patchValue({
          username: accountInfo.username,
          name: accountInfo.name,
          email: accountInfo.email
        });
      }
    });
  }

  async onSaveChanges(): Promise<void>{
    const { name, username, email } = this.accountEditForm.value;
    if (!name || !username || !email) {
      return; // Form is invalid, do not proceed
    }

    const confirmed = await this.askModal.confirm(
      'account-edit.modal.confirm-title',
      'account-edit.modal.confirm-message',
      { translate: true }
    );
    if (!confirmed) {
      return;
    }

    const updatedUser: EditUserRequest = {
      newName: name,
      newUsername: username,
      newEmail: email
    };

    this.saveChanges.emit(updatedUser);
  }
}
