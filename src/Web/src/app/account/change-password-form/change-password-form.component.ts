import { Component, inject, output } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, FormsModule, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { AskModalService } from '../../../services/ask-modal.service';
import { UpdatePasswordRequest } from '../../../model/update-password-request';
import { TestTagDirective } from '../../../directives/test-tag.directive';
import { TranslateDirective } from '@ngx-translate/core';
import { animate, style, transition, trigger } from '@angular/animations';

@Component({
  selector: 'app-change-password-form',
  imports: [FormsModule, ReactiveFormsModule, TranslateDirective, TestTagDirective],
  templateUrl: './change-password-form.component.html',
  animations: [
    trigger('reveal', [
      transition(':enter', [
        style({
          opacity: 0,
          transform: 'translateY(-5px) scale(0.95)'
        }),
        animate('120ms ease-out', style({
          opacity: 1,
          transform: 'translateY(0px) scale(1)'
        }))
      ]),
      transition(':leave', [
        animate('120ms ease-in', style({
          opacity: 0,
          transform: 'translateY(-5px) scale(0.95)'
        }))
      ])
    ]),
  ],
})
export class ChangePasswordFormComponent {
  readonly updatePassword = output<UpdatePasswordRequest>();

  readonly askModal = inject(AskModalService);
  readonly cancelEdit = output<void>();

  readonly changePasswordForm = new FormGroup({
    currentPassword: new FormControl('', [
      Validators.required,
    ]),
    newPassword: new FormControl('', [
      Validators.required,
      Validators.minLength(8),
    ]),
    confirmPassword: new FormControl('', [
      Validators.required,
      Validators.minLength(8),
      this.passwordMatchValidator,
    ]),
  });

  readonly currentPassword = this.changePasswordForm.get('currentPassword');
  readonly newPassword = this.changePasswordForm.get('newPassword');
  readonly confirmPassword = this.changePasswordForm.get('confirmPassword');

  passwordMatchValidator(
    confirmPassword: AbstractControl,
  ): ValidationErrors | null {
    const form = confirmPassword.parent;
    const password = form?.get('newPassword');
    return password && password.value !== confirmPassword.value
      ? { passwordMismatch: true }
      : null;
  }

  async onUpdatePassword(): Promise<void> {
    const { currentPassword, newPassword } = this.changePasswordForm.value;
    if (!currentPassword || !newPassword) {
      return; // Form is invalid, do not proceed
    }

    const confirmed = await this.askModal.confirm(
      'change-password.modal.confirm-title',
      'change-password.modal.confirm-message',
      { translate: true }
    );
    if (!confirmed) {
      return;
    }

    this.updatePassword.emit({
      oldPassword: currentPassword,
      newPassword: newPassword
    });
    this.changePasswordForm.reset();
  }
}
