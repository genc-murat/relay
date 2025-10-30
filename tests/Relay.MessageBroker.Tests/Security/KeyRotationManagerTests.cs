using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.Security;
using Xunit;

namespace Relay.MessageBroker.Tests.Security;

public class KeyRotationManagerTests
{
    private readonly Mock<IOptions<SecurityOptions>> _optionsMock;
    private readonly Mock<ILogger<KeyRotationManager>> _loggerMock;
    private readonly SecurityOptions _securityOptions;

    public KeyRotationManagerTests()
    {
        _optionsMock = new Mock<IOptions<SecurityOptions>>();
        _loggerMock = new Mock<ILogger<KeyRotationManager>>();
        _securityOptions = new SecurityOptions
        {
            EnableEncryption = true,
            KeyVersion = "v1",
            KeyRotationGracePeriod = TimeSpan.FromHours(24),
            EncryptionKey = Convert.ToBase64String(new byte[32]) // Dummy key
        };
        _optionsMock.Setup(o => o.Value).Returns(_securityOptions);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var manager = new KeyRotationManager(_optionsMock.Object, _loggerMock.Object);

        // Assert
        Assert.NotNull(manager);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new KeyRotationManager(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new KeyRotationManager(_optionsMock.Object, null!));
    }

    [Fact]
    public void Constructor_ShouldRegisterCurrentKeyVersion()
    {
        // Arrange & Act
        var manager = new KeyRotationManager(_optionsMock.Object, _loggerMock.Object);

        // Assert
        var metadata = manager.GetKeyVersionMetadata("v1");
        Assert.NotNull(metadata);
        Assert.Equal("v1", metadata.Version);
        Assert.True(metadata.IsActive);
    }

    [Fact]
    public void RegisterKeyVersion_WithValidParameters_ShouldRegisterSuccessfully()
    {
        // Arrange
        var manager = new KeyRotationManager(_optionsMock.Object, _loggerMock.Object);
        var activatedAt = DateTimeOffset.UtcNow.AddHours(-1);

        // Act
        manager.RegisterKeyVersion("v2", activatedAt);

        // Assert
        var metadata = manager.GetKeyVersionMetadata("v2");
        Assert.NotNull(metadata);
        Assert.Equal("v2", metadata.Version);
        Assert.Equal(activatedAt, metadata.ActivatedAt);
        Assert.False(metadata.IsActive); // Not the current version
    }

    [Fact]
    public void RegisterKeyVersion_WithNullKeyVersion_ShouldThrowArgumentNullException()
    {
        // Arrange
        var manager = new KeyRotationManager(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            manager.RegisterKeyVersion(null!, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RegisterKeyVersion_WithEmptyKeyVersion_ShouldThrowArgumentException()
    {
        // Arrange
        var manager = new KeyRotationManager(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            manager.RegisterKeyVersion("", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RegisterKeyVersion_WithWhitespaceKeyVersion_ShouldThrowArgumentException()
    {
        // Arrange
        var manager = new KeyRotationManager(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            manager.RegisterKeyVersion("   ", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RegisterKeyVersion_WithCurrentKeyVersion_ShouldMarkAsActive()
    {
        // Arrange
        var manager = new KeyRotationManager(_optionsMock.Object, _loggerMock.Object);

        // Act
        manager.RegisterKeyVersion("v1", DateTimeOffset.UtcNow);

        // Assert
        var metadata = manager.GetKeyVersionMetadata("v1");
        Assert.NotNull(metadata);
        Assert.True(metadata.IsActive);
    }

    [Fact]
    public void IsKeyVersionValid_WithCurrentKeyVersion_ShouldReturnTrue()
    {
        // Arrange
        var manager = new KeyRotationManager(_optionsMock.Object, _loggerMock.Object);

        // Act
        var result = manager.IsKeyVersionValid("v1");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsKeyVersionValid_WithNullKeyVersion_ShouldThrowArgumentNullException()
    {
        // Arrange
        var manager = new KeyRotationManager(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            manager.IsKeyVersionValid(null!));
    }

    [Fact]
    public void IsKeyVersionValid_WithEmptyKeyVersion_ShouldThrowArgumentException()
    {
        // Arrange
        var manager = new KeyRotationManager(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            manager.IsKeyVersionValid(""));
    }

    [Fact]
    public void IsKeyVersionValid_WithUnregisteredKeyVersion_ShouldReturnFalse()
    {
        // Arrange
        var manager = new KeyRotationManager(_optionsMock.Object, _loggerMock.Object);

        // Act
        var result = manager.IsKeyVersionValid("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsKeyVersionValid_WithKeyWithinGracePeriod_ShouldReturnTrue()
    {
        // Arrange
        var manager = new KeyRotationManager(_optionsMock.Object, _loggerMock.Object);
        var activatedAt = DateTimeOffset.UtcNow.AddHours(-12); // Within 24-hour grace period
        manager.RegisterKeyVersion("v2", activatedAt);

        // Act
        var result = manager.IsKeyVersionValid("v2");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsKeyVersionValid_WithKeyOutsideGracePeriod_ShouldReturnFalse()
    {
        // Arrange
        var manager = new KeyRotationManager(_optionsMock.Object, _loggerMock.Object);
        var activatedAt = DateTimeOffset.UtcNow.AddHours(-48); // Outside 24-hour grace period
        manager.RegisterKeyVersion("v2", activatedAt);

        // Act
        var result = manager.IsKeyVersionValid("v2");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetValidKeyVersions_ShouldReturnCurrentAndValidVersions()
    {
        // Arrange
        var manager = new KeyRotationManager(_optionsMock.Object, _loggerMock.Object);
        var recentActivatedAt = DateTimeOffset.UtcNow.AddHours(-12); // Within grace period
        var oldActivatedAt = DateTimeOffset.UtcNow.AddHours(-48); // Outside grace period

        manager.RegisterKeyVersion("v2", recentActivatedAt);
        manager.RegisterKeyVersion("v3", oldActivatedAt);

        // Act
        var validVersions = manager.GetValidKeyVersions();

        // Assert
        Assert.Contains("v1", validVersions); // Current version
        Assert.Contains("v2", validVersions); // Within grace period
        Assert.DoesNotContain("v3", validVersions); // Outside grace period
        Assert.Equal("v1", validVersions[0]); // Current version should be first
    }

    [Fact]
    public void GetKeyVersionMetadata_WithExistingKeyVersion_ShouldReturnMetadata()
    {
        // Arrange
        var manager = new KeyRotationManager(_optionsMock.Object, _loggerMock.Object);
        var activatedAt = DateTimeOffset.UtcNow.AddHours(-1);
        manager.RegisterKeyVersion("v2", activatedAt);

        // Act
        var metadata = manager.GetKeyVersionMetadata("v2");

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("v2", metadata.Version);
        Assert.Equal(activatedAt, metadata.ActivatedAt);
        Assert.False(metadata.IsActive);
    }

    [Fact]
    public void GetKeyVersionMetadata_WithNonExistingKeyVersion_ShouldReturnNull()
    {
        // Arrange
        var manager = new KeyRotationManager(_optionsMock.Object, _loggerMock.Object);

        // Act
        var metadata = manager.GetKeyVersionMetadata("nonexistent");

        // Assert
        Assert.Null(metadata);
    }

    [Fact]
    public void GetKeyVersionMetadata_WithNullKeyVersion_ShouldThrowArgumentNullException()
    {
        // Arrange
        var manager = new KeyRotationManager(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            manager.GetKeyVersionMetadata(null!));
    }

    [Fact]
    public void GetKeyVersionMetadata_WithEmptyKeyVersion_ShouldThrowArgumentException()
    {
        // Arrange
        var manager = new KeyRotationManager(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            manager.GetKeyVersionMetadata(""));
    }

    [Fact]
    public void CleanupExpiredKeyVersions_ShouldRemoveExpiredVersions()
    {
        // Arrange
        var manager = new KeyRotationManager(_optionsMock.Object, _loggerMock.Object);
        var recentActivatedAt = DateTimeOffset.UtcNow.AddHours(-12); // Within grace period
        var oldActivatedAt = DateTimeOffset.UtcNow.AddHours(-48); // Outside grace period

        manager.RegisterKeyVersion("v2", recentActivatedAt);
        manager.RegisterKeyVersion("v3", oldActivatedAt);

        // Act
        var removedCount = manager.CleanupExpiredKeyVersions();

        // Assert
        Assert.Equal(1, removedCount); // Only v3 should be removed
        Assert.NotNull(manager.GetKeyVersionMetadata("v1")); // Current version remains
        Assert.NotNull(manager.GetKeyVersionMetadata("v2")); // Recent version remains
        Assert.Null(manager.GetKeyVersionMetadata("v3")); // Old version removed
    }

    [Fact]
    public void CleanupExpiredKeyVersions_WithNoExpiredVersions_ShouldReturnZero()
    {
        // Arrange
        var manager = new KeyRotationManager(_optionsMock.Object, _loggerMock.Object);
        var recentActivatedAt = DateTimeOffset.UtcNow.AddHours(-12); // Within grace period
        manager.RegisterKeyVersion("v2", recentActivatedAt);

        // Act
        var removedCount = manager.CleanupExpiredKeyVersions();

        // Assert
        Assert.Equal(0, removedCount);
    }
}

public class KeyVersionMetadataTests
{
    [Fact]
    public void GetExpirationTime_ShouldReturnCorrectExpiration()
    {
        // Arrange
        var metadata = new KeyVersionMetadata
        {
            Version = "v1",
            ActivatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            IsActive = true
        };
        var gracePeriod = TimeSpan.FromHours(24);

        // Act
        var expirationTime = metadata.GetExpirationTime(gracePeriod);

        // Assert
        Assert.Equal(metadata.ActivatedAt + gracePeriod, expirationTime);
    }

    [Fact]
    public void IsExpired_WithExpiredKey_ShouldReturnTrue()
    {
        // Arrange
        var metadata = new KeyVersionMetadata
        {
            Version = "v1",
            ActivatedAt = DateTimeOffset.UtcNow.AddHours(-48), // 48 hours ago
            IsActive = false
        };
        var gracePeriod = TimeSpan.FromHours(24);

        // Act
        var isExpired = metadata.IsExpired(gracePeriod);

        // Assert
        Assert.True(isExpired);
    }

    [Fact]
    public void IsExpired_WithValidKey_ShouldReturnFalse()
    {
        // Arrange
        var metadata = new KeyVersionMetadata
        {
            Version = "v1",
            ActivatedAt = DateTimeOffset.UtcNow.AddHours(-12), // 12 hours ago
            IsActive = false
        };
        var gracePeriod = TimeSpan.FromHours(24);

        // Act
        var isExpired = metadata.IsExpired(gracePeriod);

        // Assert
        Assert.False(isExpired);
    }

    [Fact]
    public void IsExpired_WithActiveKey_ShouldReturnTrueIfPastGracePeriod()
    {
        // Arrange
        var metadata = new KeyVersionMetadata
        {
            Version = "v1",
            ActivatedAt = DateTimeOffset.UtcNow.AddHours(-48), // Past grace period
            IsActive = true
        };
        var gracePeriod = TimeSpan.FromHours(24);

        // Act
        var isExpired = metadata.IsExpired(gracePeriod);

        // Assert
        Assert.True(isExpired); // IsExpired only checks time, not active status
    }
}