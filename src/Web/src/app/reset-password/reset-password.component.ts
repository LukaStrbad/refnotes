import { Component, inject, OnInit } from '@angular/core';
import { UserService } from '../../services/user.service';
import { TranslateDirective, TranslateService } from '@ngx-translate/core';
import { LoggerService } from '../../services/logger.service';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { NotificationService } from '../../services/notification.service';
import { getTranslation } from '../../utils/translation-utils';
import { AbstractControl, FormControl, FormGroup, FormsModule, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { getRevealAnimations } from '../../utils/animations';
import { TestTagDirective } from '../../directives/test-tag.directive';

@Component({
  selector: 'app-reset-password',
  imports: [FormsModule, ReactiveFormsModule, TranslateDirective, RouterLink, TestTagDirective],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.css',
  animations: [
    getRevealAnimations(),
  ],
})
export class ResetPasswordComponent implements OnInit {
  private readonly userService = inject(UserService);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly notificationService = inject(NotificationService);
  private readonly translate = inject(TranslateService);
  private readonly log = inject(LoggerService);
  private readonly router = inject(Router);

  readonly resetPasswordForm = new FormGroup({
    newPassword: new FormControl('', [
      Validators.required,
      Validators.minLength(8),
    ]),
    confirmPassword: new FormControl('', [
      Validators.required,
      Validators.minLength(8),
      this.passwordMatchValidator,
    ]),
  })

  readonly newPassword = this.resetPasswordForm.get('newPassword');
  readonly confirmPassword = this.resetPasswordForm.get('confirmPassword');

  showError = false;
  token: string | null = null;
  username: string | null = null;

  ngOnInit(): void {
    (async () => {
      this.token = this.activatedRoute.snapshot.paramMap.get('token');
      this.username = this.activatedRoute.snapshot.queryParamMap.get('username');
      if (!this.token) {
        this.notificationService.error(
          await getTranslation(this.translate, 'reset-password.error.invalid-token')
        );
        this.showError = true;
      }

      if (!this.username) {
        this.notificationService.error(
          await getTranslation(this.translate, 'reset-password.error.invalid-username')
        );
        this.showError = true;
      }
    })();
  }

  passwordMatchValidator(
    confirmPassword: AbstractControl,
  ): ValidationErrors | null {
    const form = confirmPassword.parent;
    const password = form?.get('newPassword');
    return password && password.value !== confirmPassword.value
      ? { passwordMismatch: true }
      : null;
  }

  async onResetPassword(): Promise<void> {
    const { newPassword } = this.resetPasswordForm.value;
    if (!newPassword || !this.token || !this.username) {
      return; // Form is invalid, do not proceed
    }

    try {
      await this.userService.updatePasswordByToken({
        username: this.username,
        password: newPassword,
        token: this.token
      });
      this.notificationService.success(
        await getTranslation(this.translate, 'reset-password.success.title')
      );
      await this.router.navigate(['/login']);
    } catch (error) {
      await this.notificationService.notifyError(error, this.translate, this.log);
    }
  }
}
