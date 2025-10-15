using System.Collections.Concurrent;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class EdgeCaseTests
{
    [Fact]
    public async Task InMemoryMessageBroker_LargeMessagePayload_Test()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        var receivedMessages = new List<LargeMessage>();
        var largeData = new string('A', 1000000); // 1MB string

        await broker.SubscribeAsync<LargeMessage>(async (message, context, ct) =>
        {
            receivedMessages.Add(message);
            await context.Acknowledge();
        });

        await broker.StartAsync();

        var message = new LargeMessage { Id = 1, Data = largeData };

        // Act
        await broker.PublishAsync(message);
        await Task.Delay(100); // Give time for async dispatch

        // Assert
        Assert.Single(receivedMessages);
        Assert.Equal(1, receivedMessages[0].Id);
        Assert.Equal(largeData, receivedMessages[0].Data);
    }

    [Fact]
    public async Task InMemoryMessageBroker_MultipleSubscribers_Test()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        var subscriber1Messages = new List<TestMessage>();
        var subscriber2Messages = new List<TestMessage>();
        var subscriber3Messages = new List<TestMessage>();

        // Subscribe multiple handlers
        await broker.SubscribeAsync<TestMessage>(async (message, context, ct) =>
        {
            subscriber1Messages.Add(message);
            await context.Acknowledge();
        });

        await broker.SubscribeAsync<TestMessage>(async (message, context, ct) =>
        {
            subscriber2Messages.Add(message);
            await context.Acknowledge();
        });

        await broker.SubscribeAsync<TestMessage>(async (message, context, ct) =>
        {
            subscriber3Messages.Add(message);
            await context.Acknowledge();
        });

        await broker.StartAsync();

        var message = new TestMessage { Id = 1, Data = "Test" };

        // Act
        await broker.PublishAsync(message);
        await Task.Delay(100); // Give time for async dispatch

        // Assert
        Assert.Single(subscriber1Messages);
        Assert.Single(subscriber2Messages);
        Assert.Single(subscriber3Messages);
        Assert.Equal(1, subscriber1Messages[0].Id);
        Assert.Equal(1, subscriber2Messages[0].Id);
        Assert.Equal(1, subscriber3Messages[0].Id);
    }

    [Fact]
    public async Task InMemoryMessageBroker_SubscribeAfterPublish_Test()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        var receivedMessages = new List<TestMessage>();

        var message = new TestMessage { Id = 1, Data = "Test" };

        // Publish first
        await broker.PublishAsync(message);

        // Then subscribe
        await broker.SubscribeAsync<TestMessage>(async (message, context, ct) =>
        {
            receivedMessages.Add(message);
            await context.Acknowledge();
        });

        await broker.StartAsync();

        // Wait a bit for processing
        await Task.Delay(100);

        // Assert - In-memory broker doesn't store messages, so subscriber shouldn't receive old messages
        Assert.Empty(receivedMessages);
    }

    [Fact]
    public async Task InMemoryMessageBroker_ConcurrentSubscriptions_Test()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        var receivedCount = 0;
        var concurrentSubscriptions = 10;

        // Act - Subscribe concurrently
        var subscriptionTasks = Enumerable.Range(0, concurrentSubscriptions)
            .Select(async i =>
            {
                await broker.SubscribeAsync<TestMessage>(async (message, context, ct) =>
                {
                    Interlocked.Increment(ref receivedCount);
                    await context.Acknowledge();
                });
            });

        await Task.WhenAll(subscriptionTasks);
        await broker.StartAsync();

        // Publish a message
        var message = new TestMessage { Id = 1, Data = "Test" };
        await broker.PublishAsync(message);
        await Task.Delay(100); // Give time for async dispatch

        // Assert - All subscribers should receive the message
        Assert.Equal(concurrentSubscriptions, receivedCount);
    }

    [Fact]
    public async Task InMemoryMessageBroker_NullMessage_Test()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();

        await broker.StartAsync();

        // Act & Assert - Publishing null should throw ArgumentNullException
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.PublishAsync((object?)null));
        Assert.Equal("Value cannot be null. (Parameter 'message')", exception.Message);
    }

    [Fact]
    public async Task InMemoryMessageBroker_EmptyStringMessage_Test()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        var receivedMessages = new List<string>();

        await broker.SubscribeAsync<string>(async (message, context, ct) =>
        {
            receivedMessages.Add(message);
            await context.Acknowledge();
        });

        await broker.StartAsync();

        // Act
        await broker.PublishAsync("");
        await Task.Delay(100); // Give time for async dispatch

        // Assert
        Assert.Single(receivedMessages);
        Assert.Equal("", receivedMessages[0]);
    }

    [Fact]
    public async Task InMemoryMessageBroker_SpecialCharactersInMessage_Test()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        var receivedMessages = new List<string>();
        var specialMessage = "Special chars: \n\t\r\"'\\<>|?*";

        await broker.SubscribeAsync<string>(async (message, context, ct) =>
        {
            receivedMessages.Add(message);
            await context.Acknowledge();
        });

        await broker.StartAsync();

        // Act
        await broker.PublishAsync(specialMessage);
        await Task.Delay(100); // Give time for async dispatch

        // Assert
        Assert.Single(receivedMessages);
        Assert.Equal(specialMessage, receivedMessages[0]);
    }

    [Fact]
    public async Task InMemoryMessageBroker_CancellationToken_Test()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        var receivedMessages = new List<TestMessage>();
        var cts = new CancellationTokenSource();

        await broker.SubscribeAsync<TestMessage>(async (message, context, ct) =>
        {
            // Simulate slow processing
            await Task.Delay(1000, ct);
            receivedMessages.Add(message);
            await context.Acknowledge();
        }, cancellationToken: cts.Token);

        await broker.StartAsync();

        var message = new TestMessage { Id = 1, Data = "Test" };

        // Act
        await broker.PublishAsync(message);

        // Cancel after short delay
        await Task.Delay(100);
        cts.Cancel();

        // Wait a bit more
        await Task.Delay(200);

        // Assert - Message should not be processed due to cancellation
        Assert.Empty(receivedMessages);
    }

    private class LargeMessage
    {
        public int Id { get; set; }
        public string Data { get; set; } = string.Empty;
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Data { get; set; } = string.Empty;
    }
}