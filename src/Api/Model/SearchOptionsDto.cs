namespace Api.Model;

public record SearchOptionsDto(
    string SearchTerm,
    int Page,
    int PageSize,
    List<string>? Tags = null,
    bool IncludeFullText = false,
    string DirectoryPath = "/",
    List<string>? FileTypes = null,
    DateTime? ModifiedFrom = null,
    DateTime? ModifiedTo = null
);
