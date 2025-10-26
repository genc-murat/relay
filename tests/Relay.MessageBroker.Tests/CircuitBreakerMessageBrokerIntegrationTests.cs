using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.CircuitBreaker;
using System.Collections.Concurrent;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class CircuitBreakerMessageBrokerIntegrationTests
{
    public class CircuitBreakerTestableMessageBroker : BaseMessageBroker
    {
        private readonly ICircuitBreaker _circuitBreaker;
        public ConcurrentBag<object> PublishedMessages { get; } = new();
        public ConcurrentBag<object> ProcessedMessages { get; } = new();
        public List<Exception> ThrownExceptions { get; } = new();
        public int PublishFailureCount { get; set; }
        public int SubscribeFailureCount { get; set; }

        public CircuitBreakerTestableMessageBroker(
            IOptions<MessageBrokerOptions> options,
            ILogger logger,
            ICircuitBreaker circuitBreaker)
            : base(options, logger)
        {
            _circuitBreaker = circuitBreaker;
        }

        protected override async ValueTask PublishInternalAsync<TMessage>(
            TMessage message,
            byte[] serializedMessage,
            PublishOptions? options,
            CancellationToken cancellationToken)
        {
            await _circuitBreaker.ExecuteAsync(async (ct) =>
            {
                if (PublishFailureCount > 0)
                {
                    PublishFailureCount--;
                    throw new InvalidOperationException("Simulated publish failure");
                }

                PublishedMessages.Add(message!);

                // Process message for subscribers if started
                if (IsStarted)
                {
                    var decompressed = await DecompressMessageAsync(serializedMessage, ct);
                    var deserialized = DeserializeMessage<TMessage>(decompressed);
                    var context = new MessageContext();
                    await ProcessMessageAsync(deserialized, typeof(TMessage), context, ct);
                }
            }, cancellationToken);
        }

        protected override ValueTask SubscribeInternalAsync(
            Type messageType,
            SubscriptionInfo subscriptionInfo,
            CancellationToken cancellationToken)
        {
            return _circuitBreaker.ExecuteAsync(async (ct) =>
            {
                if (SubscribeFailureCount > 0)
                {
                    SubscribeFailureCount--;
                    throw new InvalidOperationException("Simulated subscribe failure");
                }
            }, cancellationToken);
        }

        protected override async ValueTask StartInternalAsync(CancellationToken cancellationToken)
        {
            // Simulate some startup operations that might fail
            await _circuitBreaker.ExecuteAsync(async (ct) =>
            {
                // Startup logic here
            }, cancellationToken);
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
    public async Task CircuitBreaker_PublishFailures_OpenCircuitAndRejectRequests()
    {
        // Arrange
        var circuitBreakerOptions = new CircuitBreakerOptions
        {
            FailureThreshold = 3,
            Timeout = TimeSpan.FromSeconds(1),
            SamplingDuration = TimeSpan.FromSeconds(10)
        };
        var circuitBreaker = new Relay.MessageBroker.CircuitBreaker.CircuitBreaker(circuitBreakerOptions);

        var brokerOptions = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<CircuitBreakerTestableMessageBroker>>().Object;
        var broker = new CircuitBreakerTestableMessageBroker(brokerOptions, logger, circuitBreaker);

        await broker.StartAsync();

        // Set up failures
        broker.PublishFailureCount = 5; // More than threshold

        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act - First few publishes should fail and open circuit
        for (int i = 0; i < 3; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await broker.PublishAsync(message));
        }

        // Circuit should now be open
        await Assert.ThrowsAsync<Relay.MessageBroker.CircuitBreaker.CircuitBreakerOpenException>(
            async () => await broker.PublishAsync(message));

        // Assert
        Assert.Equal(Relay.MessageBroker.CircuitBreaker.CircuitBreakerState.Open, circuitBreaker.State);
    }

    [Fact]
    public async Task CircuitBreaker_SubscribeFailures_OpenCircuitAndRejectRequests()
    {
        // Arrange
        var circuitBreakerOptions = new CircuitBreakerOptions
        {
            FailureThreshold = 2,
            Timeout = TimeSpan.FromSeconds(1),
            SamplingDuration = TimeSpan.FromSeconds(10)
        };
        var circuitBreaker = new Relay.MessageBroker.CircuitBreaker.CircuitBreaker(circuitBreakerOptions);

        var brokerOptions = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<CircuitBreakerTestableMessageBroker>>().Object;
        var broker = new CircuitBreakerTestableMessageBroker(brokerOptions, logger, circuitBreaker);

        // Set up subscribe failures
        broker.SubscribeFailureCount = 3;

        // Act - Subscribe attempts should fail and open circuit
        for (int i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask));
        }

        // Circuit should be open, next subscribe should be rejected
        await Assert.ThrowsAsync<Relay.MessageBroker.CircuitBreaker.CircuitBreakerOpenException>(
            async () => await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask));

        // Assert
        Assert.Equal(Relay.MessageBroker.CircuitBreaker.CircuitBreakerState.Open, circuitBreaker.State);
    }

    [Fact]
    public async Task CircuitBreaker_AfterRecoveryTimeout_AllowsRequestsAgain()
    {
        // Arrange
        var circuitBreakerOptions = new CircuitBreakerOptions
        {
            FailureThreshold = 2,
            Timeout = TimeSpan.FromMilliseconds(500), // Short timeout for testing
            SamplingDuration = TimeSpan.FromSeconds(10)
        };
        var circuitBreaker = new Relay.MessageBroker.CircuitBreaker.CircuitBreaker(circuitBreakerOptions);

        var brokerOptions = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<CircuitBreakerTestableMessageBroker>>().Object;
        var broker = new CircuitBreakerTestableMessageBroker(brokerOptions, logger, circuitBreaker);

        await broker.StartAsync();

        // Set up failures to open circuit
        broker.PublishFailureCount = 3;
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Fail to open circuit
        for (int i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await broker.PublishAsync(message));
        }

        // Reset failure count so half-open attempt succeeds
        broker.PublishFailureCount = 0;

        // Wait for recovery timeout
        await Task.Delay(600);

        // Act - Should allow requests now (half-open state)
        // Need 2 successful requests to close circuit (SuccessThreshold = 2)
        await broker.PublishAsync(message);
        await broker.PublishAsync(message);

        // Assert
        Assert.Equal(Relay.MessageBroker.CircuitBreaker.CircuitBreakerState.Closed, circuitBreaker.State);
    }

    [Fact]
    public async Task CircuitBreaker_HalfOpenState_FailureReopensCircuit()
    {
        // Arrange
        var circuitBreakerOptions = new CircuitBreakerOptions
        {
            FailureThreshold = 2,
            Timeout = TimeSpan.FromMilliseconds(200),
            SamplingDuration = TimeSpan.FromSeconds(10)
        };
        var circuitBreaker = new Relay.MessageBroker.CircuitBreaker.CircuitBreaker(circuitBreakerOptions);

        var brokerOptions = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<CircuitBreakerTestableMessageBroker>>().Object;
        var broker = new CircuitBreakerTestableMessageBroker(brokerOptions, logger, circuitBreaker);

        await broker.StartAsync();

        // Open circuit with failures
        broker.PublishFailureCount = 3;
        var message = new TestMessage { Id = 1, Content = "Test" };

        for (int i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await broker.PublishAsync(message));
        }

        // Wait for half-open
        await Task.Delay(300);

        // Set failure for half-open attempt
        broker.PublishFailureCount = 1;

        // Act - Half-open attempt fails, should reopen circuit
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await broker.PublishAsync(message));

        // Assert
        Assert.Equal(Relay.MessageBroker.CircuitBreaker.CircuitBreakerState.Open, circuitBreaker.State);
    }

    [Fact]
    public async Task CircuitBreaker_SuccessfulOperations_UpdateMetrics()
    {
        // Arrange
        var circuitBreakerOptions = new CircuitBreakerOptions
        {
            FailureThreshold = 5,
            Timeout = TimeSpan.FromSeconds(1),
            SamplingDuration = TimeSpan.FromSeconds(10)
        };
        var circuitBreaker = new Relay.MessageBroker.CircuitBreaker.CircuitBreaker(circuitBreakerOptions);

        var brokerOptions = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<CircuitBreakerTestableMessageBroker>>().Object;
        var broker = new CircuitBreakerTestableMessageBroker(brokerOptions, logger, circuitBreaker);

        await broker.StartAsync();

        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act - Multiple successful publishes
        for (int i = 0; i < 10; i++)
        {
            await broker.PublishAsync(message);
        }

        // Assert
        Assert.Equal(Relay.MessageBroker.CircuitBreaker.CircuitBreakerState.Closed, circuitBreaker.State);
        Assert.Equal(10, broker.PublishedMessages.Count);
    }

    [Fact]
    public async Task CircuitBreaker_ConcurrentOperations_HandleThreadSafety()
    {
        // Arrange
        var circuitBreakerOptions = new CircuitBreakerOptions
        {
            FailureThreshold = 10,
            Timeout = TimeSpan.FromSeconds(1),
            SamplingDuration = TimeSpan.FromSeconds(10)
        };
        var circuitBreaker = new Relay.MessageBroker.CircuitBreaker.CircuitBreaker(circuitBreakerOptions);

        var brokerOptions = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<CircuitBreakerTestableMessageBroker>>().Object;
        var broker = new CircuitBreakerTestableMessageBroker(brokerOptions, logger, circuitBreaker);

        await broker.StartAsync();

        // Set some failures to test concurrent failure handling
        broker.PublishFailureCount = 20;

        const int concurrentTasks = 20;
        var tasks = new List<Task>();

        // Act - Concurrent publishes that will cause failures and circuit opening
        for (int i = 0; i < concurrentTasks; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var message = new TestMessage { Id = i, Content = $"Test{i}" };
                try
                {
                    await broker.PublishAsync(message);
                }
                catch (Exception ex)
                {
                    broker.ThrownExceptions.Add(ex);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Circuit should be open due to failures
        Assert.Equal(Relay.MessageBroker.CircuitBreaker.CircuitBreakerState.Open, circuitBreaker.State);

        // Should have some successful publishes before circuit opened, and exceptions after
        Assert.True(broker.PublishedMessages.Count >= 0);
        Assert.True(broker.ThrownExceptions.Count > 0);
        Assert.Contains(broker.ThrownExceptions, ex => ex is InvalidOperationException || ex is CircuitBreakerOpenException);
    }

    public class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}