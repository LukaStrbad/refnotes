import { TestBed } from '@angular/core/testing';
import { HttpHandlerFn, HttpInterceptorFn, HttpRequest, provideHttpClient } from '@angular/common/http';

import { authInterceptor } from './auth.interceptor';
import { AuthService } from '../services/auth.service';
import { firstValueFrom, of } from 'rxjs';

describe('authInterceptor', () => {
  const interceptor: HttpInterceptorFn = (req, next) =>
    TestBed.runInInjectionContext(() => authInterceptor(req, next));

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient()]
    });
    localStorage.clear();
  });

  it('should be created', () => {
    expect(interceptor).toBeTruthy();
  });

  it('should call next without Authorization header if no access token', async () => {
    const authService = TestBed.inject(AuthService);
    authService.accessToken = null;

    const mockNext: HttpHandlerFn = jasmine.createSpy('HttpHandlerFn')
      .and.callFake(req => of(req));

    const fakeRequest = new HttpRequest('GET', 'https://example.com');

    const resultObservable = interceptor(fakeRequest, mockNext);
    const result = await firstValueFrom(resultObservable);

    expect(mockNext).toHaveBeenCalledWith(fakeRequest);
    expect(fakeRequest.clone).not.toHaveBeenCalled();
    expect(result).toBeTruthy();
  });

  it('should add Authorization header if token is valid', async () => {
    const authService = TestBed.inject(AuthService);
    spyOnProperty(authService, 'accessToken', 'set').and.callFake(() => {
      localStorage.setItem('accessToken', 'valid-token');
    });
    spyOn(authService, 'isTokenExpired').and.returnValue(false);
    authService.accessToken = 'valid-token';

    const mockNext: HttpHandlerFn = jasmine.createSpy('HttpHandlerFn')
      .and.callFake(req => of(req));

    const fakeRequest = new HttpRequest('GET', 'https://example.com');
    spyOn(fakeRequest.headers, 'set').and.callThrough();
    spyOn(fakeRequest, 'clone').and.callThrough();

    const resultObservable = interceptor(fakeRequest, mockNext);
    const result = await firstValueFrom(resultObservable);

    expect(fakeRequest.clone).toHaveBeenCalled();
    expect(fakeRequest.headers.set).toHaveBeenCalledWith('Authorization', 'Bearer valid-token');
    expect(result).toEqual(jasmine.objectContaining({
      headers: 'modified-headers'
    }));
  });

  it('should refresh tokens if expired, then continue if refresh succeeds', async () => {
    const authService = TestBed.inject(AuthService);
    spyOnProperty(authService, 'accessToken', 'set').and.callFake((value) => {
      if (value) localStorage.setItem('accessToken', value);
    });
    spyOn(authService, 'isTokenExpired').and.returnValue(true);
    const tryToRefreshTokensSpy = spyOn(authService, 'tryToRefreshTokens').and.callFake(() => {
      authService.accessToken = 'new-token';
      return Promise.resolve(true);
    });
    authService.accessToken = 'old-token';

    const mockNext: HttpHandlerFn = jasmine.createSpy('HttpHandlerFn')
      .and.callFake(req => of(req));

    const fakeRequest = new HttpRequest('GET', 'https://example.com');
    spyOn(fakeRequest.headers, 'set').and.callThrough();
    spyOn(fakeRequest, 'clone').and.callThrough();

    const resultObservable = interceptor(fakeRequest, mockNext);
    await firstValueFrom(resultObservable);

    expect(tryToRefreshTokensSpy).toHaveBeenCalled();
    expect(fakeRequest.clone).toHaveBeenCalled();
    expect(authService.accessToken).toBe('new-token');
    expect(mockNext).toHaveBeenCalled();
  });

  it('should logout if token is expired and refresh fails', async () => {
    const authService = TestBed.inject(AuthService);
    spyOnProperty(authService, 'accessToken', 'set').and.callFake((value) => {
      if (value) localStorage.setItem('accessToken', value);
    });
    spyOn(authService, 'isTokenExpired').and.returnValue(true);
    const tryToRefreshTokensSpy = spyOn(authService, 'tryToRefreshTokens').and.callFake(() => Promise.resolve(false));
    authService.accessToken = 'old-token';

    const mockNext: HttpHandlerFn = jasmine.createSpy('HttpHandlerFn')
      .and.callFake(req => of(req));
    const fakeRequest = new HttpRequest('GET', 'https://example.com');
    spyOn(fakeRequest.headers, 'set').and.callThrough();
    spyOn(fakeRequest, 'clone').and.callThrough();

    const resultObservable = interceptor(fakeRequest, mockNext);
    await firstValueFrom(resultObservable);

    expect(tryToRefreshTokensSpy).toHaveBeenCalled();
    expect(mockNext).toHaveBeenCalledWith(fakeRequest);
    expect(fakeRequest.clone).not.toHaveBeenCalled();
  });
});
