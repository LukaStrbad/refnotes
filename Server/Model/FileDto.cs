namespace Server.Model;

public record FileDto(
    string Path,
    IEnumerable<string> Tags,
    long Size,
    DateTime Created,
    DateTime Modified
);