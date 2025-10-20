using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class ColorValidationRuleHexTests
{
    private readonly ColorValidationRule _rule = new();

    [Theory]
    [InlineData("#000")] // 3-digit hex
    [InlineData("#FFF")] // 3-digit hex uppercase
    [InlineData("#123")] // 3-digit hex
    [InlineData("#000000")] // 6-digit hex
    [InlineData("#FFFFFF")] // 6-digit hex
    [InlineData("#123456")] // 6-digit hex
    [InlineData("#00000000")] // 8-digit hex with alpha
    [InlineData("#FFFFFFFF")] // 8-digit hex with alpha
    [InlineData("#12345678")] // 8-digit hex
    public async Task ValidateAsync_ValidHexColors_ReturnsEmptyErrors(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("#12")] // Too short
    [InlineData("#1234")] // 4 digits
    [InlineData("#12345")] // 5 digits
    [InlineData("#1234567")] // 7 digits
    [InlineData("#GGGGGG")] // Invalid characters
    [InlineData("#12G")] // Invalid character
    public async Task ValidateAsync_InvalidHexColors_ReturnsError(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        Assert.Single(result);
        Assert.Equal("Invalid color format. Supported formats: #RGB, #RRGGBB, #RRGGBBAA, rgb(r,g,b), rgba(r,g,b,a), hsl(h,s%,l%), hsla(h,s%,l%,a), or named colors.", result.First());
    }

    [Theory]
    [InlineData("#ABC")] // 3-digit uppercase
    [InlineData("#abc")] // 3-digit lowercase
    [InlineData("#ABCDEF")] // 6-digit mixed case
    [InlineData("#abcdef")] // 6-digit lowercase
    public async Task ValidateAsync_CaseInsensitiveHex_ReturnsEmptyErrors(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("#000000")] // Boundary: min value
    [InlineData("#FFFFFF")] // Boundary: max value
    [InlineData("#808080")] // Mid value
    [InlineData("#FF0000")] // Pure red
    [InlineData("#00FF00")] // Pure green
    [InlineData("#0000FF")] // Pure blue
    public async Task ValidateAsync_BoundaryHexColors_ReturnsEmptyErrors(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("#123456789")] // Too many digits
    [InlineData("#GGG")] // Invalid hex chars
    [InlineData("#12G")] // Mixed valid/invalid
    [InlineData("#-123")] // Negative sign
    [InlineData("# 123")] // Space in hex
    public async Task ValidateAsync_MoreInvalidHexColors_ReturnsError(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        Assert.Single(result);
        Assert.Contains("Invalid color format", result.First());
    }
}