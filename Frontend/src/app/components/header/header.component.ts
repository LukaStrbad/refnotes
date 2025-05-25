import { CommonModule } from '@angular/common';
import { AfterViewInit, Component, computed, ElementRef, output, Signal, ViewChild } from '@angular/core';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { SettingsService } from '../../../services/settings.service';
import { Theme } from '../../../model/settings';
import { TranslateDirective, TranslatePipe, TranslateService } from '@ngx-translate/core';
import { NotificationService } from '../../../services/notification.service';
import { getTranslation } from '../../../utils/translation-utils';
import { SearchComponent } from "../search/search.component";
import { filter } from 'rxjs';
import { TestTagDirective } from '../../../directives/test-tag.directive';

@Component({
  selector: 'app-header',
  imports: [CommonModule, RouterModule, TranslateDirective, TranslatePipe, SearchComponent, TestTagDirective],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css'
})
export class HeaderComponent implements AfterViewInit {
  openMobileSearch = output<void>();

  groupLink: Signal<string>;
  onGroupPage = false;

  @ViewChild('header', { static: true })
  private headerRef!: ElementRef<HTMLElement>;

  constructor(
    public auth: AuthService,
    public settings: SettingsService,
    private translate: TranslateService,
    private notificationService: NotificationService,
    private router: Router,
  ) {
    this.groupLink = computed(() => {
      const groupSettings = this.settings.group();
      const savedPath = groupSettings.rememberGroupPath ? groupSettings.groupPath : undefined;
      return savedPath ?? '/groups';
    });

    this.router.events.pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event) => {
        const url = event.urlAfterRedirects;
        this.onGroupPage = url.startsWith('/groups');
      });
  }

  ngAfterViewInit(): void {
    this.setHeaderHeightVar();

    this.headerRef.nativeElement.onresize = () => {
      this.setHeaderHeightVar();
    }
  }

  private setHeaderHeightVar() {
    document.documentElement.style.setProperty('--header-height', `${this.headerRef.nativeElement.clientHeight}px`);
  }

  setTheme(newTheme: Theme) {
    this.settings.setTheme(newTheme);
  }

  async logout() {
    await this.auth.logout();
    this.notificationService.info(await getTranslation(this.translate, 'header.logout'));
  }

  onOpenMobileSearch() {
    // Clear the active element to remove focus from header dropdown
    if (document.activeElement instanceof HTMLElement) {
      document.activeElement.blur();
    }
    this.openMobileSearch.emit()
  }
}
