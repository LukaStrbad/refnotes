import { Component } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { AuthService } from "../../services/auth.service";
import { CommonModule } from "@angular/common";
import { HttpErrorResponse, HttpResponse } from "@angular/common/http";
import { ActivatedRoute, Router } from "@angular/router";
import { getErrorMessage } from "../../utils/errorHandler";

@Component({
  selector: "app-login",
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: "./login.component.html",
  styleUrl: "./login.component.scss",
})
export class LoginComponent {
  username = "";
  password = "";
  error: string | null = null;
  message: string | null = null;

  constructor(
    private auth: AuthService,
    private router: Router,
    private activatedRoute: ActivatedRoute
  ) {
    const navigation = this.router.getCurrentNavigation();
    const info = navigation?.extras.info as LoginInfo;
    if (info) {
      this.message = info.message ?? null;
    }
  }

  async login() {
    this.error = null;
    try {
      await this.auth.login(this.username, this.password);
    } catch (e) {
      this.error = getErrorMessage(e, {
        401: "Invalid password",
        404: "Username not found",
      });
    }
  }
}

export interface LoginInfo {
  message: string | undefined;
}
