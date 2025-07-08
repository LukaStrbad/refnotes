import { Component, input } from '@angular/core';
import { FileFavoriteDetails } from '../../../model/file-favorite-details';

@Component({
  selector: 'app-favorite-file-item',
  imports: [],
  templateUrl: './favorite-file-item.component.html',
  styleUrl: './favorite-file-item.component.css'
})
export class FavoriteFileItemComponent {
  readonly favorite = input.required<FileFavoriteDetails>();
}
