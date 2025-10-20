using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class ColorValidationRuleHslTests
{
    private readonly ColorValidationRule _rule = new();

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
        Assert.Empty(result);
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
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("hsl(361,0%,0%)")] // Hue too high
    [InlineData("hsl(-1,0%,0%)")] // Negative hue
    [InlineData("hsl(0,101%,0%)")] // Saturation too high
    [InlineData("hsl(0,0%,101%)")] // Lightness too high
    [InlineData("hsl(0,-1%,0%)")] // Negative saturation
    [InlineData("hsl(0,0%,-1%)")] // Negative lightness
    public async Task ValidateAsync_InvalidHslColors_ReturnsError(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        var hue = int.Parse(color.Split('(')[1].Split(',')[0]);
        if (hue < 0 || hue > 360)
        {
            Assert.Single(result);
            Assert.Equal("Hue must be between 0 and 360 degrees.", result.First());
        }
        else
        {
            Assert.Single(result);
            Assert.Equal("Saturation and lightness must be between 0% and 100%.", result.First());
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
            Assert.Single(result);
            Assert.Equal("Hue must be between 0 and 360 degrees.", result.First());
        }
        else if (color.Contains("101"))
        {
            Assert.Single(result);
            Assert.Equal("Saturation and lightness must be between 0% and 100%.", result.First());
        }
        else if (color.Contains("1.1"))
        {
            Assert.Single(result);
            Assert.Equal("Alpha value must be between 0 and 1.", result.First());
        }
        else
        {
            Assert.Single(result);
            Assert.Contains("Invalid color format", result.First());
        }
    }

    [Theory]
    [InlineData("hsl(0,0%,0%)")] // Boundary: all min
    [InlineData("hsl(360,100%,100%)")] // Boundary: all max
    [InlineData("hsl(180,50%,50%)")] // Boundary: mid values
    [InlineData("hsl(0,100%,50%)")] // Boundary: red
    [InlineData("hsl(120,100%,50%)")] // Boundary: green
    [InlineData("hsl(240,100%,50%)")] // Boundary: blue
    public async Task ValidateAsync_BoundaryHslColors_ReturnsEmptyErrors(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("hsla(0,0%,0%,0)")] // Boundary: all min
    [InlineData("hsla(360,100%,100%,1)")] // Boundary: all max
    [InlineData("hsla(180,50%,50%,0.5)")] // Boundary: mid alpha
    [InlineData("hsla(0,100%,50%,0.0)")] // Boundary: zero alpha
    public async Task ValidateAsync_BoundaryHslaColors_ReturnsEmptyErrors(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("hsl(361,0%,0%)")] // Hue over max
    [InlineData("hsl(-1,0%,0%)")] // Hue under min
    [InlineData("hsl(0,101%,0%)")] // Saturation over max
    [InlineData("hsl(0,-1%,0%)")] // Saturation under min
    [InlineData("hsl(0,0%,101%)")] // Lightness over max
    [InlineData("hsl(0,0%,-1%)")] // Lightness under min
    [InlineData("hsl(400,200%,200%)")] // All over
    [InlineData("hsl(-10,-10%,-10%)")] // All under
    public async Task ValidateAsync_ExtremeHslBoundaries_ReturnsError(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        var hue = int.Parse(color.Split('(')[1].Split(',')[0]);
        if (hue < 0 || hue > 360)
        {
            Assert.Single(result);
            Assert.Equal("Hue must be between 0 and 360 degrees.", result.First());
        }
        else
        {
            Assert.Single(result);
            Assert.Equal("Saturation and lightness must be between 0% and 100%.", result.First());
        }
    }

    [Theory]
    [InlineData("hsla(361,0%,0%,1)")] // Hue over max
    [InlineData("hsla(-1,0%,0%,1)")] // Hue under min
    [InlineData("hsla(0,101%,0%,1)")] // Saturation over max
    [InlineData("hsla(0,-1%,0%,1)")] // Saturation under min
    [InlineData("hsla(0,0%,101%,1)")] // Lightness over max
    [InlineData("hsla(0,0%,-1%,1)")] // Lightness under min
    [InlineData("hsla(0,0%,0%,1.1)")] // Alpha over max
    [InlineData("hsla(0,0%,0%,-0.1)")] // Alpha under min
    public async Task ValidateAsync_ExtremeHslaBoundaries_ReturnsError(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        if (color.Contains("361") || color.Contains("(-1"))
        {
            Assert.Single(result);
            Assert.Equal("Hue must be between 0 and 360 degrees.", result.First());
        }
        else if (color.Contains("101") || color.Contains(",-1%"))
        {
            Assert.Single(result);
            Assert.Equal("Saturation and lightness must be between 0% and 100%.", result.First());
        }
        else if (color.Contains("1.1"))
        {
            Assert.Single(result);
            Assert.Equal("Alpha value must be between 0 and 1.", result.First());
        }
        else if (color.Contains("-0.1"))
        {
            Assert.Single(result);
            Assert.Equal("Alpha value must be between 0 and 1.", result.First());
        }
        else
        {
            Assert.Single(result);
            Assert.Contains("Invalid color format", result.First());
        }
    }

    [Theory]
    [InlineData("hsl(360,100%,100%,0.5)")] // HSL with alpha
    [InlineData("hsla(360,100%,100%)")] // HSLA with 3 values
    [InlineData("hsl(360,100%)")] // HSL with 2 values
    [InlineData("hsl(360)")] // HSL with 1 value
    public async Task ValidateAsync_IncorrectHslParameterCounts_ReturnsError(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        Assert.Single(result);
        Assert.Contains("Invalid color format", result.First());
    }

    [Theory]
    [InlineData("hsl(360.5,100%,100%)")] // Float hue
    [InlineData("hsla(360,100.5%,100%,1)")] // Float saturation
    [InlineData("hsla(360,100%,100.5%,1)")] // Float lightness
    public async Task ValidateAsync_InvalidHslNumberFormats_ReturnsError(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        Assert.Single(result);
        Assert.Contains("Invalid color format", result.First());
    }
}