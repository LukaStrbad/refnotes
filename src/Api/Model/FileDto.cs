namespace Api.Model;

public record FileDto(
    string Name,
    string Path,
    IEnumerable<string> Tags,
    long Size,
    DateTime Created,
    DateTime Modified
);
