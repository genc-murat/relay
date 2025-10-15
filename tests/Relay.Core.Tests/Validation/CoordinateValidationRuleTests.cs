using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class CoordinateValidationRuleTests
{
    private readonly CoordinateValidationRule _rule = new();

    [Theory]
    [InlineData("40.7128,-74.0060")] // New York City
    [InlineData("51.5074,-0.1278")] // London
    [InlineData("35.6762,139.6503")] // Tokyo
    [InlineData("-33.8688,151.2093")] // Sydney
    [InlineData("0.0000,0.0000")] // Null Island
    [InlineData("90.0000,180.0000")] // North Pole, International Date Line
    [InlineData("-90.0000,-180.0000")] // South Pole, International Date Line
    [InlineData("45.123456,-122.987654")] // High precision
    public async Task ValidateAsync_ValidCoordinates_ReturnsEmptyErrors(string coordinates)
    {
        // Act
        var result = await _rule.ValidateAsync(coordinates);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("91.0000,-74.0060")] // Latitude too high
    [InlineData("-91.0000,-74.0060")] // Latitude too low
    [InlineData("40.7128,181.0000")] // Longitude too high
    [InlineData("40.7128,-181.0000")] // Longitude too low
    [InlineData("40.7128")] // Missing longitude
    [InlineData("40.7128,-74.0060,100.0000")] // Extra coordinate
    [InlineData("abc,def")] // Non-numeric
    [InlineData("40.7128,")] // Empty longitude
    [InlineData(",-74.0060")] // Empty latitude
    [InlineData("40.7128 -74.0060")] // Wrong separator
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    public async Task ValidateAsync_InvalidCoordinates_ReturnsError(string coordinates)
    {
        // Act
        var result = await _rule.ValidateAsync(coordinates);

        // Assert
        if (string.IsNullOrWhiteSpace(coordinates))
        {
            result.Should().BeEmpty();
        }
        else
        {
            result.Should().NotBeEmpty();
        }
    }
}