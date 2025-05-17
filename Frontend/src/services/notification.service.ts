import { Injectable } from '@angular/core';
import { GENERIC_ERROR_CODE, getErrorMessage, HttpErrorMessages } from '../utils/errorHandler';
import { TranslateService } from '@ngx-translate/core';
import { getTranslation } from '../utils/translation-utils';
import { LoggerService } from './logger.service';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private lastId = 0;
  private readonly maxNotifications = 5;
  private readonly maxNotificationDuration = 5000; // 5 seconds

  notifications: Notification[] = [];

  constructor(private translate: TranslateService) { }

  info(message: string, title?: string): Notification {
    return this.addNotification(message, 'info', title);
  }

  success(message: string, title?: string): Notification {
    return this.addNotification(message, 'success', title);
  }

  error(message: string, title?: string): Notification {
    return this.addNotification(message, 'error', title);
  }

  warning(message: string, title?: string): Notification {
    return this.addNotification(message, 'warning', title);
  }

  /**
   * Awaits the promise and notifies the user of any errors that occur.
   * @param promise Promise to await
   * @param messages Error messages to use for the error notification
   * @returns The result of the promise
   * @throws The error that occurred
   */
  async awaitAndNotifyError<T>(
    promise: Promise<T>,
    messages: HttpErrorMessages,
    logger?: LoggerService,
  ) {
    try {
      return await promise;
    } catch (e) {
      let error = getErrorMessage(e, messages);
      // If the error is a generic error code, get the translation for it
      if (error === GENERIC_ERROR_CODE) {
        error = await getTranslation(this.translate, GENERIC_ERROR_CODE);
      }
      this.error(error);
      logger?.error('Error occurred and notified user', e);
      throw e;
    }
  }

  private addNotification(message: string, type: NotificationType, title?: string): Notification {
    const notification: Notification = {
      id: ++this.lastId,
      message,
      title,
      type
    };
    this.notifications.push(notification);
    this.trimNotifications();

    setTimeout(() => {
      this.removeNotification(notification.id);
    }, this.maxNotificationDuration);

    return notification;
  }

  removeNotification(id: number): void {
    this.notifications = this.notifications.filter(n => n.id !== id);
  }

  private trimNotifications(): void {
    if (this.notifications.length > this.maxNotifications) {
      this.notifications.splice(0, this.notifications.length - this.maxNotifications);
    }
  }
}

type NotificationType = 'info' | 'success' | 'error' | 'warning';

export interface NotificationOptions {
  message: string;
  title?: string;
  type: NotificationType;
}

export interface Notification extends NotificationOptions {
  id: number;
}
