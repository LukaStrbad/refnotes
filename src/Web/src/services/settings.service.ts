import { Injectable, Signal, signal, WritableSignal } from '@angular/core';
import { EditorMode, GroupSettings, MdEditorSettings, SearchSettings, Theme } from '../model/settings';
import { TranslateService } from '@ngx-translate/core';

@Injectable({
  providedIn: 'root'
})
export class SettingsService {
  private readonly _language: WritableSignal<string>;
  private readonly _theme: WritableSignal<Theme>;
  private readonly _mdEditor: WritableSignal<MdEditorSettings>;
  private readonly _search: WritableSignal<SearchSettings>;
  private readonly _group: WritableSignal<GroupSettings>;

  public get language(): Signal<string> {
    return this._language;
  }

  public get languageList(): string[] {
    return this.translate.getLangs();
  }

  public get theme(): Signal<Theme> {
    return this._theme;
  }

  public get mdEditor(): Signal<MdEditorSettings> {
    return this._mdEditor;
  }

  public get search(): Signal<SearchSettings> {
    return this._search;
  }

  public get group(): Signal<GroupSettings> {
    return this._group;
  }

  constructor(
    private translate: TranslateService
  ) {
    this._language = signal(this.loadLanguage());
    this._mdEditor = signal(this.loadMdEditorSettings());
    this._theme = signal(this.loadTheme());
    this._search = signal(this.loadSearchSettings());
    this._group = signal(this.loadGroupSettings());
  }

  private loadLanguage(): string {
    const language = localStorage.getItem('language');
    if (language) {
      return language;
    }
    return 'en';
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
      const s = JSON.parse(settings) as MdEditorSettings;
      // Check if the settings are valid
      return {
        editorMode: s.editorMode ?? 'SideBySide',
        showLineNumbers: s.showLineNumbers ?? true,
        wrapLines: s.wrapLines ?? false,
        experimentalFastRender: s.experimentalFastRender ?? false
      };
    }
    return {
      editorMode: 'SideBySide',
      showLineNumbers: true,
      wrapLines: false,
      experimentalFastRender: false
    };
  }

  private loadSearchSettings(): SearchSettings {
    const settings = localStorage.getItem('searchSettings');
    if (settings) {
      const s = JSON.parse(settings) as SearchSettings;
      // Check if the settings are valid
      return {
        fullTextSearch: s.fullTextSearch ?? false,
        onlySearchCurrentDir: s.onlySearchCurrentDir ?? false
      };
    }
    return {
      fullTextSearch: false,
      onlySearchCurrentDir: false
    };
  }

  private loadGroupSettings(): GroupSettings {
    const settings = localStorage.getItem('groupSettings');
    if (settings) {
      const s = JSON.parse(settings) as GroupSettings;
      // Check if the settings are valid
      return {
        rememberGroupPath: s.rememberGroupPath ?? true,
        groupPath: s.groupPath
      };
    }
    return {
      rememberGroupPath: true
    };
  }

  public setLanguage(language: string) {
    this._language.set(language);
    localStorage.setItem('language', language);
    this.translate.use(language);
  }

  public setTheme(theme: Theme) {
    this._theme.set(theme);

    if (theme === 'auto') {
      localStorage.removeItem('theme');
      theme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    } else {
      if (theme !== 'light' && theme !== 'dark') {
        theme = 'light';
        this._theme.set(theme);
      }
      localStorage.setItem('theme', theme);
    }

    document.documentElement.setAttribute('data-theme', theme);
  }

  public setMdEditorMode(mode: EditorMode) {
    this.setMdEditorSettings({ ...this._mdEditor(), editorMode: mode });
  }

  public setMdEditorSettings(settings: MdEditorSettings) {
    this._mdEditor.set({ ...settings });
    localStorage.setItem('mdEditorSettings', JSON.stringify(settings));
  }

  public setSearchSettings(settings: SearchSettings) {
    this._search.set({ ...settings });
    localStorage.setItem('searchSettings', JSON.stringify(settings));
  }

  public setGroupSettings(settings: GroupSettings) {
    this._group.set({ ...settings });
    localStorage.setItem('groupSettings', JSON.stringify(settings));
  }

  public rememberGroupPath(url: string) {
    this._group.update(group => {
      group.groupPath = url;
      return group;
    });
    this.setGroupSettings(this._group());
  }
}
