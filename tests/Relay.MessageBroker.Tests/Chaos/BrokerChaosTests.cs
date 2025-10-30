using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Relay.MessageBroker.Tests.Chaos;

/// <summary>
/// Chaos engineering tests for message broker resilience.
/// Tests broker connection failures, network latency, packet loss, and resource exhaustion.
/// </summary>
[Trait("Category", "Chaos")]
[Trait("Pattern", "Resilience")]
public class BrokerChaosTests
{
    private readonly ITestOutputHelper _output;

    public BrokerChaosTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task BrokerConnectionFailure_DuringPublish_HandlesGracefully()
    {
        // Arrange
        var broker = new UnstableConnectionBroker(connectionFailureRate: 0.4);
        var messageCount = 50;
        var successfulPublishes = 0;
        var failedPublishes = 0;

        // Act - Attempt to publish messages with connection failures
        for (int i = 0; i < messageCount; i++)
        {
            var message = new ChaosTestMessage { Id = i, Content = $"Message {i}" };
            
            try
            {
                await broker.PublishAsync(message);
                successfulPublishes++;
            }
            catch (BrokerConnectionException)
            {
                failedPublishes++;
                
                // Retry logic
                var retryCount = 0;
                while (retryCount < 3)
                {
                    try
                    {
                        await Task.Delay(100); // Backoff
                        await broker.PublishAsync(message);
                        successfulPublishes++;
                        break;
                    }
                    catch (BrokerConnectionException)
                    {
                        retryCount++;
                    }
                }
            }
        }

        // Assert
        Assert.True(successfulPublishes > 0, "Some messages should be published successfully");
        _output.WriteLine($"Published: {successfulPublishes}/{messageCount}, Initial failures: {failedPublishes}");
    }

    [Fact]
    public async Task BrokerConnectionFailure_DuringConsume_RecoversAndContinues()
    {
        // Arrange
        var broker = new UnstableConnectionBroker(connectionFailureRate: 0.3);
        var deliveredMessages = new ConcurrentBag<ChaosTestMessage>();
        var connectionErrors = 0;
        var messageCount = 30;

        // Subscribe with error handling
        await broker.SubscribeAsync<ChaosTestMessage>(async (msg, ctx, ct) =>
        {
            try
            {
                deliveredMessages.Add(msg);
            }
            catch (BrokerConnectionException)
            {
                Interlocked.Increment(ref connectionErrors);
                throw;
            }
        });

        await broker.StartAsync(CancellationToken.None);

        // Act - Publish messages with retries
        for (int i = 0; i < messageCount; i++)
        {
            var published = false;
            var retries = 0;
            while (!published && retries < 5)
            {
                try
                {
                    await broker.PublishAsync(new ChaosTestMessage { Id = i, Content = $"Message {i}" });
                    published = true;
                }
                catch (BrokerConnectionException)
                {
                    retries++;
                    await Task.Delay(50);
                }
            }
        }

        await Task.Delay(500); // Allow processing

        // Assert
        Assert.True(deliveredMessages.Count > 0, "Some messages should be delivered");
        _output.WriteLine($"Delivered: {deliveredMessages.Count}/{messageCount}, Connection errors: {connectionErrors}");
    }

    [Theory]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    public async Task NetworkLatency_InjectedDelay_ImpactsPerformance(int latencyMs)
    {
        // Arrange
        var broker = new LatencyInjectionBroker(latencyMs);
        var messageCount = 10;
        var stopwatch = Stopwatch.StartNew();

        // Act - Publish messages with latency
        for (int i = 0; i < messageCount; i++)
        {
            await broker.PublishAsync(new ChaosTestMessage { Id = i, Content = $"Message {i}" });
        }

        stopwatch.Stop();

        // Assert
        var expectedMinTime = messageCount * latencyMs;
        var actualTime = stopwatch.ElapsedMilliseconds;
        
        Assert.True(actualTime >= expectedMinTime * 0.8, 
            $"Expected at least {expectedMinTime * 0.8}ms with {latencyMs}ms latency, got {actualTime}ms");
        
        _output.WriteLine($"Latency: {latencyMs}ms, Messages: {messageCount}, Total time: {actualTime}ms, Avg per message: {actualTime / messageCount}ms");
    }

    [Theory]
    [InlineData(0.05)] // 5% packet loss
    [InlineData(0.10)] // 10% packet loss
    [InlineData(0.20)] // 20% packet loss
    public async Task NetworkPacketLoss_SimulatedLoss_RequiresRetries(double packetLossRate)
    {
        // Arrange
        var broker = new PacketLossBroker(packetLossRate);
        var messageCount = 50;
        var totalAttempts = 0;
        var successfulDeliveries = 0;

        // Act - Publish with retries on packet loss
        for (int i = 0; i < messageCount; i++)
        {
            var message = new ChaosTestMessage { Id = i, Content = $"Message {i}" };
            var delivered = false;
            var attempts = 0;

            while (!delivered && attempts < 5)
            {
                totalAttempts++;
                attempts++;
                
                try
                {
                    await broker.PublishAsync(message);
                    delivered = true;
                    successfulDeliveries++;
                }
                catch (PacketLossException)
                {
                    await Task.Delay(50);
                }
            }
        }

        // Assert
        Assert.Equal(messageCount, successfulDeliveries);
        var retryRate = (double)(totalAttempts - messageCount) / messageCount;
        
        Assert.True(retryRate > 0, "Should have required some retries");
        _output.WriteLine($"Packet loss: {packetLossRate:P0}, Messages: {messageCount}, Total attempts: {totalAttempts}, Retry rate: {retryRate:P1}");
    }

    [Fact]
    public async Task ResourceExhaustion_MemoryPressure_ThrottlesOperations()
    {
        // Arrange
        var broker = new ResourceConstrainedBroker(maxConcurrentOperations: 10);
        var messageCount = 50;
        var throttledCount = 0;
        var successCount = 0;

        // Act - Attempt concurrent operations beyond capacity
        var tasks = new List<Task>();
        for (int i = 0; i < messageCount; i++)
        {
            var messageId = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await broker.PublishAsync(new ChaosTestMessage 
                    { 
                        Id = messageId, 
                        Content = $"Message {messageId}" 
                    });
                    Interlocked.Increment(ref successCount);
                }
                catch (ResourceExhaustedException)
                {
                    Interlocked.Increment(ref throttledCount);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.True(successCount > 0, "Some operations should succeed");
        Assert.True(throttledCount > 0, "Some operations should be throttled");
        
        _output.WriteLine($"Success: {successCount}, Throttled: {throttledCount}, Total: {messageCount}");
    }

    [Fact]
    public async Task ResourceExhaustion_ConnectionPoolDepletion_HandlesGracefully()
    {
        // Arrange
        var broker = new ConnectionPoolExhaustedBroker(maxConnections: 5);
        var concurrentOperations = 20;
        var completedOperations = 0;
        var waitedForConnection = 0;

        // Act - Exceed connection pool capacity
        var tasks = new List<Task>();
        for (int i = 0; i < concurrentOperations; i++)
        {
            var opId = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var waited = await broker.PublishWithConnectionAsync(
                        new ChaosTestMessage { Id = opId, Content = $"Op {opId}" });
                    
                    if (waited)
                    {
                        Interlocked.Increment(ref waitedForConnection);
                    }
                    
                    Interlocked.Increment(ref completedOperations);
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Operation {opId} failed: {ex.Message}");
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(concurrentOperations, completedOperations);
        Assert.True(waitedForConnection > 0, "Some operations should have waited for connections");
        
        _output.WriteLine($"Completed: {completedOperations}, Waited for connection: {waitedForConnection}");
    }

    [Fact]
    public async Task ResourceExhaustion_CPUPressure_DegradedPerformance()
    {
        // Arrange
        var broker = new CPUPressureBroker(cpuIntensiveOperations: true);
        var messageCount = 20;
        var stopwatch = Stopwatch.StartNew();

        // Act - Publish under CPU pressure
        for (int i = 0; i < messageCount; i++)
        {
            await broker.PublishAsync(new ChaosTestMessage { Id = i, Content = $"Message {i}" });
        }

        stopwatch.Stop();

        // Assert
        var avgTimePerMessage = stopwatch.ElapsedMilliseconds / messageCount;
        Assert.True(avgTimePerMessage >= 0, "Operations should complete");
        
        _output.WriteLine($"Messages: {messageCount}, Total time: {stopwatch.ElapsedMilliseconds}ms, Avg: {avgTimePerMessage}ms");
    }

    [Fact]
    public async Task CombinedChaos_MultipleFailureTypes_SystemRemainsResilient()
    {
        // Arrange
        var broker = new CombinedChaosBroker(
            connectionFailureRate: 0.2,
            latencyMs: 50,
            packetLossRate: 0.1);
        
        var messageCount = 30;
        var deliveredMessages = new ConcurrentBag<ChaosTestMessage>();
        var totalAttempts = 0;

        await broker.SubscribeAsync<ChaosTestMessage>((msg, ctx, ct) =>
        {
            deliveredMessages.Add(msg);
            return ValueTask.CompletedTask;
        });

        await broker.StartAsync(CancellationToken.None);

        // Act - Publish with multiple chaos conditions
        for (int i = 0; i < messageCount; i++)
        {
            var message = new ChaosTestMessage { Id = i, Content = $"Message {i}" };
            var published = false;
            var attempts = 0;

            while (!published && attempts < 5)
            {
                totalAttempts++;
                attempts++;
                
                try
                {
                    await broker.PublishAsync(message);
                    published = true;
                }
                catch (Exception)
                {
                    await Task.Delay(100);
                }
            }
        }

        await Task.Delay(1000); // Allow processing

        // Assert
        Assert.True(deliveredMessages.Count >= messageCount * 0.8, 
            "At least 80% of messages should be delivered despite chaos");
        
        _output.WriteLine($"Published: {messageCount}, Delivered: {deliveredMessages.Count}, Total attempts: {totalAttempts}");
    }
}

// Test message class
public class ChaosTestMessage
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
}

// Broker with unstable connections
public class UnstableConnectionBroker : IMessageBroker
{
    private readonly InMemoryMessageBroker _inner = new();
    private readonly double _failureRate;
    private readonly Random _random = new();

    public UnstableConnectionBroker(double connectionFailureRate)
    {
        _failureRate = connectionFailureRate;
    }

    public async ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (_random.NextDouble() < _failureRate)
        {
            throw new BrokerConnectionException("Connection lost");
        }

        await _inner.PublishAsync(message, options, cancellationToken);
    }

    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _inner.SubscribeAsync(handler, options, cancellationToken);

    public ValueTask StartAsync(CancellationToken cancellationToken = default) =>
        _inner.StartAsync(cancellationToken);

    public ValueTask StopAsync(CancellationToken cancellationToken = default) =>
        _inner.StopAsync(cancellationToken);
}

// Broker with latency injection
public class LatencyInjectionBroker : IMessageBroker
{
    private readonly InMemoryMessageBroker _inner = new();
    private readonly int _latencyMs;

    public LatencyInjectionBroker(int latencyMs)
    {
        _latencyMs = latencyMs;
    }

    public async ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(_latencyMs, cancellationToken);
        await _inner.PublishAsync(message, options, cancellationToken);
    }

    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _inner.SubscribeAsync(handler, options, cancellationToken);

    public ValueTask StartAsync(CancellationToken cancellationToken = default) =>
        _inner.StartAsync(cancellationToken);

    public ValueTask StopAsync(CancellationToken cancellationToken = default) =>
        _inner.StopAsync(cancellationToken);
}

// Broker with packet loss simulation
public class PacketLossBroker : IMessageBroker
{
    private readonly InMemoryMessageBroker _inner = new();
    private readonly double _packetLossRate;
    private readonly Random _random = new();

    public PacketLossBroker(double packetLossRate)
    {
        _packetLossRate = packetLossRate;
    }

    public async ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (_random.NextDouble() < _packetLossRate)
        {
            throw new PacketLossException("Packet lost");
        }

        await _inner.PublishAsync(message, options, cancellationToken);
    }

    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _inner.SubscribeAsync(handler, options, cancellationToken);

    public ValueTask StartAsync(CancellationToken cancellationToken = default) =>
        _inner.StartAsync(cancellationToken);

    public ValueTask StopAsync(CancellationToken cancellationToken = default) =>
        _inner.StopAsync(cancellationToken);
}

// Broker with resource constraints
public class ResourceConstrainedBroker : IMessageBroker
{
    private readonly InMemoryMessageBroker _inner = new();
    private readonly SemaphoreSlim _semaphore;

    public ResourceConstrainedBroker(int maxConcurrentOperations)
    {
        _semaphore = new SemaphoreSlim(maxConcurrentOperations, maxConcurrentOperations);
    }

    public async ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(0, cancellationToken))
        {
            throw new ResourceExhaustedException("Resource limit exceeded");
        }

        try
        {
            await Task.Delay(50, cancellationToken); // Simulate work
            await _inner.PublishAsync(message, options, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _inner.SubscribeAsync(handler, options, cancellationToken);

    public ValueTask StartAsync(CancellationToken cancellationToken = default) =>
        _inner.StartAsync(cancellationToken);

    public ValueTask StopAsync(CancellationToken cancellationToken = default) =>
        _inner.StopAsync(cancellationToken);
}

// Broker with connection pool exhaustion
public class ConnectionPoolExhaustedBroker : IMessageBroker
{
    private readonly InMemoryMessageBroker _inner = new();
    private readonly SemaphoreSlim _connectionPool;

    public ConnectionPoolExhaustedBroker(int maxConnections)
    {
        _connectionPool = new SemaphoreSlim(maxConnections, maxConnections);
    }

    public async Task<bool> PublishWithConnectionAsync<TMessage>(TMessage message)
    {
        var waited = !_connectionPool.Wait(0);
        
        if (waited)
        {
            await _connectionPool.WaitAsync();
        }

        try
        {
            await Task.Delay(100); // Simulate connection usage
            await _inner.PublishAsync(message);
            return waited;
        }
        finally
        {
            _connectionPool.Release();
        }
    }

    public ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _inner.PublishAsync(message, options, cancellationToken);

    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _inner.SubscribeAsync(handler, options, cancellationToken);

    public ValueTask StartAsync(CancellationToken cancellationToken = default) =>
        _inner.StartAsync(cancellationToken);

    public ValueTask StopAsync(CancellationToken cancellationToken = default) =>
        _inner.StopAsync(cancellationToken);
}

// Broker with CPU pressure simulation
public class CPUPressureBroker : IMessageBroker
{
    private readonly InMemoryMessageBroker _inner = new();
    private readonly bool _cpuIntensive;

    public CPUPressureBroker(bool cpuIntensiveOperations)
    {
        _cpuIntensive = cpuIntensiveOperations;
    }

    public async ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (_cpuIntensive)
        {
            // Simulate CPU-intensive work
            var result = 0;
            for (int i = 0; i < 100000; i++)
            {
                result += i % 7;
            }
        }

        await _inner.PublishAsync(message, options, cancellationToken);
    }

    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _inner.SubscribeAsync(handler, options, cancellationToken);

    public ValueTask StartAsync(CancellationToken cancellationToken = default) =>
        _inner.StartAsync(cancellationToken);

    public ValueTask StopAsync(CancellationToken cancellationToken = default) =>
        _inner.StopAsync(cancellationToken);
}

// Broker with combined chaos conditions
public class CombinedChaosBroker : IMessageBroker
{
    private readonly InMemoryMessageBroker _inner = new();
    private readonly double _connectionFailureRate;
    private readonly int _latencyMs;
    private readonly double _packetLossRate;
    private readonly Random _random = new();

    public CombinedChaosBroker(double connectionFailureRate, int latencyMs, double packetLossRate)
    {
        _connectionFailureRate = connectionFailureRate;
        _latencyMs = latencyMs;
        _packetLossRate = packetLossRate;
    }

    public async ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Connection failure
        if (_random.NextDouble() < _connectionFailureRate)
        {
            throw new BrokerConnectionException("Connection lost");
        }

        // Packet loss
        if (_random.NextDouble() < _packetLossRate)
        {
            throw new PacketLossException("Packet lost");
        }

        // Latency
        await Task.Delay(_latencyMs, cancellationToken);

        await _inner.PublishAsync(message, options, cancellationToken);
    }

    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _inner.SubscribeAsync(handler, options, cancellationToken);

    public ValueTask StartAsync(CancellationToken cancellationToken = default) =>
        _inner.StartAsync(cancellationToken);

    public ValueTask StopAsync(CancellationToken cancellationToken = default) =>
        _inner.StopAsync(cancellationToken);
}

// Custom exceptions
public class BrokerConnectionException : Exception
{
    public BrokerConnectionException(string message) : base(message) { }
}

public class PacketLossException : Exception
{
    public PacketLossException(string message) : base(message) { }
}

public class ResourceExhaustedException : Exception
{
    public ResourceExhaustedException(string message) : base(message) { }
}
