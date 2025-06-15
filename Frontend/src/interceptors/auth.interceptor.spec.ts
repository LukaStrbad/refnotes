import { TestBed } from '@angular/core/testing';
import { HttpHandlerFn, HttpInterceptorFn, HttpRequest, provideHttpClient } from '@angular/common/http';

import { authInterceptor } from './auth.interceptor';
import { AuthService } from '../services/auth.service';
import { firstValueFrom, of } from 'rxjs';

describe('authInterceptor', () => {
  const interceptor: HttpInterceptorFn = (req, next) =>
    TestBed.runInInjectionContext(() => authInterceptor(req, next));
  let authService: jasmine.SpyObj<AuthService>;

  beforeEach(() => {
    authService = jasmine.createSpyObj('AuthService', ['isTokenExpired', 'isTokenExpiredWithSkew', 'tryToRefreshTokens'], ['accessToken']);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        { provide: AuthService, useValue: authService }
      ]
    });
    localStorage.clear();
  });

  it('should be created', () => {
    expect(interceptor).toBeTruthy();
  });

  it('should add withCredentials option', async () => {
    authService.isTokenExpired.and.returnValue(false);

    const mockNext: HttpHandlerFn = jasmine.createSpy('HttpHandlerFn')
      .and.callFake(req => of(req));

    const fakeRequest = new HttpRequest('GET', 'https://example.com');
    spyOn(fakeRequest, 'clone').and.callThrough();

    const resultObservable = interceptor(fakeRequest, mockNext);
    const result = await firstValueFrom(resultObservable);

    expect(fakeRequest.clone).toHaveBeenCalledWith({ withCredentials: true });
    expect(result).toBeTruthy();
  });

  it('should refresh tokens if expired, then continue if refresh succeeds', async () => {
    const accessTokenProperty = Object.getOwnPropertyDescriptor(authService, 'accessToken') as { get: jasmine.Spy<(this: jasmine.SpyObj<AuthService>) => string | null> };
    accessTokenProperty.get.and.returnValues('old-token', 'new-token');
    authService.isTokenExpired.and.returnValue(true);

    authService.tryToRefreshTokens.and.callFake(() => {
      return Promise.resolve(true);
    });

    const mockNext: HttpHandlerFn = jasmine.createSpy('HttpHandlerFn')
      .and.callFake(req => of(req));

    const fakeRequest = new HttpRequest('GET', 'https://example.com');
    spyOn(fakeRequest.headers, 'set').and.callThrough();
    spyOn(fakeRequest, 'clone').and.callThrough();

    const resultObservable = interceptor(fakeRequest, mockNext);
    await firstValueFrom(resultObservable);

    expect(authService.tryToRefreshTokens).toHaveBeenCalled();
    expect(fakeRequest.clone).toHaveBeenCalled();
    expect(authService.accessToken).toBe('new-token');
    expect(mockNext).toHaveBeenCalled();
  });
});
