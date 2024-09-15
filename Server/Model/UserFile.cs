using System.Text.Json.Serialization;

namespace Server.Model;

public record UserFile(
    [property: JsonIgnore] string Path
)
{
    [JsonPropertyName("name")]
    public string Name => System.IO.Path.GetFileName(Path);
}
