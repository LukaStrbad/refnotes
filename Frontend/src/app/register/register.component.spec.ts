import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RegisterComponent } from './register.component';
import {
  TranslateFakeLoader,
  TranslateLoader,
  TranslateModule, TranslateService,
} from '@ngx-translate/core';
import {AuthService} from "../../services/auth.service";
import {provideRouter} from "@angular/router";
import {provideAnimationsAsync} from "@angular/platform-browser/animations/async";

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;
  let auth: jasmine.SpyObj<AuthService>;

  let usernameInput: HTMLInputElement;
  let nameInput: HTMLInputElement;
  let emailInput: HTMLInputElement;
  let passwordInput: HTMLInputElement;
  let confirmPasswordInput: HTMLInputElement;
  let submitButton: HTMLButtonElement;

  async function setValues(username?: string, name?: string, email?: string, password?: string, confirmPassword?: string) {
    if (username) {
      usernameInput.value = username;
      usernameInput.dispatchEvent(new Event('input'));
      await fixture.whenStable();
      fixture.detectChanges();
    }
    if (name) {
      nameInput.value = name;
      nameInput.dispatchEvent(new Event('input'));
      await fixture.whenStable();
      fixture.detectChanges();
    }
    if (email) {
      emailInput.value = email;
      emailInput.dispatchEvent(new Event('input'));
      await fixture.whenStable();
      fixture.detectChanges();
    }
    if (password) {
      passwordInput.value = password;
      passwordInput.dispatchEvent(new Event('input'));
      await fixture.whenStable();
      fixture.detectChanges();
    }
    if (confirmPassword) {
      confirmPasswordInput.value = confirmPassword;
      confirmPasswordInput.dispatchEvent(new Event('input'));
      await fixture.whenStable();
      fixture.detectChanges();
    }
  }

  beforeEach(async () => {
    auth = jasmine.createSpyObj('AuthService', ['register']);

    await TestBed.configureTestingModule({
      imports: [
        RegisterComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
      providers: [
        provideRouter([]),
        provideAnimationsAsync(),
        TranslateService,
        { provide: AuthService, useValue: auth },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    usernameInput = fixture.nativeElement.querySelector('input[name="username"]');
    nameInput = fixture.nativeElement.querySelector('input[name="name"]');
    emailInput = fixture.nativeElement.querySelector('input[name="email"]');
    passwordInput = fixture.nativeElement.querySelector('input[name="password"]');
    confirmPasswordInput = fixture.nativeElement.querySelector('input[name="confirmPassword"]');
    submitButton = fixture.nativeElement.querySelector('button[type="submit"]');
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have an invalid form when empty', () => {
    expect(component.registrationForm.valid).toBeFalse();
  });

  it('should call register method when form is valid', async () => {
    await setValues('testuser', 'Test User', 'test@example.com', 'password123', 'password123');
    fixture.detectChanges();

    expect(submitButton.disabled).toBeFalse();
    submitButton.click();
    expect(auth.register).toHaveBeenCalledWith('testuser', 'Test User', 'test@example.com', 'password123');
  });

  it('should validate password match', () => {
    component.password?.setValue('password123');
    component.confirmPassword?.setValue('password321');
    expect(component.confirmPassword?.errors).toEqual({ passwordMismatch: true });
  });

  it('should display errors when data is invalid', async () => {
    await setValues('a', '', 'a', 'a', 'a');

    expect(submitButton.disabled).toBeTrue();

    const usernameError = fixture.nativeElement.querySelector('[data-test="username-error"]');
    // const nameError = fixture.nativeElement.querySelector('[data-test="name-error"]');
    const emailError = fixture.nativeElement.querySelector('[data-test="email-error"]');
    const passwordError = fixture.nativeElement.querySelector('[data-test="password-error"]');
    const confirmPasswordError = fixture.nativeElement.querySelector('[data-test="confirmPassword-error"]');

    expect(usernameError).toBeTruthy();
    // TODO: Fix this assertion
    // expect(nameError).toBeTruthy();
    expect(emailError).toBeTruthy();
    expect(passwordError).toBeTruthy();
    expect(confirmPasswordError).toBeTruthy();
  })
});
