import { HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { from, lastValueFrom } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => from(authHandler(req, next));

async function authHandler(req: HttpRequest<unknown>, next: HttpHandlerFn): Promise<HttpEvent<unknown>> {
  const authService = inject(AuthService);

  // Allow cookies to be sent with the request
  req = req.clone({
    withCredentials: true,
  });

  const accessToken = authService.accessToken;

  if (accessToken && authService.isTokenExpired()) {
    await authService.tryToRefreshTokens();
  }

  return await lastValueFrom(next(req));
}
