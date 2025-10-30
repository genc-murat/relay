using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker;
using Relay.MessageBroker.Security;
using System.Security.Cryptography;
using Xunit;

namespace Relay.MessageBroker.Tests.Security;

public class EncryptionMessageBrokerDecoratorTests
{
    private readonly Mock<IMessageBroker> _innerBrokerMock;
    private readonly Mock<IMessageEncryptor> _encryptorMock;
    private readonly Mock<IOptions<SecurityOptions>> _optionsMock;
    private readonly SecurityOptions _securityOptions;
    private readonly Mock<ILogger<EncryptionMessageBrokerDecorator>> _loggerMock;

    public EncryptionMessageBrokerDecoratorTests()
    {
        _innerBrokerMock = new Mock<IMessageBroker>();
        _encryptorMock = new Mock<IMessageEncryptor>();
        _optionsMock = new Mock<IOptions<SecurityOptions>>();
        _securityOptions = new SecurityOptions
        {
            EnableEncryption = true,
            EncryptionAlgorithm = "AES256-GCM",
            KeyVersion = "v1",
            EncryptionKey = Convert.ToBase64String(new byte[32])
        };
        _optionsMock.Setup(o => o.Value).Returns(_securityOptions);
        _loggerMock = new Mock<ILogger<EncryptionMessageBrokerDecorator>>();
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var decorator = new EncryptionMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        // Assert
        Assert.NotNull(decorator);
    }

    [Fact]
    public void Constructor_WithNullInnerBroker_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EncryptionMessageBrokerDecorator(
                null!,
                _encryptorMock.Object,
                _optionsMock.Object,
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullEncryptor_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EncryptionMessageBrokerDecorator(
                _innerBrokerMock.Object,
                null!,
                _optionsMock.Object,
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EncryptionMessageBrokerDecorator(
                _innerBrokerMock.Object,
                _encryptorMock.Object,
                null!,
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EncryptionMessageBrokerDecorator(
                _innerBrokerMock.Object,
                _encryptorMock.Object,
                _optionsMock.Object,
                null!));
    }

    [Fact]
    public void Constructor_WithNullOptionsValue_ShouldThrowArgumentNullException()
    {
        // Arrange
        _optionsMock.Setup(o => o.Value).Returns((SecurityOptions)null!);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EncryptionMessageBrokerDecorator(
                _innerBrokerMock.Object,
                _encryptorMock.Object,
                _optionsMock.Object,
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithInvalidOptions_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _securityOptions.EnableEncryption = true;
        _securityOptions.EncryptionKey = null;
        _securityOptions.KeyVaultUrl = null;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new EncryptionMessageBrokerDecorator(
                _innerBrokerMock.Object,
                _encryptorMock.Object,
                _optionsMock.Object,
                _loggerMock.Object));
    }

    [Fact]
    public async Task PublishAsync_WithEncryptionDisabled_ShouldPublishDirectly()
    {
        // Arrange
        _securityOptions.EnableEncryption = false;
        var decorator = new EncryptionMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        var message = new TestMessage { Content = "test" };
        var options = new PublishOptions();

        // Act
        await decorator.PublishAsync(message, options);

        // Assert
        _innerBrokerMock.Verify(b => b.PublishAsync(message, options, default), Times.Once);
        _encryptorMock.Verify(e => e.EncryptAsync(It.IsAny<byte[]>(), default), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var decorator = new EncryptionMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await decorator.PublishAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task PublishAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var decorator = new EncryptionMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        await decorator.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await decorator.PublishAsync(new TestMessage()));
    }

    [Fact]
    public async Task PublishAsync_WithEncryptionEnabled_ShouldEncryptAndPublish()
    {
        // Arrange
        var decorator = new EncryptionMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        var message = new TestMessage { Content = "test" };
        var originalBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(message);
        var encryptedBytes = new byte[originalBytes.Length + 16]; // Mock encrypted data
        RandomNumberGenerator.Fill(encryptedBytes);

        _encryptorMock.Setup(e => e.EncryptAsync(originalBytes, default)).ReturnsAsync(encryptedBytes);
        _encryptorMock.Setup(e => e.GetKeyVersion()).Returns("v1");

        // Act
        await decorator.PublishAsync(message);

        // Assert
        _encryptorMock.Verify(e => e.EncryptAsync(It.Is<byte[]>(b => b.Length == originalBytes.Length), default), Times.Once);
        _innerBrokerMock.Verify(b => b.PublishAsync(
            It.Is<object>(w => w.GetType().Name == "EncryptedMessageWrapper"), // Check it's the wrapper type
            It.Is<PublishOptions>(o =>
                o.Headers != null &&
                o.Headers.ContainsKey(EncryptionMetadata.KeyVersionHeaderKey) &&
                o.Headers.ContainsKey(EncryptionMetadata.AlgorithmHeaderKey) &&
                o.Headers.ContainsKey(EncryptionMetadata.EncryptedAtHeaderKey)),
            default), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithExistingHeaders_ShouldAddEncryptionHeaders()
    {
        // Arrange
        var decorator = new EncryptionMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        var message = new TestMessage { Content = "test" };
        var encryptedBytes = new byte[32];
        RandomNumberGenerator.Fill(encryptedBytes);

        _encryptorMock.Setup(e => e.EncryptAsync(It.IsAny<byte[]>(), default)).ReturnsAsync(encryptedBytes);
        _encryptorMock.Setup(e => e.GetKeyVersion()).Returns("v1");

        var options = new PublishOptions
        {
            Headers = new Dictionary<string, object> { ["existing"] = "value" }
        };

        // Act
        await decorator.PublishAsync(message, options);

        // Assert
        _innerBrokerMock.Verify(b => b.PublishAsync(
            It.IsAny<object>(),
            It.Is<PublishOptions>(o =>
                o.Headers!.ContainsKey("existing") &&
                o.Headers.ContainsKey(EncryptionMetadata.KeyVersionHeaderKey)),
            default), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithEncryptionException_ShouldThrowEncryptionException()
    {
        // Arrange
        var decorator = new EncryptionMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        var message = new TestMessage { Content = "test" };
        var encryptionException = new EncryptionException("Encryption failed");

        _encryptorMock.Setup(e => e.EncryptAsync(It.IsAny<byte[]>(), default)).ThrowsAsync(encryptionException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EncryptionException>(async () =>
            await decorator.PublishAsync(message));

        Assert.Equal(encryptionException, exception);
    }

    [Fact]
    public async Task PublishAsync_WithUnexpectedException_ShouldWrapInEncryptionException()
    {
        // Arrange
        var decorator = new EncryptionMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        var message = new TestMessage { Content = "test" };
        var unexpectedException = new InvalidOperationException("Unexpected error");

        _encryptorMock.Setup(e => e.EncryptAsync(It.IsAny<byte[]>(), default)).ThrowsAsync(unexpectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EncryptionException>(async () =>
            await decorator.PublishAsync(message));

        Assert.Contains("Failed to encrypt and publish message", exception.Message);
        Assert.Equal(unexpectedException, exception.InnerException);
    }

    [Fact]
    public async Task SubscribeAsync_WithEncryptionDisabled_ShouldSubscribeDirectly()
    {
        // Arrange
        _securityOptions.EnableEncryption = false;
        var decorator = new EncryptionMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        Func<TestMessage, MessageContext, CancellationToken, ValueTask> handler = (msg, ctx, ct) => ValueTask.CompletedTask;
        var options = new SubscriptionOptions();

        // Act
        await decorator.SubscribeAsync(handler, options);

        // Assert
        _innerBrokerMock.Verify(b => b.SubscribeAsync(handler, options, default), Times.Once);
        _encryptorMock.Verify(e => e.DecryptAsync(It.IsAny<byte[]>(), default), Times.Never);
    }

    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var decorator = new EncryptionMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await decorator.SubscribeAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task SubscribeAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var decorator = new EncryptionMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        await decorator.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await decorator.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask));
    }











    [Fact]
    public async Task StartAsync_ShouldDelegateToInnerBroker()
    {
        // Arrange
        var decorator = new EncryptionMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        // Act
        await decorator.StartAsync();

        // Assert
        _innerBrokerMock.Verify(b => b.StartAsync(default), Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldDelegateToInnerBroker()
    {
        // Arrange
        var decorator = new EncryptionMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        // Act
        await decorator.StopAsync();

        // Assert
        _innerBrokerMock.Verify(b => b.StopAsync(default), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeEncryptorAndInnerBroker()
    {
        // Arrange
        var encryptorMock = new Mock<IMessageEncryptor>();
        var asyncDisposableEncryptorMock = encryptorMock.As<IAsyncDisposable>();

        var brokerMock = new Mock<IMessageBroker>();
        var asyncDisposableBrokerMock = brokerMock.As<IAsyncDisposable>();

        var decorator = new EncryptionMessageBrokerDecorator(
            brokerMock.Object,
            encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        // Act
        await decorator.DisposeAsync();

        // Assert
        asyncDisposableEncryptorMock.Verify(d => d.DisposeAsync(), Times.Once);
        asyncDisposableBrokerMock.Verify(d => d.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_WithSyncDisposableEncryptor_ShouldDispose()
    {
        // Arrange
        var encryptorMock = new Mock<IMessageEncryptor>();
        var disposableEncryptorMock = encryptorMock.As<IDisposable>();

        var decorator = new EncryptionMessageBrokerDecorator(
            _innerBrokerMock.Object,
            encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        // Act
        await decorator.DisposeAsync();

        // Assert
        disposableEncryptorMock.Verify(d => d.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_MultipleCalls_ShouldNotThrow()
    {
        // Arrange
        var decorator = new EncryptionMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        // Act
        await decorator.DisposeAsync();
        await decorator.DisposeAsync(); // Second call should not throw

        // Assert - No exception thrown
    }

    private class TestMessage
    {
        public string Content { get; set; } = string.Empty;
    }
}