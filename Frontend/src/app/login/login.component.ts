import { Component } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { AuthService } from "../../services/auth.service";
import { CommonModule, LowerCasePipe } from "@angular/common";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { getErrorMessage } from "../../utils/errorHandler";
import { TranslateDirective, TranslatePipe } from "@ngx-translate/core";

@Component({
  selector: "app-login",
  imports: [
    CommonModule,
    FormsModule,
    TranslateDirective,
    TranslatePipe,
    LowerCasePipe,
    RouterLink
  ],
  templateUrl: "./login.component.html",
  styleUrl: "./login.component.css"
})
export class LoginComponent {
  username = "";
  password = "";
  error: string | null = null;
  message: string | null = null;
  redirectUrl?: string;

  constructor(
    private auth: AuthService,
    private router: Router,
    private route: ActivatedRoute,
  ) {
    const navigation = this.router.getCurrentNavigation();
    const info = navigation?.extras.info as LoginInfo;
    if (info) {
      this.message = info.message ?? null;
    }

    this.redirectUrl = this.route.snapshot.queryParamMap.get("redirectUrl") ?? undefined;
  }

  async login() {

    this.error = null;
    try {
      await this.auth.login(this.username, this.password, this.redirectUrl);
    } catch (e) {
      this.error = getErrorMessage(e, {
        401: "login.errors.invalidPassword",
        404: "login.errors.usernameNotFound",
      });
    }
  }
}

export interface LoginInfo {
  message: string | undefined;
}
