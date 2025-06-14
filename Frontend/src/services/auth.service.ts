import { Inject, Injectable, signal, Signal, WritableSignal } from '@angular/core';
import { environment } from '../environments/environment';
import { HttpBackend, HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { User } from '../model/user';
import { Router } from '@angular/router';
import { jwtDecode, JwtPayload } from "jwt-decode";
import { LoginInfo } from "../app/login/login.component";
import { getStatusCode } from '../utils/errorHandler';
import { CookieService } from './cookie.service';

const apiUrl = environment.apiUrl + '/auth';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  /**
   * The currently logged-in user, or null if no user is logged in.
   */
  user: User | null = null;
  /**
   * The authentication token, or null if no user is logged in.
   */
  private token: DecodedToken | null = null;
  private _isUserLoggedIn: WritableSignal<boolean> = signal(false);
  public isUserLoggedIn: Signal<boolean> = this._isUserLoggedIn;

  private http!: HttpClient;

  get accessToken(): string | null {
    return this.cookieService.getCookie('accessToken');
  }

  constructor(
    private httpBackend: HttpBackend,
    private router: Router,
    private cookieService: CookieService,
    @Inject('Window') private window: Window,
  ) {
    this.init().then();
  }

  // This method is separate for testing purposes
  async init() {
    // This ignores all interceptors
    // This is needed to avoid an infinite loop when refreshing the token in the auth interceptor
    this.http = new HttpClient(this.httpBackend);

    if (!this.setUserAndToken()) {
      const redirectUrl = this.getRedirectUrl();
      const navigationUrl = this.getLoggedOutNavigationUrl();

      await this.router.navigate([navigationUrl], {
        queryParams: {
          redirectUrl
        }
      });
    }
  }

  getRedirectUrl(): string | undefined {
    const url = new URL(this.window.location.href);

    // Return the redirect URL if it's already set
    const redirectUrl = url.searchParams.get('redirectUrl');
    if (redirectUrl) {
      return redirectUrl;
    }

    // Don't redirect to /, /login or /signup
    if (url.pathname === '/' || url.pathname === '/login' || url.pathname === '/signup') {
      return undefined;
    }

    return this.window.location.href.split(this.window.location.origin)[1];
  }

  getLoggedOutNavigationUrl(): string {
    // When user refreshes on the signup page, we must redirect to the signup page
    if (this.window.location.pathname === '/signup') {
      return '/signup';
    }

    // In other cases, redirect to the login page
    return '/login';
  }

  // For testing purposes
  overrideHttpClient(http: HttpClient) {
    this.http = http;
  }

  isTokenExpired() {
    if (!this.token) {
      return true;
    }

    return this.token.exp * 1000 < Date.now();
  }

  async tryToRefreshTokens() {
    return this.refreshTokens().then(() => {
      this.setUserAndToken();
      return true;
    }, (reason) => {
      const status = getStatusCode(reason);

      if (status === 401) {
        this.logout("auth.sessionExpired", false).then();
      } else {
        this.logout("auth.error", false).then();
      }

      return false;
    });
  }

  async login(username: string, password: string, redirectUrl?: string) {
    await firstValueFrom(
      this.http.post(`${apiUrl}/login`, { username, password }, { withCredentials: true })
    );
    this.setUserAndToken();
    try {
      await this.router.navigate([redirectUrl ?? '/browser']);
    } catch (e) {
      console.error('Error navigating to redirect URL:', e);
    }
  }

  async register(username: string, name: string, email: string, password: string, redirectUrl?: string) {
    await firstValueFrom(
      this.http.post(`${apiUrl}/register`, { username, name, email, password }, { withCredentials: true })
    );
    this.setUserAndToken();
    await this.router.navigate([redirectUrl ?? '/browser']);
  }

  private setUserAndToken() {
    const accessToken = this.accessToken;
    if (!accessToken) {
      this.unsetUserAndToken();
      return false;
    }

    const decodedToken = jwtDecode(accessToken);
    // Check if token has the required fields (role is optional)
    if ("id" in decodedToken && "unique_name" in decodedToken && "given_name" in decodedToken && "email" in decodedToken) {
      this.token = decodedToken as DecodedToken;
      this.setUser(this.token);

      this._isUserLoggedIn.set(!this.isTokenExpired());
      return true;
    } else {
      throw new Error(`Invalid access token: ${accessToken}`);
    }
  }

  private unsetUserAndToken() {
    this.user = null;
    this.token = null;
    this._isUserLoggedIn.set(false);
    // Remove the access token cookie
    this.cookieService.setCookie('accessToken', '', new Date(0));
  }

  private setUser(token: DecodedToken) {
    this.user = {
      id: parseInt(token.id),
      username: token.unique_name,
      name: token.given_name,
      email: token.email,
      role: token.role
    };
  }

  async logout(reason: undefined | string = undefined, navigateToLogin = true) {
    this.unsetUserAndToken();
    if (!navigateToLogin) {
      return;
    }

    await this.router.navigate(['/login'], {
      info: {
        message: reason
      } as LoginInfo
    });
  }

  async refreshTokens() {
    await firstValueFrom(
      this.http.post(`${apiUrl}/refreshAccessToken`, this.accessToken, { withCredentials: true })
    );
  }

}

interface DecodedToken extends JwtPayload {
  id: string;
  unique_name: string;
  given_name: string;
  email: string;
  role?: string | string[];
  exp: number;
}
