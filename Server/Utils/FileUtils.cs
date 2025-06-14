using System.Text.RegularExpressions;

namespace Server.Utils;

public static partial class FileUtils
{
    public static bool IsTextFile(string name) => name.EndsWith(".txt", StringComparison.InvariantCultureIgnoreCase);

    public static bool IsMarkdownFile(string name) =>
        name.EndsWith(".md", StringComparison.InvariantCultureIgnoreCase) ||
        name.EndsWith(".markdown", StringComparison.InvariantCultureIgnoreCase);
    
    public static string NormalizePath(string path)
    {
        var regex = NormalizePathRegex();
        return regex.Replace(path, "/");
    }

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

    public static string GetContentType(string name)
    {
       if (IsTextFile(name))
           return "text/plain";
       
       if (IsMarkdownFile(name))
           return "text/markdown";
       
       var extension = Path.GetExtension(name);
       
       // Image types and other
       return extension switch
       {
           ".png" => "image/png",
           ".jpg" => "image/jpeg",
           ".jpeg" => "image/jpeg",
           ".gif" => "image/gif",
           ".svg" => "image/svg+xml",
           ".webp" => "image/webp",
           ".ico" => "image/x-icon",
           ".bmp" => "image/bmp",
           ".tiff" => "image/tiff",
           _ => "application/octet-stream" // Default
       };
    }

    /// <summary>
    /// Regex to replace multiple backslashes and forward slashes with a single slash.
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"[\\/]+")]
    private static partial Regex NormalizePathRegex();
}
