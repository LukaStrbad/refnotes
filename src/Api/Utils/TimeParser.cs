using System.Text.RegularExpressions;

namespace Api.Utils;

public static partial class TimeParser
{
    public static TimeSpan ParseTimeString(string? timeString)
    {
        // Treat empty or null strings as zero
        if (string.IsNullOrWhiteSpace(timeString))
        {
            return TimeSpan.Zero;
        }
        
        // Check if timeString is just a number, then treat it as seconds
        if (int.TryParse(timeString, out var seconds))
            return TimeSpan.FromSeconds(seconds);
        
        var totalTime = TimeSpan.Zero;
        // Use regular expression to find all occurrences of the pattern
        var matches = TimeRegex().Matches(timeString);
        
        // If no matches were found, throw an exception
        if (matches.Count == 0)
            throw new FormatException("Invalid time string.");

        foreach (Match match in matches)
        {
            if (!match.Success) continue;
            
            var value = int.Parse(match.Groups[1].Value);
            var unit = match.Groups[2].Value;

            totalTime += unit switch
            {
                "s" => TimeSpan.FromSeconds(value),
                "m" => TimeSpan.FromMinutes(value),
                "h" => TimeSpan.FromHours(value),
                "d" => TimeSpan.FromDays(value),
                _ => throw new FormatException("Invalid time unit.")
            };
        }

        return totalTime;
    }

    [GeneratedRegex(@"(\d+)([smhd])")]
    private static partial Regex TimeRegex();
}