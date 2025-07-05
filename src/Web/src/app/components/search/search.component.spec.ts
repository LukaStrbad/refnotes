import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { SearchComponent } from './search.component';
import { SearchService } from '../../../services/search.service';
import { TagService } from '../../../services/tag.service';
import { SettingsService } from '../../../services/settings.service';
import { TranslateFakeLoader, TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { signal } from '@angular/core';

describe('SearchComponent', () => {
  let component: SearchComponent;
  let fixture: ComponentFixture<SearchComponent>;
  let searchService: jasmine.SpyObj<SearchService>;
  let tagService: jasmine.SpyObj<TagService>;
  let settingsService: jasmine.SpyObj<SettingsService>;

  beforeEach(async () => {
    searchService = jasmine.createSpyObj('SearchService', ['searchFiles']);
    tagService = jasmine.createSpyObj('TagService', ['listAllCached']);
    settingsService = jasmine.createSpyObj('SettingsService', ['setSearchSettings'], {
      search: signal({ fullTextSearch: false, onlySearchCurrentDir: false })
    });

    searchService.searchFiles.and.resolveTo([]);
    tagService.listAllCached.and.returnValue(of(['tag1', 'tag2']));

    await TestBed.configureTestingModule({
      imports: [
        SearchComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        })
      ],
      providers: [
        TranslateService,
        { provide: SearchService, useValue: searchService },
        { provide: TagService, useValue: tagService },
        { provide: SettingsService, useValue: settingsService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SearchComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with default search options', () => {
    expect(component.searchOptions).toEqual({
      searchTerm: '',
      page: 0,
      pageSize: 100,
      includeFullText: false
    });
    expect(component.fullSize).toBeFalse();
    expect(component.onlySearchCurrentDirectory).toBeFalse();
  });

  it('should load tags on init', async () => {
    await fixture.whenStable();
    expect(component.allTags).toEqual([
      { name: 'tag1', checked: false },
      { name: 'tag2', checked: false }
    ]);
  });

  it('should perform search when search term changes', fakeAsync(() => {
    searchService.searchFiles.calls.reset(); // Reset the call count

    component.searchOptions.searchTerm = 'test';
    component.onSearch();
    tick(100); // Wait for debounce

    expect(searchService.searchFiles).toHaveBeenCalledWith({
      searchTerm: 'test',
      page: 0,
      pageSize: 100,
      includeFullText: false,
      directoryPath: '/',
      tags: undefined
    });
  }));

  it('should not search if all search options are empty', fakeAsync(() => {
    searchService.searchFiles.calls.reset(); // Reset the call count

    component.searchOptions.searchTerm = '';
    component.onSearch();
    tick(100);

    expect(component.results).toBeNull();
    expect(searchService.searchFiles).not.toHaveBeenCalled();
  }));

  it('should update search settings when include fulltext changes', () => {
    component.searchOptions.includeFullText = true;
    component.updateSearchSettings();

    expect(settingsService.setSearchSettings).toHaveBeenCalledWith({
      fullTextSearch: true,
      onlySearchCurrentDir: false
    });
  });

  it('should clear date filters', async () => {
    const testDate = new Date();
    component.searchOptions.modifiedFrom = testDate;
    component.searchOptions.modifiedTo = testDate;

    component.clearDateFrom();
    expect(component.searchOptions.modifiedFrom).toBeUndefined();

    component.clearDateTo();
    expect(component.searchOptions.modifiedTo).toBeUndefined();
  });

  it('should expand search on mobile', () => {
    component.isMobile = true;
    component.expand();

    expect(component.fullSize).toBeTrue();
    expect(history.state.fullSearchOpen).toBeTrue();
  });

  it('should filter by tags when tags are selected', fakeAsync(() => {
    searchService.searchFiles.calls.reset(); // Reset the call count

    component.allTags[0].checked = true; // Select first tag
    component.onSearch();
    tick(100);

    expect(searchService.searchFiles).toHaveBeenCalledWith({
      searchTerm: '',
      page: 0,
      pageSize: 100,
      includeFullText: false,
      directoryPath: '/',
      tags: ['tag1']
    });
  }));

  describe('keyboard shortcuts', () => {
    it('should focus search on Ctrl+K', () => {
      const event = new KeyboardEvent('keydown', {
        key: 'k',
        ctrlKey: true
      });
      spyOn(event, 'preventDefault');
      spyOn(component.searchInput.nativeElement, 'focus');

      window.dispatchEvent(event);

      expect(event.preventDefault).toHaveBeenCalled();
      expect(component.searchInput.nativeElement.focus).toHaveBeenCalled();
    });

    it('should close expanded search on Escape', () => {
      component.fullSize = true;

      component.searchInput.nativeElement.focus();
      window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
      fixture.detectChanges();

      expect(component.fullSize).toBeFalse();
    });
  });

  describe('path detection', () => {
    beforeEach(() => {
      // Reset location for each test
      window.history.pushState({}, '', '/');
    });

    it('should detect root path in browser route', fakeAsync(() => {
      searchService.searchFiles.calls.reset(); // Reset the call count

      window.history.pushState({}, '', '/browser');
      component.searchOptions.searchTerm = 'test';
      component.onSearch();
      tick(100); // Wait for debounce

      expect(searchService.searchFiles).toHaveBeenCalledWith(
        jasmine.objectContaining({ directoryPath: '/' })
      );
    }));

    it('should detect nested path in browser route', fakeAsync(() => {
      searchService.searchFiles.calls.reset(); // Reset the call count

      window.history.pushState({}, '', '/browser/docs/files');
      component.onlySearchCurrentDirectory = true;
      component.searchOptions.searchTerm = 'test'; // Add search term to trigger search
      component.onSearch();
      tick(100); // Wait for debounce

      expect(searchService.searchFiles).toHaveBeenCalledWith(
      jasmine.objectContaining({ directoryPath: '/docs/files' })
      );
    }));

    it('should detect directory path from file preview route', fakeAsync(() => {
      searchService.searchFiles.calls.reset(); // Reset the call count

      window.history.pushState({}, '', '/file/docs/files/test.md/preview');
      component.onlySearchCurrentDirectory = true;
      component.searchOptions.searchTerm = 'test'; // Add search term to trigger search
      component.onSearch();
      tick(100); // Wait for debounce

      expect(searchService.searchFiles).toHaveBeenCalledWith(
      jasmine.objectContaining({ directoryPath: '/docs/files' })
      );
    }));
  });
});
