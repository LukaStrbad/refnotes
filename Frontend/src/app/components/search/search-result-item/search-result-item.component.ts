import { Component, Input, OnInit } from '@angular/core';
import { FileSearchResult } from '../../../../model/file-search-result';
import { SearchOptions } from '../../../../model/search-options';
import { splitDirAndName } from '../../../../utils/path-utils';
import { isEditable, isViewable } from '../../../../utils/file-utils';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-search-result-item',
  imports: [RouterLink],
  templateUrl: './search-result-item.component.html',
  styleUrl: './search-result-item.component.css'
})
export class SearchResultItemComponent implements OnInit {
  @Input({ required: true }) item!: FileSearchResult;
  @Input({ required: true }) searchOptions!: SearchOptions;

  filename = '';
  dirPath = '';
  isEditable = false;
  isViewable = false;

  ngOnInit(): void {
    [this.dirPath, this.filename] = splitDirAndName(this.item.path);
    this.isEditable = isEditable(this.filename);
    this.isViewable = isViewable(this.filename);
  }

  calculateTagIntersection() {
    const searchTags = this.searchOptions.tags || [];
    const resultTags = this.item.tags || [];
    return resultTags.filter(tag => searchTags.includes(tag));
  }
}
