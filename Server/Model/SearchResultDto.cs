namespace Server.Model;

public record SearchResultDto(
    List<FileSearchResultDto> Files,
    List<string> Directories
);