using System.Text.RegularExpressions;

namespace Api.Utils;

public static partial class MarkdownUtils
{
    public static async IAsyncEnumerable<string> GetImagesAsync(Stream content)
    {
        var imageRegex = MdImageRegex();
        using var sr = new StreamReader(content);
        while (await sr.ReadLineAsync() is { } line)
        {
            var match = imageRegex.Match(line);
            if (!match.Success) continue;
            var filename = match.Groups["filename"].Value;
            yield return filename;
        }
    }

    [GeneratedRegex("""!\[[^\]]*\]\((?<filename>.*?)(?=\"|\))(?<optionalpart>\".*\")?\)""")]
    private static partial Regex MdImageRegex();
}
