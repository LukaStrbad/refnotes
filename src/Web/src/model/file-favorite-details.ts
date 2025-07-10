import { File } from './file';


export interface FileFavoriteDetails {
  fileInfo: File;
  groupId?: number;
  favoriteDate: Date | string;
}
