using Relay.Core.Validation.Rules;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Validation.Rules;

public class BitcoinAddressValidationRuleTests
{
    private readonly BitcoinAddressValidationRule _rule = new();

    [Theory]
    [InlineData("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", true)] // Mainnet P2PKH
    [InlineData("1dice8EMZmqKvrGE4Qc9bUFf9PX3xaYDp", true)] // Mainnet P2PKH
    [InlineData("3J98t1WpEZ73CNmQviecrnyiWrnqRhWNLy", true)] // Mainnet P2SH
    [InlineData("bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4", true)] // Mainnet Bech32 P2WPKH
    [InlineData("mipcBbFg9gMiCh81Kj8tqqdgoZub1ZJRfn", true)] // Testnet P2PKH
    [InlineData("2N2JD6wb56AfK4tfmM6PwdVmoYk2dCKf4Br", true)] // Testnet P2SH
    [InlineData("tb1qw508d6qejxtdg4y5r3zarvary0c5xw7kxpjzsx", true)] // Testnet Bech32
    [InlineData("", true)] // Empty should pass
    [InlineData("invalid", false)] // Invalid format
    [InlineData("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfN", true)] // Valid Base58 (33 chars)
    [InlineData("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNaInvalid", false)] // Too long
    [InlineData("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfN0", false)] // Invalid character
    [InlineData("bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t5", true)] // Valid Bech32
    [InlineData("tc1qw508d6qejxtdg4y5r3zarvary0c5xw7kxpjzsx", false)] // Invalid HRP
    public async Task ValidateAsync_WithVariousAddresses_ShouldReturnExpectedResult(string address, bool isValid)
    {
        // Act
        var result = await _rule.ValidateAsync(address);

        // Assert
        if (isValid)
        {
            Assert.Empty(result);
        }
        else
        {
            Assert.NotEmpty(result);
        }
    }

    [Theory]
    [InlineData("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", true)] // Mainnet P2PKH
    [InlineData("3J98t1WpEZ73CNmQviecrnyiWrnqRhWNLy", true)] // Mainnet P2SH
    [InlineData("bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4", true)] // Mainnet Bech32
    [InlineData("mipcBbFg9gMiCh81Kj8tqqdgoZub1ZJRfn", false)] // Testnet P2PKH
    [InlineData("2N2JD6wb56AfK4tfmM6PwdVmoYk2dCKf4Br", false)] // Testnet P2SH
    [InlineData("tb1qw508d6qejxtdg4y5r3zarvary0c5xw7kxpjzsx", false)] // Testnet Bech32
    public async Task ValidateAsync_WithMainnetNetwork_ShouldOnlyValidateMainnetAddresses(string address, bool isValid)
    {
        // Arrange
        var rule = new BitcoinAddressValidationRule(BitcoinAddressValidationRule.BitcoinNetwork.Mainnet);

        // Act
        var result = await rule.ValidateAsync(address);

        // Assert
        if (isValid)
        {
            Assert.Empty(result);
        }
        else
        {
            Assert.NotEmpty(result);
        }
    }

    [Theory]
    [InlineData("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", false)] // Mainnet P2PKH
    [InlineData("3J98t1WpEZ73CNmQviecrnyiWrnqRhWNLy", false)] // Mainnet P2SH
    [InlineData("bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4", false)] // Mainnet Bech32
    [InlineData("mipcBbFg9gMiCh81Kj8tqqdgoZub1ZJRfn", true)] // Testnet P2PKH
    [InlineData("2N2JD6wb56AfK4tfmM6PwdVmoYk2dCKf4Br", true)] // Testnet P2SH
    [InlineData("tb1qw508d6qejxtdg4y5r3zarvary0c5xw7kxpjzsx", true)] // Testnet Bech32
    public async Task ValidateAsync_WithTestnetNetwork_ShouldOnlyValidateTestnetAddresses(string address, bool isValid)
    {
        // Arrange
        var rule = new BitcoinAddressValidationRule(BitcoinAddressValidationRule.BitcoinNetwork.Testnet);

        // Act
        var result = await rule.ValidateAsync(address);

        // Assert
        if (isValid)
        {
            Assert.Empty(result);
        }
        else
        {
            Assert.NotEmpty(result);
        }
    }

    [Theory]
    [InlineData(null, true)] // null should pass (empty validation)
    [InlineData("", true)] // empty should pass (empty validation)
    [InlineData("   ", true)] // whitespace should pass (empty validation)
    public async Task ValidateAsync_WithNullOrEmptyInput_ShouldReturnEmptyResult(string address, bool isValid)
    {
        // Act
        var result = await _rule.ValidateAsync(address);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithCustomErrorMessage_ShouldReturnCustomMessage()
    {
        // Arrange
        var customErrorMessage = "Custom error message";
        var rule = new BitcoinAddressValidationRule(errorMessage: customErrorMessage);
        var invalidAddress = "invalid-address";

        // Act
        var result = await rule.ValidateAsync(invalidAddress);

        // Assert
        Assert.Single(result);
        Assert.Equal(customErrorMessage, result.First());
    }

    [Fact]
    public async Task ValidateAsync_WithValidAddressAndWhitespace_ShouldTrimAndValidate()
    {
        // Arrange
        var validAddressWithWhitespace = "  1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa  ";

        // Act
        var result = await _rule.ValidateAsync(validAddressWithWhitespace);

        // Assert
        Assert.Empty(result);
    }
}