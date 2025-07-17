import { Component, computed, inject, Resource, resource, ResourceRef, Signal } from '@angular/core';
import { UserService } from '../../services/user.service';
import { UserResponse } from '../../model/user-response';
import { TranslateDirective, TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CommonModule, NgClass } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { TestTagDirective } from '../../directives/test-tag.directive';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { EditAccountFormComponent } from "./edit-account-form/edit-account-form.component";
import { EditUserRequest } from '../../model/edit-user-request';
import { NotificationService } from '../../services/notification.service';
import { LoggerService } from '../../services/logger.service';
import { getTranslation } from '../../utils/translation-utils';

@Component({
  selector: 'app-account',
  imports: [TranslateDirective, TranslatePipe, NgClass, TestTagDirective, FormsModule, CommonModule, ReactiveFormsModule, EditAccountFormComponent],
  templateUrl: './account.component.html',
  styleUrl: './account.component.css'
})
export class AccountComponent {
  private readonly userService = inject(UserService);
  private readonly auth = inject(AuthService);
  private readonly translate = inject(TranslateService);
  private readonly notification = inject(NotificationService);
  private readonly log = inject(LoggerService);

  sentConfirmationEmail = false;
  editingProfile = false;

  readonly accountInfoResource: Resource<UserResponse | undefined>;
  readonly avatarInitials: Signal<string>;
  readonly avatarColor: Signal<string>;

  constructor() {
    this.accountInfoResource = resource({
      params: () => ({ user: this.auth.user() }),
      loader: async () => await this.userService.getAccountInfo(),
    }).asReadonly();

    this.avatarInitials = computed(() => {
      const resourceValue = this.accountInfoResource.value();
      return resourceValue ? this.getAvatarInitials(resourceValue) : '';
    });

    this.avatarColor = computed(() => {
      const initials = this.avatarInitials();
      return initials ? this.getAvatarColor(initials) : '';
    });
  }

  resendEmailConfirmation(): void {
    this.userService.resendEmailConfirmation().then(() => {
      this.sentConfirmationEmail = true;
    });
  }

  private getAvatarInitials(user: UserResponse): string {
    const name = user.name.trim();
    const username = user.username.trim();
    const base = name || username || 'U';
    // If name exists, use first two initials, else use first two of username
    if (name) {
      const parts = name.split(' ').filter(Boolean);
      if (parts.length === 1) {
        return parts[0].slice(0, 2).toUpperCase();
      } else if (parts.length > 1) {
        return (parts[0][0] + parts[1][0]).toUpperCase();
      }
    }
    return base.slice(0, 2).toUpperCase();
  }

  private getAvatarColor(avatarInitials: string): string {
    const colors = ['#D16149', '#10CC31', '#3357FF', '#F1C40F', '#8E44AD'];
    const index = avatarInitials.charCodeAt(0) % colors.length;
    return colors[index];
  }

  editProfile(): void {
    this.editingProfile = true;
  }

  cancelEdit(): void {
    this.editingProfile = false;
  }

  async saveChanges(updatedUser: EditUserRequest) {
    await this.notification.awaitAndNotifyError(
      this.userService.editAccount(updatedUser, this.translate.currentLang),
      {
        400: 'account.error.edit-username-exists',
        default: 'account.error.edit-failed'
      },
      this.log
    );

    this.notification.success(await getTranslation(this.translate, 'account.success.edit-success'));
    this.editingProfile = false;
  }

  changePassword(): void {
    throw new Error('Change password functionality is not implemented yet.');
  }

  async logout(): Promise<void> {
    await this.auth.logout(undefined, true);
  }
}
