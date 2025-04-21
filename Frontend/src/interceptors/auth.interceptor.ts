import { HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { from, lastValueFrom } from 'rxjs';

let refreshTokenPromise: Promise<boolean> | null = null;

export const authInterceptor: HttpInterceptorFn = (req, next) => from(authHandler(req, next));

async function authHandler(req: HttpRequest<unknown>, next: HttpHandlerFn): Promise<HttpEvent<unknown>> {
  const authService = inject(AuthService);

  // Allow cookies to be sent with the request
  req = req.clone({
    withCredentials: true,
  });

  const accessToken = authService.accessToken;

  if (accessToken && authService.isTokenExpired()) {
    // Check if we are already refreshing the token to avoid multiple requests
    if (refreshTokenPromise === null) {
      refreshTokenPromise = authService.tryToRefreshTokens();
    }

    await refreshTokenPromise;
    refreshTokenPromise = null;
  }

  return await lastValueFrom(next(req));
}
