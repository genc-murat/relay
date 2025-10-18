using System.Linq;
using System.Threading.Tasks;
 using Relay.Core.Validation.Rules;
 using Xunit;

namespace Relay.Core.Tests.Validation;

public class MacAddressValidationRuleTests
{
    private readonly MacAddressValidationRule _rule = new();

    [Theory]
    [InlineData("00:11:22:33:44:55")] // Colon format
    [InlineData("00-11-22-33-44-55")] // Dash format
    [InlineData("001122334455")] // No separator format
    [InlineData("AA:BB:CC:DD:EE:FF")] // Uppercase
    [InlineData("aa:bb:cc:dd:ee:ff")] // Lowercase
    [InlineData("A1:B2:C3:D4:E5:F6")] // Mixed case
    public async Task ValidateAsync_ValidMacAddress_ReturnsEmptyErrors(string macAddress)
    {
        // Act
        var result = await _rule.ValidateAsync(macAddress);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("00:11:22:33:44")] // Too short
    [InlineData("00:11:22:33:44:55:66")] // Too long
    [InlineData("00:11:22:33:44:ZZ")] // Invalid character
    [InlineData("00:11:22:33:44:55:")] // Trailing separator
    [InlineData(":00:11:22:33:44:55")] // Leading separator
    [InlineData("00112233445")] // Wrong length no separator
    [InlineData("not a mac")] // Plain text
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    public async Task ValidateAsync_InvalidMacAddress_ReturnsError(string macAddress)
    {
        // Act
        var result = await _rule.ValidateAsync(macAddress);

        // Assert
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            Assert.Empty(result);
        }
        else
        {
            Assert.Single(result);
            Assert.Equal("Invalid MAC address format. Use XX:XX:XX:XX:XX:XX, XX-XX-XX-XX-XX-XX, or XXXXXXXXXXXX.", result.First());
        }
    }
}