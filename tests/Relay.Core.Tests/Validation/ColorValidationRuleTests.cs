using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        Assert.Empty(result);
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
        Assert.Empty(result);
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
    [InlineData("redd")] // Typo
    [InlineData("blu")] // Abbreviation
    [InlineData("notacolor")] // Invalid name
    [InlineData("color-red")] // Invalid format
    public async Task ValidateAsync_InvalidNamedColors_ReturnsError(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        Assert.Single(result);
        Assert.Contains("Invalid color format", result.First());
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
        Assert.Single(result);
        Assert.Contains("Invalid color format", result.First());
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

    [Theory]
    [InlineData(" rgb(255,255,255) ")] // Leading/trailing spaces
    [InlineData("  #FFFFFF  ")] // Leading/trailing spaces
    [InlineData(" red ")] // Leading/trailing spaces
    [InlineData("\tblue\t")] // Tabs
    [InlineData("\n green \n")] // Newlines
    public async Task ValidateAsync_TrimmedInputs_ReturnsEmptyErrors(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_CancellationToken_ThrowsWhenCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _rule.ValidateAsync("red", cts.Token));
    }

    [Theory]
    [InlineData("aqua")]
    [InlineData("azure")]
    [InlineData("beige")]
    [InlineData("bisque")]
    [InlineData("blanchedalmond")]
    [InlineData("blueviolet")]
    [InlineData("burlywood")]
    [InlineData("cadetblue")]
    [InlineData("chartreuse")]
    [InlineData("chocolate")]
    [InlineData("coral")]
    [InlineData("cornflowerblue")]
    [InlineData("cornsilk")]
    [InlineData("crimson")]
    [InlineData("darkblue")]
    [InlineData("darkcyan")]
    [InlineData("darkgoldenrod")]
    [InlineData("darkgray")]
    [InlineData("darkgreen")]
    [InlineData("darkkhaki")]
    [InlineData("darkmagenta")]
    [InlineData("darkolivegreen")]
    [InlineData("darkorange")]
    [InlineData("darkorchid")]
    [InlineData("darkred")]
    [InlineData("darksalmon")]
    [InlineData("darkseagreen")]
    [InlineData("darkslateblue")]
    [InlineData("darkslategray")]
    [InlineData("darkslategrey")]
    [InlineData("darkturquoise")]
    [InlineData("darkviolet")]
    [InlineData("deeppink")]
    [InlineData("deepskyblue")]
    [InlineData("dimgray")]
    [InlineData("dimgrey")]
    [InlineData("dodgerblue")]
    [InlineData("firebrick")]
    [InlineData("floralwhite")]
    [InlineData("forestgreen")]
    [InlineData("fuchsia")]
    [InlineData("gainsboro")]
    [InlineData("ghostwhite")]
    [InlineData("gold")]
    [InlineData("goldenrod")]
    [InlineData("gray")]
    [InlineData("greenyellow")]
    [InlineData("grey")]
    [InlineData("honeydew")]
    [InlineData("hotpink")]
    [InlineData("indianred")]
    [InlineData("indigo")]
    [InlineData("ivory")]
    [InlineData("khaki")]
    [InlineData("lavender")]
    [InlineData("lavenderblush")]
    [InlineData("lawngreen")]
    [InlineData("lemonchiffon")]
    [InlineData("lightblue")]
    [InlineData("lightcoral")]
    [InlineData("lightcyan")]
    [InlineData("lightgoldenrodyellow")]
    [InlineData("lightgray")]
    [InlineData("lightgreen")]
    [InlineData("lightgrey")]
    [InlineData("lightpink")]
    [InlineData("lightsalmon")]
    [InlineData("lightseagreen")]
    [InlineData("lightskyblue")]
    [InlineData("lightslategray")]
    [InlineData("lightslategrey")]
    [InlineData("lightsteelblue")]
    [InlineData("lightyellow")]
    [InlineData("lime")]
    [InlineData("limegreen")]
    [InlineData("linen")]
    [InlineData("magenta")]
    [InlineData("maroon")]
    [InlineData("mediumaquamarine")]
    [InlineData("mediumblue")]
    [InlineData("mediumorchid")]
    [InlineData("mediumpurple")]
    [InlineData("mediumseagreen")]
    [InlineData("mediumslateblue")]
    [InlineData("mediumspringgreen")]
    [InlineData("mediumturquoise")]
    [InlineData("mediumvioletred")]
    [InlineData("midnightblue")]
    [InlineData("mintcream")]
    [InlineData("mistyrose")]
    [InlineData("moccasin")]
    [InlineData("navajowhite")]
    [InlineData("navy")]
    [InlineData("oldlace")]
    [InlineData("olive")]
    [InlineData("olivedrab")]
    [InlineData("orangered")]
    [InlineData("orchid")]
    [InlineData("palegoldenrod")]
    [InlineData("palegreen")]
    [InlineData("paleturquoise")]
    [InlineData("palevioletred")]
    [InlineData("papayawhip")]
    [InlineData("peachpuff")]
    [InlineData("peru")]
    [InlineData("pink")]
    [InlineData("plum")]
    [InlineData("powderblue")]
    [InlineData("rebeccapurple")]
    [InlineData("rosybrown")]
    [InlineData("royalblue")]
    [InlineData("saddlebrown")]
    [InlineData("salmon")]
    [InlineData("sandybrown")]
    [InlineData("seagreen")]
    [InlineData("seashell")]
    [InlineData("sienna")]
    [InlineData("silver")]
    [InlineData("skyblue")]
    [InlineData("slateblue")]
    [InlineData("slategray")]
    [InlineData("slategrey")]
    [InlineData("snow")]
    [InlineData("springgreen")]
    [InlineData("steelblue")]
    [InlineData("tan")]
    [InlineData("teal")]
    [InlineData("thistle")]
    [InlineData("tomato")]
    [InlineData("turquoise")]
    [InlineData("violet")]
    [InlineData("wheat")]
    [InlineData("whitesmoke")]
    public async Task ValidateAsync_AllRemainingNamedColors_ReturnsEmptyErrors(string color)
    {
        // Act
        var result = await _rule.ValidateAsync(color);

        // Assert
        Assert.Empty(result);
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
}