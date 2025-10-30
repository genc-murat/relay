using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.Security;
using Xunit;

namespace Relay.MessageBroker.Tests.Security;

public class SecurityMessageBrokerDecoratorTests
{
    private readonly Mock<IMessageBroker> _innerBrokerMock;
    private readonly Mock<IMessageAuthenticator> _authenticatorMock;
    private readonly Mock<IOptions<AuthenticationOptions>> _authOptionsMock;
    private readonly Mock<ILogger<SecurityMessageBrokerDecorator>> _loggerMock;
    private readonly Mock<ILogger<SecurityEventLogger>> _securityLoggerMock;
    private readonly SecurityEventLogger _securityEventLogger;
    private readonly AuthenticationOptions _authOptions;

    public SecurityMessageBrokerDecoratorTests()
    {
        _innerBrokerMock = new Mock<IMessageBroker>();
        _authenticatorMock = new Mock<IMessageAuthenticator>();
        _authOptionsMock = new Mock<IOptions<AuthenticationOptions>>();
        _loggerMock = new Mock<ILogger<SecurityMessageBrokerDecorator>>();
        _securityLoggerMock = new Mock<ILogger<SecurityEventLogger>>();
        _securityEventLogger = new SecurityEventLogger(_securityLoggerMock.Object);

        _authOptions = new AuthenticationOptions
        {
            EnableAuthentication = true,
            JwtIssuer = "test-issuer",
            JwtAudience = "test-audience",
            JwtSigningKey = Convert.ToBase64String(new byte[32])
        };
        _authOptionsMock.Setup(o => o.Value).Returns(_authOptions);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        // Assert
        Assert.NotNull(decorator);
    }

    [Fact]
    public void Constructor_WithNullInnerBroker_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SecurityMessageBrokerDecorator(
                null!,
                _authenticatorMock.Object,
                _authOptionsMock.Object,
                _loggerMock.Object,
                _securityEventLogger));
    }

    [Fact]
    public void Constructor_WithNullAuthenticator_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SecurityMessageBrokerDecorator(
                _innerBrokerMock.Object,
                null!,
                _authOptionsMock.Object,
                _loggerMock.Object,
                _securityEventLogger));
    }

    [Fact]
    public void Constructor_WithNullAuthOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SecurityMessageBrokerDecorator(
                _innerBrokerMock.Object,
                _authenticatorMock.Object,
                null!,
                _loggerMock.Object,
                _securityEventLogger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SecurityMessageBrokerDecorator(
                _innerBrokerMock.Object,
                _authenticatorMock.Object,
                _authOptionsMock.Object,
                null!,
                _securityEventLogger));
    }

    [Fact]
    public void Constructor_WithNullSecurityEventLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SecurityMessageBrokerDecorator(
                _innerBrokerMock.Object,
                _authenticatorMock.Object,
                _authOptionsMock.Object,
                _loggerMock.Object,
                null!));
    }

    [Fact]
    public async Task PublishAsync_WithAuthenticationDisabled_ShouldPublishDirectly()
    {
        // Arrange
        _authOptions.EnableAuthentication = false;
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var message = new TestMessage { Content = "test" };
        var options = new PublishOptions();

        // Act
        await decorator.PublishAsync(message, options);

        // Assert
        _innerBrokerMock.Verify(x => x.PublishAsync(message, options, default), Times.Once);
        _authenticatorMock.Verify(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _authenticatorMock.Verify(x => x.AuthorizeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await decorator.PublishAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task PublishAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        await decorator.DisposeAsync();
        var message = new TestMessage { Content = "test" };

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await decorator.PublishAsync(message));
    }

    [Fact]
    public async Task PublishAsync_WithValidTokenAndPermissions_ShouldPublishSuccessfully()
    {
        // Arrange
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var message = new TestMessage { Content = "test" };
        var options = new PublishOptions
        {
            Headers = new Dictionary<string, object> { ["Authorization"] = "Bearer valid-token" }
        };

        _authenticatorMock.Setup(x => x.ValidateTokenAsync("valid-token", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(true));
        _authenticatorMock.Setup(x => x.AuthorizeAsync("valid-token", "publish", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(true));

        // Act
        await decorator.PublishAsync(message, options);

        // Assert
        _innerBrokerMock.Verify(x => x.PublishAsync(message, options, default), Times.Once);
        _authenticatorMock.Verify(x => x.ValidateTokenAsync("valid-token", It.IsAny<CancellationToken>()), Times.Once);
        _authenticatorMock.Verify(x => x.AuthorizeAsync("valid-token", "publish", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithNoToken_ShouldThrowAuthenticationException()
    {
        // Arrange
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var message = new TestMessage { Content = "test" };
        var options = new PublishOptions(); // No headers

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(async () =>
            await decorator.PublishAsync(message, options));

        Assert.Contains("Authentication token is required", exception.Message);
        _innerBrokerMock.Verify(x => x.PublishAsync(It.IsAny<TestMessage>(), It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        _securityLoggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Warning), It.IsAny<EventId>(), It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("SECURITY: Unauthorized publish attempt")), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithInvalidToken_ShouldThrowAuthenticationException()
    {
        // Arrange
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var message = new TestMessage { Content = "test" };
        var options = new PublishOptions
        {
            Headers = new Dictionary<string, object> { ["Authorization"] = "Bearer invalid-token" }
        };

        _authenticatorMock.Setup(x => x.ValidateTokenAsync("invalid-token", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(false));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(async () =>
            await decorator.PublishAsync(message, options));

        Assert.Contains("Invalid authentication token", exception.Message);
        _innerBrokerMock.Verify(x => x.PublishAsync(It.IsAny<TestMessage>(), It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        _securityLoggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Warning), It.IsAny<EventId>(), It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("SECURITY: Unauthorized publish attempt")), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithValidTokenButInsufficientPermissions_ShouldThrowAuthenticationException()
    {
        // Arrange
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var message = new TestMessage { Content = "test" };
        var options = new PublishOptions
        {
            Headers = new Dictionary<string, object> { ["Authorization"] = "Bearer valid-token" }
        };

        _authenticatorMock.Setup(x => x.ValidateTokenAsync("valid-token", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(true));
        _authenticatorMock.Setup(x => x.AuthorizeAsync("valid-token", "publish", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(false));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(async () =>
            await decorator.PublishAsync(message, options));

        Assert.Contains("Insufficient permissions", exception.Message);
        _innerBrokerMock.Verify(x => x.PublishAsync(It.IsAny<TestMessage>(), It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubscribeAsync_WithAuthenticationDisabled_ShouldSubscribeDirectly()
    {
        // Arrange
        _authOptions.EnableAuthentication = false;
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        Func<TestMessage, MessageContext, CancellationToken, ValueTask> handler = (msg, ctx, ct) => ValueTask.CompletedTask;
        var options = new SubscriptionOptions();

        // Act
        await decorator.SubscribeAsync(handler, options);

        // Assert
        _innerBrokerMock.Verify(x => x.SubscribeAsync(It.IsAny<Func<TestMessage, MessageContext, CancellationToken, ValueTask>>(), options, default), Times.Once);
        _authenticatorMock.Verify(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await decorator.SubscribeAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task SubscribeAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        await decorator.DisposeAsync();
        Func<TestMessage, MessageContext, CancellationToken, ValueTask> handler = (msg, ctx, ct) => ValueTask.CompletedTask;

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await decorator.SubscribeAsync(handler));
    }

    [Fact]
    public async Task SubscribeAsync_HandlerWithValidToken_ShouldCallOriginalHandler()
    {
        // Arrange
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var handlerCalled = false;
        Func<TestMessage, MessageContext, CancellationToken, ValueTask> handler = (msg, ctx, ct) =>
        {
            handlerCalled = true;
            return ValueTask.CompletedTask;
        };

        // Set up the inner broker to call our secure handler
        _innerBrokerMock.Setup(x => x.SubscribeAsync(It.IsAny<Func<TestMessage, MessageContext, CancellationToken, ValueTask>>(), It.IsAny<SubscriptionOptions>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask)
            .Callback<Func<TestMessage, MessageContext, CancellationToken, ValueTask>, SubscriptionOptions?, CancellationToken>((secureHandler, opts, ct) =>
            {
                // Simulate calling the secure handler
                var message = new TestMessage { Content = "test" };
                var context = new MessageContext
                {
                    Headers = new Dictionary<string, object> { ["Authorization"] = "Bearer valid-token" }
                };
                secureHandler(message, context, ct).GetAwaiter().GetResult();
            });

        _authenticatorMock.Setup(x => x.ValidateTokenAsync("valid-token", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(true));
        _authenticatorMock.Setup(x => x.AuthorizeAsync("valid-token", "subscribe", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(true));

        // Act
        await decorator.SubscribeAsync(handler);

        // Assert
        Assert.True(handlerCalled);
        _authenticatorMock.Verify(x => x.ValidateTokenAsync("valid-token", It.IsAny<CancellationToken>()), Times.Once);
        _authenticatorMock.Verify(x => x.AuthorizeAsync("valid-token", "subscribe", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubscribeAsync_HandlerWithNoToken_ShouldRejectMessage()
    {
        // Arrange
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var rejectCalled = false;
        Func<TestMessage, MessageContext, CancellationToken, ValueTask> handler = (msg, ctx, ct) =>
        {
            return ValueTask.CompletedTask;
        };

        Func<TestMessage, MessageContext, CancellationToken, ValueTask>? capturedSecureHandler = null;

        _innerBrokerMock.Setup(x => x.SubscribeAsync(It.IsAny<Func<TestMessage, MessageContext, CancellationToken, ValueTask>>(), It.IsAny<SubscriptionOptions>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask)
            .Callback<Func<TestMessage, MessageContext, CancellationToken, ValueTask>, SubscriptionOptions?, CancellationToken>((secureHandler, opts, ct) =>
            {
                capturedSecureHandler = secureHandler;
            });

        // Act
        await decorator.SubscribeAsync(handler);

        // Now test the captured secure handler
        var message = new TestMessage { Content = "test" };
        var context = new MessageContext
        {
            Headers = new Dictionary<string, object>(), // No token
            Reject = (requeue) =>
            {
                rejectCalled = true;
                return ValueTask.CompletedTask;
            }
        };

        // Assert that the secure handler throws AuthenticationException
        await Assert.ThrowsAsync<AuthenticationException>(async () =>
            await capturedSecureHandler!(message, context, CancellationToken.None));

        Assert.True(rejectCalled);
        _securityLoggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Warning), It.IsAny<EventId>(), It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("SECURITY: Unauthorized subscribe attempt")), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldDelegateToInnerBroker()
    {
        // Arrange
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var cancellationToken = new CancellationToken();

        // Act
        await decorator.StartAsync(cancellationToken);

        // Assert
        _innerBrokerMock.Verify(x => x.StartAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldDelegateToInnerBroker()
    {
        // Arrange
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var cancellationToken = new CancellationToken();

        // Act
        await decorator.StopAsync(cancellationToken);

        // Assert
        _innerBrokerMock.Verify(x => x.StopAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public void ExtractTokenFromHeaders_WithAuthorizationBearerHeader_ShouldExtractToken()
    {
        // Arrange
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var headers = new Dictionary<string, object>
        {
            ["Authorization"] = "Bearer test-token-123"
        };

        // Act
        var token = InvokeExtractTokenFromHeaders(decorator, headers);

        // Assert
        Assert.Equal("test-token-123", token);
    }

    [Fact]
    public void ExtractTokenFromHeaders_WithAuthorizationHeaderWithoutBearer_ShouldReturnHeaderValue()
    {
        // Arrange
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var headers = new Dictionary<string, object>
        {
            ["Authorization"] = "test-token-123"
        };

        // Act
        var token = InvokeExtractTokenFromHeaders(decorator, headers);

        // Assert
        Assert.Equal("test-token-123", token);
    }

    [Fact]
    public void ExtractTokenFromHeaders_WithXAuthTokenHeader_ShouldExtractToken()
    {
        // Arrange
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var headers = new Dictionary<string, object>
        {
            ["X-Auth-Token"] = "test-token-456"
        };

        // Act
        var token = InvokeExtractTokenFromHeaders(decorator, headers);

        // Assert
        Assert.Equal("test-token-456", token);
    }

    [Fact]
    public void ExtractTokenFromHeaders_WithAuthorizationAndXAuthToken_ShouldPreferAuthorization()
    {
        // Arrange
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var headers = new Dictionary<string, object>
        {
            ["Authorization"] = "Bearer auth-token",
            ["X-Auth-Token"] = "xauth-token"
        };

        // Act
        var token = InvokeExtractTokenFromHeaders(decorator, headers);

        // Assert
        Assert.Equal("auth-token", token);
    }

    [Fact]
    public void ExtractTokenFromHeaders_WithNullHeaders_ShouldReturnNull()
    {
        // Arrange
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        // Act
        var token = InvokeExtractTokenFromHeaders(decorator, null);

        // Assert
        Assert.Null(token);
    }

    [Fact]
    public void ExtractTokenFromHeaders_WithEmptyHeaders_ShouldReturnNull()
    {
        // Arrange
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var headers = new Dictionary<string, object>();

        // Act
        var token = InvokeExtractTokenFromHeaders(decorator, headers);

        // Assert
        Assert.Null(token);
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeInnerBrokerIfAsyncDisposable()
    {
        // Arrange
        var asyncDisposableMock = new Mock<IMessageBroker>();
        asyncDisposableMock.As<IAsyncDisposable>();
        var decorator = new SecurityMessageBrokerDecorator(
            asyncDisposableMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        // Act
        await decorator.DisposeAsync();

        // Assert
        asyncDisposableMock.As<IAsyncDisposable>().Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_MultipleCalls_ShouldNotThrow()
    {
        // Arrange
        var decorator = new SecurityMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _authenticatorMock.Object,
            _authOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        // Act
        await decorator.DisposeAsync();
        await decorator.DisposeAsync();

        // Assert - No exception thrown
    }

    // Helper method to access private ExtractTokenFromHeaders method
    private string? InvokeExtractTokenFromHeaders(SecurityMessageBrokerDecorator decorator, Dictionary<string, object>? headers)
    {
        var method = typeof(SecurityMessageBrokerDecorator).GetMethod("ExtractTokenFromHeaders", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (string?)method?.Invoke(decorator, new object?[] { headers });
    }

    private class TestMessage
    {
        public string Content { get; set; } = string.Empty;
    }
}