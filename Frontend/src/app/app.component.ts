import { Component, OnDestroy, OnInit, ViewContainerRef } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { HeaderComponent } from './components/header/header.component';
import { TranslateService } from "@ngx-translate/core";
import { SettingsService } from '../services/settings.service';
import { NotificationService } from '../services/notification.service';
import { NgClass } from '@angular/common';
import { AskModalService } from '../services/ask-modal.service';
import { TestTagDirective } from '../directives/test-tag.directive';
import { SearchComponent } from "./components/search/search.component";
import { filter, Subscription } from 'rxjs';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, HeaderComponent, NgClass, TestTagDirective, SearchComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit, OnDestroy {
  private navSubscription?: Subscription;

  constructor(
    translate: TranslateService,
    private settings: SettingsService,
    public notificationService: NotificationService,
    viewContainer: ViewContainerRef,
    askModal: AskModalService,
    private router: Router
  ) {
    translate.addLangs(['en', 'hr']);
    translate.setDefaultLang('en');
    const lang = this.settings.language();
    translate.use(lang);

    // Set the view container for the AskModalService
    askModal.viewContainer = viewContainer;
  }

  ngOnInit(): void {
    this.navSubscription = this.router.events.pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(this.handleNavigationEnd.bind(this));
  }

  ngOnDestroy(): void {
    this.navSubscription?.unsubscribe();
  }

  private handleNavigationEnd(event: NavigationEnd) {
    const url = event.urlAfterRedirects;

    if (url.startsWith('/groups')) {
      this.settings.rememberGroupPath(url);
    }
  }

  removeNotification(id: number) {
    this.notificationService.removeNotification(id);
  }
}
