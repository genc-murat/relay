using System.Linq;
using System.Threading.Tasks;
 using Relay.Core.Validation.Rules;
 using Xunit;

namespace Relay.Core.Tests.Validation;

public class IbanValidationRuleTests
{
    private readonly IbanValidationRule _rule = new();

    [Theory]
    [InlineData("GB29 NWBK 6016 1331 9268 19")] // Valid UK IBAN
    [InlineData("DE89370400440532013000")] // Valid German IBAN
    [InlineData("FR7630006000011234567890189")] // Valid French IBAN
    [InlineData("GB29NWBK60161331926819")] // Valid IBAN without spaces
    public async Task ValidateAsync_ValidIban_ReturnsEmptyErrors(string iban)
    {
        // Act
        var result = await _rule.ValidateAsync(iban);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("GB29 NWBK 6016 1331 9268 18")] // Invalid check digits
    [InlineData("XX89370400440532013000")] // Invalid country code
    [InlineData("GB2A NWBK 6016 1331 9268 19")] // Invalid check digits (letters)
    [InlineData("GB")] // Too short
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    public async Task ValidateAsync_InvalidIban_ReturnsError(string iban)
    {
        // Act
        var result = await _rule.ValidateAsync(iban);

        // Assert
        if (string.IsNullOrWhiteSpace(iban))
        {
            Assert.Empty(result);
        }
        else
        {
            Assert.Single(result);
            Assert.True(result.First().Contains("IBAN length is invalid.") ||
                result.First().Contains("IBAN must start with a valid country code.") ||
                result.First().Contains("IBAN check digits are invalid.") ||
                result.First().Contains("IBAN checksum is invalid."));
        }
    }
}