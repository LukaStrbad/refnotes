namespace Server.Model;

public class ResponseDirectory
{
    public required string Name { get; init; }
    public required List<string> Files { get; init; }
    public required List<string> Directories { get; init; }
}