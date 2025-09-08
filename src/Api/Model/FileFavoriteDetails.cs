using System.Text.Json.Serialization;

namespace Api.Model;

public record FileFavoriteDetails(
    FileResponse FileInfo,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    GroupDetails? Group,
    DateTime FavoriteDate
);
