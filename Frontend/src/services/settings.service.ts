import { effect, Injectable, Signal, signal, WritableSignal } from '@angular/core';
import { EditorMode, MdEditorSettings, Theme } from '../model/settings';

@Injectable({
  providedIn: 'root'
})
export class SettingsService {
  private _theme: WritableSignal<Theme>;
  private _mdEditor: WritableSignal<MdEditorSettings>;

  public get theme(): Signal<Theme> {
    return this._theme;
  }

  public get mdEditor(): Signal<MdEditorSettings> {
    return this._mdEditor;
  }

  constructor() {
    this._mdEditor = signal(this.loadMdEditorSettings());
    this._theme = signal(this.loadTheme());
  }

  private loadTheme(): Theme {
    const theme = localStorage.getItem('theme');
    if (theme) {
      return theme as Theme;
    }
    return 'auto';
  }

  private loadMdEditorSettings(): MdEditorSettings {
    const settings = localStorage.getItem('mdEditorSettings');
    if (settings) {
      return JSON.parse(settings);
    }
    return {
      editorMode: 'SideBySide',
      showLineNumbers: true,
      wrapLines: false
    };
  }

  public setTheme(theme: Theme) {
    this._theme.set(theme);
    if (theme === 'auto') {
      localStorage.removeItem('theme');
      theme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    } else {
      if (theme !== 'light' && theme !== 'dark') {
        theme = 'light';
      }
      localStorage.setItem('theme', theme);
    }

    document.documentElement.setAttribute('data-theme', theme);
  }

  public setMdEditorMode(mode: EditorMode) {
    this.setMdEditorSettings({ ...this._mdEditor(), editorMode: mode });
  }

  public setMdEditorSettings(settings: MdEditorSettings) {
    this._mdEditor.set(settings);
    localStorage.setItem('mdEditorSettings', JSON.stringify(settings));
  }
}
