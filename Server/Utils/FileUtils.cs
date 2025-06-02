namespace Server.Utils;

public static class FileUtils
{
    public static bool IsTextFile(string name) => name.EndsWith(".txt", StringComparison.InvariantCultureIgnoreCase);

    public static bool IsMarkdownFile(string name) =>
        name.EndsWith(".md", StringComparison.InvariantCultureIgnoreCase) ||
        name.EndsWith(".markdown", StringComparison.InvariantCultureIgnoreCase);
    
    public static string NormalizePath(string path) => path.Replace("\\", "/");
    
    /// <summary>
    /// Gets directory path from the given path with slashes as separators
    /// </summary>
    /// <param name="path">File or directory path</param>
    /// <returns></returns>
    private static string GetDirectoryPath(string path)
    {
        var dirName = Path.GetDirectoryName(path);
        return dirName is null ? "/" : NormalizePath(dirName);
    }

    public static (string, string) SplitDirAndFile(string path)
    {
        var directoryName = GetDirectoryPath(path);
        var fileName = Path.GetFileName(path);
        return (directoryName, fileName);
    }
}
