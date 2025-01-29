namespace Server.Model;

public record DirectoryDto(string Name, IEnumerable<FileDto> Files, IEnumerable<string> Directories);