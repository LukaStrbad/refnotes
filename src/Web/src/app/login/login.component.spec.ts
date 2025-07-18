import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoginComponent } from './login.component';
import { TranslateFakeLoader, TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { Navigation, provideRouter, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { HttpErrorResponse } from '@angular/common/http';
import { UserService } from '../../services/user.service';
import { AskModalService } from '../../services/ask-modal.service';
import { click } from '../../tests/click-utils';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let auth: jasmine.SpyObj<AuthService>;
  let askModal: jasmine.SpyObj<AskModalService>;
  let userService: jasmine.SpyObj<UserService>;
  let nativeElement: HTMLElement;

  beforeEach(async () => {
    auth = jasmine.createSpyObj('AuthService', ['login']);
    askModal = jasmine.createSpyObj('AskModalService', ['confirm']);
    userService = jasmine.createSpyObj('UserService', ['sendPasswordResetEmail']);

    await TestBed.configureTestingModule({
      imports: [
        LoginComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader
          }
        })
      ],
      providers: [
        provideRouter([]),
        TranslateService,
        { provide: AuthService, useValue: auth },
        { provide: AskModalService, useValue: askModal },
        { provide: UserService, useValue: userService },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    nativeElement = fixture.nativeElement as HTMLElement;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call auth.login with provided username and password', async () => {
    component.username.set('testUser');
    component.password.set('testPassword');
    await component.login();

    expect(auth.login).toHaveBeenCalledWith('testUser', 'testPassword', undefined);
  });

  it('should not set error on successful login', async () => {
    auth.login.and.resolveTo();
    component.username.set('testUser');
    component.password.set('testPassword');
    await component.login();

    expect(component.error).toBeNull();
  });

  it('should set error message to invalidPassword on 401 error', async () => {
    const error = new HttpErrorResponse({ status: 401 });
    auth.login.and.rejectWith(error);
    await component.login();

    expect(component.error).toBe('login.errors.invalidPassword');
  });

  it('should set error message to usernameNotFound on 404 error', async () => {
    const error = new HttpErrorResponse({ status: 404 });
    auth.login.and.rejectWith(error);
    await component.login();

    expect(component.error).toBe('login.errors.usernameNotFound');
  });

  it('should set error message to default on unknown error', async () => {
    const error = { status: 500 };
    auth.login.and.rejectWith(error);
    await component.login();

    expect(component.error).toBeTruthy();
    expect(component.error).not.toBe('login.errors.invalidPassword');
    expect(component.error).not.toBe('login.errors.usernameNotFound');
  });

  it('should set message field from router navigation extras', () => {
    const router = TestBed.inject(Router);
    spyOn(router, 'getCurrentNavigation').and.returnValue({
      extras: {
        info: { message: 'test message' }
      }
    } as Navigation);

    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;

    expect(component.message).toBe('test message');
  });

  it('should call userService.sendPasswordResetEmail with username and current language', async () => {
    askModal.confirm.and.resolveTo(true);
    const translate = TestBed.inject(TranslateService);
    spyOnProperty(translate, 'currentLang', 'get').and.returnValue('en');

    const forgotPasswordButton = nativeElement.querySelector('button[data-test="login.button.forgot-password"]') as HTMLButtonElement;
    const usernameInput = nativeElement.querySelector('input[name="username"]') as HTMLInputElement;

    // Disabled when username is invalid
    expect(forgotPasswordButton.disabled).toBeTrue();

    usernameInput.value = 'testUser';
    usernameInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    // Enabled when username is valid
    expect(forgotPasswordButton.disabled).toBeFalse();

    // Simulate button click
    click(forgotPasswordButton);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(userService.sendPasswordResetEmail).toHaveBeenCalledWith('testUser', 'en');
  });
});
