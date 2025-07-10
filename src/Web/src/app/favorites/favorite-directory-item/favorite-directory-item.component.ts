import { Component, computed, input, OnDestroy, OnInit, output } from '@angular/core';
import { DirectoryFavoriteDetails } from '../../../model/directory-favorite-details';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { convertDateLocale, getFormattedDate } from '../../../utils/date-utils';
import { TranslateDirective, TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { joinPaths, splitDirAndName } from '../../../utils/path-utils';
import { TestTagDirective } from '../../../directives/test-tag.directive';

@Component({
  selector: 'app-favorite-directory-item',
  imports: [CommonModule, RouterLink, TranslateDirective, TranslatePipe, TestTagDirective],
  templateUrl: './favorite-directory-item.component.html',
  styleUrl: './favorite-directory-item.component.css'
})
export class FavoriteDirectoryItemComponent implements OnInit, OnDestroy {
  readonly favorite = input.required<DirectoryFavoriteDetails>();
  readonly removeFavorite = output<DirectoryFavoriteDetails>();

  readonly directoryName = computed(() => {
    const [, name] = splitDirAndName(this.favorite().path);
    return name;
  });

  readonly routerLink = computed(this.getRouterLink.bind(this));

  favoriteDateFormatted = '';

  private langChangeSubscription?: Subscription;

  constructor(
    private translate: TranslateService,
  ) { }

  ngOnInit(): void {
    let lang = convertDateLocale(this.translate.currentLang);
    this.langChangeSubscription = this.translate.onDefaultLangChange.subscribe(() => {
      lang = convertDateLocale(this.translate.currentLang);
    });

    getFormattedDate(this.translate, this.favorite().favoriteDate, lang)
      .then(date => this.favoriteDateFormatted = date);
  }

  ngOnDestroy(): void {
    this.langChangeSubscription?.unsubscribe();
  }

  onRemoveFavorite(): void {
    this.removeFavorite.emit(this.favorite());
  }

  private getRouterLink(): string {
    const favorite = this.favorite();
    const basePath = favorite.group ? `/groups/${favorite.group.id}/browser` : '/browser';
    return joinPaths(basePath, favorite.path);
  }
}
