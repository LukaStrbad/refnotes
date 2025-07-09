using System.Text.Json.Serialization;

namespace Api.Model;

public record FileFavoriteDetails(
    FileDto FileInfo,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? GroupId,
    DateTime FavoriteDate
);
