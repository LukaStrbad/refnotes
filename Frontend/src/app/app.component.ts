import { Component, ViewContainerRef } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './components/header/header.component';
import { TranslateService } from "@ngx-translate/core";
import { SettingsService } from '../services/settings.service';
import { NotificationService } from '../services/notification.service';
import { NgClass } from '@angular/common';
import { AskModalService } from '../services/ask-modal.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, HeaderComponent, NgClass],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'Frontend';

  constructor(
    translate: TranslateService,
    settings: SettingsService,
    public notificationService: NotificationService,
    viewContainer: ViewContainerRef,
    askModal: AskModalService,
  ) {
    translate.addLangs(['en', 'hr']);
    translate.setDefaultLang('en');
    const lang = settings.language();
    translate.use(lang);

    // Set the view container for the AskModalService
    askModal.viewContainer = viewContainer;
  }

  removeNotification(id: number) {
    this.notificationService.removeNotification(id);
  }
}
