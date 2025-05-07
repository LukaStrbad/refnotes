import { Component, ElementRef, ViewChild, OnInit, OnDestroy, AfterViewInit, Input } from '@angular/core';
import { SearchService } from '../../../services/search.service';
import { FileSearchResult } from '../../../model/file-search-result';
import { FormsModule } from '@angular/forms';
import { SearchOptions } from '../../../model/search-options';
import { SearchResultItemComponent } from './search-result-item/search-result-item.component';
import { NgClass } from '@angular/common';
import { TagService } from '../../../services/tag.service';
import { firstValueFrom, lastValueFrom } from 'rxjs';
import { TranslateDirective, TranslatePipe, TranslateService } from '@ngx-translate/core';
import { SettingsService } from '../../../services/settings.service';
import { joinPaths, splitDirAndName } from '../../../utils/path-utils';
import Pikaday, { PikadayOptions } from 'pikaday';

@Component({
  selector: 'app-search',
  imports: [FormsModule, SearchResultItemComponent, NgClass, TranslateDirective, TranslatePipe],
  templateUrl: './search.component.html',
  styleUrl: './search.component.css'
})
export class SearchComponent implements OnInit, OnDestroy, AfterViewInit {
  @Input() isMobile = false;

  searchOptions: SearchOptions = {
    searchTerm: '',
    page: 0,
    pageSize: 100,
    includeFullText: this.settings.search().fullTextSearch
  };

  fullSize = false;
  onlySearchCurrentDirectory = this.settings.search().onlySearchCurrentDir;
  today: string;
  allTags: CheckedTag[] = [];

  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;
  @ViewChild('searchInputContainer') searchInputContainer!: ElementRef<HTMLElement>;
  @ViewChild('dateFromPicker') dateFromPicker!: ElementRef<HTMLInputElement>;
  @ViewChild('dateToPicker') dateToPicker!: ElementRef<HTMLInputElement>;

  results: FileSearchResult[] | null = null;
  private inputTimeout: number | null = null;

  private pickerFrom?: Pikaday;
  private pickerTo?: Pikaday;

  constructor(
    public searchService: SearchService,
    private tagService: TagService,
    public settings: SettingsService,
    private translate: TranslateService,
  ) {
    this.refreshTags().then();

    const date = new Date();
    date.setHours(23, 59, 59, 999);
    this.today = date.toISOString().replace(/\..*/, '');
  }

  private getCurrentDirectoryPath(): string | null {
    const url = window.location.pathname;
    // Match /file/:path/edit or /file/:path/preview
    const fileRouteMatch = url.match(/^\/file\/(.+)\/(edit|preview)/);
    // Match /browser/... or /browser
    const browserRouteMatch = url.match(/^\/browser(?:\/(.+))?/);

    let fullPath: string | null = null;

    if (fileRouteMatch) {
      fullPath = decodeURIComponent(fileRouteMatch[1]);
      const [dirPath] = splitDirAndName(fullPath);
      // If splitting results in an empty string (e.g., file at root), return '/'
      return dirPath || '/';
    } else if (browserRouteMatch) {
      // If group 1 exists (path after /browser/), return it decoded.
      // Otherwise (if it's just /browser), return '/' for the root.
      const dirPath = browserRouteMatch[1] ? decodeURIComponent(browserRouteMatch[1]) : '/';
      return joinPaths('/', dirPath);
    }

    return null;
  }


  async refreshTags() {
    const observable = this.tagService.listAllCached();

    // This will first read the cached value and then the network value
    this.allTags = (await firstValueFrom(observable)).map(tag => {
      return { name: tag, checked: false };
    });
    this.allTags = (await lastValueFrom(observable)).map(tag => {
      return { name: tag, checked: false };
    });
  }

  private keydownHandler = (event: KeyboardEvent) => {
    // Check if the key pressed is 'k' and if Ctrl or Meta (Command) key is also pressed
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      this.searchInput?.nativeElement.focus();
    }

    if (!(document.activeElement instanceof HTMLElement
      && this.searchInputContainer?.nativeElement.contains(document.activeElement))) {
      return;
    }

    if (event.key === 'Escape') {
      document.activeElement.blur();

      if (this.fullSize) {
        this.fullSize = false;
      }
    }

    if (event.key === 'Enter' && !this.fullSize) {
      this.fullSize = true;
    }

  };

  private popstateHandler = () => {
    if (!history.state?.fullSearchOpen && this.fullSize) {
      this.fullSize = false;
    }
  }

  ngOnInit() {
    window.addEventListener('keydown', this.keydownHandler);

    if (this.isMobile) {
      window.addEventListener('popstate', this.popstateHandler);
    }

    this.searchService.searchFiles(this.searchOptions)
      .then((results) => {
        this.results = results;
      });
  }

  ngAfterViewInit(): void {
    this.setPikadayOptions(this.translate.currentLang);

    this.translate.onLangChange.subscribe((event) => {
      this.setPikadayOptions(event.lang);
    })
  }

  setPikadayOptions(lang: string) {
    let dateFormat = 'DD/MM/YYYY';
    if (lang === 'hr') {
      dateFormat = 'DD.MM.YYYY';
    }

    const options: PikadayOptions = {
      firstDay: 1,
      maxDate: new Date(),
      format: dateFormat,
    }

    if (lang === 'hr') {
      options.i18n = {
        previousMonth: 'Prošli mjesec',
        nextMonth: 'Sljedeći mjesec',
        months: [
          'Siječanj', 'Veljača', 'Ožujak', 'Travanj', 'Svibanj', 'Lipanj',
          'Srpanj', 'Kolovoz', 'Rujan', 'Listopad', 'Studeni', 'Prosinac'
        ],
        weekdays: [
          'Nedjelja', 'Ponedjeljak', 'Utorak', 'Srijeda', 'Četvrtak', 'Petak', 'Subota'
        ],
        weekdaysShort: ['Ned.', 'Pon.', 'Uto.', 'Sri.', 'Čet.', 'Pet.', 'Sub.']
      }
    }

    this.pickerFrom?.destroy();
    this.pickerFrom = new Pikaday({
      ...options,
      field: this.dateFromPicker.nativeElement,
      onSelect: (date) => {
        this.searchOptions.modifiedFrom = date;
        this.onSearch();
      },
    });

    this.pickerTo?.destroy();
    this.pickerTo = new Pikaday({
      ...options,
      field: this.dateToPicker.nativeElement,
      onSelect: (date) => {
        this.searchOptions.modifiedTo = date;
        this.onSearch();
      },
    });
  }

  ngOnDestroy() {
    window.removeEventListener('keydown', this.keydownHandler);
    this.pickerFrom?.destroy();
    this.pickerTo?.destroy();
  }

  async onSearch() {
    const enabledTags = this.allTags.filter(tag => tag.checked).map(tag => tag.name);

    // If all search options are empty, set results to null and return
    if (this.searchOptions.searchTerm === ''
      && enabledTags.length === 0
      && (this.searchOptions.fileTypes?.length ?? 0) === 0
      && this.searchOptions.modifiedFrom === undefined
      && this.searchOptions.modifiedTo === undefined) {
      this.results = null;
      return;
    }

    this.searchOptions.directoryPath = this.onlySearchCurrentDirectory ? this.getCurrentDirectoryPath() ?? undefined : '/';
    if (enabledTags.length > 0) {
      this.searchOptions.tags = enabledTags;
    } else {
      this.searchOptions.tags = undefined;
    }

    if (this.inputTimeout) {
      clearTimeout(this.inputTimeout);
    }

    // Set a timeout to delay the search request
    this.inputTimeout = setTimeout(() => {
      this.searchService.searchFiles(this.searchOptions).then((results) => {
        this.results = results;
      });
    }, 100);
  }

  updateSearchSettings() {
    this.settings.setSearchSettings({
      fullTextSearch: this.searchOptions.includeFullText ?? false,
      onlySearchCurrentDir: this.onlySearchCurrentDirectory
    });
  }

  clearDateFrom() {
    this.searchOptions.modifiedFrom = undefined;
    this.pickerFrom?.clear();
    this.onSearch();
  }

  clearDateTo() {
    this.searchOptions.modifiedTo = undefined;
    this.pickerTo?.clear();
    this.onSearch();
  }

  expand() {
    this.fullSize = true;
    history.pushState({ fullSearchOpen: true }, '');
  }

  focus() {
    this.searchInput?.nativeElement.focus();
  }
}

interface CheckedTag {
  name: string;
  checked: boolean;
}
