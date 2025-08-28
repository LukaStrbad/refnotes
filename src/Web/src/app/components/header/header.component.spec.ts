import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HeaderComponent } from './header.component';
import {
  TranslateFakeLoader,
  TranslateLoader,
  TranslateModule,
  TranslateService,
} from '@ngx-translate/core';
import { AuthService } from '../../../services/auth.service';
import { provideRouter } from '@angular/router';
import { SettingsService } from '../../../services/settings.service';
import { Component, signal, WritableSignal } from '@angular/core';
import { By } from '@angular/platform-browser';
import { click } from "../../../tests/click-utils";
import { SearchService } from '../../../services/search.service';
import { TagService } from '../../../services/tag.service';
import { GroupSettings, SearchSettings, Theme } from '../../../model/settings';

@Component({ selector: 'app-search', template: '' }) class SearchStubComponent { }

describe('HeaderComponent', () => {
  let component: HeaderComponent;
  let fixture: ComponentFixture<HeaderComponent>;
  let auth: jasmine.SpyObj<AuthService>;
  let settings: jasmine.SpyObj<SettingsService>;
  let groupSettingsSignal: WritableSignal<GroupSettings>;

  beforeEach(async () => {
    auth = jasmine.createSpyObj('AuthService', ['logout', 'isUserLoggedIn']);

    groupSettingsSignal = signal<GroupSettings>({ rememberGroupPath: true });
    settings = jasmine.createSpyObj<SettingsService>('SettingsService', ['setTheme'], {
      theme: signal<Theme>('auto'),
      search: signal<SearchSettings>({
        fullTextSearch: false,
        onlySearchCurrentDir: false
      }),
      group: groupSettingsSignal,
      languageList: ['en'],
    });
    const searchService = jasmine.createSpyObj<SearchService>('SearchService', ['searchFiles']);
    searchService.searchFiles.and.resolveTo([]);
    const tagService = jasmine.createSpyObj<TagService>('TagService', ['listAllCached']);

    await TestBed.configureTestingModule({
      imports: [
        HeaderComponent,
        SearchStubComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        })
      ],
      providers: [
        provideRouter([]),
        TranslateService,
        { provide: AuthService, useValue: auth },
        { provide: SettingsService, useValue: settings },
        { provide: SearchService, useValue: searchService },
        { provide: TagService, useValue: tagService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HeaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call setTheme on settings service', () => {
    component.setTheme('dark');
    expect(settings.setTheme).toHaveBeenCalledWith('dark');
  });

  it('should call auth.logout when logout is called', async () => {
    await component.logout();
    expect(auth.logout).toHaveBeenCalled();
  });

  it('should show navigation menu and logout button when user is logged in', () => {
    auth.isUserLoggedIn.and.returnValue(true);
    fixture.detectChanges();

    const navMenu = fixture.debugElement.query(By.css('.navbar-center'));

    expect(navMenu).toBeTruthy();
  });

  it('should hide navigation menu when user is not logged in', () => {
    auth.isUserLoggedIn.and.returnValue(false);
    fixture.detectChanges();
    const navMenu = fixture.debugElement.query(By.css('.navbar-center'));
    expect(navMenu).toBeFalsy();
  });

  it('should show correct theme icon based on current theme', () => {
    // Test dark theme
    settings.setTheme('dark');
    fixture.detectChanges();
    let themeIcon = fixture.debugElement.query(By.css('.bi-moon-fill'));
    expect(themeIcon).toBeTruthy();

    // Test light theme
    settings.setTheme('light');
    fixture.detectChanges();
    themeIcon = fixture.debugElement.query(By.css('.bi-sun-fill'));
    expect(themeIcon).toBeTruthy();

    // Test auto theme
    settings.setTheme('auto');
    fixture.detectChanges();
    themeIcon = fixture.debugElement.query(By.css('.bi-circle-half'));
    expect(themeIcon).toBeTruthy();
  });

  it('should call setTheme when theme buttons are clicked', () => {
    const themeButtons = fixture.debugElement.queryAll(
      By.css('.theme-controller'),
    );

    click(themeButtons[0]); // Auto
    expect(settings.setTheme).toHaveBeenCalledWith('auto');

    click(themeButtons[1]); // Light
    expect(settings.setTheme).toHaveBeenCalledWith('light');

    click(themeButtons[2]); // Dark
    expect(settings.setTheme).toHaveBeenCalledWith('dark');
  });

  it('should link to remembered group path when not on groups page', () => {
    auth.isUserLoggedIn.and.returnValue(true);
    groupSettingsSignal.set({ rememberGroupPath: true, groupPath: '/groups/1/browser' });
    component.onGroupPage = false;
    fixture.detectChanges();

    const groupLink = fixture.debugElement.query(By.css('[data-test="header.groups-link"]')).nativeElement as HTMLAnchorElement;

    expect(groupLink.getAttribute('href')).toBe('/groups/1/browser');
  });

  it('should link to /groups when on groups page', () => {
    auth.isUserLoggedIn.and.returnValue(true);
    groupSettingsSignal.set({ rememberGroupPath: true, groupPath: '/groups/1/browser' });
    component.onGroupPage = true;
    fixture.detectChanges();

    const groupLink = fixture.debugElement.query(By.css('[data-test="header.groups-link"]')).nativeElement as HTMLAnchorElement;

    expect(groupLink.getAttribute('href')).toBe('/groups');
  });
});
