import { Component, computed, input, OnDestroy, OnInit, output } from '@angular/core';
import { FileFavoriteDetails } from '../../../model/file-favorite-details';
import { ByteSizePipe } from '../../../pipes/byte-size.pipe';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { isEditable } from '../../../utils/file-utils';
import { TranslateDirective, TranslatePipe, TranslateService } from '@ngx-translate/core';
import { convertDateLocale, getFormattedDate } from '../../../utils/date-utils';
import { FileIconComponent } from "../../components/file-icon/file-icon.component";
import { TestTagDirective } from '../../../directives/test-tag.directive';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-favorite-file-item',
  standalone: true,
  imports: [CommonModule, ByteSizePipe, RouterLink, TranslateDirective, TranslatePipe, FileIconComponent, TestTagDirective],
  templateUrl: './favorite-file-item.component.html',
  styleUrl: './favorite-file-item.component.css'
})
export class FavoriteFileItemComponent implements OnInit, OnDestroy {
  readonly favorite = input.required<FileFavoriteDetails>();
  readonly removeFavorite = output<FileFavoriteDetails>();

  readonly isEditable = computed(() => isEditable(this.favorite().fileInfo.path));

  favoriteDateFormatted = '';
  modifiedDateFormatted = '';

  langChangeSubscription?: Subscription;

  constructor(
    private translate: TranslateService
  ) { }

  ngOnInit(): void {
    let lang = convertDateLocale(this.translate.currentLang);
    this.langChangeSubscription = this.translate.onDefaultLangChange.subscribe(() => {
      lang = convertDateLocale(this.translate.currentLang);
    });

    getFormattedDate(this.translate, this.favorite().favoriteDate, lang)
      .then(date => this.favoriteDateFormatted = date)

    getFormattedDate(this.translate, this.favorite().fileInfo.modified, lang)
      .then(date => this.modifiedDateFormatted = date);
  }

  ngOnDestroy(): void {
    this.langChangeSubscription?.unsubscribe();
  }

  onRemoveFavorite(): void {
    this.removeFavorite.emit(this.favorite());
  }
}
