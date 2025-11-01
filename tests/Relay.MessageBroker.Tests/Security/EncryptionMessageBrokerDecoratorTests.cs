using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.Security;
using System.Security.Cryptography;

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
    public async Task SubscribeAsync_WithEncryptionEnabled_ShouldSubscribeToEncryptedWrapper()
    {
        // Arrange
        var decorator = new EncryptionMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        var handlerCalled = false;
        Func<TestMessage, MessageContext, CancellationToken, ValueTask> handler = 
            (msg, ctx, ct) => { handlerCalled = true; return ValueTask.CompletedTask; };

        // Setup mock to call the handler when subscribed
        _innerBrokerMock
            .Setup(b => b.SubscribeAsync<EncryptedMessageWrapper>(It.IsAny<Func<EncryptedMessageWrapper, MessageContext, CancellationToken, ValueTask>>(), It.IsAny<SubscriptionOptions?>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await decorator.SubscribeAsync(handler);

        // Assert that we subscribed to EncryptedMessageWrapper, not TestMessage
        _innerBrokerMock.Verify(b => b.SubscribeAsync<EncryptedMessageWrapper>(It.IsAny<Func<EncryptedMessageWrapper, MessageContext, CancellationToken, ValueTask>>(), It.IsAny<SubscriptionOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubscribeAsync_WithEncryptedMessage_ShouldDecryptAndCallHandler()
    {
        // Arrange
        _securityOptions.EnableEncryption = true;
        var decorator = new EncryptionMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        var originalMessage = new TestMessage { Content = "test content" };
        var originalBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(originalMessage);
        var encryptedBytes = new byte[originalBytes.Length + 16];
        RandomNumberGenerator.Fill(encryptedBytes);

        var handlerCalled = false;
        TestMessage? receivedMessage = null;
        MessageContext? receivedContext = null;

        Func<TestMessage, MessageContext, CancellationToken, ValueTask> handler = 
            (msg, ctx, ct) =>
            {
                receivedMessage = msg;
                receivedContext = ctx;
                handlerCalled = true;
                return ValueTask.CompletedTask;
            };

        // Setup encryptor to return the original bytes when decrypting
        _encryptorMock.Setup(e => e.DecryptAsync(encryptedBytes, It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalBytes);
        _encryptorMock.Setup(e => e.GetKeyVersion()).Returns("v1");

        // Create encrypted wrapper
        var encryptedWrapper = new EncryptedMessageWrapper
        {
            EncryptedPayload = encryptedBytes,
            MessageType = typeof(TestMessage).FullName
        };

        var context = new MessageContext
        {
            Headers = new Dictionary<string, object>
            {
                [EncryptionMetadata.KeyVersionHeaderKey] = "v1",
                [EncryptionMetadata.AlgorithmHeaderKey] = "AES256-GCM"
            }
        };

        // Get the actual DecryptingHandler by triggering the subscription
        Func<EncryptedMessageWrapper, MessageContext, CancellationToken, ValueTask>? actualHandler = null;
        _innerBrokerMock
            .Setup(b => b.SubscribeAsync<EncryptedMessageWrapper>(It.IsAny<Func<EncryptedMessageWrapper, MessageContext, CancellationToken, ValueTask>>(), It.IsAny<SubscriptionOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<Func<EncryptedMessageWrapper, MessageContext, CancellationToken, ValueTask>, SubscriptionOptions?, CancellationToken>(
                (h, _, _) => actualHandler = h)
            .Returns(ValueTask.CompletedTask);

        // Trigger the subscription to get the DecryptingHandler
        await decorator.SubscribeAsync(handler);

        // Act - Call the DecryptingHandler with encrypted message
        await actualHandler!(encryptedWrapper, context, CancellationToken.None);

        // Assert
        Assert.True(handlerCalled);
        Assert.NotNull(receivedMessage);
        Assert.Equal(originalMessage.Content, receivedMessage.Content);
        Assert.Equal(context, receivedContext);
    }

    [Fact]
    public async Task SubscribeAsync_WithEncryptionException_ShouldThrowEncryptionException()
    {
        // Arrange
        _securityOptions.EnableEncryption = true;
        var decorator = new EncryptionMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        var encryptedBytes = new byte[32];
        RandomNumberGenerator.Fill(encryptedBytes);

        var handler = (TestMessage msg, MessageContext ctx, CancellationToken ct) => ValueTask.CompletedTask;

        // Setup encryptor to throw an encryption exception
        var encryptionException = new EncryptionException("Decryption failed");
        _encryptorMock.Setup(e => e.DecryptAsync(encryptedBytes, It.IsAny<CancellationToken>()))
            .ThrowsAsync(encryptionException);

        // Create encrypted wrapper
        var encryptedWrapper = new EncryptedMessageWrapper
        {
            EncryptedPayload = encryptedBytes,
            MessageType = typeof(TestMessage).FullName
        };

        var context = new MessageContext
        {
            Headers = new Dictionary<string, object>
            {
                [EncryptionMetadata.KeyVersionHeaderKey] = "v1"
            }
        };

        Func<EncryptedMessageWrapper, MessageContext, CancellationToken, ValueTask>? actualHandler = null;
        _innerBrokerMock
            .Setup(b => b.SubscribeAsync<EncryptedMessageWrapper>(It.IsAny<Func<EncryptedMessageWrapper, MessageContext, CancellationToken, ValueTask>>(), It.IsAny<SubscriptionOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<Func<EncryptedMessageWrapper, MessageContext, CancellationToken, ValueTask>, SubscriptionOptions?, CancellationToken>(
                (h, _, _) => actualHandler = h)
            .Returns(ValueTask.CompletedTask);

        // Trigger the subscription to get the DecryptingHandler
        await decorator.SubscribeAsync(handler);

        // Act & Assert
        await Assert.ThrowsAsync<EncryptionException>(async () =>
            await actualHandler!(encryptedWrapper, context, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribeAsync_WithUnexpectedException_ShouldWrapInEncryptionException()
    {
        // Arrange
        _securityOptions.EnableEncryption = true;
        var decorator = new EncryptionMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        var encryptedBytes = new byte[32];
        RandomNumberGenerator.Fill(encryptedBytes);

        var handler = (TestMessage msg, MessageContext ctx, CancellationToken ct) => ValueTask.CompletedTask;

        // Setup encryptor to throw an unexpected exception
        var unexpectedException = new InvalidOperationException("Unexpected error");
        _encryptorMock.Setup(e => e.DecryptAsync(encryptedBytes, It.IsAny<CancellationToken>()))
            .ThrowsAsync(unexpectedException);

        // Create encrypted wrapper
        var encryptedWrapper = new EncryptedMessageWrapper
        {
            EncryptedPayload = encryptedBytes,
            MessageType = typeof(TestMessage).FullName
        };

        var context = new MessageContext
        {
            Headers = new Dictionary<string, object>
            {
                [EncryptionMetadata.KeyVersionHeaderKey] = "v1"
            }
        };

        Func<EncryptedMessageWrapper, MessageContext, CancellationToken, ValueTask>? actualHandler = null;
        _innerBrokerMock
            .Setup(b => b.SubscribeAsync<EncryptedMessageWrapper>(It.IsAny<Func<EncryptedMessageWrapper, MessageContext, CancellationToken, ValueTask>>(), It.IsAny<SubscriptionOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<Func<EncryptedMessageWrapper, MessageContext, CancellationToken, ValueTask>, SubscriptionOptions?, CancellationToken>(
                (h, _, _) => actualHandler = h)
            .Returns(ValueTask.CompletedTask);

        // Trigger the subscription to get the DecryptingHandler
        await decorator.SubscribeAsync(handler);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EncryptionException>(async () =>
            await actualHandler!(encryptedWrapper, context, CancellationToken.None));

        Assert.Contains("Failed to decrypt message", exception.Message);
        Assert.Equal(unexpectedException, exception.InnerException);
    }

    [Fact]
    public async Task SubscribeAsync_WithNullDeserializedMessage_ShouldThrowEncryptionException()
    {
        // Arrange
        _securityOptions.EnableEncryption = true;
        var decorator = new EncryptionMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _encryptorMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        var encryptedBytes = new byte[32];
        // To get null from deserialization, we'll simulate it by mocking the deserialization behavior
        var decryptedBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new TestMessage { Content = "test" });

        var handler = (TestMessage msg, MessageContext ctx, CancellationToken ct) => ValueTask.CompletedTask;

        // Setup encryptor
        _encryptorMock.Setup(e => e.DecryptAsync(encryptedBytes, It.IsAny<CancellationToken>()))
            .ReturnsAsync(decryptedBytes);
        _encryptorMock.Setup(e => e.GetKeyVersion()).Returns("v1");

        // We need to simulate the actual scenario where deserialization returns null
        // Since it's an internal method, we'll trigger the condition by using specific data
        
        // Create encrypted wrapper
        var encryptedWrapper = new EncryptedMessageWrapper
        {
            EncryptedPayload = encryptedBytes,
            MessageType = typeof(TestMessage).FullName
        };

        var context = new MessageContext
        {
            Headers = new Dictionary<string, object>
            {
                [EncryptionMetadata.KeyVersionHeaderKey] = "v1"
            }
        };

        Func<EncryptedMessageWrapper, MessageContext, CancellationToken, ValueTask>? actualHandler = null;
        _innerBrokerMock
            .Setup(b => b.SubscribeAsync<EncryptedMessageWrapper>(It.IsAny<Func<EncryptedMessageWrapper, MessageContext, CancellationToken, ValueTask>>(), It.IsAny<SubscriptionOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<Func<EncryptedMessageWrapper, MessageContext, CancellationToken, ValueTask>, SubscriptionOptions?, CancellationToken>(
                (h, _, _) => actualHandler = h)
            .Returns(ValueTask.CompletedTask);

        // Trigger the subscription to get the DecryptingHandler
        await decorator.SubscribeAsync(handler);

        // Act & Assert
        // To make deserialization return null, we need to mock the behavior differently.
        // Actually, the scenario where message becomes null would happen if the deserialized content is invalid.
        // In practice, System.Text.Json.JsonSerializer.Deserialize<T> will not return null when T is a non-nullable reference type
        // unless the JSON is explicitly null. Let me test the scenario with a different approach.
        
        // To properly test the null check, we need to use JsonSerializerOptions that allow null
        // But even then, for reference types, if the JSON is just 'null', it will return null
        // Create a scenario where JSON deserialization could return null
        var nullBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes((object?)null);
        _encryptorMock.Setup(e => e.DecryptAsync(encryptedBytes, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nullBytes);
        
        var exception = await Assert.ThrowsAsync<EncryptionException>(async () =>
            await actualHandler!(encryptedWrapper, context, CancellationToken.None));

        Assert.Contains("Failed to deserialize decrypted message", exception.Message);
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