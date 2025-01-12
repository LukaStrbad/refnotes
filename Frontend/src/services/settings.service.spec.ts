import { TestBed } from '@angular/core/testing';

import { SettingsService } from './settings.service';
import {
  TranslateFakeLoader,
  TranslateLoader,
  TranslateModule,
  TranslateService,
} from '@ngx-translate/core';
import {MdEditorSettings, Theme} from '../model/settings';

describe('SettingsService', () => {
  let service: SettingsService;
  let translate: TranslateService;
  let storage: { [key: string]: string } = {};

  beforeEach(() => {
    spyOn(localStorage, 'getItem').and.callFake((key: string) => storage[key] ?? null);
    spyOn(localStorage, 'setItem').and.callFake((key: string, value: string) => {
      storage[key] = value;
    });

    TestBed.configureTestingModule({
      imports: [
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
      providers: [TranslateService],
    });
    storage = {};
    service = TestBed.inject(SettingsService);
    translate = TestBed.inject(TranslateService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should return default language if no language is stored', () => {
    expect(service.language()).toBe('en');
  });

  it('should return stored language if language is stored', () => {
    localStorage.setItem('language', 'hr');
    // This will create a new instance of the service and load the stored language
    service = new SettingsService(translate);
    expect(service.language()).toBe('hr');
  });

  it('should set and store language', () => {
    const translate = TestBed.inject(TranslateService);
    spyOn(translate, 'use');
    service.setLanguage('hr');

    expect(service.language()).toBe('hr');
    expect(localStorage.getItem('language')).toBe('hr');
    expect(translate.use).toHaveBeenCalledWith('hr');
  });

  it('should return default theme if no theme is stored', () => {
    expect(service.theme()).toBe('auto');
  });

  it('should return stored theme if theme is stored', () => {
    localStorage.setItem('theme', 'dark');
    service = new SettingsService(translate);
    expect(service.theme()).toBe('dark');
  });

  it('should set preferred theme if theme is auto', () => {
    service.setTheme('auto');

    expect(service.theme()).toBe('auto');
    expect(localStorage.getItem('theme')).toBeNull();
  });

  it('should set light theme if theme is invalid', () => {
    service.setTheme('invalid' as Theme);

    expect(service.theme()).toBe('light');
    expect(localStorage.getItem('theme')).toBe('light');
  });

  it('should set and store theme', () => {
    service.setTheme('light');
    expect(service.theme()).toBe('light');
    expect(localStorage.getItem('theme')).toBe('light');
  });

  it('should return default mdEditor settings if no settings are stored', () => {
    expect(service.mdEditor()).toEqual({
      editorMode: 'SideBySide',
      showLineNumbers: true,
      wrapLines: false,
    });
  });

  it('should return stored mdEditor settings if settings are stored', () => {
    const settings = JSON.stringify(<MdEditorSettings>{
      editorMode: 'PreviewOnly',
      showLineNumbers: false,
      wrapLines: true,
    });
    localStorage.setItem('mdEditorSettings', settings);
    service = new SettingsService(translate);

    expect(service.mdEditor()).toEqual({
      editorMode: 'PreviewOnly',
      showLineNumbers: false,
      wrapLines: true,
    });
  });

  it('should set and store mdEditor settings', () => {
    const settings: MdEditorSettings = {
      editorMode: 'PreviewOnly',
      showLineNumbers: false,
      wrapLines: true,
    };
    service.setMdEditorSettings(settings);

    expect(service.mdEditor()).toEqual(settings);
    expect(localStorage.getItem('mdEditorSettings')).toBe(
      JSON.stringify(settings),
    );
  });
});
