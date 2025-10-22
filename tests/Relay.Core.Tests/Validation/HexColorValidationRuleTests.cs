using System.Linq;
using System.Threading.Tasks;
 using Relay.Core.Validation.Rules;
 using Xunit;

#pragma warning disable xUnit1012 // Null should not be used for type parameter

namespace Relay.Core.Tests.Validation;

public class HexColorValidationRuleTests
{
    private readonly HexColorValidationRule _rule = new();

    [Theory]
    [InlineData("#FF0000")] // Red
    [InlineData("#00FF00")] // Green
    [InlineData("#0000FF")] // Blue
    [InlineData("#FFF")] // White (3-digit)
    [InlineData("#000")] // Black (3-digit)
    [InlineData("#123456")] // Random color
    [InlineData("#ABCDEF")] // Random color uppercase
    [InlineData("#abcdef")] // Random color lowercase
    [InlineData("#FF000080")] // Red with alpha
    [InlineData("#00FF0080")] // Green with alpha
    public async Task ValidateAsync_ValidHexColor_ReturnsEmptyErrors(string hexColor)
    {
        // Act
        var result = await _rule.ValidateAsync(hexColor);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("#FF000")] // Too short
    [InlineData("#FF00000")] // Too long
    [InlineData("#GGG")] // Invalid characters
    [InlineData("FF0000")] // Missing #
    [InlineData("#FF0000800")] // Too long with alpha
    [InlineData("#FF00008")] // Invalid length with alpha
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    public async Task ValidateAsync_InvalidHexColor_ReturnsError(string hexColor)
    {
        // Act
        var result = await _rule.ValidateAsync(hexColor);

        // Assert
        if (string.IsNullOrWhiteSpace(hexColor))
        {
            Assert.Empty(result);
        }
        else
        {
            Assert.Single(result);
            Assert.Equal("Invalid hexadecimal color format. Use #RGB, #RRGGBB, or #RRGGBBAA.", result.First());
        }
    }
}