namespace Server.Utils;

public static class FileUtils
{
    public static bool IsTextFile(string name) => name.EndsWith(".txt", StringComparison.InvariantCultureIgnoreCase);

    public static bool IsMarkdownFile(string name) =>
        name.EndsWith(".md", StringComparison.InvariantCultureIgnoreCase) ||
        name.EndsWith(".markdown", StringComparison.InvariantCultureIgnoreCase);
}
