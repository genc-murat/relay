using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class ColorValidationRuleTests
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
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("rgb(0,0,0)")] // Black
    [InlineData("rgb(255,255,255)")] // White
    [InlineData("rgb(255,0,0)")] // Red
    [InlineData("rgb(0,255,0)")] // Green
    [InlineData("rgb(0,0,255)")] // Blue
    [InlineData("rgb(128,128,128)")] // Gray
    [InlineData("RGB(255,255,255)")] // Case insensitive
    [InlineData("rgb( 255 , 255 , 255 )")] // Extra spaces
    public async Task ValidateAsync_ValidRgbColors_ReturnsEmptyErrors(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("rgba(0,0,0,0)")] // Transparent black
    [InlineData("rgba(255,255,255,1)")] // Opaque white
    [InlineData("rgba(255,0,0,0.5)")] // Semi-transparent red
    [InlineData("rgba(0,255,0,0.25)")] // Semi-transparent green
    [InlineData("RGBA(0,0,255,1)")] // Case insensitive
    [InlineData("rgba( 128 , 128 , 128 , 0.5 )")] // Extra spaces
    public async Task ValidateAsync_ValidRgbaColors_ReturnsEmptyErrors(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("hsl(0,0%,0%)")] // Black
    [InlineData("hsl(360,100%,100%)")] // White
    [InlineData("hsl(0,100%,50%)")] // Red
    [InlineData("hsl(120,100%,50%)")] // Green
    [InlineData("hsl(240,100%,50%)")] // Blue
    [InlineData("hsl(60,100%,50%)")] // Yellow
    [InlineData("HSL(180,50%,50%)")] // Case insensitive
    [InlineData("hsl( 0 , 0% , 0% )")] // Extra spaces
    public async Task ValidateAsync_ValidHslColors_ReturnsEmptyErrors(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("hsla(0,0%,0%,0)")] // Transparent black
    [InlineData("hsla(360,100%,100%,1)")] // Opaque white
    [InlineData("hsla(0,100%,50%,0.5)")] // Semi-transparent red
    [InlineData("hsla(120,100%,50%,0.25)")] // Semi-transparent green
    [InlineData("HSLA(240,100%,50%,1)")] // Case insensitive
    [InlineData("hsla( 60 , 100% , 50% , 0.5 )")] // Extra spaces
    public async Task ValidateAsync_ValidHslaColors_ReturnsEmptyErrors(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("red")]
    [InlineData("blue")]
    [InlineData("green")]
    [InlineData("yellow")]
    [InlineData("black")]
    [InlineData("white")]
    [InlineData("purple")]
    [InlineData("orange")]
    [InlineData("RED")] // Case insensitive
    [InlineData("Blue")]
    [InlineData("GREEN")]
    public async Task ValidateAsync_ValidNamedColors_ReturnsEmptyErrors(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    [InlineData("   ")] // Whitespace
    public async Task ValidateAsync_EmptyOrWhitespace_ReturnsEmptyErrors(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        result.Should().BeEmpty();
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
        result.Should().ContainSingle().Which.Should().Contain("Invalid color format");
    }

    [Theory]
    [InlineData("rgb(256,0,0)")] // Red value too high
    [InlineData("rgb(0,256,0)")] // Green value too high
    [InlineData("rgb(0,0,256)")] // Blue value too high
    [InlineData("rgb(-1,0,0)")] // Negative red
    [InlineData("rgb(0,-1,0)")] // Negative green
    [InlineData("rgb(0,0,-1)")] // Negative blue
    [InlineData("rgb(255,255)")] // Missing blue
    [InlineData("rgb(255,255,255,255)")] // Too many values
    public async Task ValidateAsync_InvalidRgbColors_ReturnsError(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        result.Should().ContainSingle("RGB values must be between 0 and 255.");
    }

    [Theory]
    [InlineData("rgba(256,0,0,1)")] // Red value too high
    [InlineData("rgba(0,256,0,1)")] // Green value too high
    [InlineData("rgba(0,0,256,1)")] // Blue value too high
    [InlineData("rgba(0,0,0,1.1)")] // Alpha too high
    [InlineData("rgba(0,0,0,-0.1)")] // Negative alpha
    [InlineData("rgba(255,255,255)")] // Missing alpha
    public async Task ValidateAsync_InvalidRgbaColors_ReturnsError(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        if (color.Contains("1.1") || color.Contains("-0.1"))
        {
            result.Should().ContainSingle("Alpha value must be between 0 and 1.");
        }
        else if (color.Contains("256") || color.Contains("-1"))
        {
            result.Should().ContainSingle("RGB values must be between 0 and 255.");
        }
        else
        {
            result.Should().ContainSingle().Which.Should().Contain("Invalid color format");
        }
    }

    [Theory]
    [InlineData("hsl(361,0%,0%)")] // Hue too high
    [InlineData("hsl(-1,0%,0%)")] // Negative hue
    [InlineData("hsl(0,101%,0%)")] // Saturation too high
    [InlineData("hsl(0,0%,101%)")] // Lightness too high
    [InlineData("hsl(0,-1%,0%)")] // Negative saturation
    [InlineData("hsl(0,0%,-1%)")] // Negative lightness
    [InlineData("hsl(360,100%)")] // Missing lightness
    public async Task ValidateAsync_InvalidHslColors_ReturnsError(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        if (color.Contains("361") || color.Contains("-1"))
        {
            result.Should().ContainSingle("Hue must be between 0 and 360 degrees.");
        }
        else
        {
            result.Should().ContainSingle("Saturation and lightness must be between 0% and 100%.");
        }
    }

    [Theory]
    [InlineData("hsla(361,0%,0%,1)")] // Hue too high
    [InlineData("hsla(0,101%,0%,1)")] // Saturation too high
    [InlineData("hsla(0,0%,101%,1)")] // Lightness too high
    [InlineData("hsla(0,0%,0%,1.1)")] // Alpha too high
    [InlineData("hsla(360,100%,100%)")] // Missing alpha
    public async Task ValidateAsync_InvalidHslaColors_ReturnsError(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        if (color.Contains("361"))
        {
            result.Should().ContainSingle("Hue must be between 0 and 360 degrees.");
        }
        else if (color.Contains("101"))
        {
            result.Should().ContainSingle("Saturation and lightness must be between 0% and 100%.");
        }
        else if (color.Contains("1.1"))
        {
            result.Should().ContainSingle("Alpha value must be between 0 and 1.");
        }
        else
        {
            result.Should().ContainSingle().Which.Should().Contain("Invalid color format");
        }
    }

    [Theory]
    [InlineData("redd")] // Typo
    [InlineData("blu")] // Abbreviation
    [InlineData("notacolor")] // Invalid name
    [InlineData("color-red")] // Invalid format
    public async Task ValidateAsync_InvalidNamedColors_ReturnsError(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        result.Should().ContainSingle().Which.Should().Contain("Invalid color format");
    }

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
        result.Should().ContainSingle().Which.Should().Contain("Invalid color format");
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
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("aliceblue")]
    [InlineData("antiquewhite")]
    [InlineData("yellowgreen")]
    [InlineData("ALICEBLUE")] // Uppercase
    [InlineData("AntiqueWhite")] // Mixed case
    public async Task ValidateAsync_AllNamedColors_ReturnsEmptyErrors(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        result.Should().BeEmpty();
    }
}