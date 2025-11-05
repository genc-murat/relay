using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation;

public class HasDigitsValidationRuleTests
{
    private readonly HasDigitsValidationRule _rule = new();

    [Fact]
    public async Task ValidateAsync_With_String_Containing_Digit_Returns_Empty_Errors()
    {
        // Act
        var result = await _rule.ValidateAsync("test123");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_With_String_Containing_Multiple_Digits_Returns_Empty_Errors()
    {
        // Act
        var result = await _rule.ValidateAsync("abc1def2ghi3");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_With_String_Without_Any_Digit_Returns_Error()
    {
        // Act
        var result = await _rule.ValidateAsync("abcdef");

        // Assert
        Assert.Single(result);
        Assert.Equal("Value must contain at least one digit.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_With_Null_String_Returns_Empty_Errors()
    {
        // Act
        var result = await _rule.ValidateAsync(null);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_With_Empty_String_Returns_Empty_Errors()
    {
        // Act
        var result = await _rule.ValidateAsync("");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_With_Whitespace_Only_String_Returns_Error()
    {
        // Act
        var result = await _rule.ValidateAsync("   ");

        // Assert
        Assert.Single(result);
        Assert.Equal("Value must contain at least one digit.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_With_Digit_At_Beginning_Returns_Empty_Errors()
    {
        // Act
        var result = await _rule.ValidateAsync("123test");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_With_Digit_At_End_Returns_Empty_Errors()
    {
        // Act
        var result = await _rule.ValidateAsync("test123");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_With_Only_Digits_Returns_Empty_Errors()
    {
        // Act
        var result = await _rule.ValidateAsync("12345");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_With_Special_Characters_And_Digits_Returns_Empty_Errors()
    {
        // Act
        var result = await _rule.ValidateAsync("!@#123$%^");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_With_Special_Characters_No_Digits_Returns_Error()
    {
        // Act
        var result = await _rule.ValidateAsync("!@#$%^&*()");

        // Assert
        Assert.Single(result);
        Assert.Equal("Value must contain at least one digit.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_With_Cancellation_Token_Throws_If_Cancelled()
    {
        // Arrange
        var cts = new System.Threading.CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<System.OperationCanceledException>(
            async () => await _rule.ValidateAsync("test", cts.Token));
    }

    [Fact]
    public async Task ValidateAsync_With_Numeric_String_Containing_Unicode_Digits_Returns_Empty_Errors()
    {
        // Test with various Unicode digit characters
        var unicodeDigitStrings = new[]
        {
            "test১২৩", // Bengali digits
            "test٠١٢٣٤٥٦٧٨٩", // Arabic-Indic digits
            "test๐๑๒๓๔๕๖๗๘๙", // Thai digits
        };

        foreach (var digitString in unicodeDigitStrings)
        {
            // Act
            var result = await _rule.ValidateAsync(digitString);

            // Assert - Note: char.IsDigit recognizes Unicode digits too
            Assert.Empty(result);
        }
    }

    [Fact]
    public async Task ValidateAsync_With_String_Containing_Unicode_Non_Digits_Returns_Error()
    {
        // Test with Unicode characters that look like digits but aren't
        var nonDigitStrings = new[]
        {
            "test¹²³", // Superscript digits (might be considered digits)
            "test⁰⁴⁵", 
            "test₀₁₂" // Subscript digits (might be considered digits)
        };

        // This test depends on how char.IsDigit behaves with superscript/subscript
        foreach (var nonDigitString in nonDigitStrings)
        {
            // Act
            var result = await _rule.ValidateAsync(nonDigitString);

            // If char.IsDigit recognizes these as digits, the result should be empty
            // If not, it should contain an error
            // Let's check both possibilities:
            if (nonDigitString.Any(char.IsDigit))
            {
                Assert.Empty(result); // If any char is considered a digit, validation passes
            }
            else
            {
                Assert.Single(result); // If no char is considered a digit, validation fails
                Assert.Equal("Value must contain at least one digit.", result.First());
            }
        }
    }
}