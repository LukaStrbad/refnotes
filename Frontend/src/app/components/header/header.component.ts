import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { headerRoutes } from '../../app.routes';
import { Route, RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent {
  theme = 'auto';
  headerRoutes = headerRoutes;

  constructor(public auth: AuthService) {
    this.theme = localStorage.getItem('theme') ?? 'auto';
  }

  setTheme(newTheme: string) {
    this.theme = newTheme;
    if (newTheme === 'auto') {
      localStorage.removeItem('theme');
      newTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    } else {
      if (newTheme !== 'light' && newTheme !== 'dark') {
        newTheme = 'light';
      }
      localStorage.setItem('theme', newTheme);
    }

    document.documentElement.setAttribute('data-theme', newTheme);
  }

  handleClick() {
    const elem = document.activeElement as HTMLElement | null;
    elem?.blur();
  }

  getRouteTitle(route: Route) {
    switch (route.path) {
      case 'homepage':
        return 'Home';
      default:
        return route.path;
    }
  }

  async logout() {
    await this.auth.logout();
  }
}
