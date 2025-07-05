import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { NotificationService } from './notification.service';
import { TranslateFakeLoader, TranslateLoader, TranslateModule } from '@ngx-translate/core';

export function createMockNotificationService(): jasmine.SpyObj<NotificationService> {
  return jasmine.createSpyObj('NotificationService', [
    'info',
    'success',
    'error',
    'warning',
    'awaitAndNotifyError',
    'removeNotification'
  ]);
}

describe('NotificationService', () => {
  let service: NotificationService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ]
    });
    service = TestBed.inject(NotificationService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should an info notification', () => {
    const message = 'Test info message';
    const title = 'Test info title';
    const notification = service.info(message, title);

    expect(notification).toBeTruthy();
    expect(notification.type).toBe('info');
    expect(notification.message).toBe(message);
    expect(notification.title).toBe(title);
    expect(service.notifications.length).toBe(1);
    expect(service.notifications[0]).toEqual(notification);
  });

  it('should add success, error, and warning notifications', () => {
    const message = 'Test message';
    const title = 'Test title';

    const successNotification = service.success(message, title);
    expect(successNotification.type).toBe('success');

    const errorNotification = service.error(message, title);
    expect(errorNotification.type).toBe('error');

    const warningNotification = service.warning(message, title);
    expect(warningNotification.type).toBe('warning');

    expect(service.notifications.length).toBe(3);
    expect(service.notifications[0]).toEqual(successNotification);
    expect(service.notifications[1]).toEqual(errorNotification);
    expect(service.notifications[2]).toEqual(warningNotification);
  });

  it('should remove notification after 5 seconds', fakeAsync(() => {
    const message = 'Test message';
    const title = 'Test title';

    service.success(message, title);
    expect(service.notifications.length).toBe(1);

    // Simulate the passage of time
    tick(5000);

    expect(service.notifications.length).toBe(0);
  }));

  it('awaitAndNotifyError should add error notification on promise rejection', async () => {
    const errorMessage = 'Test error message';
    const promise = new Promise((_, reject) => reject(new Error(errorMessage)));

    try {
      await service.awaitAndNotifyError(promise, { default: errorMessage })
    } catch {
      // Expected error
    }

    expect(service.notifications.length).toBe(1);
    expect(service.notifications[0].type).toBe('error');
    expect(service.notifications[0].message).toBe(errorMessage);
  });
});
