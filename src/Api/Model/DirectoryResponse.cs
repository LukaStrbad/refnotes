namespace Api.Model;

public record DirectoryResponse(
    string Name,
    IEnumerable<FileResponse> Files,
    IEnumerable<string> Directories
);
