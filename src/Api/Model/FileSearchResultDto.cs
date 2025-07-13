using System.Text.Json.Serialization;

namespace Api.Model;

public record FileSearchResultDto(
    string Path,
    List<string> Tags,
    [property: JsonIgnore] string FilesystemName,
    DateTime Modified,
    bool FoundByFullText = false
)
{
    internal FileSearchResultInternal ToInternal() => new(Path, Tags, FilesystemName, Modified, FoundByFullText);
}

internal record FileSearchResultInternal(
    string Path,
    List<string> Tags,
    string FilesystemName,
    DateTime Modified,
    bool FoundByFullText = false
)
{
    public FileSearchResultDto ToDto() => new(Path, Tags, FilesystemName, Modified, FoundByFullText);
}
