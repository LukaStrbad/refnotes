import { Component } from '@angular/core';
import { AuthService } from '../../../services/auth.service';
import { TranslateDirective } from '@ngx-translate/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-home-button',
  imports: [TranslateDirective, RouterLink],
  templateUrl: './home-button.component.html',
  styleUrl: './home-button.component.css'
})
export class HomeButtonComponent {
  constructor(
    public authService: AuthService
  ) { }
}
