namespace Api.Model;

public record FileResponse(
    string Name,
    string Path,
    IEnumerable<string> Tags,
    long Size,
    DateTime Created,
    DateTime Modified
);
