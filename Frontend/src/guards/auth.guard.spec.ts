import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, CanActivateFn, Router, RouterStateSnapshot } from '@angular/router';

import { authGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';
import { inject } from '@angular/core';

describe('authGuard', () => {
  let authService: jasmine.SpyObj<AuthService>;
  let activatedRouteSnapshot: ActivatedRouteSnapshot;
  let routerStateSnapshot: jasmine.SpyObj<RouterStateSnapshot>;
  let router: jasmine.SpyObj<Router>;

  const executeGuard = () =>
    TestBed.runInInjectionContext(() => authGuard(activatedRouteSnapshot, routerStateSnapshot));

  beforeEach(() => {
    authService = jasmine.createSpyObj('AuthService', ['isUserLoggedIn', 'tryToRefreshTokens']);
    activatedRouteSnapshot = {} as ActivatedRouteSnapshot;
    routerStateSnapshot = jasmine.createSpyObj('RouterStateSnapshot', [], ['url']);
    router = jasmine.createSpyObj('Router', ['parseUrl']);

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
      ]
    });
  });

  it('should be created', () => {
    expect(executeGuard).toBeTruthy();
  });

  it('should allow access if user is logged in', async () => {
    authService.isUserLoggedIn.and.returnValue(true);

    const result = await executeGuard();
    expect(result).toBeTrue();
  });

  it('should try to refresh tokens if user is not logged in', async () => {
    authService.isUserLoggedIn.and.returnValue(false);
    authService.tryToRefreshTokens.and.callFake(() => {
      authService.isUserLoggedIn.and.returnValue(true);
      return Promise.resolve(true);
    });

    const result = await executeGuard();
    expect(authService.tryToRefreshTokens).toHaveBeenCalled();
    expect(result).toBeTrue();
  });

  it('should redirect to login if user is still not logged in after trying to refresh tokens', async () => {
    authService.isUserLoggedIn.and.returnValue(false);
    authService.tryToRefreshTokens.and.resolveTo(false);
    routerStateSnapshot.url = '/some-protected-route';
    router.parseUrl.and.returnValue(router.parseUrl('/login?redirectUrl=%2Fsome-protected-route'));

    await executeGuard();
    expect(authService.tryToRefreshTokens).toHaveBeenCalled();
    expect(router.parseUrl).toHaveBeenCalledWith('/login?redirectUrl=%2Fsome-protected-route');
  });
});
