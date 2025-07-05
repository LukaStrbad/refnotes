import { Component } from '@angular/core';
import { SettingsService } from '../../services/settings.service';
import { GroupSettings, MdEditorSettings, Theme } from '../../model/settings';
import { FormsModule } from '@angular/forms';
import { TranslateDirective, TranslatePipe } from '@ngx-translate/core';
import { TestTagDirective } from '../../directives/test-tag.directive';

@Component({
  selector: 'app-settings',
  imports: [FormsModule, TranslateDirective, TestTagDirective, TranslatePipe],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css',
})
export class SettingsComponent {
  mdEditor: MdEditorSettings;
  group: GroupSettings;

  selectedLanguage: string;

  constructor(public settings: SettingsService) {
    this.mdEditor = settings.mdEditor();
    this.group = settings.group();
    this.selectedLanguage = settings.language();
  }

  setTheme(theme: Theme) {
    this.settings.setTheme(theme);
  }

  updateMdEditorSettings() {
    this.settings.setMdEditorSettings(this.mdEditor);
  }

  updateGroupSettings() {
    this.settings.setGroupSettings(this.group);
  }

  updateLanguage() {
    this.settings.setLanguage(this.selectedLanguage);
  }
}
