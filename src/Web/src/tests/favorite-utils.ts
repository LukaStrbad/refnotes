import { DirectoryFavoriteDetails } from "../model/directory-favorite-details";
import { FileFavoriteDetails } from "../model/file-favorite-details";
import { GroupDetails } from "../model/user-group";

export function createFileFavoriteDetails(name: string, group?: GroupDetails): FileFavoriteDetails {
    return {
        fileInfo: {
            name: name,
            path: `/${name}`,
            size: 1234,
            tags: [],
            modified: new Date(),
            created: new Date(),
        },
        favoriteDate: new Date(),
        group,
    };
};

export function createDirectoryFavoriteDetails(path: string, group?: GroupDetails): DirectoryFavoriteDetails {
    return {
        path,
        favoriteDate: new Date(),
        group,
    };
}
