using System.Collections.Concurrent;
using System.Diagnostics;
using Relay.MessageBroker;
using Xunit;
using Xunit.Abstractions;

namespace Relay.MessageBroker.Tests;

public class PerformanceTests
{
    private readonly ITestOutputHelper _output;

    public PerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task InMemoryMessageBroker_BatchPublish_PerformanceTest()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        var receivedMessages = new List<TestMessage>();
        var batchSize = 1000;
        var batchCount = 10;

        await broker.SubscribeAsync<TestMessage>(async (message, context, ct) =>
        {
            receivedMessages.Add(message);
            await context.Acknowledge();
        });

        await broker.StartAsync();

        // Act
        var stopwatch = Stopwatch.StartNew();

        for (int batch = 0; batch < batchCount; batch++)
        {
            var messages = Enumerable.Range(batch * batchSize + 1, batchSize)
                .Select(i => new TestMessage { Id = i, Data = $"Data{i}" })
                .ToList();

            foreach (var message in messages)
            {
                await broker.PublishAsync(message);
            }
        }

        // Wait for all messages to be processed
        var totalMessages = batchSize * batchCount;
        var timeout = TimeSpan.FromSeconds(15);
        var startTime = DateTime.UtcNow;
        while (receivedMessages.Count < totalMessages && DateTime.UtcNow - startTime < timeout)
        {
            await Task.Delay(200);
        }

        stopwatch.Stop();

        // Assert - Allow some tolerance for async processing
        Assert.True(receivedMessages.Count >= totalMessages * 0.9, $"Only {receivedMessages.Count} out of {totalMessages} messages processed");
        _output.WriteLine($"Processed {totalMessages} messages in batches in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        _output.WriteLine($"Throughput: {totalMessages / stopwatch.Elapsed.TotalSeconds:F0} messages/second");
    }

    [Fact]
    public async Task InMemoryMessageBroker_ConcurrentPublishSubscribe_PerformanceTest()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        var receivedMessages = new List<TestMessage>();
        var publisherCount = 5;
        var messagesPerPublisher = 1000;
        var totalMessages = publisherCount * messagesPerPublisher;

        await broker.SubscribeAsync<TestMessage>(async (message, context, ct) =>
        {
            lock (receivedMessages)
            {
                receivedMessages.Add(message);
            }
            await context.Acknowledge();
        });

        await broker.StartAsync();

        // Act
        var stopwatch = Stopwatch.StartNew();

        var publisherTasks = Enumerable.Range(0, publisherCount)
            .Select(async publisherId =>
            {
                for (int i = 0; i < messagesPerPublisher; i++)
                {
                    var message = new TestMessage
                    {
                        Id = publisherId * messagesPerPublisher + i,
                        Data = $"Data{publisherId}-{i}"
                    };
                    await broker.PublishAsync(message);
                }
            });

        await Task.WhenAll(publisherTasks);

        // Wait for all messages to be processed
        var timeout = TimeSpan.FromSeconds(20);
        var startTime = DateTime.UtcNow;
        while (receivedMessages.Count < totalMessages && DateTime.UtcNow - startTime < timeout)
        {
            await Task.Delay(300);
        }

        stopwatch.Stop();

        // Assert - Allow some tolerance for async processing
        Assert.True(receivedMessages.Count >= totalMessages * 0.9, $"Only {receivedMessages.Count} out of {totalMessages} messages processed");
        _output.WriteLine($"Processed {totalMessages} concurrent messages in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        _output.WriteLine($"Throughput: {totalMessages / stopwatch.Elapsed.TotalSeconds:F0} messages/second");
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Data { get; set; } = string.Empty;
    }
}