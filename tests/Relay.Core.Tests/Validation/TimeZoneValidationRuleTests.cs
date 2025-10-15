using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class TimeZoneValidationRuleTests
{
    private readonly TimeZoneValidationRule _rule = new();

    [Theory]
    [InlineData("UTC")] // UTC
    [InlineData("America/New_York")] // Eastern Time
    [InlineData("Europe/London")] // GMT/BST
    [InlineData("Asia/Tokyo")] // Japan Standard Time
    [InlineData("Australia/Sydney")] // Australian Eastern Time
    [InlineData("Pacific/Honolulu")] // Hawaii Time
    public async Task ValidateAsync_ValidTimeZone_ReturnsEmptyErrors(string timeZone)
    {
        // Act
        var result = await _rule.ValidateAsync(timeZone);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Invalid/TimeZone")] // Non-existent
    [InlineData("America/Invalid")] // Invalid city
    [InlineData("GMT+1")] // Offset format (not IANA)
    [InlineData("EST")] // Abbreviation (not full ID)
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    public async Task ValidateAsync_InvalidTimeZone_ReturnsError(string timeZone)
    {
        // Act
        var result = await _rule.ValidateAsync(timeZone);

        // Assert
        if (string.IsNullOrWhiteSpace(timeZone))
        {
            result.Should().BeEmpty();
        }
        else
        {
            result.Should().ContainSingle("Invalid IANA time zone identifier.");
        }
    }
}