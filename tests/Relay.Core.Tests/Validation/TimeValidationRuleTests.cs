using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class TimeValidationRuleTests
{
    private readonly TimeValidationRule _rule = new();

    [Theory]
    [InlineData("00:00")]
    [InlineData("12:30")]
    [InlineData("23:59")]
    [InlineData("09:15")]
    [InlineData("1:23")]
    [InlineData("2:34")]
    public async Task ValidateAsync_Valid24HourTimes_ReturnsEmptyErrors(string time)
    {
        // Act
        var result = await _rule.ValidateAsync(time);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("00:00:00")]
    [InlineData("12:30:45")]
    [InlineData("23:59:59")]
    [InlineData("09:15:30")]
    [InlineData("1:23:45")]
    [InlineData("2:34:56")]
    public async Task ValidateAsync_Valid24HourTimesWithSeconds_ReturnsEmptyErrors(string time)
    {
        // Act
        var result = await _rule.ValidateAsync(time);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("12:00 AM")]
    [InlineData("1:30 PM")]
    [InlineData("11:59 AM")]
    [InlineData("12:00 PM")]
    [InlineData("3:45 pm")]
    [InlineData("9:15 am")]
    public async Task ValidateAsync_Valid12HourTimes_ReturnsEmptyErrors(string time)
    {
        // Act
        var result = await _rule.ValidateAsync(time);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("12:00:00 AM")]
    [InlineData("1:30:45 PM")]
    [InlineData("11:59:59 AM")]
    [InlineData("12:00:00 PM")]
    [InlineData("3:45:30 pm")]
    [InlineData("9:15:20 am")]
    public async Task ValidateAsync_Valid12HourTimesWithSeconds_ReturnsEmptyErrors(string time)
    {
        // Act
        var result = await _rule.ValidateAsync(time);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("25:00")] // Invalid hour
    [InlineData("12:60")] // Invalid minute
    [InlineData("12:30:60")] // Invalid second
    [InlineData("24:00")] // 24:00 is not valid
    [InlineData("13:99")] // Invalid minute
    public async Task ValidateAsync_InvalidTimeFormats_ReturnsError(string time)
    {
        // Act
        var result = await _rule.ValidateAsync(time);

        // Assert
        result.Should().ContainSingle().Which.Should().Contain("Invalid time format");
    }

    [Theory]
    [InlineData("13:00 AM")] // 13 AM is invalid
    [InlineData("0:30 AM")] // 0 AM is invalid
    [InlineData("13:00 PM")] // 13 PM is invalid
    [InlineData("12:60 AM")] // Invalid minute
    [InlineData("1:30:60 PM")] // Invalid second
    public async Task ValidateAsync_Invalid12HourTimes_ReturnsError(string time)
    {
        // Act
        var result = await _rule.ValidateAsync(time);

        // Assert
        result.Should().ContainSingle().Which.Should().Contain("Invalid");
    }

    [Theory]
    [InlineData("12:00")] // 12:00 without AM/PM
    [InlineData("1:30")] // 1:30 without AM/PM
    public async Task ValidateAsync_AmbiguousTimesWithoutAmPm_ReturnsEmptyErrors(string time)
    {
        // Act
        var result = await _rule.ValidateAsync(time);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    [InlineData("   ")] // Whitespace
    public async Task ValidateAsync_EmptyOrWhitespace_ReturnsEmptyErrors(string time)
    {
        // Act
        var result = await _rule.ValidateAsync(time);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("12:00 AM ")] // Trailing space
    [InlineData(" 12:00 AM")] // Leading space
    [InlineData("12:00AM")] // No space
    [InlineData("12:00am")] // Lowercase
    [InlineData("12:00PM")] // Uppercase
    public async Task ValidateAsync_TimeWithVariousSpacing_ReturnsEmptyErrors(string time)
    {
        // Act
        var result = await _rule.ValidateAsync(time);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("24:00")] // 24:00 not allowed
    [InlineData("25:30")] // Invalid hour
    [InlineData("12:99")] // Invalid minute
    [InlineData("12:30:99")] // Invalid second
    public async Task ValidateAsync_Invalid24HourTimes_ReturnsError(string time)
    {
        // Act
        var result = await _rule.ValidateAsync(time);

        // Assert
        result.Should().ContainSingle().Which.Should().Contain("Invalid");
    }

    [Theory]
    [InlineData("00:00")] // Midnight
    [InlineData("23:59")] // Almost midnight
    [InlineData("12:00")] // Noon
    [InlineData("11:59")] // Almost noon
    public async Task ValidateAsync_BoundaryTimes_ReturnsEmptyErrors(string time)
    {
        // Act
        var result = await _rule.ValidateAsync(time);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("12:00 AM")] // Midnight 12-hour
    [InlineData("11:59 PM")] // Almost midnight 12-hour
    [InlineData("12:00 PM")] // Noon 12-hour
    [InlineData("11:59 AM")] // Almost noon 12-hour
    public async Task ValidateAsync_Boundary12HourTimes_ReturnsEmptyErrors(string time)
    {
        // Act
        var result = await _rule.ValidateAsync(time);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("not a time")]
    [InlineData("12")]
    [InlineData("12:")]
    [InlineData(":30")]
    [InlineData("12:30:45:67")]
    [InlineData("12-30")]
    [InlineData("12.30")]
    public async Task ValidateAsync_CompletelyInvalidFormats_ReturnsError(string time)
    {
        // Act
        var result = await _rule.ValidateAsync(time);

        // Assert
        result.Should().ContainSingle("Invalid time format. Expected formats: HH:MM, HH:MM:SS, HH:MM AM/PM, or HH:MM:SS AM/PM");
    }

    [Theory]
    [InlineData("12:30 AMZ")] // Invalid AM/PM
    [InlineData("12:30 XM")] // Invalid AM/PM
    [InlineData("12:30 A")] // Incomplete AM/PM
    public async Task ValidateAsync_InvalidAmPmFormats_ReturnsError(string time)
    {
        // Act
        var result = await _rule.ValidateAsync(time);

        // Assert
        result.Should().ContainSingle().Which.Should().Contain("Invalid");
    }
}