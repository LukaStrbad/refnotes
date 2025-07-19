import { Component, inject, OnInit } from '@angular/core';
import { UserService } from '../../services/user.service';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { NotificationService } from '../../services/notification.service';
import { TranslateDirective, TranslateService } from '@ngx-translate/core';
import { getTranslation } from '../../utils/translation-utils';
import { LoadingState } from '../../model/loading-state';
import { getErrorMessage } from '../../utils/errorHandler';
import { LoggerService } from '../../services/logger.service';

@Component({
  selector: 'app-confirm-email',
  imports: [TranslateDirective, RouterLink],
  templateUrl: './confirm-email.component.html',
  styleUrl: './confirm-email.component.css'
})
export class ConfirmEmailComponent implements OnInit {
  private readonly userService = inject(UserService);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly notificationService = inject(NotificationService);
  private readonly translate = inject(TranslateService);
  private readonly log = inject(LoggerService);

  loadingState = LoadingState.Loading;
  readonly LoadingState = LoadingState;

  sentConfirmationEmail = false;

  ngOnInit(): void {
    (async () => {
      const token = this.activatedRoute.snapshot.paramMap.get('token');
      if (!token) {
        this.notificationService.error(await getTranslation(this.translate, 'confirm-email.error.invalid-token'));
        this.loadingState = LoadingState.Error;
        return;
      }

      try {
        await this.userService.confirmEmail(token);

        this.loadingState = LoadingState.Loaded;
        this.notificationService.success(await getTranslation(this.translate, 'confirm-email.success.title'));
      } catch (error) {
        const errorMessage = getErrorMessage(error, {
          400: await getTranslation(this.translate, 'confirm-email.error.invalid-token'),
          default: await getTranslation(this.translate, 'confirm-email.error.generic')
        });
        this.log.error('Error confirming email:', errorMessage, error);

        this.notificationService.error(errorMessage);
        this.loadingState = LoadingState.Error;
      }
    })();
  }

  async resendEmailConfirmation() {
    this.sentConfirmationEmail = true;
    await this.notificationService.awaitAndNotifyError(
      this.userService.resendEmailConfirmation(),
      {
        400: await getTranslation(this.translate, 'confirm-email.error.resend'),
        default: await getTranslation(this.translate, 'confirm-email.error.resend-generic')
      }
    );

    this.notificationService.success(await getTranslation(this.translate, 'confirm-email.success.resend'));
  }
}
