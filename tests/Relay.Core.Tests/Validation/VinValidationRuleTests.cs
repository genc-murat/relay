using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class VinValidationRuleTests
{
    private readonly VinValidationRule _rule = new();

    [Theory]
    [InlineData("1HGCM82633A123456")] // Valid VIN
    public async Task ValidateAsync_ValidVin_ReturnsEmptyErrors(string vin)
    {
        // Act
        var result = await _rule.ValidateAsync(vin);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("1HGCM82633A12345")] // Too short
    [InlineData("1HGCM82633A1234567")] // Too long
    [InlineData("1HGCM82633A12345O")] // Contains invalid character 'O'
    [InlineData("1HGCM82633A12345I")] // Contains invalid character 'I'
    [InlineData("1HGCM82633A12345Q")] // Contains invalid character 'Q'
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    [InlineData("   ")] // Whitespace
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

    [Theory]
    [InlineData("1HGCM82633A123457")] // Invalid check digit
    [InlineData("JH4KA8260MC000001")] // Invalid check digit
    [InlineData("1FTFW1ET4DFC12346")] // Invalid check digit
    public async Task ValidateAsync_InvalidCheckDigit_ReturnsError(string vin)
    {
        // Act
        var result = await _rule.ValidateAsync(vin);

        // Assert
        result.Should().ContainSingle("Invalid VIN check digit.");
    }

    [Fact]
    public async Task ValidateAsync_CancellationToken_ThrowsWhenCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var validVin = "1HGCM82633A123456";

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _rule.ValidateAsync(validVin, cts.Token));
    }

    [Theory]
    [InlineData("1hgcm82633a123456")] // Lowercase version of valid VIN
    public async Task ValidateAsync_CaseInsensitive_ReturnsEmptyErrors(string vin)
    {
        // Act
        var result = await _rule.ValidateAsync(vin);

        // Assert
        result.Should().BeEmpty();
    }



    [Fact]
    public async Task ValidateAsync_AllValidCharacters_ReturnsEmptyErrors()
    {
        // Arrange - Use a known valid VIN
        var vin = "1HGCM82633A123456";

        // Act
        var result = await _rule.ValidateAsync(vin);

        // Assert
        result.Should().BeEmpty();
    }
}