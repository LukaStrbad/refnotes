
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ConfirmEmailComponent } from './confirm-email.component';
import { ActivatedRoute } from '@angular/router';
import { NotificationService } from '../../services/notification.service';
import { createMockNotificationService } from '../../services/notification.service.spec';
import { UserService } from '../../services/user.service';
import { LoggerService } from '../../services/logger.service';
import { createMockLoggerService } from '../../services/logger.service.spec';
import { TranslateFakeLoader, TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { LoadingState } from '../../model/loading-state';

describe('ConfirmEmailComponent', () => {
  let component: ConfirmEmailComponent;
  let fixture: ComponentFixture<ConfirmEmailComponent>;
  let notificationService: jasmine.SpyObj<NotificationService>;
  let userService: jasmine.SpyObj<UserService>;
  let logger: jasmine.SpyObj<LoggerService>;
  let activatedRoute: { snapshot: { paramMap: { get: jasmine.Spy } } };

  beforeEach(async () => {
    notificationService = createMockNotificationService();
    userService = jasmine.createSpyObj('UserService', ['confirmEmail', 'resendEmailConfirmation']);
    logger = createMockLoggerService();
    activatedRoute = {
      snapshot: {
        paramMap: {
          get: jasmine.createSpy('get')
        }
      }
    };

    await TestBed.configureTestingModule({
      imports: [
        ConfirmEmailComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        })
      ],
      providers: [
        { provide: NotificationService, useValue: notificationService },
        { provide: UserService, useValue: userService },
        { provide: LoggerService, useValue: logger },
        { provide: ActivatedRoute, useValue: activatedRoute },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ConfirmEmailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show error and set loadingState to Error if no token in route', async () => {
    activatedRoute.snapshot.paramMap.get.and.returnValue(null);
    notificationService.error.and.returnValue({ id: 1, message: 'err', type: 'error' });

    // Re-run ngOnInit
    component.ngOnInit();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(notificationService.error).toHaveBeenCalled();
    expect(component.loadingState).toBe(LoadingState.Error);
  });

  it('should call confirmEmail and show success if token is present', async () => {
    activatedRoute.snapshot.paramMap.get.and.returnValue('token123');
    userService.confirmEmail.and.resolveTo();
    notificationService.success.and.returnValue({ id: 2, message: 'ok', type: 'success' });

    component.ngOnInit();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(userService.confirmEmail).toHaveBeenCalledWith('token123');
    expect(notificationService.success).toHaveBeenCalled();
    expect(component.loadingState).toBe(LoadingState.Loaded);
  });

  it('should handle error from confirmEmail and log error', fakeAsync(() => {
    activatedRoute.snapshot.paramMap.get.and.returnValue('token123');
    const error = new Error('fail');
    userService.confirmEmail.and.rejectWith(error);
    notificationService.error.and.returnValue({ id: 3, message: 'err', type: 'error' });
    logger.error.and.returnValue(undefined);

    component.ngOnInit();
    tick();

    expect(userService.confirmEmail).toHaveBeenCalledWith('token123');
    expect(notificationService.error).toHaveBeenCalled();
    expect(logger.error).toHaveBeenCalled();
    expect(component.loadingState).toBe(LoadingState.Error);
  }));

  it('should call resendEmailConfirmation and show success', fakeAsync(() => {
    userService.resendEmailConfirmation.and.resolveTo();
    notificationService.awaitAndNotifyError.and.resolveTo();
    notificationService.success.and.returnValue({ id: 4, message: 'ok', type: 'success' });

    component.sentConfirmationEmail = false;
    component.resendEmailConfirmation();
    tick();

    expect(component.sentConfirmationEmail).toBeTrue();
    expect(notificationService.awaitAndNotifyError).toHaveBeenCalled();
    expect(notificationService.success).toHaveBeenCalled();
  }));
});
