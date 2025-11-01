using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Xunit;

namespace Relay.MessageBroker.Outbox;

public class OutboxMessageBrokerDecoratorTests
{
    private readonly IMessageBroker _mockInnerBroker;
    private readonly IOutboxStore _mockOutboxStore;
    private readonly OutboxOptions _options;

    public OutboxMessageBrokerDecoratorTests()
    {
        _mockInnerBroker = new MockMessageBroker();
        _mockOutboxStore = new MockOutboxStore();
        _options = new OutboxOptions();
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Arrange
        var options = Options.Create(_options);

        // Act
        var decorator = new OutboxMessageBrokerDecorator(_mockInnerBroker, _mockOutboxStore, options, NullLogger<OutboxMessageBrokerDecorator>.Instance);

        // Assert
        Assert.NotNull(decorator);
    }

    [Fact]
    public void Constructor_WithNullInnerBroker_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new OutboxMessageBrokerDecorator(null!, _mockOutboxStore, options, NullLogger<OutboxMessageBrokerDecorator>.Instance));
        Assert.Equal("innerBroker", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullOutboxStore_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new OutboxMessageBrokerDecorator(_mockInnerBroker, null!, options, NullLogger<OutboxMessageBrokerDecorator>.Instance));
        Assert.Equal("outboxStore", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new OutboxMessageBrokerDecorator(_mockInnerBroker, _mockOutboxStore, null!, NullLogger<OutboxMessageBrokerDecorator>.Instance));
        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new OutboxMessageBrokerDecorator(_mockInnerBroker, _mockOutboxStore, options, null!));
        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public async Task PublishAsync_WhenOutboxDisabled_ShouldDelegateToInnerBroker()
    {
        // Arrange
        var mockInnerBroker = new MockMessageBroker();
        var options = Options.Create(new OutboxOptions { Enabled = false });
        var decorator = new OutboxMessageBrokerDecorator(mockInnerBroker, _mockOutboxStore, options, NullLogger<OutboxMessageBrokerDecorator>.Instance);
        var testMessage = new TestMessage { Content = "Test content" };

        // Act
        await decorator.PublishAsync(testMessage);

        // Assert
        Assert.True(mockInnerBroker.PublishCalled);
        Assert.Equal(testMessage, mockInnerBroker.LastPublishedMessage);
    }

    [Fact]
    public async Task PublishAsync_WhenOutboxEnabled_ShouldStoreInOutbox()
    {
        // Arrange
        var mockOutboxStore = new MockOutboxStore();
        var options = Options.Create(new OutboxOptions { Enabled = true });
        var decorator = new OutboxMessageBrokerDecorator(_mockInnerBroker, mockOutboxStore, options, NullLogger<OutboxMessageBrokerDecorator>.Instance);
        var testMessage = new TestMessage { Content = "Test content" };
        var publishOptions = new PublishOptions
        {
            RoutingKey = "test.route",
            Exchange = "test-exchange",
            Headers = new Dictionary<string, object> { { "key1", "value1" } }
        };

        // Act
        await decorator.PublishAsync(testMessage, publishOptions);

        // Assert
        Assert.Single(mockOutboxStore.StoredMessages);
        var storedMessage = mockOutboxStore.StoredMessages.First();
        Assert.Equal(typeof(TestMessage).Name, storedMessage.MessageType);
        
        // Verify payload was serialized properly
        var deserializedMessage = JsonSerializer.Deserialize<TestMessage>(storedMessage.Payload);
        Assert.Equal(testMessage.Content, deserializedMessage.Content);
        
        Assert.Equal(publishOptions.RoutingKey, storedMessage.RoutingKey);
        Assert.Equal(publishOptions.Exchange, storedMessage.Exchange);
        Assert.Equal(publishOptions.Headers["key1"], storedMessage.Headers["key1"]);
    }

    [Fact]
    public async Task PublishAsync_WhenMessageIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new OutboxOptions { Enabled = true });
        var decorator = new OutboxMessageBrokerDecorator(_mockInnerBroker, _mockOutboxStore, options, NullLogger<OutboxMessageBrokerDecorator>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            decorator.PublishAsync<TestMessage>(null!).AsTask());
    }

    [Fact]
    public async Task SubscribeAsync_ShouldDelegateToInnerBroker()
    {
        // Arrange
        var mockInnerBroker = new MockMessageBroker();
        var options = Options.Create(_options);
        var decorator = new OutboxMessageBrokerDecorator(mockInnerBroker, _mockOutboxStore, options, NullLogger<OutboxMessageBrokerDecorator>.Instance);
        bool handlerCalled = false;
        TestMessage receivedMessage = null;

        // Act
        await decorator.SubscribeAsync<TestMessage>((message, context, token) =>
        {
            handlerCalled = true;
            receivedMessage = message;
            return ValueTask.CompletedTask;
        });

        // Assert
        Assert.True(mockInnerBroker.SubscribeCalled);
    }

    [Fact]
    public async Task StartAsync_ShouldDelegateToInnerBroker()
    {
        // Arrange
        var mockInnerBroker = new MockMessageBroker();
        var options = Options.Create(_options);
        var decorator = new OutboxMessageBrokerDecorator(mockInnerBroker, _mockOutboxStore, options, NullLogger<OutboxMessageBrokerDecorator>.Instance);

        // Act
        await decorator.StartAsync();

        // Assert
        Assert.True(mockInnerBroker.StartCalled);
    }

    [Fact]
    public async Task StopAsync_ShouldDelegateToInnerBroker()
    {
        // Arrange
        var mockInnerBroker = new MockMessageBroker();
        var options = Options.Create(_options);
        var decorator = new OutboxMessageBrokerDecorator(mockInnerBroker, _mockOutboxStore, options, NullLogger<OutboxMessageBrokerDecorator>.Instance);

        // Act
        await decorator.StopAsync();

        // Assert
        Assert.True(mockInnerBroker.StopCalled);
    }

    // Supporting classes for testing
    private class TestMessage
    {
        public string Content { get; set; } = string.Empty;
    }

    private class MockMessageBroker : IMessageBroker
    {
        public bool PublishCalled { get; private set; }
        public object? LastPublishedMessage { get; set; }
        public bool SubscribeCalled { get; private set; }
        public bool StartCalled { get; private set; }
        public bool StopCalled { get; private set; }

        public ValueTask PublishAsync<TMessage>(TMessage message, PublishOptions? options = null, CancellationToken cancellationToken = default)
        {
            PublishCalled = true;
            LastPublishedMessage = message;
            return ValueTask.CompletedTask;
        }

        public ValueTask SubscribeAsync<TMessage>(Func<TMessage, MessageContext, CancellationToken, ValueTask> handler, SubscriptionOptions? options = null, CancellationToken cancellationToken = default)
        {
            SubscribeCalled = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            StartCalled = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            StopCalled = true;
            return ValueTask.CompletedTask;
        }
    }

    private class MockOutboxStore : IOutboxStore
    {
        public List<OutboxMessage> StoredMessages { get; } = new List<OutboxMessage>();

        public ValueTask<OutboxMessage> StoreAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            // Assign a new ID to the message to simulate real behavior
            message.Id = Guid.NewGuid();
            StoredMessages.Add(message);
            return ValueTask.FromResult(message);
        }

        public ValueTask<IEnumerable<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            var pendingMessages = StoredMessages.Take(batchSize);
            return ValueTask.FromResult<IEnumerable<OutboxMessage>>(pendingMessages);
        }

        public ValueTask MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask<IEnumerable<OutboxMessage>> GetFailedAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IEnumerable<OutboxMessage>>(new List<OutboxMessage>());
        }
    }
}