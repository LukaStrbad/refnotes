namespace Api.Model;

public record FileFavoriteDetails(FileDto FileInfo, int? GroupId, DateTime FavoriteDate);
