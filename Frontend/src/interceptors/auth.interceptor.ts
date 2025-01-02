import { HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { from, lastValueFrom } from 'rxjs';

let refreshTokenPromise: Promise<boolean> | null = null;

export const authInterceptor: HttpInterceptorFn = (req, next) => from(authHandler(req, next));

async function authHandler(req: HttpRequest<unknown>, next: HttpHandlerFn): Promise<HttpEvent<unknown>> {
  const authService = inject(AuthService);
  let accessToken = authService.accessToken;

  if (accessToken === null) {
    return lastValueFrom(next(req));
  }

  if (authService.isTokenExpired()) {
    // Check if we are already refreshing the token to avoid multiple requests
    if (refreshTokenPromise === null) {
      refreshTokenPromise = authService.tryToRefreshTokens();
    }

    const result = await refreshTokenPromise;
    refreshTokenPromise = null;

    if (result) {
      accessToken = authService.accessToken;
    } else {
      authService.logout().then();
    }
  }

  const newReq = req.clone({
    headers: req.headers.set('Authorization', `Bearer ${accessToken}`)
  });

  return lastValueFrom(next(newReq));
}
