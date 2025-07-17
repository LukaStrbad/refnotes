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
import { TranslateDirective, TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../services/auth.service';
import {
  animate,
  style,
  transition,
  trigger,
} from '@angular/animations';
import { TestTagDirective } from "../../directives/test-tag.directive";

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
    trigger('reveal', [
      transition(':enter', [
        style({
          opacity: 0,
          transform: 'translateY(-5px) scale(0.95)'
        }),
        animate('120ms ease-out', style({
          opacity: 1,
          transform: 'translateY(0px) scale(1)'
        }))
      ]),
      transition(':leave', [
        animate('120ms ease-in', style({
          opacity: 0,
          transform: 'translateY(-5px) scale(0.95)'
        }))
      ])
    ]),
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
    private route: ActivatedRoute
  ) {
    this.redirectUrl = this.route.snapshot.queryParamMap.get("redirectUrl") ?? undefined;
  }

  async register() {
    const { username, name, email, password } = this.registrationForm.value;

    if (!username || !name || !email || !password) {
      return;
    }

    await this.auth.register(username, name, email, password, this.redirectUrl);
  }
}
