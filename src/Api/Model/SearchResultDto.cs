namespace Api.Model;

public record SearchResultDto(
    List<FileSearchResultDto> Files,
    List<string> Directories
);
