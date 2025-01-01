import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './components/header/header.component';
import { TranslateService } from "@ngx-translate/core";
import { SettingsService } from '../services/settings.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, HeaderComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'Frontend';

  constructor(
    private translate: TranslateService,
    private settings: SettingsService
  ) {
    translate.addLangs(['en', 'hr']);
    translate.setDefaultLang('en');
    const lang = settings.language();
    translate.use(lang);
  }
}
