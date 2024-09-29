namespace Server.Model;

public record ResponseDirectory(string Name, List<string> Files, List<string> Directories)
{
}