import { Component, inject, OnInit } from '@angular/core';
import { UserService } from '../../services/user.service';
import { UserResponse } from '../../model/user-response';
import { TranslateDirective, TranslatePipe } from '@ngx-translate/core';
import { NgClass } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { TestTagDirective } from '../../directives/test-tag.directive';

@Component({
  selector: 'app-account',
  imports: [TranslateDirective, TranslatePipe, NgClass, TestTagDirective],
  templateUrl: './account.component.html',
  styleUrl: './account.component.css'
})
export class AccountComponent implements OnInit {
  private readonly userService = inject(UserService);
  private readonly auth = inject(AuthService);

  accountInfo?: UserResponse;
  sentConfirmationEmail = false;

  avatarInitials = '';
  avatarColor?: string;

  ngOnInit(): void {
    this.userService.getAccountInfo().then(user => {
      this.avatarInitials = this.getAvatarInitials(user);
      this.avatarColor = this.getAvatarColor(this.avatarInitials);
      this.accountInfo = user;
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
    throw new Error('Edit profile functionality is not implemented yet.');
  }

  changePassword(): void {
    throw new Error('Change password functionality is not implemented yet.');
  }

  async logout(): Promise<void> {
    await this.auth.logout(undefined, true);
  }
}
