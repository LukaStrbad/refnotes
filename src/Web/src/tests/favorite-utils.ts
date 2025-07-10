import { DirectoryFavoriteDetails } from "../model/directory-favorite-details";
import { FileFavoriteDetails } from "../model/file-favorite-details";

export function createFileFavoriteDetails(name: string, groupId?: number): FileFavoriteDetails {
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
        groupId,
    };
};

export function createDirectoryFavoriteDetails(path: string, groupId?: number): DirectoryFavoriteDetails {
    return {
        path,
        favoriteDate: new Date(),
        groupId,
    };
}
