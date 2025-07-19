import { ComponentFixture, fakeAsync, TestBed, tick } from '@angular/core/testing';

import { ResetPasswordComponent } from './reset-password.component';
import { TranslateFakeLoader, TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { UserService } from '../../services/user.service';
import { ActivatedRoute } from '@angular/router';
import { NotificationService } from '../../services/notification.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

describe('ResetPasswordComponent', () => {
  let component: ResetPasswordComponent;
  let fixture: ComponentFixture<ResetPasswordComponent>;
  let userService: jasmine.SpyObj<UserService>;
  let notificationService: jasmine.SpyObj<NotificationService>;
  const activatedRoute: { snapshot: { paramMap: { get: () => string | null }, queryParamMap: { get: () => string | null } } } = {
    snapshot: {
      paramMap: {
        get: () => 'test-token',
      },
      queryParamMap: {
        get: () => 'test-username',
      },
    }
  };
  // let nativeElement: HTMLElement;

  beforeEach(async () => {
    userService = jasmine.createSpyObj('UserService', ['updatePasswordByToken']);
    notificationService = jasmine.createSpyObj('NotificationService', ['error', 'success']);

    await TestBed.configureTestingModule({
      imports: [
        ResetPasswordComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
      providers: [
        provideAnimationsAsync(),
        { provide: UserService, useValue: userService },
        { provide: ActivatedRoute, useValue: activatedRoute },
        { provide: NotificationService, useValue: notificationService },
      ]
    })
      .compileComponents();

    fixture = TestBed.createComponent(ResetPasswordComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    // nativeElement = fixture.nativeElement as HTMLElement;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show error if token is invalid', fakeAsync(() => {
    activatedRoute.snapshot.paramMap.get = () => null;

    component.ngOnInit();
    tick();

    expect(component.showError).toBeTrue();
  }));

  it('should show error if username is invalid', fakeAsync(() => {
    activatedRoute.snapshot.queryParamMap.get = () => null;

    component.ngOnInit();
    tick();

    expect(component.showError).toBeTrue();
  }));

  // TODO: Fix this test
  // in it's current state, it only works when running just this test, failing when running all tests
  // it('should reset password on form submission', fakeAsync(async () => {
  //   const newPassword = 'new-password';
  //   const confirmPassword = 'new-password';
  //   const router = TestBed.inject(Router);
  //   const navigateSpy = spyOn(router, 'navigate');

  //   const passwordInput = nativeElement.querySelector('[data-test="reset-password.new"]') as HTMLInputElement;
  //   const confirmPasswordInput = nativeElement.querySelector('[data-test="reset-password.confirm"]') as HTMLInputElement;
  //   const submitButton = nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;

  //   passwordInput.value = newPassword;
  //   confirmPasswordInput.value = confirmPassword;
  //   passwordInput.dispatchEvent(new Event('input'));
  //   confirmPasswordInput.dispatchEvent(new Event('input'));

  //   expect(component.resetPasswordForm.valid).toBeTrue();

  //   // click(submitButton); // This doesn't seem to work
  //   await component.onResetPassword();
  //   fixture.detectChanges();
  //   await fixture.whenStable();

  //   expect(userService.updatePasswordByToken).toHaveBeenCalledWith(
  //     {
  //       username: 'test-username',
  //       password: newPassword,
  //       token: 'test-token'
  //     }
  //   );
  //   expect(navigateSpy).toHaveBeenCalledWith(['/login']);
  // }));
});
