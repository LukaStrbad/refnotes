using System.Text.Json.Serialization;

namespace Api.Model;

public record DirectoryFavoriteDetails(
    string Path,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    GroupDetails? Group,
    DateTime FavoriteDate
);
