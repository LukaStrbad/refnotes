namespace Server.Model;

public record FileDto(string Name, IEnumerable<string> Tags);