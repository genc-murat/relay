using FluentAssertions;
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
        _broker.PublishedMessages.Should().HaveCount(1);
        _broker.PublishedMessages[0].Message.Should().BeEquivalentTo(message);
        _broker.PublishedMessages[0].MessageType.Should().Be(typeof(TestMessage));
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
        _broker.PublishedMessages.Should().HaveCount(1);
        _broker.PublishedMessages[0].Options.Should().BeEquivalentTo(options);
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
        receivedMessages.Should().HaveCount(1);
        receivedMessages[0].Should().BeEquivalentTo(message);
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
        receivedMessages1.Should().HaveCount(1);
        receivedMessages2.Should().HaveCount(1);
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
        receivedMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task StartAsync_ShouldAllowMessageDelivery()
    {
        // Arrange
        var receivedMessages = new List<TestMessage>();
        await _broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            receivedMessages.Add(msg);
            return ValueTask.CompletedTask;
        });

        // Act
        await _broker.StartAsync();
        await _broker.PublishAsync(new TestMessage { Id = 1, Content = "Test" });
        await Task.Delay(100);

        // Assert
        receivedMessages.Should().HaveCount(1);
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
        receivedMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task Clear_ShouldRemoveAllMessagesAndSubscriptions()
    {
        // Arrange
        await _broker.PublishAsync(new TestMessage { Id = 1, Content = "Test" });

        // Act
        _broker.Clear();

        // Assert
        _broker.PublishedMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        TestMessage? message = null;

        // Act
        Func<Task> act = async () => await _broker.PublishAsync(message!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        Func<TestMessage, MessageContext, CancellationToken, ValueTask>? handler = null;

        // Act
        Func<Task> act = async () => await _broker.SubscribeAsync(handler!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
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
        receivedContext.Should().NotBeNull();
        receivedContext!.MessageId.Should().NotBeNullOrEmpty();
        receivedContext.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        receivedContext.RoutingKey.Should().Be("test.routing");
        receivedContext.Exchange.Should().Be("test-exchange");
        receivedContext.Headers.Should().ContainKey("key");
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
        _broker.PublishedMessages.Should().HaveCount(2);
        _broker.PublishedMessages.Select(m => m.MessageType).Should().Contain(typeof(TestMessage));
        _broker.PublishedMessages.Select(m => m.MessageType).Should().Contain(typeof(AnotherTestMessage));
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
        receivedTestMessages.Should().HaveCount(1);
        receivedAnotherMessages.Should().HaveCount(1);
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
        receivedMessages.Should().HaveCount(1);
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
        receivedContext.Should().NotBeNull();
        var ack = async () => await receivedContext!.Acknowledge();
        var reject = async () => await receivedContext!.Reject(false);
        await ack.Should().NotThrowAsync();
        await reject.Should().NotThrowAsync();
    }
}
