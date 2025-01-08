import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { Router } from '@angular/router';
import { HttpBackend, HttpClient, HttpErrorResponse, HttpEvent, HttpResponse } from '@angular/common/http';
import { createExpiredAccessToken, createValidAccessToken } from '../tests/token-utils';
import { of, throwError } from 'rxjs';

describe('AuthService', () => {
  let service: AuthService;
  let http: jasmine.SpyObj<HttpClient>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    http = jasmine.createSpyObj('HttpClient', ['get', 'post']);
    router = jasmine.createSpyObj('Router', ['navigate']);

    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        { provide: HttpBackend },
        { provide: Router, useValue: router }
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
    localStorage.setItem('accessToken', createValidAccessToken());
    service.init();
    expect(service.isUserLoggedIn()).toBeTrue();
  });

  it('should not set user logged in if token is expired', () => {
    localStorage.setItem('accessToken', createExpiredAccessToken());
    service.init();
    expect(service.isUserLoggedIn()).toBeFalse();
  });

  it('should set logout if tokens cannot be refreshed', async () => {
    localStorage.setItem('accessToken', createExpiredAccessToken());
    service.init();

    await service.tryToRefreshTokens();

    expect(service.isUserLoggedIn()).toBeFalse();
    expect(service.accessToken).toBeNull();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('should login user if credentials are correct', async () => {
    const accessToken = createValidAccessToken();
    // httpBackend.handle.and.returnValue(of(httpResponse));
    http.post.and.returnValue(of(accessToken));

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
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('should register user if request succeeds', async () => {
    const accessToken = createValidAccessToken();
    http.post.and.returnValue(of(accessToken));

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
    expect(router.navigate).not.toHaveBeenCalled();
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
