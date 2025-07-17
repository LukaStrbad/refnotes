import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { Router } from '@angular/router';
import { HttpBackend, HttpClient, HttpErrorResponse } from '@angular/common/http';
import { createExpiredAccessToken, createValidAccessToken } from '../tests/token-utils';
import { of } from 'rxjs';
import { CookieService } from './cookie.service';
import { TranslateFakeLoader, TranslateLoader, TranslateModule } from '@ngx-translate/core';

describe('AuthService', () => {
  let service: AuthService;
  let http: jasmine.SpyObj<HttpClient>;
  let router: jasmine.SpyObj<Router>;
  let cookieService: jasmine.SpyObj<CookieService>;
  const windowMock = { location: { href: 'http://localhost:4200/', pathname: '/', origin: 'http://localhost:4200' } };

  function setAccessTokenCookie(token: string) {
    const expires = new Date(Date.now() + 1000 * 60 * 60); // 1 hour from now
    cookieService.setCookie('accessToken', token, expires);
  }

  beforeEach(() => {
    http = jasmine.createSpyObj('HttpClient', ['get', 'post']);
    router = jasmine.createSpyObj('Router', ['navigate']);
    router.navigate.and.resolveTo(true);

    const cookies: Record<string, string> = {};
    cookieService = jasmine.createSpyObj('CookieService', ['getCookie', 'setCookie']);
    cookieService.getCookie.and.callFake((name: string) => {
      return cookies[name] ?? null;
    });
    cookieService.setCookie.and.callFake((name: string, value: string, expires: Date) => {
      if (expires.getTime() < Date.now()) {
        delete cookies[name];
        return;
      }
      cookies[name] = value;
    });

    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
      providers: [
        { provide: HttpBackend },
        { provide: Router, useValue: router },
        { provide: CookieService, useValue: cookieService },
        { provide: 'Window', useValue: windowMock },
      ]
    });
    service = TestBed.inject(AuthService);
    service.overrideHttpClient(http);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should not set user logged in if there is no token', () => {
    expect(service.isUserLoggedIn()).toBeFalse();
  });

  it('should set user logged in if token is present and not expired', () => {
    setAccessTokenCookie(createValidAccessToken());
    service.init();
    expect(service.isUserLoggedIn()).toBeTrue();
  });

  it('should not set user logged in if token is expired', () => {
    setAccessTokenCookie(createExpiredAccessToken());
    service.init();
    expect(service.isUserLoggedIn()).toBeFalse();
  });

  it('should set logout if tokens cannot be refreshed', async () => {
    setAccessTokenCookie(createExpiredAccessToken());
    service.init();

    await service.tryToRefreshTokens();

    expect(service.isUserLoggedIn()).toBeFalse();
    expect(service.accessToken).toBeNull();
  });

  it('should login user if credentials are correct', async () => {
    const accessToken = createValidAccessToken();
    setAccessTokenCookie(accessToken);
    http.post.and.returnValue(of(undefined));

    await service.login('admin', 'admin');

    expect(service.accessToken).toBe(accessToken);
    expect(service.isUserLoggedIn()).toBeTrue();
    expect(router.navigate).toHaveBeenCalledWith(['/browser']);
  });

  it('should not login user if credentials are incorrect', async () => {
    http.post.and.throwError(new HttpErrorResponse({ status: 401 }));

    await expectAsync(service.login('admin', 'admin')).toBeRejected();

    expect(service.accessToken).toBeNull();
    expect(service.isUserLoggedIn()).toBeFalse();
    expect(router.navigate).not.toHaveBeenCalledWith(['/browser']);
  });

  it('should register user if request succeeds', async () => {
    const accessToken = createValidAccessToken();
    setAccessTokenCookie(accessToken);
    http.post.and.returnValue(of(undefined));

    await service.register('admin', 'admin', 'admin@admin.com', 'admin');

    expect(service.accessToken).toBe(accessToken);
    expect(service.isUserLoggedIn()).toBeTrue();
    expect(router.navigate).toHaveBeenCalledWith(['/browser']);
  });

  it('should not register user if request fails', async () => {
    http.post.and.throwError(new HttpErrorResponse({ status: 400 }));

    await expectAsync(service.register('admin', 'admin', 'admin@admin.com', 'admin')).toBeRejected();

    expect(service.accessToken).toBeNull();
    expect(service.isUserLoggedIn()).toBeFalse();
    expect(router.navigate).not.toHaveBeenCalledWith(['/browser']);
  });

  it('should logout user', async () => {
    localStorage.setItem('accessToken', createValidAccessToken());
    service.init();

    const reason = "auth.sessionExpired";

    await service.logout(reason);

    expect(service.isUserLoggedIn()).toBeFalse();
    expect(service.accessToken).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(
      ['/login'],
      { info: { message: reason } }
    );
  });
});
