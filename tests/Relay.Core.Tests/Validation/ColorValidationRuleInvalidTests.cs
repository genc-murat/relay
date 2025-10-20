using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class ColorValidationRuleInvalidTests
{
    private readonly ColorValidationRule _rule = new();

    [Theory]
    [InlineData("rgb(255,255,255,0.5)")] // RGB with alpha (should be RGBA)
    [InlineData("hsl(360,100%,100%,0.5)")] // HSL with alpha (should be HSLA)
    [InlineData("hsv(360,100%,100%)")] // HSV not supported
    [InlineData("cmyk(0,0,0,0)")] // CMYK not supported
    public async Task ValidateAsync_UnsupportedColorFormats_ReturnsError(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        Assert.Single(result);
        Assert.Contains("Invalid color format", result.First());
    }

    [Theory]
    [InlineData("rgb(255,255,255,255)")] // RGB with 4 values
    [InlineData("hsl(360,100%,100%,100%)")] // HSL with 4 values
    [InlineData("rgba(255,255,255)")] // RGBA with 3 values
    [InlineData("hsla(360,100%,100%)")] // HSLA with 3 values
    [InlineData("rgb(255,255)")] // RGB with 2 values
    [InlineData("rgb(255)")] // RGB with 1 value
    [InlineData("hsl(360,100%)")] // HSL with 2 values
    [InlineData("hsl(360)")] // HSL with 1 value
    public async Task ValidateAsync_IncorrectParameterCounts_ReturnsError(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        Assert.Single(result);
        Assert.Contains("Invalid color format", result.First());
    }

    [Theory]
    [InlineData("rgb(255.5,255,255)")] // Float values
    [InlineData("rgba(255,255,255,0.5.5)")] // Multiple decimals
    [InlineData("hsl(360.5,100%,100%)")] // Float hue
    [InlineData("hsla(360,100.5%,100%,1)")] // Float saturation
    [InlineData("hsla(360,100%,100.5%,1)")] // Float lightness
    public async Task ValidateAsync_InvalidNumberFormats_ReturnsError(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        Assert.Single(result);
        Assert.Contains("Invalid color format", result.First());
    }

    [Theory]
    [InlineData("rgb(255,255,255,0.5)")] // RGB with alpha
    [InlineData("hsl(360,100%,100%,0.5)")] // HSL with alpha
    [InlineData("hsv(360,100%,100%)")] // HSV format
    [InlineData("cmyk(0,0,0,0)")] // CMYK format
    [InlineData("lab(50,0,0)")] // LAB format
    [InlineData("xyz(0.5,0.5,0.5)")] // XYZ format
    public async Task ValidateAsync_MoreUnsupportedFormats_ReturnsError(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        Assert.Single(result);
        Assert.Contains("Invalid color format", result.First());
    }
}