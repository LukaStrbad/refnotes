import { Component, input } from '@angular/core';
import { DirectoryFavoriteDetails } from '../../../model/directory-favorite-details';

@Component({
  selector: 'app-favorite-directory-item',
  imports: [],
  templateUrl: './favorite-directory-item.component.html',
  styleUrl: './favorite-directory-item.component.css'
})
export class FavoriteDirectoryItemComponent {
  readonly favorite = input.required<DirectoryFavoriteDetails>();
}
