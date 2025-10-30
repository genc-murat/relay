using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.Security;
using System.Security.Cryptography;
using Xunit;

namespace Relay.MessageBroker.Tests.Security;

public class AzureKeyVaultKeyProviderTests
{
    private readonly Mock<IOptions<SecurityOptions>> _optionsMock;
    private readonly SecurityOptions _securityOptions;
    private readonly Mock<ILogger<AzureKeyVaultKeyProvider>> _loggerMock;

    public AzureKeyVaultKeyProviderTests()
    {
        _optionsMock = new Mock<IOptions<SecurityOptions>>();
        _securityOptions = new SecurityOptions
        {
            KeyVaultUrl = "https://test.vault.azure.net/",
            KeyVersion = "v1",
            EncryptionKey = Convert.ToBase64String(new byte[32])
        };
        _optionsMock.Setup(o => o.Value).Returns(_securityOptions);
        _loggerMock = new Mock<ILogger<AzureKeyVaultKeyProvider>>();
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var provider = new AzureKeyVaultKeyProvider(_optionsMock.Object, _loggerMock.Object);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AzureKeyVaultKeyProvider(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AzureKeyVaultKeyProvider(_optionsMock.Object, null!));
    }

    [Fact]
    public void Constructor_WithNullOptionsValue_ShouldThrowArgumentNullException()
    {
        // Arrange
        _optionsMock.Setup(o => o.Value).Returns((SecurityOptions)null!);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AzureKeyVaultKeyProvider(_optionsMock.Object, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLoggerValue_ShouldThrowArgumentNullException()
    {
        // Arrange
        var nullLogger = (ILogger<AzureKeyVaultKeyProvider>)null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AzureKeyVaultKeyProvider(_optionsMock.Object, nullLogger));
    }

    [Fact]
    public void Constructor_WithMissingKeyVaultUrl_ShouldThrowArgumentException()
    {
        // Arrange
        _securityOptions.KeyVaultUrl = null;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new AzureKeyVaultKeyProvider(_optionsMock.Object, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithEmptyKeyVaultUrl_ShouldThrowArgumentException()
    {
        // Arrange
        _securityOptions.KeyVaultUrl = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new AzureKeyVaultKeyProvider(_optionsMock.Object, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithWhitespaceKeyVaultUrl_ShouldThrowArgumentException()
    {
        // Arrange
        _securityOptions.KeyVaultUrl = "   ";

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new AzureKeyVaultKeyProvider(_optionsMock.Object, _loggerMock.Object));
    }

    [Fact]
    public async Task GetKeyAsync_WithNullKeyVersion_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = new AzureKeyVaultKeyProvider(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await provider.GetKeyAsync(null!));
    }

    [Fact]
    public async Task GetKeyAsync_WithEmptyKeyVersion_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new AzureKeyVaultKeyProvider(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await provider.GetKeyAsync(string.Empty));
    }

    [Fact]
    public async Task GetKeyAsync_WithWhitespaceKeyVersion_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new AzureKeyVaultKeyProvider(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await provider.GetKeyAsync("   "));
    }

    [Fact]
    public async Task GetKeyAsync_WithKeyFromEnvironmentVariable_ShouldReturnKey()
    {
        // Arrange
        var provider = new AzureKeyVaultKeyProvider(_optionsMock.Object, _loggerMock.Object);
        var keyVersion = "v2";
        var expectedKey = new byte[32];
        RandomNumberGenerator.Fill(expectedKey);
        var keyBase64 = Convert.ToBase64String(expectedKey);

        // Set environment variable
        Environment.SetEnvironmentVariable($"RELAY_ENCRYPTION_KEY_{keyVersion.ToUpperInvariant()}", keyBase64);

        try
        {
            // Act
            var result = await provider.GetKeyAsync(keyVersion);

            // Assert
            Assert.Equal(expectedKey, result);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable($"RELAY_ENCRYPTION_KEY_{keyVersion.ToUpperInvariant()}", null);
        }
    }

    [Fact]
    public async Task GetKeyAsync_WithKeyFromDefaultEnvironmentVariable_ShouldReturnKey()
    {
        // Arrange
        var provider = new AzureKeyVaultKeyProvider(_optionsMock.Object, _loggerMock.Object);
        var expectedKey = new byte[32];
        RandomNumberGenerator.Fill(expectedKey);
        var keyBase64 = Convert.ToBase64String(expectedKey);

        // Set default environment variable
        Environment.SetEnvironmentVariable("RELAY_ENCRYPTION_KEY", keyBase64);

        try
        {
            // Act
            var result = await provider.GetKeyAsync(_securityOptions.KeyVersion);

            // Assert
            Assert.Equal(expectedKey, result);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("RELAY_ENCRYPTION_KEY", null);
        }
    }

    [Fact]
    public async Task GetKeyAsync_WithKeyFromOptions_ShouldReturnKey()
    {
        // Arrange
        var provider = new AzureKeyVaultKeyProvider(_optionsMock.Object, _loggerMock.Object);
        var expectedKey = new byte[32];
        RandomNumberGenerator.Fill(expectedKey);
        _securityOptions.EncryptionKey = Convert.ToBase64String(expectedKey);

        // Act
        var result = await provider.GetKeyAsync(_securityOptions.KeyVersion);

        // Assert
        Assert.Equal(expectedKey, result);
    }

    [Fact]
    public async Task GetKeyAsync_WithInvalidBase64_ShouldThrowEncryptionException()
    {
        // Arrange
        var provider = new AzureKeyVaultKeyProvider(_optionsMock.Object, _loggerMock.Object);
        _securityOptions.EncryptionKey = "invalid-base64";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EncryptionException>(async () =>
            await provider.GetKeyAsync(_securityOptions.KeyVersion));

        Assert.Contains("Invalid base64 format", exception.Message);
    }

    [Fact]
    public async Task GetKeyAsync_WithWrongKeySize_ShouldThrowEncryptionException()
    {
        // Arrange
        var provider = new AzureKeyVaultKeyProvider(_optionsMock.Object, _loggerMock.Object);
        var wrongSizeKey = new byte[16]; // Should be 32
        _securityOptions.EncryptionKey = Convert.ToBase64String(wrongSizeKey);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EncryptionException>(async () =>
            await provider.GetKeyAsync(_securityOptions.KeyVersion));

        Assert.Contains("Invalid key size", exception.Message);
        Assert.Contains("Expected 32 bytes", exception.Message);
    }

    [Fact]
    public async Task GetKeyAsync_WithNoKeyFound_ShouldThrowEncryptionException()
    {
        // Arrange
        var provider = new AzureKeyVaultKeyProvider(_optionsMock.Object, _loggerMock.Object);
        _securityOptions.EncryptionKey = null;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EncryptionException>(async () =>
            await provider.GetKeyAsync("nonexistent"));

        Assert.Contains("Encryption key not found", exception.Message);
    }

    [Fact]
    public async Task GetKeyAsync_CacheHit_ShouldReturnCachedKey()
    {
        // Arrange
        var provider = new AzureKeyVaultKeyProvider(_optionsMock.Object, _loggerMock.Object);
        var keyVersion = _securityOptions.KeyVersion;
        var expectedKey = new byte[32];
        RandomNumberGenerator.Fill(expectedKey);
        _securityOptions.EncryptionKey = Convert.ToBase64String(expectedKey);

        // First call to cache the key
        await provider.GetKeyAsync(keyVersion);

        // Modify options so if it wasn't cached, it would fail
        _securityOptions.EncryptionKey = null;

        // Act
        var result = await provider.GetKeyAsync(keyVersion);

        // Assert
        Assert.Equal(expectedKey, result);
    }



    [Fact]
    public async Task GetPreviousKeyVersionsAsync_WithEmptyCache_ShouldReturnEmptyList()
    {
        // Arrange
        var provider = new AzureKeyVaultKeyProvider(_optionsMock.Object, _loggerMock.Object);
        var gracePeriod = TimeSpan.FromHours(1);

        // Act
        var result = await provider.GetPreviousKeyVersionsAsync(gracePeriod);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPreviousKeyVersionsAsync_WithCachedKeysWithinGracePeriod_ShouldReturnVersions()
    {
        // Arrange
        var provider = new AzureKeyVaultKeyProvider(_optionsMock.Object, _loggerMock.Object);
        var gracePeriod = TimeSpan.FromHours(1);
        var keyBase64 = Convert.ToBase64String(new byte[32]);

        // Set encryption key for fallback
        _securityOptions.EncryptionKey = keyBase64;
        _securityOptions.KeyVersion = "v3"; // Set current to v3 so v1 and v2 are previous

        // Set env vars for v1 and v2
        Environment.SetEnvironmentVariable("RELAY_ENCRYPTION_KEY_V1", keyBase64);
        Environment.SetEnvironmentVariable("RELAY_ENCRYPTION_KEY_V2", keyBase64);

        try
        {
            // Cache some keys
            await provider.GetKeyAsync("v1");
            await provider.GetKeyAsync("v2");

            // Act
            var result = await provider.GetPreviousKeyVersionsAsync(gracePeriod);

            // Assert
            Assert.Contains("v1", result);
            Assert.Contains("v2", result);
            // Should not include current version
            Assert.DoesNotContain(_securityOptions.KeyVersion, result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("RELAY_ENCRYPTION_KEY_V1", null);
            Environment.SetEnvironmentVariable("RELAY_ENCRYPTION_KEY_V2", null);
            _securityOptions.KeyVersion = "v1"; // Reset
        }
    }

    [Fact]
    public async Task GetPreviousKeyVersionsAsync_WithCachedKeysOutsideGracePeriod_ShouldReturnEmptyList()
    {
        // Arrange
        var provider = new AzureKeyVaultKeyProvider(_optionsMock.Object, _loggerMock.Object);
        var gracePeriod = TimeSpan.FromSeconds(1);
        var keyBase64 = Convert.ToBase64String(new byte[32]);

        // Set encryption key for fallback
        _securityOptions.EncryptionKey = keyBase64;
        _securityOptions.KeyVersion = "v3"; // Set current to v3 so v1 and v2 are previous

        // Set env vars for v1 and v2
        Environment.SetEnvironmentVariable("RELAY_ENCRYPTION_KEY_V1", keyBase64);
        Environment.SetEnvironmentVariable("RELAY_ENCRYPTION_KEY_V2", keyBase64);

        try
        {
            // Cache some keys
            await provider.GetKeyAsync("v1");
            await provider.GetKeyAsync("v2");

            // Wait for grace period to expire
            await Task.Delay(1100);

            // Act
            var result = await provider.GetPreviousKeyVersionsAsync(gracePeriod);

            // Assert
            Assert.Empty(result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("RELAY_ENCRYPTION_KEY_V1", null);
            Environment.SetEnvironmentVariable("RELAY_ENCRYPTION_KEY_V2", null);
            _securityOptions.KeyVersion = "v1"; // Reset
        }
    }
}