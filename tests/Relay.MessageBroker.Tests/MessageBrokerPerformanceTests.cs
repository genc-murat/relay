using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Concurrent;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Relay.MessageBroker.Tests;

public class MessageBrokerPerformanceTests
{
    private readonly ITestOutputHelper _output;

    public MessageBrokerPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public class PerformanceTestableMessageBroker : BaseMessageBroker
    {
        public ConcurrentBag<object> PublishedMessages { get; } = new();
        public ConcurrentBag<object> ProcessedMessages { get; } = new();
        public long TotalProcessingTimeMs;
        public int MessageCount;

        public PerformanceTestableMessageBroker(
            IOptions<MessageBrokerOptions> options,
            ILogger logger)
            : base(options, logger)
        {
        }

        protected override async ValueTask PublishInternalAsync<TMessage>(
            TMessage message,
            byte[] serializedMessage,
            PublishOptions? options,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            PublishedMessages.Add(message!);

            // Process message for subscribers if started and there are subscriptions
            if (IsStarted && _subscriptions.ContainsKey(typeof(TMessage)))
            {
                var decompressed = await DecompressMessageAsync(serializedMessage, cancellationToken);
                var deserialized = DeserializeMessage<TMessage>(decompressed);
                var context = new MessageContext();
                await ProcessMessageAsync(deserialized, typeof(TMessage), context, cancellationToken);
            }

            stopwatch.Stop();
            Interlocked.Add(ref TotalProcessingTimeMs, stopwatch.ElapsedMilliseconds);
            Interlocked.Increment(ref MessageCount);
        }

        protected override ValueTask SubscribeInternalAsync(
            Type messageType,
            SubscriptionInfo subscriptionInfo,
            CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        protected override ValueTask StartInternalAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        protected override ValueTask StopInternalAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        protected override ValueTask DisposeInternalAsync()
        {
            return ValueTask.CompletedTask;
        }

        public async ValueTask TestProcessMessageAsync(object message, Type messageType, MessageContext context)
        {
            await ProcessMessageAsync(message, messageType, context);
        }
    }

    [Fact]
    public async Task PerformanceTest_HighThroughputPublishing_ShouldMeetMinimumRequirements()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<PerformanceTestableMessageBroker>>().Object;
        var broker = new PerformanceTestableMessageBroker(options, logger);

        await broker.StartAsync();

        const int messageCount = 10000;
        const int minMessagesPerSecond = 5000; // Minimum acceptable throughput

        var messages = new TestMessage[messageCount];
        for (int i = 0; i < messageCount; i++)
        {
            messages[i] = new TestMessage { Id = i, Data = $"Data{i}" };
        }

        // Act
        var stopwatch = Stopwatch.StartNew();
        var publishTasks = messages.Select(msg => broker.PublishAsync(msg).AsTask()).ToArray();
        await Task.WhenAll(publishTasks);
        stopwatch.Stop();

        var totalTimeSeconds = stopwatch.Elapsed.TotalSeconds;
        var messagesPerSecond = messageCount / totalTimeSeconds;

        // Assert
        _output.WriteLine($"Published {messageCount} messages in {totalTimeSeconds:F2} seconds");
        _output.WriteLine($"Throughput: {messagesPerSecond:F0} messages/second");
        _output.WriteLine($"Average latency: {broker.TotalProcessingTimeMs / (double)broker.MessageCount:F2} ms per message");

        Assert.True(messagesPerSecond >= minMessagesPerSecond,
            $"Throughput {messagesPerSecond:F0} msg/s below minimum {minMessagesPerSecond} msg/s");

        Assert.Equal(messageCount, broker.PublishedMessages.Count);
    }

    [Fact]
    public async Task PerformanceTest_ConcurrentPublishing_ShouldMaintainConsistency()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<PerformanceTestableMessageBroker>>().Object;
        var broker = new PerformanceTestableMessageBroker(options, logger);

        await broker.StartAsync();

        const int publisherCount = 10;
        const int messagesPerPublisher = 1000;
        const int totalMessages = publisherCount * messagesPerPublisher;

        // Act
        var stopwatch = Stopwatch.StartNew();
        var publishTasks = new List<Task>();

        for (int p = 0; p < publisherCount; p++)
        {
            var publisherId = p;
            publishTasks.Add(Task.Run(async () =>
            {
                for (int m = 0; m < messagesPerPublisher; m++)
                {
                    var message = new TestMessage
                    {
                        Id = publisherId * messagesPerPublisher + m,
                        Data = $"Publisher{publisherId}-Message{m}"
                    };
                    await broker.PublishAsync(message);
                }
            }));
        }

        await Task.WhenAll(publishTasks);
        stopwatch.Stop();

        // Assert
        var totalTimeSeconds = stopwatch.Elapsed.TotalSeconds;
        var messagesPerSecond = totalMessages / totalTimeSeconds;

        _output.WriteLine($"Concurrent publishing: {totalMessages} messages in {totalTimeSeconds:F2} seconds");
        _output.WriteLine($"Concurrent throughput: {messagesPerSecond:F0} messages/second");

        Assert.Equal(totalMessages, broker.PublishedMessages.Count);

        // Verify all messages are unique
        var messageIds = broker.PublishedMessages
            .OfType<TestMessage>()
            .Select(m => m.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(totalMessages, messageIds.Count);
        for (int i = 0; i < totalMessages; i++)
        {
            Assert.Contains(i, messageIds);
        }
    }

    [Fact]
    public async Task PerformanceTest_PublishSubscribeLatency_ShouldBeAcceptable()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<PerformanceTestableMessageBroker>>().Object;
        var broker = new PerformanceTestableMessageBroker(options, logger);

        const int maxAcceptableLatencyMs = 50; // Maximum acceptable round-trip latency
        var receivedMessages = new ConcurrentBag<TestMessage>();
        var latencies = new ConcurrentBag<long>();

        // Subscribe to messages
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            var latency = Stopwatch.GetTimestamp() - msg.Timestamp;
            latencies.Add(Stopwatch.GetElapsedTime(msg.Timestamp, Stopwatch.GetTimestamp()).Milliseconds);
            receivedMessages.Add(msg);
            return ValueTask.CompletedTask;
        });

        await broker.StartAsync();

        const int messageCount = 1000;

        // Act
        var publishTasks = new List<Task>();
        for (int i = 0; i < messageCount; i++)
        {
            var message = new TestMessage { Id = i, Data = $"Data{i}", Timestamp = Stopwatch.GetTimestamp() };
            publishTasks.Add(broker.PublishAsync(message).AsTask());
        }

        await Task.WhenAll(publishTasks);

        // Give time for processing
        await Task.Delay(100);

        // Assert
        Assert.Equal(messageCount, receivedMessages.Count);

        var averageLatency = latencies.Average();
        var maxLatency = latencies.Max();
        var p95Latency = latencies.OrderBy(l => l).ElementAt((int)(latencies.Count * 0.95));

        _output.WriteLine($"Average latency: {averageLatency:F2} ms");
        _output.WriteLine($"Max latency: {maxLatency} ms");
        _output.WriteLine($"95th percentile latency: {p95Latency} ms");

        Assert.True(averageLatency <= maxAcceptableLatencyMs,
            $"Average latency {averageLatency:F2}ms exceeds maximum {maxAcceptableLatencyMs}ms");
    }

    [Fact]
    public async Task PerformanceTest_LargeMessageHandling_ShouldNotDegradeSignificantly()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<PerformanceTestableMessageBroker>>().Object;
        var broker = new PerformanceTestableMessageBroker(options, logger);

        await broker.StartAsync();

        // Create messages of different sizes
        var smallMessage = new TestMessage { Id = 1, Data = "Small" };
        var mediumMessage = new TestMessage { Id = 2, Data = new string('X', 10000) }; // 10KB
        var largeMessage = new TestMessage { Id = 3, Data = new string('Y', 100000) }; // 100KB

        // Act - Measure time for different message sizes
        var stopwatch = Stopwatch.StartNew();
        await broker.PublishAsync(smallMessage);
        var smallTime = stopwatch.ElapsedMilliseconds;

        stopwatch.Restart();
        await broker.PublishAsync(mediumMessage);
        var mediumTime = stopwatch.ElapsedMilliseconds;

        stopwatch.Restart();
        await broker.PublishAsync(largeMessage);
        var largeTime = stopwatch.ElapsedMilliseconds;

        // Assert
        _output.WriteLine($"Small message (bytes): {smallTime}ms");
        _output.WriteLine($"Medium message (10KB): {mediumTime}ms");
        _output.WriteLine($"Large message (100KB): {largeTime}ms");

        // Large message should not take excessively longer (allowing for reasonable scaling)
        var scalingFactor = (double)largeTime / smallTime;
        _output.WriteLine($"Scaling factor: {scalingFactor:F2}x");

        // Assert reasonable scaling (large message shouldn't be more than 10x slower than small)
        Assert.True(scalingFactor <= 10.0,
            $"Large message processing scaled {scalingFactor:F2}x, which is excessive");

        Assert.Equal(3, broker.PublishedMessages.Count);
    }

    [Fact]
    public async Task PerformanceTest_MemoryUsage_UnderLoad()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<PerformanceTestableMessageBroker>>().Object;
        var broker = new PerformanceTestableMessageBroker(options, logger);

        await broker.StartAsync();

        const int messageCount = 5000;
        long initialMemory = GC.GetTotalMemory(true);

        // Act - Publish many messages
        var publishTasks = new List<Task>();
        for (int i = 0; i < messageCount; i++)
        {
            var message = new TestMessage { Id = i, Data = $"Data{i}" };
            publishTasks.Add(broker.PublishAsync(message).AsTask());
        }

        await Task.WhenAll(publishTasks);

        // Force GC to get accurate memory measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        long finalMemory = GC.GetTotalMemory(true);

        long memoryUsed = finalMemory - initialMemory;
        double memoryPerMessage = (double)memoryUsed / messageCount;

        // Assert
        _output.WriteLine($"Memory used: {memoryUsed / 1024.0:F2} KB");
        _output.WriteLine($"Memory per message: {memoryPerMessage:F2} bytes");

        // Should not use excessive memory per message
        Assert.True(memoryPerMessage <= 1000,
            $"Memory usage {memoryPerMessage:F2} bytes per message is too high");

        Assert.Equal(messageCount, broker.PublishedMessages.Count);
    }

    [Fact]
    public async Task PerformanceTest_SubscriberScaling_ShouldHandleMultipleSubscribers()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<PerformanceTestableMessageBroker>>().Object;
        var broker = new PerformanceTestableMessageBroker(options, logger);

        const int subscriberCount = 100;
        var receivedCounts = new int[subscriberCount];

        // Subscribe multiple handlers
        for (int i = 0; i < subscriberCount; i++)
        {
            var index = i;
            await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
            {
                Interlocked.Increment(ref receivedCounts[index]);
                return ValueTask.CompletedTask;
            });
        }

        await broker.StartAsync();

        const int messageCount = 100;

        // Act
        var stopwatch = Stopwatch.StartNew();
        var publishTasks = new List<Task>();
        for (int i = 0; i < messageCount; i++)
        {
            var message = new TestMessage { Id = i, Data = $"Data{i}" };
            publishTasks.Add(broker.PublishAsync(message).AsTask());
        }

        await Task.WhenAll(publishTasks);
        stopwatch.Stop();

        // Give time for all subscribers to process
        await Task.Delay(500);

        // Assert
        var totalTimeSeconds = stopwatch.Elapsed.TotalSeconds;
        var totalDeliveries = subscriberCount * messageCount;
        var deliveriesPerSecond = totalDeliveries / totalTimeSeconds;

        _output.WriteLine($"Delivered {totalDeliveries} messages to {subscriberCount} subscribers in {totalTimeSeconds:F2}s");
        _output.WriteLine($"Delivery rate: {deliveriesPerSecond:F0} deliveries/second");

        // Verify all subscribers received all messages
        foreach (var count in receivedCounts)
        {
            Assert.Equal(messageCount, count);
        }

        // Should maintain reasonable delivery rate
        Assert.True(deliveriesPerSecond >= 1000,
            $"Delivery rate {deliveriesPerSecond:F0} deliveries/s is too low");
    }

    public class TestMessage
    {
        public int Id { get; set; }
        public string Data { get; set; } = string.Empty;
        public long Timestamp { get; set; }
    }
}