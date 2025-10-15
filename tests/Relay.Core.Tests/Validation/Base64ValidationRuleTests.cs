using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class Base64ValidationRuleTests
{
    private readonly Base64ValidationRule _rule = new();

    [Theory]
    [InlineData("SGVsbG8gV29ybGQ=")] // "Hello World" in Base64
    [InlineData("dGVzdA==")] // "test" in Base64
    [InlineData("YQ==")] // "a" in Base64
    [InlineData("")] // Empty string
    public async Task ValidateAsync_ValidBase64_ReturnsEmptyErrors(string base64)
    {
        // Act
        var result = await _rule.ValidateAsync(base64);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("SGVsbG8gV29ybGQ")] // Missing padding
    [InlineData("SGVsbG8gV29ybGQ==")] // Invalid padding
    [InlineData("Invalid Base64!")] // Invalid characters
    [InlineData("SGVsbG8gV29ybGQ===")] // Too much padding
    [InlineData(null)] // Null
    public async Task ValidateAsync_InvalidBase64_ReturnsError(string base64)
    {
        // Act
        var result = await _rule.ValidateAsync(base64);

        // Assert
        if (string.IsNullOrWhiteSpace(base64))
        {
            result.Should().BeEmpty();
        }
        else
        {
            result.Should().ContainSingle().Which.Should().Match(s =>
                s.Contains("Invalid Base64 format.") ||
                s.Contains("Invalid Base64 data."));
        }
    }
}