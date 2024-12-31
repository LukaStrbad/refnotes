import { Component } from '@angular/core';
import { SettingsService } from '../../services/settings.service';
import { MdEditorSettings, Theme } from '../../model/settings';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-settings',
  imports: [FormsModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent {
  mdEditor: MdEditorSettings;

  constructor(
    public settings: SettingsService
  ) {
    this.mdEditor = settings.mdEditor();
  }

  setTheme(theme: Theme) {
    this.settings.setTheme(theme);
  }

  updateMdEditorSettings() {
    this.settings.setMdEditorSettings(this.mdEditor);
  }
}
