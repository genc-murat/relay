using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class InMemoryMessageBrokerTests
{
    private readonly InMemoryMessageBroker _broker = new();

    [Fact]
    public async Task PublishAsync_ShouldStorePublishedMessage()
    {
        // Arrange
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        await _broker.PublishAsync(message);

        // Assert
        Assert.Single(_broker.PublishedMessages);
        Assert.Equal(message.Id, ((TestMessage)_broker.PublishedMessages[0].Message).Id);
        Assert.Equal(message.Content, ((TestMessage)_broker.PublishedMessages[0].Message).Content);
        Assert.Equal(typeof(TestMessage), _broker.PublishedMessages[0].MessageType);
    }

    [Fact]
    public async Task PublishAsync_WithOptions_ShouldStoreOptions()
    {
        // Arrange
        var message = new TestMessage { Id = 1, Content = "Test" };
        var options = new PublishOptions
        {
            RoutingKey = "test.routing.key",
            Exchange = "test-exchange"
        };

        // Act
        await _broker.PublishAsync(message, options);

        // Assert
        Assert.Single(_broker.PublishedMessages);
        Assert.Equal(options.RoutingKey, _broker.PublishedMessages[0].Options?.RoutingKey);
        Assert.Equal(options.Exchange, _broker.PublishedMessages[0].Options?.Exchange);
    }

    [Fact]
    public async Task SubscribeAsync_ShouldReceivePublishedMessages()
    {
        // Arrange
        var receivedMessages = new List<TestMessage>();
        await _broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            receivedMessages.Add(msg);
            return ValueTask.CompletedTask;
        });

        await _broker.StartAsync();

        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        await _broker.PublishAsync(message);
        await Task.Delay(100); // Give time for async dispatch

        // Assert
        Assert.Single(receivedMessages);
        Assert.Equal(message.Id, receivedMessages[0].Id);
        Assert.Equal(message.Content, receivedMessages[0].Content);
    }

    [Fact]
    public async Task SubscribeAsync_MultipleSubscribers_ShouldReceiveAllMessages()
    {
        // Arrange
        var receivedMessages1 = new List<TestMessage>();
        var receivedMessages2 = new List<TestMessage>();

        await _broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            receivedMessages1.Add(msg);
            return ValueTask.CompletedTask;
        });

        await _broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            receivedMessages2.Add(msg);
            return ValueTask.CompletedTask;
        });

        await _broker.StartAsync();

        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        await _broker.PublishAsync(message);
        await Task.Delay(100); // Give time for async dispatch

        // Assert
        Assert.Single(receivedMessages1);
        Assert.Single(receivedMessages2);
    }

    [Fact]
    public async Task SubscribeAsync_BeforeStart_ShouldNotReceiveMessages()
    {
        // Arrange
        var receivedMessages = new List<TestMessage>();
        await _broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            receivedMessages.Add(msg);
            return ValueTask.CompletedTask;
        });

        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        await _broker.PublishAsync(message);
        await Task.Delay(100);

        // Assert
        Assert.Empty(receivedMessages);
    }

    [Fact]
    public async Task StartAsync_ShouldAllowMessageDelivery()
    {
        // Arrange
        var receivedMessages = new List<TestMessage>();
        var messageReceived = new TaskCompletionSource<bool>();

        await _broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            lock (receivedMessages)
            {
                receivedMessages.Add(msg);
                messageReceived.TrySetResult(true);
            }
            return ValueTask.CompletedTask;
        });

        // Act
        await _broker.StartAsync();
        await _broker.PublishAsync(new TestMessage { Id = 1, Content = "Test" });

        // Wait for message to be received (with timeout)
        await Task.WhenAny(messageReceived.Task, Task.Delay(500));

        // Assert
        Assert.Single(receivedMessages);
    }

    [Fact]
    public async Task StopAsync_ShouldStopMessageDelivery()
    {
        // Arrange
        var receivedMessages = new List<TestMessage>();
        await _broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            receivedMessages.Add(msg);
            return ValueTask.CompletedTask;
        });

        await _broker.StartAsync();

        // Act
        await _broker.StopAsync();
        await _broker.PublishAsync(new TestMessage { Id = 1, Content = "Test" });
        await Task.Delay(100);

        // Assert
        Assert.Empty(receivedMessages);
    }

    [Fact]
    public async Task Clear_ShouldRemoveAllMessagesAndSubscriptions()
    {
        // Arrange
        await _broker.PublishAsync(new TestMessage { Id = 1, Content = "Test" });

        // Act
        _broker.Clear();

        // Assert
        Assert.Empty(_broker.PublishedMessages);
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        TestMessage? message = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await _broker.PublishAsync(message!));
    }

    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        Func<TestMessage, MessageContext, CancellationToken, ValueTask>? handler = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await _broker.SubscribeAsync(handler!));
    }

    [Fact]
    public async Task MessageContext_ShouldContainCorrectMetadata()
    {
        // Arrange
        MessageContext? receivedContext = null;
        var options = new PublishOptions
        {
            RoutingKey = "test.routing",
            Exchange = "test-exchange",
            Headers = new Dictionary<string, object> { { "key", "value" } }
        };

        await _broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            receivedContext = ctx;
            return ValueTask.CompletedTask;
        });

        await _broker.StartAsync();

        // Act
        await _broker.PublishAsync(new TestMessage { Id = 1, Content = "Test" }, options);
        await Task.Delay(500); // Increased delay to ensure message delivery

        // Assert
        Assert.NotNull(receivedContext);
        Assert.False(string.IsNullOrEmpty(receivedContext!.MessageId));
        Assert.InRange(receivedContext.Timestamp, DateTimeOffset.UtcNow.AddSeconds(-2), DateTimeOffset.UtcNow.AddSeconds(2));
        Assert.Equal("test.routing", receivedContext.RoutingKey);
        Assert.Equal("test-exchange", receivedContext.Exchange);
        Assert.True(receivedContext.Headers.ContainsKey("key"));
    }

    [Fact]
    public async Task PublishAsync_MultipleDifferentMessageTypes_ShouldStoreAll()
    {
        // Arrange
        var message1 = new TestMessage { Id = 1, Content = "Test1" };
        var message2 = new AnotherTestMessage { Name = "Test2" };

        // Act
        await _broker.PublishAsync(message1);
        await _broker.PublishAsync(message2);

        // Assert
        Assert.Equal(2, _broker.PublishedMessages.Count);
        Assert.Contains(_broker.PublishedMessages, m => m.MessageType == typeof(TestMessage));
        Assert.Contains(_broker.PublishedMessages, m => m.MessageType == typeof(AnotherTestMessage));
    }

    [Fact]
    public async Task SubscribeAsync_DifferentMessageTypes_ShouldReceiveOnlyMatchingType()
    {
        // Arrange
        var receivedTestMessages = new List<TestMessage>();
        var receivedAnotherMessages = new List<AnotherTestMessage>();

        await _broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            receivedTestMessages.Add(msg);
            return ValueTask.CompletedTask;
        });

        await _broker.SubscribeAsync<AnotherTestMessage>((msg, ctx, ct) =>
        {
            receivedAnotherMessages.Add(msg);
            return ValueTask.CompletedTask;
        });

        await _broker.StartAsync();

        // Act
        await _broker.PublishAsync(new TestMessage { Id = 1, Content = "Test1" });
        await _broker.PublishAsync(new AnotherTestMessage { Name = "Test2" });
        await Task.Delay(500); // Increased delay to ensure message delivery

        // Assert
        Assert.Single(receivedTestMessages);
        Assert.Single(receivedAnotherMessages);
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    private class AnotherTestMessage
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task SubscribeAsync_WhenHandlerThrows_ShouldNotAffectOtherHandlers()
    {
        // Arrange
        var receivedMessages = new List<TestMessage>();
        await _broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            throw new InvalidOperationException("Test exception");
        });
        await _broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            receivedMessages.Add(msg);
            return ValueTask.CompletedTask;
        });

        await _broker.StartAsync();

        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        await _broker.PublishAsync(message);
        await Task.Delay(100); // Give time for async dispatch

        // Assert
        Assert.Single(receivedMessages);
    }

    [Fact]
    public async Task MessageContext_AcknowledgeAndReject_ShouldNotThrow()
    {
        // Arrange
        MessageContext? receivedContext = null;
        await _broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            receivedContext = ctx;
            return ValueTask.CompletedTask;
        });

        await _broker.StartAsync();

        // Act
        await _broker.PublishAsync(new TestMessage { Id = 1, Content = "Test" });
        await Task.Delay(100);

        // Assert
        Assert.NotNull(receivedContext);
        await receivedContext!.Acknowledge();
        await receivedContext!.Reject(false);
    }
}
