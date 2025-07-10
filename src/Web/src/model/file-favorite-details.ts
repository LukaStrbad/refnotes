import { File } from './file';
import { GroupDetails } from './user-group';


export interface FileFavoriteDetails {
  fileInfo: File;
  group?: GroupDetails;
  favoriteDate: Date | string;
}
