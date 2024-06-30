namespace Server.Model;

public record UserDirectory(string Path, List<UserFile> Files, List<string> Directories);
