import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './components/header/header.component';
import { TranslateService } from "@ngx-translate/core";
import { SettingsService } from '../services/settings.service';
import { NotificationService } from '../services/notification.service';
import { NgClass } from '@angular/common';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, HeaderComponent, NgClass],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'Frontend';

  constructor(
    private translate: TranslateService,
    private settings: SettingsService,
    public notificationService: NotificationService,
  ) {
    translate.addLangs(['en', 'hr']);
    translate.setDefaultLang('en');
    const lang = settings.language();
    translate.use(lang);
  }

  removeNotification(id: number) {
    this.notificationService.removeNotification(id);
  }
}
