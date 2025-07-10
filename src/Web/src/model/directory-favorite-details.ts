import { GroupDetails } from "./user-group";

export interface DirectoryFavoriteDetails {
  path: string;
  group?: GroupDetails;
  favoriteDate: Date;
}
