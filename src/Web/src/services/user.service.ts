import { inject, Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AuthService } from './auth.service';
import { TranslateService } from '@ngx-translate/core';
import { UserResponse } from '../model/user-response';
import { EditUserRequest } from '../model/edit-user-request';
import { UpdatePasswordRequest } from '../model/update-password-request';
import { UpdatePasswordByTokenRequest } from '../model/update-password-by-token-request';
import { generateHttpParams } from '../utils/http-utils';

const apiUrl = environment.apiUrl + '/user';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly translate = inject(TranslateService);

  async getAccountInfo() {
    return firstValueFrom(
      this.http.get<UserResponse>(`${apiUrl}/accountInfo`),
    );
  }

  async confirmEmail(token: string) {
    await firstValueFrom(
      this.http.post(`${apiUrl}/confirmEmail/${token}`, {}),
    );
    this.auth.setUserAndToken();
  }

  async resendEmailConfirmation() {
    const lang = this.translate.currentLang;
    await firstValueFrom(
      this.http.post(`${apiUrl}/resendEmailConfirmation?lang=${lang}`, {}, { responseType: 'text' }),
    );
  }

  async editAccount(editUserRequest: EditUserRequest, lang: string) {
    const user = await firstValueFrom(
      this.http.post<UserResponse>(`${apiUrl}/edit?lang=${lang}`, editUserRequest)
    );

    // Update the auth service with the new user data
    this.auth.setUserAndToken();
    return user;
  }

  async updatePassword(updatePasswordRequest: UpdatePasswordRequest) {
    await firstValueFrom(
      this.http.post(`${apiUrl}/updatePassword`, updatePasswordRequest, { responseType: 'text' }),
    );
  }

  async updatePasswordByToken(request: UpdatePasswordByTokenRequest) {
    await firstValueFrom(
      this.http.post(`${apiUrl}/updatePasswordByToken`, request, { responseType: 'text' }),
    );
  }

  async sendPasswordResetEmail(username: string, lang: string) {
    const params = generateHttpParams({ username, lang });
    await firstValueFrom(
      this.http.post(`${apiUrl}/sendPasswordResetEmail`, null, { responseType: 'text', params })
    );
  }
}
