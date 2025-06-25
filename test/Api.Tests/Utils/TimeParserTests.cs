namespace Api.Tests.Utils;

using Api.Utils;

public class TimeParserTests
{
    [Theory]
    [InlineData("5", 5)]
    [InlineData("10", 10)]
    [InlineData("60", 60)]
    public void ParseTimeString_WithInteger_ReturnsCorrectTimeSpan(string input, int expectedSeconds)
    {
        // Act
        var result = TimeParser.ParseTimeString(input);
        
        // Assert
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), result);
    }
    
    [Theory]
    [InlineData("5s", 5)]
    [InlineData("10s", 10)]
    [InlineData("60s", 60)]
    public void ParseTimeString_WithSeconds_ReturnsCorrectTimeSpan(string input, int expectedSeconds)
    {
        // Act
        var result = TimeParser.ParseTimeString(input);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), result);
    }

    [Theory]
    [InlineData("5m", 5)]
    [InlineData("10m", 10)]
    [InlineData("60m", 60)]
    public void ParseTimeString_WithMinutes_ReturnsCorrectTimeSpan(string input, int expectedMinutes)
    {
        // Act
        var result = TimeParser.ParseTimeString(input);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), result);
    }

    [Theory]
    [InlineData("1h", 1)]
    [InlineData("2h", 2)]
    [InlineData("24h", 24)]
    public void ParseTimeString_WithHours_ReturnsCorrectTimeSpan(string input, int expectedHours)
    {
        // Act
        var result = TimeParser.ParseTimeString(input);

        // Assert
        Assert.Equal(TimeSpan.FromHours(expectedHours), result);
    }

    [Theory]
    [InlineData("1d", 1)]
    [InlineData("7d", 7)]
    [InlineData("30d", 30)]
    public void ParseTimeString_WithDays_ReturnsCorrectTimeSpan(string input, int expectedDays)
    {
        // Act
        var result = TimeParser.ParseTimeString(input);

        // Assert
        Assert.Equal(TimeSpan.FromDays(expectedDays), result);
    }

    [Theory]
    [InlineData("1h30m", 1, 30, 0)]
    [InlineData("2h15m30s", 2, 15, 30)]
    [InlineData("1d6h", 30, 0, 0)] // 1 day = 24 hours + 6 hours = 30 hours
    public void ParseTimeString_WithMultipleUnits_ReturnsCorrectTimeSpan(string input, int expectedHours, int expectedMinutes, int expectedSeconds)
    {
        // Act
        var result = TimeParser.ParseTimeString(input);

        // Assert
        var expected = TimeSpan.FromHours(expectedHours) + 
                      TimeSpan.FromMinutes(expectedMinutes) + 
                      TimeSpan.FromSeconds(expectedSeconds);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1h 30m", 1, 30, 0)] // Space between units
    [InlineData("2h, 15m, 30s", 2, 15, 30)] // Commas between units
    [InlineData("1d and 6h", 30, 0, 0)] // Text between units
    public void ParseTimeString_WithSeparators_ReturnsCorrectTimeSpan(string input, int expectedHours, int expectedMinutes, int expectedSeconds)
    {
        // Act
        var result = TimeParser.ParseTimeString(input);

        // Assert
        var expected = TimeSpan.FromHours(expectedHours) + 
                      TimeSpan.FromMinutes(expectedMinutes) + 
                      TimeSpan.FromSeconds(expectedSeconds);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    public void ParseTimeString_WithNoTimeUnits_ReturnsZeroTimeSpan(string input)
    {
        // Act
        var result = TimeParser.ParseTimeString(input);

        // Assert
        Assert.Equal(TimeSpan.Zero, result);
    }

    [Theory]
    [InlineData("5x")]
    [InlineData("10y")]
    [InlineData("60z")]
    public void ParseTimeString_WithInvalidUnits_ThrowsFormatException(string input)
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => TimeParser.ParseTimeString(input));
    }
}