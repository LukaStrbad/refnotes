import { Component, ElementRef, ViewChild, OnInit, OnDestroy } from '@angular/core';
import { SearchService } from '../../../services/search.service';
import { FileSearchResult } from '../../../model/file-search-result';
import { FormsModule } from '@angular/forms';
import { SearchOptions } from '../../../model/search-options';
import { SearchResultItemComponent } from "./search-result-item/search-result-item.component";
import { NgClass } from '@angular/common';
import { TagService } from '../../../services/tag.service';
import { firstValueFrom, lastValueFrom } from 'rxjs';
import { TranslateDirective } from '@ngx-translate/core';
import { ActivatedRoute } from '@angular/router';
import { SettingsService } from '../../../services/settings.service';

@Component({
  selector: 'app-search',
  imports: [FormsModule, SearchResultItemComponent, NgClass, TranslateDirective],
  templateUrl: './search.component.html',
  styleUrl: './search.component.css'
})
export class SearchComponent implements OnInit, OnDestroy {
  searchOptions: SearchOptions = {
    searchTerm: 'test',
    page: 0,
    pageSize: 100,
    includeFullText: this.settings.search().fullTextSearch,
  };

  fullSize = false;
  onlySearchCurrentDirectory = this.settings.search().onlySearchCurrentDir;
  today: string;
  allTags: CheckedTag[] = [];

  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;
  @ViewChild('searchInputContainer') searchInputContainer!: ElementRef<HTMLElement>;
  results: FileSearchResult[] | null = null;
  private inputTimeout: number | null = null;

  constructor(
    public searchService: SearchService,
    private tagService: TagService,
    public settings: SettingsService,
  ) {
    this.refreshTags().then();

    const date = new Date();
    date.setHours(23, 59, 59, 999);
    this.today = date.toISOString().replace(/\..*/, '');
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

    if (event.key === 'Escape' && document.activeElement instanceof HTMLElement
      && this.searchInputContainer?.nativeElement.contains(document.activeElement)) {
      document.activeElement.blur();
    }

  };

  ngOnInit() {
    window.addEventListener('keydown', this.keydownHandler);

    this.searchService.searchFiles({ searchTerm: this.searchOptions.searchTerm, page: 0, pageSize: 100 })
      .then((results) => {
        this.results = results;
      });
  }

  ngOnDestroy() {
    window.removeEventListener('keydown', this.keydownHandler);
  }

  async onSearchInput() {
    // If all search options are empty, set results to null and return
    if (this.searchOptions.searchTerm === '' && this.searchOptions.tags?.length === 0
      && this.searchOptions.fileTypes?.length === 0 && this.searchOptions.modifiedFrom === undefined
      && this.searchOptions.modifiedTo === undefined) {
      this.results = null;
      return;
    }

    // this.searchOptions.directoryPath = this.onlySearchCurrentDirectory ? this.

    if (this.inputTimeout) {
      clearTimeout(this.inputTimeout);
    }

    // Set a timeout to delay the search request
    this.inputTimeout = setTimeout(async () => {
      this.results = await this.searchService.searchFiles(this.searchOptions);
    }, 100);
  }

  updateSearchSettings()
  {
    console.log('updateSearchSettings', this.searchOptions.includeFullText, this.onlySearchCurrentDirectory);
    this.settings.setSearchSettings({
      fullTextSearch: this.searchOptions.includeFullText ?? false,
      onlySearchCurrentDir: this.onlySearchCurrentDirectory
    });
  }
}

interface CheckedTag {
  name: string;
  checked: boolean;
}
