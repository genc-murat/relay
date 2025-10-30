using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.Security;
using System.Security.Cryptography;
using Xunit;

namespace Relay.MessageBroker.Tests.Security;

public class AesMessageEncryptorTests
{
    private readonly Mock<IKeyProvider> _keyProviderMock;
    private readonly Mock<IOptions<SecurityOptions>> _optionsMock;
    private readonly SecurityOptions _securityOptions;
    private readonly Mock<ILogger<AesMessageEncryptor>> _loggerMock;
    private readonly byte[] _testKey;

    public AesMessageEncryptorTests()
    {
        _keyProviderMock = new Mock<IKeyProvider>();
        _optionsMock = new Mock<IOptions<SecurityOptions>>();
        _securityOptions = new SecurityOptions
        {
            EnableEncryption = true,
            EncryptionAlgorithm = "AES256-GCM",
            KeyVersion = "v1",
            EncryptionKey = Convert.ToBase64String(new byte[32]) // Dummy key for validation
        };
        _optionsMock.Setup(o => o.Value).Returns(_securityOptions);
        _loggerMock = new Mock<ILogger<AesMessageEncryptor>>();
        _testKey = new byte[32];
        RandomNumberGenerator.Fill(_testKey);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var encryptor = new AesMessageEncryptor(_optionsMock.Object, _keyProviderMock.Object, _loggerMock.Object);

        // Assert
        Assert.NotNull(encryptor);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AesMessageEncryptor(null!, _keyProviderMock.Object, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullKeyProvider_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AesMessageEncryptor(_optionsMock.Object, null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AesMessageEncryptor(_optionsMock.Object, _keyProviderMock.Object, null!));
    }

    [Fact]
    public async Task EncryptAsync_WithValidData_ShouldReturnEncryptedData()
    {
        // Arrange
        var encryptor = new AesMessageEncryptor(_optionsMock.Object, _keyProviderMock.Object, _loggerMock.Object);
        var plainData = "Hello, World!"u8.ToArray();
        _keyProviderMock.Setup(kp => kp.GetKeyAsync("v1", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(_testKey));

        // Act
        var encryptedData = await encryptor.EncryptAsync(plainData);

        // Assert
        Assert.NotNull(encryptedData);
        Assert.True(encryptedData.Length > plainData.Length);
        // Encrypted data should be larger due to nonce (12 bytes) + tag (16 bytes)
        Assert.Equal(plainData.Length + 12 + 16, encryptedData.Length);
    }

    [Fact]
    public async Task EncryptAsync_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var encryptor = new AesMessageEncryptor(_optionsMock.Object, _keyProviderMock.Object, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await encryptor.EncryptAsync(null!));
    }

    [Fact]
    public async Task EncryptAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var encryptor = new AesMessageEncryptor(_optionsMock.Object, _keyProviderMock.Object, _loggerMock.Object);
        await encryptor.DisposeAsync();
        var plainData = "Hello, World!"u8.ToArray();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await encryptor.EncryptAsync(plainData));
    }

    [Fact]
    public async Task DecryptAsync_WithValidEncryptedData_ShouldReturnOriginalData()
    {
        // Arrange
        var encryptor = new AesMessageEncryptor(_optionsMock.Object, _keyProviderMock.Object, _loggerMock.Object);
        var plainData = "Hello, World!"u8.ToArray();
        _keyProviderMock.Setup(kp => kp.GetKeyAsync("v1", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(_testKey));

        // Encrypt first
        var encryptedData = await encryptor.EncryptAsync(plainData);

        // Act
        var decryptedData = await encryptor.DecryptAsync(encryptedData);

        // Assert
        Assert.Equal(plainData, decryptedData);
    }

    [Fact]
    public async Task DecryptAsync_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var encryptor = new AesMessageEncryptor(_optionsMock.Object, _keyProviderMock.Object, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await encryptor.DecryptAsync(null!));
    }

    [Fact]
    public async Task DecryptAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var encryptor = new AesMessageEncryptor(_optionsMock.Object, _keyProviderMock.Object, _loggerMock.Object);
        await encryptor.DisposeAsync();
        var encryptedData = new byte[32];

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await encryptor.DecryptAsync(encryptedData));
    }

    [Fact]
    public async Task DecryptAsync_WithInvalidData_ShouldThrowEncryptionException()
    {
        // Arrange
        var encryptor = new AesMessageEncryptor(_optionsMock.Object, _keyProviderMock.Object, _loggerMock.Object);
        var invalidData = new byte[10]; // Too short
        _keyProviderMock.Setup(kp => kp.GetKeyAsync("v1", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(_testKey));

        // Act & Assert
        await Assert.ThrowsAsync<EncryptionException>(async () => await encryptor.DecryptAsync(invalidData));
    }

    [Fact]
    public async Task DecryptAsync_WithWrongKey_ShouldThrowEncryptionException()
    {
        // Arrange
        var encryptor = new AesMessageEncryptor(_optionsMock.Object, _keyProviderMock.Object, _loggerMock.Object);
        var plainData = "Hello, World!"u8.ToArray();
        var wrongKey = new byte[32];
        RandomNumberGenerator.Fill(wrongKey);

        _keyProviderMock.Setup(kp => kp.GetKeyAsync("v1", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(_testKey)); // Use correct key for encryption

        // Encrypt with correct key
        var encryptedData = await encryptor.EncryptAsync(plainData);

        // Change mock to return wrong key for decryption
        _keyProviderMock.Setup(kp => kp.GetKeyAsync("v1", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(wrongKey));

        // Act & Assert
        await Assert.ThrowsAsync<EncryptionException>(async () => await encryptor.DecryptAsync(encryptedData));
    }

    [Fact]
    public async Task DecryptAsync_WithKeyRotation_ShouldTryPreviousKeys()
    {
        // Arrange
        var encryptor = new AesMessageEncryptor(_optionsMock.Object, _keyProviderMock.Object, _loggerMock.Object);
        var plainData = "Hello, World!"u8.ToArray();
        var oldKey = new byte[32];
        RandomNumberGenerator.Fill(oldKey);

        // Encrypt with old key
        _keyProviderMock.Setup(kp => kp.GetKeyAsync("v1", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(oldKey));
        var encryptedData = await encryptor.EncryptAsync(plainData);

        // Setup current key to be different, and previous versions to include v1
        var currentKey = new byte[32];
        RandomNumberGenerator.Fill(currentKey);
        _keyProviderMock.Setup(kp => kp.GetKeyAsync("v2", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(currentKey));
        _keyProviderMock.Setup(kp => kp.GetPreviousKeyVersionsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<IReadOnlyList<string>>(new[] { "v1" }));

        // Change options to current version
        _securityOptions.KeyVersion = "v2";

        // Act
        var decryptedData = await encryptor.DecryptAsync(encryptedData);

        // Assert
        Assert.Equal(plainData, decryptedData);
    }

    [Fact]
    public void GetKeyVersion_ShouldReturnCurrentKeyVersion()
    {
        // Arrange
        var encryptor = new AesMessageEncryptor(_optionsMock.Object, _keyProviderMock.Object, _loggerMock.Object);

        // Act
        var keyVersion = encryptor.GetKeyVersion();

        // Assert
        Assert.Equal("v1", keyVersion);
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeKeyProvider_WhenAsyncDisposable()
    {
        // Arrange
        var asyncDisposableMock = new Mock<IKeyProvider>();
        asyncDisposableMock.As<IAsyncDisposable>();
        var encryptor = new AesMessageEncryptor(_optionsMock.Object, asyncDisposableMock.Object, _loggerMock.Object);

        // Act
        await encryptor.DisposeAsync();

        // Assert
        asyncDisposableMock.As<IAsyncDisposable>().Verify(ad => ad.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeKeyProvider_WhenDisposable()
    {
        // Arrange
        var disposableMock = new Mock<IKeyProvider>();
        disposableMock.As<IDisposable>();
        var encryptor = new AesMessageEncryptor(_optionsMock.Object, disposableMock.Object, _loggerMock.Object);

        // Act
        await encryptor.DisposeAsync();

        // Assert
        disposableMock.As<IDisposable>().Verify(d => d.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_MultipleCalls_ShouldNotThrow()
    {
        // Arrange
        var encryptor = new AesMessageEncryptor(_optionsMock.Object, _keyProviderMock.Object, _loggerMock.Object);

        // Act
        await encryptor.DisposeAsync();
        await encryptor.DisposeAsync();

        // Assert - No exception thrown
    }

    [Fact]
    public async Task EncryptAsync_WithEmptyData_ShouldWork()
    {
        // Arrange
        var encryptor = new AesMessageEncryptor(_optionsMock.Object, _keyProviderMock.Object, _loggerMock.Object);
        var emptyData = Array.Empty<byte>();
        _keyProviderMock.Setup(kp => kp.GetKeyAsync("v1", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(_testKey));

        // Act
        var encryptedData = await encryptor.EncryptAsync(emptyData);
        var decryptedData = await encryptor.DecryptAsync(encryptedData);

        // Assert
        Assert.NotNull(encryptedData);
        Assert.Equal(12 + 16, encryptedData.Length); // nonce + tag
        Assert.Equal(emptyData, decryptedData);
    }

    [Fact]
    public async Task EncryptAsync_WithLargeData_ShouldWork()
    {
        // Arrange
        var encryptor = new AesMessageEncryptor(_optionsMock.Object, _keyProviderMock.Object, _loggerMock.Object);
        var largeData = new byte[1024 * 1024]; // 1 MB
        RandomNumberGenerator.Fill(largeData);
        _keyProviderMock.Setup(kp => kp.GetKeyAsync("v1", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(_testKey));

        // Act
        var encryptedData = await encryptor.EncryptAsync(largeData);
        var decryptedData = await encryptor.DecryptAsync(encryptedData);

        // Assert
        Assert.NotNull(encryptedData);
        Assert.Equal(largeData.Length + 12 + 16, encryptedData.Length);
        Assert.Equal(largeData, decryptedData);
    }

    [Fact]
    public async Task DecryptAsync_WithTamperedData_ShouldThrow()
    {
        // Arrange
        var encryptor = new AesMessageEncryptor(_optionsMock.Object, _keyProviderMock.Object, _loggerMock.Object);
        var plainData = "Hello, World!"u8.ToArray();
        _keyProviderMock.Setup(kp => kp.GetKeyAsync("v1", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(_testKey));

        // Encrypt
        var encryptedData = await encryptor.EncryptAsync(plainData);

        // Tamper with data
        encryptedData[0] ^= 1;

        // Act & Assert
        await Assert.ThrowsAsync<EncryptionException>(async () => await encryptor.DecryptAsync(encryptedData));
    }

    [Fact]
    public async Task Constructor_WithInvalidOptions_ShouldThrowDuringValidation()
    {
        // Arrange
        var invalidOptions = new SecurityOptions
        {
            EnableEncryption = true,
            KeyVersion = "",
            KeyRotationGracePeriod = TimeSpan.FromHours(-1)
        };
        var invalidOptionsMock = new Mock<IOptions<SecurityOptions>>();
        invalidOptionsMock.Setup(o => o.Value).Returns(invalidOptions);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new AesMessageEncryptor(invalidOptionsMock.Object, _keyProviderMock.Object, _loggerMock.Object));
    }
}
