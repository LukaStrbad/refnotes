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
import { RouterLink } from '@angular/router';
import { TranslateDirective, TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../services/auth.service';
import {
  animate,
  group,
  query,
  style,
  transition,
  trigger,
} from '@angular/animations';
import {TestTagDirective} from "../../directives/test-tag.directive";

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
  animations: [
    trigger('reveal', [
      transition(
        '* => *',
        [
          query(':self', [style({ height: '{{startHeight}}px' })]),
          query(':enter', [style({ opacity: 0, scale: 0.0 })], {
            optional: true,
          }),
          query(
            ':leave',
            [
              style({ opacity: 1, scale: 1 }),
              animate('0.2s ease-in', style({ opacity: 0, scale: 0.0 })),
            ],
            { optional: true },
          ),
          group([
            query(':self', [animate('0.2s ease-in', style({ height: '*' }))]),
            query(
              ':enter',
              [animate('0.2s ease-in', style({ opacity: 1, scale: 1 }))],
              { optional: true },
            ),
          ]),
        ],
        { params: { startHeight: 0 } },
      ),
    ]),
  ],
})
export class RegisterComponent {
  registrationForm = new FormGroup({
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

  get username() {
    return this.registrationForm.get('username');
  }

  get name() {
    return this.registrationForm.get('name');
  }

  get email() {
    return this.registrationForm.get('email');
  }

  get password() {
    return this.registrationForm.get('password');
  }

  get confirmPassword() {
    return this.registrationForm.get('confirmPassword');
  }

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

  constructor(private auth: AuthService) {}

  async register() {
    const { username, name, email, password } = this.registrationForm.value;

    if (!username || !name || !email || !password) {
      return;
    }

    await this.auth.register(username, name, email, password);
  }
}
