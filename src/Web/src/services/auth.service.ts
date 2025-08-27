import { Injectable, signal, WritableSignal } from '@angular/core';
import { environment } from '../environments/environment';
import { HttpBackend, HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { User } from '../model/user';
import { Router } from '@angular/router';
import { jwtDecode, JwtPayload } from "jwt-decode";
import { LoginInfo } from "../app/login/login.component";
import { getStatusCode } from '../utils/errorHandler';
import { CookieService } from './cookie.service';
import { TranslateService } from '@ngx-translate/core';

const apiUrl = environment.apiUrl + '/auth';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly _user = signal<User | null>(null);
  /**
   * The currently logged-in user, or null if no user is logged in.
   */
  public readonly user = this._user.asReadonly();
  /**
   * The authentication token, or null if no user is logged in.
   */
  private token: DecodedToken | null = null;
  private readonly _isUserLoggedIn: WritableSignal<boolean> = signal(false);
  public readonly isUserLoggedIn = this._isUserLoggedIn.asReadonly();
  private refreshTokenPromise: Promise<boolean> | null = null;

  private http!: HttpClient;

  constructor(
    private httpBackend: HttpBackend,
    private router: Router,
    private cookieService: CookieService,
    private translate: TranslateService,
  ) {
    // This ignores all interceptors
    // This is needed to avoid an infinite loop when refreshing the token in the auth interceptor
    this.http = new HttpClient(this.httpBackend);

    this.setUserAndToken();
  }

  // For testing purposes
  overrideHttpClient(http: HttpClient) {
    this.http = http;
  }

  isTokenExpired(): boolean {
    if (!this.token) {
      return true;
    }

    return this.token.exp * 1000 < Date.now();
  }

  /**
   * Checks if the token is expired with a skew of 1 minute.
   * This is useful to avoid issues with clock skew between the client and server.
   */
  isTokenExpiredWithSkew() {
    if (!this.token) {
      return true;
    }

    // 1 minute skew
    const skewSeconds = 60;

    // Check if the token is expired with a skew
    return (this.token.exp + skewSeconds) * 1000 < Date.now();
  }

  async tryToRefreshTokens() {
    // Return saved promise if a refresh is already in progress
    if (this.refreshTokenPromise) {
      return this.refreshTokenPromise;
    }

    this.refreshTokenPromise = this.refreshTokens()
      .then(() => {
        this.setUserAndToken();
        return true;
      })
      .catch((reason) => {
        const status = getStatusCode(reason);

        if (status === 401) {
          this.logout("auth.sessionExpired", false).then();
        } else {
          this.logout("auth.error", false).then();
        }

        return false;
      })
      .finally(() => {
        this.refreshTokenPromise = null;
      });

    return this.refreshTokenPromise;
  }

  async login(username: string, password: string, redirectUrl?: string) {
    await firstValueFrom(
      this.http.post(`${apiUrl}/login`, { username, password })
    );
    this.setUserAndToken();
    try {
      await this.router.navigateByUrl(redirectUrl ?? '/browser');
    } catch (e) {
      console.error('Error navigating to redirect URL:', e);
    }
  }

  async register(username: string, name: string, email: string, password: string, redirectUrl?: string) {
    const lang = this.translate.currentLang;
    await firstValueFrom(
      this.http.post(`${apiUrl}/register?lang=${lang}`, { username, name, email, password }, { withCredentials: true })
    );
    this.setUserAndToken();
    await this.router.navigate([redirectUrl ?? '/browser']);
  }

  setUserAndToken() {
    const accessToken = this.cookieService.getCookie('accessToken');
    if (!accessToken) {
      this.unsetUserAndToken();
      return false;
    }

    const decodedToken = jwtDecode(accessToken);
    // Check if token has the required fields (role is optional)
    if ("id" in decodedToken && "unique_name" in decodedToken && "given_name" in decodedToken && "email" in decodedToken) {
      this.token = decodedToken as DecodedToken;
      this.setUser(this.token);

      this._isUserLoggedIn.set(!this.isTokenExpiredWithSkew());
      return true;
    } else {
      throw new Error(`Invalid access token: ${accessToken}`);
    }
  }

  private unsetUserAndToken() {
    this._user.set(null);
    this.token = null;
    this._isUserLoggedIn.set(false);
    // Remove the access token cookie
    this.cookieService.setCookie('accessToken', '', new Date(0));
  }

  private setUser(token: DecodedToken) {
    this._user.set({
      id: parseInt(token.id),
      username: token.unique_name,
      name: token.given_name,
      email: token.email,
      role: token.role
    });
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
      this.http.post(`${apiUrl}/refreshAccessToken`, this.cookieService.getCookie('accessToken'), { withCredentials: true })
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
