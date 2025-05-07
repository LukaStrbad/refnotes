import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SearchResultItemComponent } from './search-result-item.component';
import { ActivatedRoute } from '@angular/router';
import { FileSearchResult } from '../../../../model/file-search-result';
import { SearchOptions } from '../../../../model/search-options';

describe('SearchResultItemComponent', () => {
  let component: SearchResultItemComponent;
  let fixture: ComponentFixture<SearchResultItemComponent>;
  let nativeElement: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SearchResultItemComponent],
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => null } } } }
      ]
    })
      .compileComponents();

    fixture = TestBed.createComponent(SearchResultItemComponent);
    component = fixture.componentInstance;

    nativeElement = fixture.nativeElement as HTMLElement;
  });

  function setInputs(item: FileSearchResult, searchOptions: SearchOptions) {
    component.item = item;
    component.searchOptions = searchOptions;
    component.ngOnInit();
    fixture.detectChanges();
  }

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render filename and directory', () => {
    setInputs({ path: '/dir/file.txt', tags: [], modified: new Date(), foundByFullText: false }, { searchTerm: '', page: 0, pageSize: 10 });
    const filename = nativeElement.querySelector('[data-test="search.result.filename"]');
    const dir = nativeElement.querySelector('[data-test="search.result.dir"]');
    expect(filename?.textContent).toContain('file.txt');
    expect(dir?.textContent).toContain('/dir');
  });

  it('should render tag intersection', () => {
    setInputs({ path: '/dir/file.txt', tags: ['tag1', 'tag2'], modified: new Date(), foundByFullText: false }, { searchTerm: '', page: 0, pageSize: 10, tags: ['tag2', 'tag3'] });
    const tagBadges = nativeElement.querySelectorAll('[data-test="search.result.tag"]');
    expect(Array.from(tagBadges).some(badge => badge.textContent === 'tag2')).toBeTrue();
  });

  it('should show "text" badge if foundByFullText is true', () => {
    setInputs({ path: '/dir/file.txt', tags: [], modified: new Date(), foundByFullText: true }, { searchTerm: '', page: 0, pageSize: 10 });
    const textBadge = nativeElement.querySelector('[data-test="search.result.text-badge"]');
    expect(textBadge).toBeTruthy();
    expect(textBadge?.textContent).toContain('text');
  });

  it('should always show folder button', () => {
    setInputs({ path: '/dir/file.txt', tags: [], modified: new Date(), foundByFullText: false }, { searchTerm: '', page: 0, pageSize: 10 });
    const folderBtn = nativeElement.querySelector('[data-test="search.result.open-folder"]');
    expect(folderBtn).toBeTruthy();
    expect(folderBtn?.getAttribute('href')).toContain('/browser');
  });
});
