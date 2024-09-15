import { Injectable, signal, Signal, WritableSignal } from '@angular/core';
import { environment } from '../environments/environment';
import { HttpClient } from '@angular/common/http';
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
    private http: HttpClient,
    private router: Router
  ) {
    const accessToken = this.accessToken;
    if (accessToken) {
      const decodedToken = this.setUserAndToken(accessToken);

      const expired = decodedToken.exp * 1000 < Date.now();

      if (expired) {
        this.refreshTokens().then(() => {
          this._isUserLoggedIn.set(true);
          console.log("tokens refreshed");
        }, (reason) => {
          const status = getStatusCode(reason);

          if (status === 401) {
            this.logout("Session expired").then();
          } else {
            this.logout("An error occurred").then();
          }
        });
      } else {
        this._isUserLoggedIn.set(true);
      }
    }
  }

  async login(username: string, password: string) {
    this.accessToken = await firstValueFrom(
      this.http.post<string>(`${apiUrl}/login`, { username, password }, { withCredentials: true })
    );
    await this.router.navigate(['/homepage']);
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

  async logout(reason: undefined | string = undefined) {
    this.accessToken = null;
    await this.router.navigate(['/login'], {
      info: <LoginInfo>{
        message: reason
      }
    });
  }

  async refreshTokens() {
    const newAccessToken = await firstValueFrom(
      this.http.post<string>(`${apiUrl}/refresh`, JSON.stringify(this.accessToken), { headers: { 'Content-Type': 'application/json' }, withCredentials: true })
    );
    this.accessToken = newAccessToken;
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
