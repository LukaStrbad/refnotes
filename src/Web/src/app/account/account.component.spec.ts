import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AccountComponent } from './account.component';
import { UserService } from '../../services/user.service';
import { AuthService } from '../../services/auth.service';
import { UserResponse } from '../../model/user-response';
import { TranslateFakeLoader, TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { click } from '../../tests/click-utils';
import { signal } from '@angular/core';
import { User } from '../../model/user';

describe('AccountComponent', () => {
  let component: AccountComponent;
  let fixture: ComponentFixture<AccountComponent>;
  let userService: jasmine.SpyObj<UserService>;
  let authService: jasmine.SpyObj<AuthService>;
  let nativeElement: HTMLElement;
  const userSignal = signal<User | null>(null);

  beforeEach(async () => {
    userService = jasmine.createSpyObj('UserService', ['getAccountInfo', 'resendEmailConfirmation']);
    authService = jasmine.createSpyObj('AuthService', ['logout'], { user: userSignal });

    await TestBed.configureTestingModule({
      imports: [
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
      providers: [
        AccountComponent,
        { provide: UserService, useValue: userService },
        { provide: AuthService, useValue: authService }
      ]
    })
      .compileComponents();

    fixture = TestBed.createComponent(AccountComponent);
    component = fixture.componentInstance;
    nativeElement = fixture.nativeElement as HTMLElement;
  });

  it('should display user account information', async () => {
    const mockUser: UserResponse = {
      id: 1,
      name: 'John Doe',
      username: 'johndoe',
      email: 'john.doe@example.com',
      roles: ['user'],
      emailConfirmed: true
    };
    userSignal.set(mockUser);
    userService.getAccountInfo.and.resolveTo(mockUser);

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const nameElement = nativeElement.querySelector('[data-test="account.name"]');
    const usernameElement = nativeElement.querySelector('[data-test="account.username"]');
    const emailElement = nativeElement.querySelector('[data-test="account.email"]');

    expect(component.accountInfoResource.value()).toEqual(mockUser);
    expect(nameElement?.textContent).toContain(mockUser.name);
    expect(usernameElement?.textContent).toContain(mockUser.username);
    expect(emailElement?.textContent).toContain(mockUser.email);
  });

  it('should send confirmation email if email is not confirmed', async () => {
    const mockUser: UserResponse = {
      id: 1,
      name: 'John Doe',
      username: 'johndoe',
      email: 'john.doe@example.com',
      roles: ['user'],
      emailConfirmed: false
    };
    userSignal.set(mockUser);
    userService.getAccountInfo.and.resolveTo(mockUser);
    userService.resendEmailConfirmation.and.resolveTo();

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const sendEmailButton = nativeElement.querySelector('[data-test="account.resend-confirmation-email"]') as HTMLButtonElement;
    expect(sendEmailButton).toBeTruthy();

    click(sendEmailButton);

    await fixture.whenStable();
    fixture.detectChanges();

    expect(userService.resendEmailConfirmation).toHaveBeenCalled();
    expect(sendEmailButton.disabled).toBeTrue();
  });

  it('should logout when logout button is clicked', async () => {
    const mockUser: UserResponse = {
      id: 1,
      name: 'John Doe',
      username: 'johndoe',
      email: 'john.doe@example.com',
      roles: ['user'],
      emailConfirmed: true
    };
    userSignal.set(mockUser);
    userService.getAccountInfo.and.resolveTo(mockUser);
    authService.logout.and.resolveTo();

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const logoutButton = nativeElement.querySelector('[data-test="account.button.logout"]') as HTMLButtonElement;
    expect(logoutButton).toBeTruthy();

    click(logoutButton);

    await fixture.whenStable();
    fixture.detectChanges();

    expect(authService.logout).toHaveBeenCalledWith(undefined, true);
  });
});
