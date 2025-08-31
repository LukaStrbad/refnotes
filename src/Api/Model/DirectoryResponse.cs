using Data.Model;

namespace Api.Model;

public record DirectoryResponse(
    string Name,
    IEnumerable<FileResponse> Files,
    IEnumerable<SharedFileResponse> SharedFiles,
    IEnumerable<string> Directories
);
