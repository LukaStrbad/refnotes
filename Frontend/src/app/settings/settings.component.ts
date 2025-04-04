import { Component } from '@angular/core';
import { SettingsService } from '../../services/settings.service';
import { MdEditorSettings, Theme } from '../../model/settings';
import { FormsModule } from '@angular/forms';
import { TranslateDirective } from '@ngx-translate/core';
import { TestTagDirective } from '../../directives/test-tag.directive';

@Component({
  selector: 'app-settings',
  imports: [FormsModule, TranslateDirective, TestTagDirective],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css',
})
export class SettingsComponent {
  mdEditor: MdEditorSettings;

  selectedLanguage: string;

  constructor(public settings: SettingsService) {
    this.mdEditor = settings.mdEditor();
    this.selectedLanguage = settings.language();
  }

  setTheme(theme: Theme) {
    this.settings.setTheme(theme);
  }

  updateMdEditorSettings() {
    this.settings.setMdEditorSettings(this.mdEditor);
  }

  updateLanguage() {
    this.settings.setLanguage(this.selectedLanguage);
  }
}
