using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class ColorValidationRuleBoundaryTests
{
    private readonly ColorValidationRule _rule = new();

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
}