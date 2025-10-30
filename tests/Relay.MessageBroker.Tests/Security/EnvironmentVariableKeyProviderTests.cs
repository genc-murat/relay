using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.Security;
using System.Security.Cryptography;
using Xunit;

namespace Relay.MessageBroker.Tests.Security;

public class EnvironmentVariableKeyProviderTests
{
    private readonly Mock<IOptions<SecurityOptions>> _optionsMock;
    private readonly SecurityOptions _securityOptions;
    private readonly Mock<ILogger<EnvironmentVariableKeyProvider>> _loggerMock;

    public EnvironmentVariableKeyProviderTests()
    {
        _optionsMock = new Mock<IOptions<SecurityOptions>>();
        _securityOptions = new SecurityOptions
        {
            KeyVersion = "v1",
            EncryptionKey = Convert.ToBase64String(new byte[32])
        };
        _optionsMock.Setup(o => o.Value).Returns(_securityOptions);
        _loggerMock = new Mock<ILogger<EnvironmentVariableKeyProvider>>();
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var provider = new EnvironmentVariableKeyProvider(_optionsMock.Object, _loggerMock.Object);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EnvironmentVariableKeyProvider(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EnvironmentVariableKeyProvider(_optionsMock.Object, null!));
    }

    [Fact]
    public void Constructor_WithNullOptionsValue_ShouldThrowArgumentNullException()
    {
        // Arrange
        _optionsMock.Setup(o => o.Value).Returns((SecurityOptions)null!);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EnvironmentVariableKeyProvider(_optionsMock.Object, _loggerMock.Object));
    }

    [Fact]
    public async Task GetKeyAsync_WithNullKeyVersion_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = new EnvironmentVariableKeyProvider(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await provider.GetKeyAsync(null!));
    }

    [Fact]
    public async Task GetKeyAsync_WithEmptyKeyVersion_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new EnvironmentVariableKeyProvider(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await provider.GetKeyAsync(string.Empty));
    }

    [Fact]
    public async Task GetKeyAsync_WithWhitespaceKeyVersion_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new EnvironmentVariableKeyProvider(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await provider.GetKeyAsync("   "));
    }

    [Fact]
    public async Task GetKeyAsync_WithKeyFromVersionSpecificEnvironmentVariable_ShouldReturnKey()
    {
        // Arrange
        var provider = new EnvironmentVariableKeyProvider(_optionsMock.Object, _loggerMock.Object);
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
    public async Task GetKeyAsync_WithKeyFromGenericEnvironmentVariable_ShouldReturnKey()
    {
        // Arrange
        var provider = new EnvironmentVariableKeyProvider(_optionsMock.Object, _loggerMock.Object);
        var expectedKey = new byte[32];
        RandomNumberGenerator.Fill(expectedKey);
        var keyBase64 = Convert.ToBase64String(expectedKey);

        // Set generic environment variable
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
        var provider = new EnvironmentVariableKeyProvider(_optionsMock.Object, _loggerMock.Object);
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
        var provider = new EnvironmentVariableKeyProvider(_optionsMock.Object, _loggerMock.Object);
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
        var provider = new EnvironmentVariableKeyProvider(_optionsMock.Object, _loggerMock.Object);
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
        var provider = new EnvironmentVariableKeyProvider(_optionsMock.Object, _loggerMock.Object);
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
        var keyVersion = "v1";
        var expectedKey = new byte[32];
        RandomNumberGenerator.Fill(expectedKey);
        
        // Clean environment variables before test to avoid interference
        var envVarName = $"RELAY_ENCRYPTION_KEY_{keyVersion.ToUpperInvariant().Replace(".", "_")}";
        var originalEnvVar = Environment.GetEnvironmentVariable(envVarName);
        Environment.SetEnvironmentVariable(envVarName, null);
        
        var genericEnvVar = Environment.GetEnvironmentVariable("RELAY_ENCRYPTION_KEY");
        Environment.SetEnvironmentVariable("RELAY_ENCRYPTION_KEY", null);

        try
        {
            // Create two separate options objects
            var optionsWithKey = new SecurityOptions
            {
                KeyVersion = keyVersion,
                EncryptionKey = Convert.ToBase64String(expectedKey)
            };
            
            var optionsWithoutKey = new SecurityOptions
            {
                KeyVersion = keyVersion,
                EncryptionKey = null
            };
            
            var testOptionsMock = new Mock<IOptions<SecurityOptions>>();
            var shouldReturnOptionsWithKey = true;
            
            // Set up mock to return the options conditionally based on a flag
            testOptionsMock.Setup(o => o.Value)
                .Returns(() => shouldReturnOptionsWithKey ? optionsWithKey : optionsWithoutKey);
            
            var provider = new EnvironmentVariableKeyProvider(testOptionsMock.Object, _loggerMock.Object);

            // First call to cache the key - use the options with key
            shouldReturnOptionsWithKey = true;
            await provider.GetKeyAsync(keyVersion);

            // Second call should use cached value - change options to not have key
            shouldReturnOptionsWithKey = false;

            // Act - this should return the cached key
            var result = await provider.GetKeyAsync(keyVersion);

            // Assert
            Assert.Equal(expectedKey, result);
        }
        finally
        {
            // Restore original environment variables
            Environment.SetEnvironmentVariable(envVarName, originalEnvVar);
            Environment.SetEnvironmentVariable("RELAY_ENCRYPTION_KEY", genericEnvVar);
        }
    }

    [Fact]
    public async Task GetPreviousKeyVersionsAsync_WithV2CurrentVersion_ShouldReturnV1()
    {
        // Arrange
        var provider = new EnvironmentVariableKeyProvider(_optionsMock.Object, _loggerMock.Object);
        _securityOptions.KeyVersion = "v2";
        var keyBase64 = Convert.ToBase64String(new byte[32]);

        // Set environment variable for v1
        Environment.SetEnvironmentVariable("RELAY_ENCRYPTION_KEY_V1", keyBase64);

        try
        {
            // Act
            var result = await provider.GetPreviousKeyVersionsAsync(TimeSpan.FromHours(1));

            // Assert
            Assert.Contains("v1", result);
            Assert.DoesNotContain("v2", result);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("RELAY_ENCRYPTION_KEY_V1", null);
        }
    }

    [Fact]
    public async Task GetPreviousKeyVersionsAsync_WithSemanticVersion_ShouldReturnPreviousVersions()
    {
        // Arrange
        var provider = new EnvironmentVariableKeyProvider(_optionsMock.Object, _loggerMock.Object);
        _securityOptions.KeyVersion = "2.0";
        var keyBase64 = Convert.ToBase64String(new byte[32]);

        // Set environment variables for previous versions
        Environment.SetEnvironmentVariable("RELAY_ENCRYPTION_KEY_1_0", keyBase64);

        try
        {
            // Act
            var result = await provider.GetPreviousKeyVersionsAsync(TimeSpan.FromHours(1));

            // Assert
            Assert.Contains("1.0", result);
            Assert.DoesNotContain("2.0", result);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("RELAY_ENCRYPTION_KEY_1_0", null);
        }
    }

    [Fact]
    public async Task GetPreviousKeyVersionsAsync_WithNoPreviousVersions_ShouldReturnEmptyList()
    {
        // Arrange
        var provider = new EnvironmentVariableKeyProvider(_optionsMock.Object, _loggerMock.Object);
        _securityOptions.KeyVersion = "v1";

        // Act
        var result = await provider.GetPreviousKeyVersionsAsync(TimeSpan.FromHours(1));

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPreviousKeyVersionsAsync_WithV3CurrentVersion_ShouldReturnV1AndV2()
    {
        // Arrange
        var provider = new EnvironmentVariableKeyProvider(_optionsMock.Object, _loggerMock.Object);
        _securityOptions.KeyVersion = "v3";
        var keyBase64 = Convert.ToBase64String(new byte[32]);

        // Set environment variables for v1 and v2
        Environment.SetEnvironmentVariable("RELAY_ENCRYPTION_KEY_V1", keyBase64);
        Environment.SetEnvironmentVariable("RELAY_ENCRYPTION_KEY_V2", keyBase64);

        try
        {
            // Act
            var result = await provider.GetPreviousKeyVersionsAsync(TimeSpan.FromHours(1));

            // Assert
            Assert.Contains("v1", result);
            Assert.Contains("v2", result);
            Assert.DoesNotContain("v3", result);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("RELAY_ENCRYPTION_KEY_V1", null);
            Environment.SetEnvironmentVariable("RELAY_ENCRYPTION_KEY_V2", null);
        }
    }
}