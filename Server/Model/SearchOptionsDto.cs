namespace Server.Model;

public record SearchOptionsDto(
    string SearchTerm,
    int Page,
    int PageSize,
    bool IncludeFullText = false
);