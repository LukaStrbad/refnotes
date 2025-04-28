import { CommonModule } from '@angular/common';
import { AfterViewInit, Component, ElementRef, ViewChild } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { SettingsService } from '../../../services/settings.service';
import { Theme } from '../../../model/settings';
import { TranslateDirective, TranslatePipe, TranslateService } from '@ngx-translate/core';
import { NotificationService } from '../../../services/notification.service';
import { getTranslation } from '../../../utils/translation-utils';
import { SearchComponent } from "../search/search.component";

@Component({
  selector: 'app-header',
  imports: [CommonModule, RouterModule, TranslateDirective, TranslatePipe, SearchComponent],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css'
})
export class HeaderComponent implements AfterViewInit {
  @ViewChild('header', { static: true })
  private headerRef!: ElementRef<HTMLElement>;

  constructor(
    public auth: AuthService,
    public settings: SettingsService,
    private translate: TranslateService,
    private notificationService: NotificationService,
  ) { }

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
}
