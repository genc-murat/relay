using System.Linq;
using System.Threading.Tasks;
 using Relay.Core.Validation.Rules;
 using Xunit;

namespace Relay.Core.Tests.Validation;

public class DurationValidationRuleTests
{
    private readonly DurationValidationRule _rule = new();

    [Theory]
    [InlineData("P1Y")] // 1 year
    [InlineData("P1M")] // 1 month
    [InlineData("P1W")] // 1 week
    [InlineData("P1D")] // 1 day
    [InlineData("PT1H")] // 1 hour
    [InlineData("PT1M")] // 1 minute
    [InlineData("PT1S")] // 1 second
    [InlineData("P1Y2M3DT4H5M6S")] // Complex ISO 8601
    [InlineData("P1DT2H")] // Day and hour
    [InlineData("PT30M")] // 30 minutes
    [InlineData("P7D")] // 7 days
    public async Task ValidateAsync_ValidIso8601Durations_ReturnsEmptyErrors(string duration)
    {
        // Act
        var result = await _rule.ValidateAsync(duration);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("00:30:00")] // 30 minutes
    [InlineData("1:23:45")] // 1 hour 23 minutes 45 seconds
    [InlineData("12:00:00")] // 12 hours
    [InlineData("1.12:30:00")] // 1 day 12 hours 30 minutes
    [InlineData("0:05:30")] // 5 minutes 30 seconds
    [InlineData("23:59:59")] // Almost 1 day
    public async Task ValidateAsync_ValidTimeSpanFormats_ReturnsEmptyErrors(string duration)
    {
        // Act
        var result = await _rule.ValidateAsync(duration);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("1 day")]
    [InlineData("2 hours")]
    [InlineData("30 minutes")]
    [InlineData("45 seconds")]
    [InlineData("1 day 2 hours")]
    [InlineData("2 hours 30 minutes")]
    [InlineData("1 hour 30 minutes 45 seconds")]
    [InlineData("7 days 3 hours")]
    [InlineData("1day")] // No space
    [InlineData("2hours")] // No space
    [InlineData("30minutes")] // No space
    public async Task ValidateAsync_ValidSimpleTextFormats_ReturnsEmptyErrors(string duration)
    {
        // Act
        var result = await _rule.ValidateAsync(duration);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("1.00:00:00")] // 1 day
    [InlineData("2.12:00:00")] // 2 days 12 hours
    [InlineData("0.00:30:00")] // 30 minutes
    public async Task ValidateAsync_ValidDotNetTimeSpanFormats_ReturnsEmptyErrors(string duration)
    {
        // Act
        var result = await _rule.ValidateAsync(duration);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    [InlineData("   ")] // Whitespace
    public async Task ValidateAsync_EmptyOrWhitespace_ReturnsEmptyErrors(string duration)
    {
        // Act
        var result = await _rule.ValidateAsync(duration);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("P")] // Empty duration
    [InlineData("PT")] // Empty time part
    [InlineData("P1X")] // Invalid unit
    [InlineData("P1Y2X")] // Invalid unit in middle
    [InlineData("1Y2M")] // Missing P
    [InlineData("P1Y2M3D4H5M6S7X")] // Extra invalid part
    public async Task ValidateAsync_InvalidIso8601Formats_ReturnsError(string duration)
    {
        // Act
        var result = await _rule.ValidateAsync(duration);

        // Assert
        Assert.Single(result);
        Assert.Contains("Invalid duration format", result.First());
    }

    [Theory]
    [InlineData("12:60:00")] // Invalid minute
    [InlineData("12:30:60")] // Invalid second
    [InlineData("12:30")] // Missing seconds
    [InlineData("1:2:3")] // Single digits
    [InlineData("1.25:00:00")] // Invalid day format
    public async Task ValidateAsync_InvalidTimeSpanFormats_ReturnsError(string duration)
    {
        // Act
        var result = await _rule.ValidateAsync(duration);

        // Assert
        Assert.Single(result);
        Assert.Contains("Invalid duration format", result.First());
    }

    [Theory]
    [InlineData("1 dayz")] // Invalid unit
    [InlineData("2 hourz")] // Invalid unit
    [InlineData("30 minutez")] // Invalid unit
    [InlineData("1 day 2 invalid")] // Invalid unit
    [InlineData("day 1")] // Wrong order
    [InlineData("1 2 hours")] // Missing unit
    public async Task ValidateAsync_InvalidSimpleTextFormats_ReturnsError(string duration)
    {
        // Act
        var result = await _rule.ValidateAsync(duration);

        // Assert
        Assert.Single(result);
        Assert.Contains("Invalid duration format", result.First());
    }

    [Theory]
    [InlineData("not a duration")]
    [InlineData("random text")]
    [InlineData("123")]
    [InlineData("1:2")]
    [InlineData("P1")]
    [InlineData("T1H")]
    public async Task ValidateAsync_CompletelyInvalidFormats_ReturnsError(string duration)
    {
        // Act
        var result = await _rule.ValidateAsync(duration);

        // Assert
        Assert.Single(result);
        Assert.Equal("Invalid duration format. Supported formats: ISO 8601 (P1Y2M3DT4H5M6S), TimeSpan (HH:MM:SS), or simple text (1 day 2 hours 30 minutes)", result.First());
    }

    [Theory]
    [InlineData("P0D")] // Zero duration
    [InlineData("PT0S")] // Zero seconds
    [InlineData("P0Y0M0DT0H0M0S")] // All zero
    public async Task ValidateAsync_ZeroDurations_ReturnsEmptyErrors(string duration)
    {
        // Act
        var result = await _rule.ValidateAsync(duration);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("P365D")] // 365 days
    [InlineData("P52W")] // 52 weeks
    [InlineData("P12M")] // 12 months
    [InlineData("P10Y")] // 10 years
    public async Task ValidateAsync_LargeDurations_ReturnsEmptyErrors(string duration)
    {
        // Act
        var result = await _rule.ValidateAsync(duration);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("00:00:01")] // 1 second
    [InlineData("00:00:59")] // 59 seconds
    [InlineData("00:59:59")] // 59 minutes 59 seconds
    [InlineData("23:59:59")] // Max time
    public async Task ValidateAsync_BoundaryTimeSpanValues_ReturnsEmptyErrors(string duration)
    {
        // Act
        var result = await _rule.ValidateAsync(duration);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("1 day 2 hours 3 minutes 4 seconds")]
    [InlineData("7 days")]
    [InlineData("24 hours")]
    [InlineData("60 minutes")]
    [InlineData("3600 seconds")]
    public async Task ValidateAsync_ComplexSimpleFormats_ReturnsEmptyErrors(string duration)
    {
        // Act
        var result = await _rule.ValidateAsync(duration);

        // Assert
        Assert.Empty(result);
    }
}