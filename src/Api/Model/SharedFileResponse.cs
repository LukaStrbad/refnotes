namespace Api.Model;

public record SharedFileResponse(
    int SharedFileId,
    string Name,
    string Path,
    IEnumerable<string> Tags,
    long Size,
    DateTime Created,
    DateTime Modified
);
