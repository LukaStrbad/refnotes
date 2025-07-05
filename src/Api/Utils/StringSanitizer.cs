namespace Api.Utils;

public static class StringSanitizer
{
    public static string SanitizeLog(string input)
    {
        return input.ReplaceLineEndings("");
    }
}
