import { Component, model } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { AuthService } from "../../services/auth.service";
import { CommonModule, LowerCasePipe } from "@angular/common";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { getErrorMessage } from "../../utils/errorHandler";
import { TranslateDirective, TranslatePipe, TranslateService } from "@ngx-translate/core";
import { AskModalService } from "../../services/ask-modal.service";
import { UserService } from "../../services/user.service";
import { getTranslation } from "../../utils/translation-utils";
import { TestTagDirective } from "../../directives/test-tag.directive";

@Component({
  selector: "app-login",
  imports: [
    CommonModule,
    FormsModule,
    TranslateDirective,
    TranslatePipe,
    LowerCasePipe,
    RouterLink,
    TestTagDirective,
  ],
  templateUrl: "./login.component.html",
  styleUrl: "./login.component.css"
})
export class LoginComponent {
  readonly username = model("");
  readonly password = model("");
  error: string | null = null;
  message: string | null = null;
  redirectUrl?: string;

  constructor(
    private auth: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private translate: TranslateService,
    private userService: UserService,
    private askModal: AskModalService,
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
      await this.auth.login(this.username(), this.password(), this.redirectUrl);
    } catch (e) {
      this.error = getErrorMessage(e, {
        401: "login.errors.invalidPassword",
        404: "login.errors.usernameNotFound",
      });
    }
  }

  async sendPasswordResetEmail() {
    const askModalBody = await getTranslation(this.translate, "login.modal.forgot-password.body", { username: this.username() });
    const confirmed = await this.askModal.confirm(
      "login.modal.forgot-password.title",
      "login.modal.forgot-password.message",
      { translate: true, body: askModalBody }
    );

    if (!confirmed) {
      return;
    }

    await this.userService.sendPasswordResetEmail(this.username(), this.translate.currentLang);
  }
}

export interface LoginInfo {
  message: string | undefined;
}
