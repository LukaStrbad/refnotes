namespace Server.Model;

public record FileSearchResultDto(string Path, List<string> Tags, bool FoundByFullText = false);