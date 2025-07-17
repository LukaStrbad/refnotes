import { inject, Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AuthService } from './auth.service';
import { TranslateService } from '@ngx-translate/core';
import { UserResponse } from '../model/user-response';

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
}
