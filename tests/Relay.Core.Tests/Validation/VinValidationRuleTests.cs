using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class VinValidationRuleTests
{
    private readonly VinValidationRule _rule = new();



    [Theory]
    [InlineData("1HGCM82633A12345")] // Too short
    [InlineData("1HGCM82633A1234567")] // Too long
    [InlineData("1HGCM82633A12345O")] // Contains invalid character 'O'
    [InlineData("1HGCM82633A12345I")] // Contains invalid character 'I'
    [InlineData("1HGCM82633A12345Q")] // Contains invalid character 'Q'
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    public async Task ValidateAsync_InvalidVin_ReturnsError(string vin)
    {
        // Act
        var result = await _rule.ValidateAsync(vin);

        // Assert
        if (string.IsNullOrWhiteSpace(vin))
        {
            result.Should().BeEmpty();
        }
        else
        {
            result.Should().ContainSingle().Which.Should().Match(s =>
                s.Contains("VIN must be exactly 17 characters long.") ||
                s.Contains("VIN contains invalid characters."));
        }
    }
}