import { inject } from '@angular/core';
import { CanActivateFn, RedirectCommand, Router } from '@angular/router';
import { AuthService } from '../src/services/auth.service';

export const authGuard: CanActivateFn = async (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const loggedIn = authService.isUserLoggedIn();

  if (loggedIn) {
    return true;
  }

  // If logged out, try to refresh tokens and check again
  await authService.tryToRefreshTokens();
  if (authService.isUserLoggedIn()) {
    return true;
  }

  // Redirect to login page with the return URL
  const redirectUrl = state.url;
  // return router.parseUrl(`/login?redirectUrl=${encodeURIComponent(redirectUrl)}`);
  return router.parseUrl(`/login?redirectUrl=${redirectUrl}`);
};
