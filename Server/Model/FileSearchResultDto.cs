using System.Text.Json.Serialization;

namespace Server.Model;

public record FileSearchResultDto(
    string Path,
    List<string> Tags,
    [property: JsonIgnore] string FilesystemName,
    DateTime Modified,
    bool FoundByFullText = false
);