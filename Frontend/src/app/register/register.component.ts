import { CommonModule, LowerCasePipe } from '@angular/common';
import { Component } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, FormsModule, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslateDirective, TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  imports: [
    CommonModule,
    FormsModule,
    TranslateDirective,
    TranslatePipe,
    LowerCasePipe,
    RouterLink,
    ReactiveFormsModule
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  registrationForm = new FormGroup({
    username: new FormControl('', [Validators.required, Validators.minLength(4)]),
    name: new FormControl('', Validators.required),
    email: new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', [Validators.required, Validators.minLength(8)]),
    confirmPassword: new FormControl('', [
      Validators.required,
      Validators.minLength(8),
      this.passwordMatchValidator
    ])
  });

  passwordMatchValidator(confirmPassword: AbstractControl): ValidationErrors | null {
    const form = confirmPassword.parent;
    const password = form?.get('password');
    return password && password.value !== confirmPassword.value ? { passwordMismatch: true } : null;
  }

  get username() { return this.registrationForm.get('username'); }
  get name() { return this.registrationForm.get('name'); }
  get email() { return this.registrationForm.get('email'); }
  get password() { return this.registrationForm.get('password'); }
  get confirmPassword() { return this.registrationForm.get('confirmPassword'); }

  constructor(
    private auth: AuthService
  ) { }

  async register() {
    const { username, name, email, password } = this.registrationForm.value;

    if (!username || !name || !email || !password) {
      return;
    }

    await this.auth.register(username, name, email, password);
  }
}
