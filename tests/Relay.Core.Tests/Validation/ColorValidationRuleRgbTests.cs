using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class ColorValidationRuleRgbTests
{
    private readonly ColorValidationRule _rule = new();

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
        Assert.Empty(result);
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
        Assert.Empty(result);
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
            Assert.Single(result);
            Assert.Equal("Alpha value must be between 0 and 1.", result.First());
        }
        else if (color.Contains("256") || color.Contains("-1"))
        {
            Assert.Single(result);
            Assert.Equal("RGB values must be between 0 and 255.", result.First());
        }
        else
        {
            Assert.Single(result);
        Assert.Contains("Invalid color format", result.First());
        }
    }

    [Theory]
    [InlineData("rgb(0,0,0)")] // Boundary: all min
    [InlineData("rgb(255,255,255)")] // Boundary: all max
    [InlineData("rgb(0,255,0)")] // Boundary: green max
    [InlineData("rgb(255,0,255)")] // Boundary: red and blue max
    public async Task ValidateAsync_BoundaryRgbColors_ReturnsEmptyErrors(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("rgba(0,0,0,0)")] // Boundary: all min
    [InlineData("rgba(255,255,255,1)")] // Boundary: all max
    [InlineData("rgba(128,128,128,0.5)")] // Boundary: mid alpha
    [InlineData("rgba(255,0,0,0.0)")] // Boundary: zero alpha
    public async Task ValidateAsync_BoundaryRgbaColors_ReturnsEmptyErrors(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("rgb(256,0,0)")] // Over max
    [InlineData("rgb(-1,0,0)")] // Under min
    [InlineData("rgb(255,256,0)")] // One over
    [InlineData("rgb(0,-1,0)")] // One under
    [InlineData("rgb(255,0,-1)")] // One under
    [InlineData("rgb(300,300,300)")] // All over
    [InlineData("rgb(-10,-10,-10)")] // All under
    public async Task ValidateAsync_ExtremeRgbBoundaries_ReturnsError(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        Assert.Single(result);
        Assert.Equal("RGB values must be between 0 and 255.", result.First());
    }

    [Theory]
    [InlineData("rgba(256,0,0,1)")] // RGB over max
    [InlineData("rgba(-1,0,0,1)")] // RGB under min
    [InlineData("rgba(255,255,255,1.1)")] // Alpha over max
    [InlineData("rgba(255,255,255,-0.1)")] // Alpha under min
    [InlineData("rgba(255,255,255,2)")] // Alpha way over
    [InlineData("rgba(255,255,255,-1)")] // Alpha way under
    public async Task ValidateAsync_ExtremeRgbaBoundaries_ReturnsError(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        if (color.Contains(",1.1") || color.Contains(",-0.1") || color.Contains(",2") || color.Contains(",-1"))
        {
            Assert.Single(result);
            Assert.Equal("Alpha value must be between 0 and 1.", result.First());
        }
        else
        {
            Assert.Single(result);
            Assert.Equal("RGB values must be between 0 and 255.", result.First());
        }
    }

    [Theory]
    [InlineData("rgb(255,255,255,0.5)")] // RGB with alpha
    [InlineData("rgba(255,255,255)")] // RGBA with 3 values
    [InlineData("rgb(255,255)")] // RGB with 2 values
    [InlineData("rgb(255)")] // RGB with 1 value
    public async Task ValidateAsync_IncorrectRgbParameterCounts_ReturnsError(string color)
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
    public async Task ValidateAsync_InvalidRgbNumberFormats_ReturnsError(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        Assert.Single(result);
        Assert.Contains("Invalid color format", result.First());
    }
}