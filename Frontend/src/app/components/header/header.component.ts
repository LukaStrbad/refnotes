import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { headerRoutes } from '../../app.routes';
import { Route, RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { SettingsService } from '../../../services/settings.service';
import { Theme } from '../../../model/settings';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-header',
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent {
  headerRoutes = headerRoutes;

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

  getRouteTitle(route: Route) {
    switch (route.path) {
      case 'homepage':
        return 'Home';
      case 'settings':
        return 'Settings';
      default:
        return route.path;
    }
  }

  async logout() {
    await this.auth.logout();
  }
}
