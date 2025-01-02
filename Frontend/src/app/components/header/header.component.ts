import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { SettingsService } from '../../../services/settings.service';
import { Theme } from '../../../model/settings';
import { TranslateDirective, TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-header',
  imports: [CommonModule, RouterModule, TranslateDirective, TranslatePipe],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent {
  constructor(
    public auth: AuthService,
    public settings: SettingsService
  ) { }

  setTheme(newTheme: Theme) {
    this.settings.setTheme(newTheme);
  }

  handleClick() {
    const elem = document.activeElement as HTMLElement | null;
    elem?.blur();
  }

  async logout() {
    await this.auth.logout();
  }
}
