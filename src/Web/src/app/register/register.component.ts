import { CommonModule, LowerCasePipe } from '@angular/common';
import { Component } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslateDirective, TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../services/auth.service';
import { TestTagDirective } from "../../directives/test-tag.directive";
import { getRevealAnimations } from '../../utils/animations';
import { NotificationService } from '../../services/notification.service';
import { LoggerService } from '../../services/logger.service';

@Component({
  selector: 'app-register',
  imports: [
    CommonModule,
    FormsModule,
    TranslateDirective,
    TranslatePipe,
    LowerCasePipe,
    RouterLink,
    ReactiveFormsModule,
    TestTagDirective,
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css',
  // changeDetection: ChangeDetectionStrategy.OnPush,
  animations: [
    getRevealAnimations(),
  ],
})
export class RegisterComponent {
  readonly registrationForm = new FormGroup({
    username: new FormControl('', [
      Validators.required,
      Validators.minLength(4),
    ]),
    name: new FormControl('', Validators.required),
    email: new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', [
      Validators.required,
      Validators.minLength(8),
    ]),
    confirmPassword: new FormControl('', [
      Validators.required,
      Validators.minLength(8),
      this.passwordMatchValidator,
    ]),
  });

  passwordMatchValidator(
    confirmPassword: AbstractControl,
  ): ValidationErrors | null {
    const form = confirmPassword.parent;
    const password = form?.get('password');
    return password && password.value !== confirmPassword.value
      ? { passwordMismatch: true }
      : null;
  }

  readonly username = this.registrationForm.get('username');
  readonly name = this.registrationForm.get('name');
  readonly email = this.registrationForm.get('email');
  readonly password = this.registrationForm.get('password');
  readonly confirmPassword = this.registrationForm.get('confirmPassword');

  // displayUsernameError: boolean | null = null;

  get displayUsernameError() {
    return (
      this.username &&
      this.username.invalid &&
      (this.username.dirty || this.username.touched)
    );
  }

  get displayNameError() {
    return (
      this.name && this.name.invalid && (this.name.dirty || this.name.touched)
    );
  }

  get displayEmailError() {
    return (
      this.email &&
      this.email.invalid &&
      (this.email.dirty || this.email.touched)
    );
  }

  get displayPasswordError() {
    return (
      this.password &&
      this.password.invalid &&
      (this.password.dirty || this.password.touched)
    );
  }

  get displayConfirmPasswordError() {
    return (
      this.confirmPassword &&
      this.confirmPassword.invalid &&
      (this.confirmPassword.dirty || this.confirmPassword.touched)
    );
  }

  readonly redirectUrl?: string;

  constructor(
    private auth: AuthService,
    private route: ActivatedRoute,
    private notificationService: NotificationService,
    private translate: TranslateService,
    private log: LoggerService,
  ) {
    this.redirectUrl = this.route.snapshot.queryParamMap.get("redirectUrl") ?? undefined;
  }

  async register() {
    const { username, name, email, password } = this.registrationForm.value;

    if (!username || !name || !email || !password) {
      return;
    }

    try {
      await this.auth.register(username, name, email, password, this.redirectUrl);
    } catch (error) {
      await this.notificationService.notifyError(error, this.translate, this.log);
    }
  }
}
