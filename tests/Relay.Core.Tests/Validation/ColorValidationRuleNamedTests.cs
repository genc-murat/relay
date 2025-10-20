using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class ColorValidationRuleNamedTests
{
    private readonly ColorValidationRule _rule = new();

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
}