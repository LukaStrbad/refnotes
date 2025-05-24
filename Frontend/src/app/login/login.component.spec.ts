import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoginComponent } from './login.component';
import { TranslateFakeLoader, TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { Navigation, provideRouter, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { HttpErrorResponse } from '@angular/common/http';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let auth: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    auth = jasmine.createSpyObj('AuthService', ['login']);

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
        { provide: AuthService, useValue: auth }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call auth.login with provided username and password', async () => {
    component.username = 'testUser';
    component.password = 'testPassword';
    await component.login();

    expect(auth.login).toHaveBeenCalledWith('testUser', 'testPassword', undefined);
  });

  it('should not set error on successful login', async () => {
    auth.login.and.resolveTo();
    component.username = 'testUser';
    component.password = 'testPassword';
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
});
