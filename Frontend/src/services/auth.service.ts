import { inject, Injectable, signal, Signal, WritableSignal } from '@angular/core';
import { environment } from '../environments/environment';
import { HttpBackend, HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { User } from '../model/user';
import { Router } from '@angular/router';
import { jwtDecode, JwtPayload } from "jwt-decode";
import { LoginInfo } from "../app/login/login.component";
import { getStatusCode } from '../utils/errorHandler';

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

  get accessToken() {
    const accessToken = localStorage.getItem('accessToken');
    return accessToken;
  }

  set accessToken(accessToken: string | null) {
    if (accessToken) {
      this.setUserAndToken(accessToken);
      this._isUserLoggedIn.set(true);
    } else {
      this.user = null;
      this.token = null;
      localStorage.removeItem('accessToken');
      this._isUserLoggedIn.set(false);
    }
  }

  constructor(
    private httpBackend: HttpBackend,
    private router: Router
  ) {
    this.init();
  }

  // This method is separate for testing purposes
  init() {
    // This ignores all interceptors
    // This is needed to avoid an infinite loop when refreshing the token in the auth interceptor
    this.http = new HttpClient(this.httpBackend);

    const accessToken = this.accessToken;
    if (accessToken) {
      this.setUserAndToken(accessToken);

      if (!this.isTokenExpired()) {
        this._isUserLoggedIn.set(true);
      }
    }
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
      this._isUserLoggedIn.set(true);
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

  async login(username: string, password: string) {
    this.accessToken = await firstValueFrom(
      this.http.post(`${apiUrl}/login`, { username, password }, { withCredentials: true, responseType: 'text' })
    );
    console.log("Navigating to browser");
    await this.router.navigate(['/browser']);
  }

  async register(username: string, name: string, email: string, password: string) {
    this.accessToken = await firstValueFrom(
      this.http.post(`${apiUrl}/register`, { username, name, email, password }, { withCredentials: true, responseType: 'text' })
    );
    await this.router.navigate(['/browser']);
  }

  private setUserAndToken(accessToken: string) {
    const decodedToken = jwtDecode(accessToken);
    // Check if token has the required fields (role is optional)
    if ("id" in decodedToken && "unique_name" in decodedToken && "given_name" in decodedToken && "email" in decodedToken) {
      this.token = decodedToken as DecodedToken;
      this.setUser(this.token);
      localStorage.setItem('accessToken', accessToken);

      return this.token;
    } else {
      throw new Error(`Invalid access token: ${accessToken}`);
    }
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
    this.accessToken = null;
    if (!navigateToLogin) {
      return;
    }

    await this.router.navigate(['/login'], {
      info: <LoginInfo>{
        message: reason
      }
    });
  }

  async refreshTokens() {
    this.accessToken = await firstValueFrom(
      this.http.post(`${apiUrl}/refreshAccessToken`, this.accessToken,
        { withCredentials: true, responseType: 'text' })
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
