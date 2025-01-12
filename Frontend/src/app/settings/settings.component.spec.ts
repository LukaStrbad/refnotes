import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SettingsComponent } from './settings.component';
import { SettingsService } from '../../services/settings.service';
import {
  TranslateFakeLoader,
  TranslateLoader,
  TranslateModule,
  TranslateService,
} from '@ngx-translate/core';

describe('SettingsComponent', () => {
  let component: SettingsComponent;
  let fixture: ComponentFixture<SettingsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        SettingsComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
      providers: [TranslateService, SettingsService],
    }).compileComponents();

    fixture = TestBed.createComponent(SettingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with default mdEditor settings and selected language', () => {
    expect(component.mdEditor).toEqual({
      editorMode: 'SideBySide',
      showLineNumbers: true,
      wrapLines: false,
    });
    expect(component.selectedLanguage).toBe('en');
  });

  it('should update theme when setTheme is called', () => {
    const settings = TestBed.inject(SettingsService);
    spyOn(settings, 'setTheme');

    const radios: NodeListOf<HTMLInputElement> = document.querySelectorAll(
      'input[type="radio"]',
    );
    let darkThemeRadio: HTMLInputElement | null = null;
    radios.forEach((radio) => {
      if (radio.value === 'dark') {
        darkThemeRadio = radio;
      }
    });

    expect(darkThemeRadio).toBeTruthy();
    darkThemeRadio!.click();

    expect(settings.setTheme).toHaveBeenCalledWith('dark');
  });

  it('should update mdEditor settings when updateMdEditorSettings is called', () => {
    const settings = TestBed.inject(SettingsService);
    spyOn(settings, 'setMdEditorSettings');

    const editorModeSelect = document.querySelector(
      'select[data-test="editor-mode-select"]',
    ) as HTMLSelectElement;
    const options = editorModeSelect.querySelectorAll('option');
    let previewOnlyIndex = -1;
    options.forEach((option, index) => {
      if (option.value === 'PreviewOnly') {
        previewOnlyIndex = index;
      }
    });

    expect(previewOnlyIndex).toBeGreaterThan(0);
    editorModeSelect.selectedIndex = previewOnlyIndex;
    editorModeSelect.dispatchEvent(new Event('change'));

    expect(settings.setMdEditorSettings).toHaveBeenCalledWith(
      jasmine.objectContaining({ editorMode: 'PreviewOnly' }),
    );
  });

  it('should update language when updateLanguage is called', () => {
    const settings = TestBed.inject(SettingsService);
    spyOn(settings, 'setLanguage');
    spyOnProperty(settings, 'languageList', 'get').and.returnValue([
      'en',
      'hr',
    ]);

    fixture.detectChanges();

    const languageSelect = document.querySelector(
      'select[data-test="language-select"]',
    ) as HTMLSelectElement;
    const options = languageSelect.querySelectorAll('option');
    let hrIndex = -1;
    options.forEach((option, index) => {
      if (option.value === 'hr') {
        hrIndex = index;
      }
    });

    expect(hrIndex).toBeGreaterThan(0);
    languageSelect.selectedIndex = hrIndex;
    languageSelect.dispatchEvent(new Event('change'));

    expect(settings.setLanguage).toHaveBeenCalledWith('hr');
  });
});
