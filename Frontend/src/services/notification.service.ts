import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private lastId = 0;
  private readonly maxNotifications = 5;
  private readonly maxNotificationDuration = 5000; // 5 seconds

  notifications: Notification[] = [];

  constructor() { }

  info(message: string, title?: string): Notification {
    return this.addNotification({ message, title, type: 'info' });
  }

  success(message: string, title?: string): Notification {
    return this.addNotification({ message, title, type: 'success' });
  }

  error(message: string, title?: string): Notification {
    return this.addNotification({ message, title, type: 'error' });
  }

  warning(message: string, title?: string): Notification {
    return this.addNotification({ message, title, type: 'warning' });
  }

  private addNotification(options: NotificationOptions): Notification {
    const notification = { ...options, id: this.lastId++ };
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

export interface NotificationOptions {
  message: string;
  title?: string;
  type: 'info' | 'success' | 'error' | 'warning';
}

export interface Notification extends NotificationOptions {
  id: number;
}
