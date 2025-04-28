import { Component, ElementRef, ViewChild, OnInit, OnDestroy } from '@angular/core';
import { SearchService } from '../../../services/search.service';
import { FileSearchResult } from '../../../model/file-search-result';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-search',
  imports: [FormsModule],
  templateUrl: './search.component.html',
  styleUrl: './search.component.css'
})
export class SearchComponent implements OnInit, OnDestroy {
  searchTerm: string = 'test';

  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;
  results: FileSearchResult[] | null = null;
  private inputTimeout: number | null = null;

  constructor(public searchService: SearchService) { }

  private keydownHandler = (event: KeyboardEvent) => {
    // Check if the key pressed is 'k' and if Ctrl or Meta (Command) key is also pressed
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      this.searchInput?.nativeElement.focus();
    }
  };

  ngOnInit() {
    window.addEventListener('keydown', this.keydownHandler);

    this.searchService.searchFiles({ searchTerm: this.searchTerm, page: 0, pageSize: 100 })
      .then((results) => {
        this.results = results;
      });
  }

  ngOnDestroy() {
    window.removeEventListener('keydown', this.keydownHandler);
  }

  async onSearchInput() {
    if (this.searchTerm === '') {
      this.results = null;
      return;
    }

    if (this.inputTimeout) {
      clearTimeout(this.inputTimeout);
    }

    // Set a timeout to delay the search request
    this.inputTimeout = setTimeout(async () => {
      this.results = await this.searchService.searchFiles({ searchTerm: this.searchTerm, page: 0, pageSize: 100 });
    }, 100);
  }
}
