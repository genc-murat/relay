using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation.Rules;

public class EthereumAddressValidationRuleTests
{
    private readonly EthereumAddressValidationRule _rule = new();

    [Theory]
    [InlineData("0x742d35Cc6634C0532925a3b8D4C9db96C4b4Db45", true)] // Valid checksum address
    [InlineData("0x742d35cc6634c0532925a3b8d4c9db96c4b4db45", true)] // Valid lowercase address
    [InlineData("0x742D35CC6634C0532925A3B8D4C9DB96C4B4DB45", false)] // Invalid uppercase address
    [InlineData("742d35Cc6634C0532925a3b8D4C9db96C4b4Db45", true)] // Valid without 0x prefix
    [InlineData("0x0000000000000000000000000000000000000000", true)] // Zero address
    [InlineData("0x000000000000000000000000000000000000dead", true)] // Dead address
    [InlineData("0xd8dA6BF26964aF9D7eEd9e03E53415D37aA96045", true)] // Vitalik's address
    [InlineData("", true)] // Empty should pass (empty validation)
    [InlineData("invalid", false)] // Invalid format
    [InlineData("0x", false)] // Too short
    [InlineData("0x742d35Cc6634C0532925a3b8D4C9db96C4b4Db4", false)] // Too short (39 chars)
    [InlineData("0x742d35Cc6634C0532925a3b8D4C9db96C4b4Db456", false)] // Too long (41 chars)
    [InlineData("0x742d35Cc6634C0532925a3b8D4C9db96C4b4Db4g", false)] // Invalid hex character
    [InlineData("0x742d35Cc6634C0532925a3b8D4C9db96C4b4Db4 ", false)] // Trailing space
    [InlineData(" 0x742d35Cc6634C0532925a3b8D4C9db96C4b4Db45", true)] // Leading space (trimmed)
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
    [InlineData("0x742d35Cc6634C0532925a3b8D4C9db96C4b4Db45", true)] // Valid checksum
    [InlineData("0x742d35cc6634c0532925a3b8d4c9db96c4b4db45", false)] // Lowercase (invalid when checksum required)
    [InlineData("0x742D35CC6634C0532925A3B8D4C9DB96C4B4DB45", false)] // Uppercase (invalid)
    [InlineData("0x742d35Cc6634c0532925a3b8d4c9db96c4b4db45", false)] // Mixed case but invalid checksum
    public async Task ValidateAsync_WithChecksumRequired_ShouldOnlyValidateChecksumAddresses(string address, bool isValid)
    {
        // Arrange
        var rule = new EthereumAddressValidationRule(requireChecksum: true);

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
    [InlineData("vitalik.eth", true)] // Valid ENS name
    [InlineData("nick.eth", true)] // Valid ENS name
    [InlineData("example.eth", true)] // Valid ENS name
    [InlineData("subdomain.example.eth", true)] // Valid ENS name with subdomain
    [InlineData("test.xyz", true)] // Valid alternative TLD
    [InlineData("invalid", false)] // No TLD
    [InlineData("invalid.", false)] // Ends with dot
    [InlineData(".invalid", false)] // Starts with dot
    [InlineData("-test.eth", false)] // Starts with hyphen
    [InlineData("test-.eth", false)] // Ends with hyphen
    [InlineData("te.st.eth", true)] // Contains dot in label (subdomain)
    [InlineData("test@eth", false)] // Invalid character
    [InlineData("", true)] // Empty should pass (empty validation)
    public async Task ValidateAsync_WithEnsNamesAllowed_ShouldValidateEnsNames(string ensName, bool isValid)
    {
        // Arrange
        var rule = new EthereumAddressValidationRule(allowEnsNames: true);

        // Act
        var result = await rule.ValidateAsync(ensName);

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
    [InlineData("vitalik.eth", false)] // ENS name not allowed
    [InlineData("0x742d35Cc6634C0532925a3b8D4C9db96C4b4Db45", true)] // Regular address allowed
    public async Task ValidateAsync_WithEnsNamesDisallowed_ShouldRejectEnsNames(string address, bool isValid)
    {
        // Arrange
        var rule = new EthereumAddressValidationRule(allowEnsNames: false);

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
        var rule = new EthereumAddressValidationRule(errorMessage: customErrorMessage);
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
        var validAddressWithWhitespace = "  0x742d35Cc6634C0532925a3b8D4C9db96C4b4Db45  ";

        // Act
        var result = await _rule.ValidateAsync(validAddressWithWhitespace);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("0x742d35Cc6634C0532925a3b8D4C9db96C4b4Db45", true)] // Valid checksum
    [InlineData("0x742d35cc6634c0532925a3b8d4c9db96c4b4db45", true)] // Valid lowercase
    [InlineData("742d35Cc6634C0532925a3b8D4C9db96C4b4Db45", true)] // Valid without prefix
    public async Task ValidateAsync_WithBothEnsAndChecksumEnabled_ShouldValidateBoth(string address, bool isValid)
    {
        // Arrange
        var rule = new EthereumAddressValidationRule(allowEnsNames: true, requireChecksum: false);

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

    [Fact]
    public async Task ValidateAsync_WithEnsAndChecksumRequired_ShouldValidateBoth()
    {
        // Arrange
        var rule = new EthereumAddressValidationRule(allowEnsNames: true, requireChecksum: true);

        // Test ENS name (should pass)
        var ensResult = await rule.ValidateAsync("vitalik.eth");
        Assert.Empty(ensResult);

        // Test checksum address (should pass)
        var checksumResult = await rule.ValidateAsync("0x742d35Cc6634C0532925a3b8D4C9db96C4b4Db45");
        Assert.Empty(checksumResult);

        // Test lowercase address (should fail - checksum required)
        var lowercaseResult = await rule.ValidateAsync("0x742d35cc6634c0532925a3b8d4c9db96c4b4db45");
        Assert.NotEmpty(lowercaseResult);
    }

    [Theory]
    [InlineData("0x0000000000000000000000000000000000000000", true)] // Zero address
    [InlineData("0x000000000000000000000000000000000000dead", true)] // Dead address
    [InlineData("0xffffffffffffffffffffffffffffffffffffffff", true)] // Max address
    [InlineData("0x1234567890123456789012345678901234567890", true)] // Sequential address
    public async Task ValidateAsync_WithSpecialAddresses_ShouldValidateCorrectly(string address, bool isValid)
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
}