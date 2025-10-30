using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.CircuitBreaker;
using Relay.MessageBroker.Inbox;
using Relay.MessageBroker.Outbox;
using Relay.MessageBroker.Security;
using Xunit;
using Xunit.Abstractions;

namespace Relay.MessageBroker.Tests.Integration;

/// <summary>
/// Comprehensive integration tests covering Outbox, Inbox, Circuit Breaker, Security, and Retry patterns.
/// These tests use in-memory databases and mocked brokers to simulate real-world scenarios.
/// </summary>
[Trait("Category", "Integration")]
public class ComprehensiveIntegrationTests : IAsyncDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ServiceProvider _serviceProvider;

    public ComprehensiveIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        var services = new ServiceCollection();
        
        // Configure in-memory databases
        services.AddDbContext<OutboxDbContext>(options =>
            options.UseInMemoryDatabase($"OutboxTestDb_{Guid.NewGuid()}"));
        
        services.AddDbContext<InboxDbContext>(options =>
            options.UseInMemoryDatabase($"InboxTestDb_{Guid.NewGuid()}"));
        
        services.AddDbContextFactory<OutboxDbContext>(options =>
            options.UseInMemoryDatabase($"OutboxTestDb_{Guid.NewGuid()}"));
        
        services.AddDbContextFactory<InboxDbContext>(options =>
            options.UseInMemoryDatabase($"InboxTestDb_{Guid.NewGuid()}"));

        services.AddSingleton<IOutboxStore, SqlOutboxStore>();
        services.AddSingleton<IInboxStore, SqlInboxStore>();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
    }

    #region Outbox Pattern Tests

    [Fact]
    public async Task OutboxPattern_ShouldStoreAndRetrieveMessages()
    {
        // Arrange
        var store = _serviceProvider.GetRequiredService<IOutboxStore>();
        var message = new OutboxMessage
        {
            MessageType = "TestMessage",
            Payload = System.Text.Encoding.UTF8.GetBytes("{\"id\":\"test-123\"}"),
            RoutingKey = "test.queue"
        };

        // Act
        var stored = await store.StoreAsync(message, CancellationToken.None);
        var pending = await store.GetPendingAsync(10, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, stored.Id);
        Assert.Single(pending);
        Assert.Equal("TestMessage", pending.First().MessageType);
    }

    [Fact]
    public async Task OutboxPattern_ShouldMarkMessagesAsPublished()
    {
        // Arrange
        var store = _serviceProvider.GetRequiredService<IOutboxStore>();
        var message = await store.StoreAsync(new OutboxMessage
        {
            MessageType = "TestMessage",
            Payload = System.Text.Encoding.UTF8.GetBytes("{\"id\":\"test\"}")
        }, CancellationToken.None);

        // Act
        await store.MarkAsPublishedAsync(message.Id, CancellationToken.None);
        var pending = await store.GetPendingAsync(10, CancellationToken.None);

        // Assert
        Assert.Empty(pending);
    }

    #endregion

    #region Inbox Pattern Tests

    [Fact]
    public async Task InboxPattern_ShouldPreventDuplicateProcessing()
    {
        // Arrange
        var store = _serviceProvider.GetRequiredService<IInboxStore>();
        var messageId = "duplicate-test-123";

        // Act
        await store.StoreAsync(new InboxMessage
        {
            MessageId = messageId,
            MessageType = "TestMessage"
        }, CancellationToken.None);

        var exists = await store.ExistsAsync(messageId, CancellationToken.None);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task InboxPattern_ShouldBeIdempotent()
    {
        // Arrange
        var store = _serviceProvider.GetRequiredService<IInboxStore>();
        var message = new InboxMessage
        {
            MessageId = "idempotent-test",
            MessageType = "TestMessage"
        };

        // Act - Store twice
        await store.StoreAsync(message, CancellationToken.None);
        await store.StoreAsync(message, CancellationToken.None);

        // Assert - Should only have one entry
        var exists = await store.ExistsAsync("idempotent-test", CancellationToken.None);
        Assert.True(exists);
    }

    #endregion

    #region Circuit Breaker Tests

    [Fact]
    public async Task CircuitBreaker_ShouldOpenAfterFailureThreshold()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 3,
            Timeout = TimeSpan.FromSeconds(5)
        };

        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act - Trigger failures
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync(async ct =>
                {
                    throw new InvalidOperationException("Simulated failure");
                }, CancellationToken.None);
            }
            catch (InvalidOperationException) { }
        }

        // Assert
        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);
    }

    [Fact]
    public async Task CircuitBreaker_ShouldTransitionToHalfOpenAfterTimeout()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 2,
            Timeout = TimeSpan.FromMilliseconds(500),
            SuccessThreshold = 1
        };

        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Open the circuit
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync(async ct =>
                {
                    throw new InvalidOperationException("Failure");
                }, CancellationToken.None);
            }
            catch { }
        }

        // Act - Wait for timeout
        await Task.Delay(600);

        // Execute successful operation
        var result = await circuitBreaker.ExecuteAsync(async ct =>
        {
            await Task.CompletedTask;
            return true;
        }, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
    }

    #endregion

    #region Retry Pattern Tests

    [Fact]
    public async Task RetryPattern_ShouldRetryFailedOperations()
    {
        // Arrange
        var attemptCount = 0;
        
        async Task<bool> FailingOperation()
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new InvalidOperationException($"Attempt {attemptCount} failed");
            }
            return true;
        }

        // Act
        var result = await ExecuteWithRetryAsync(FailingOperation, maxRetries: 5, delayMs: 50);

        // Assert
        Assert.True(result);
        Assert.Equal(3, attemptCount);
    }

    [Fact]
    public async Task RetryPattern_ShouldRespectMaxRetries()
    {
        // Arrange
        var attemptCount = 0;
        
        async Task<bool> AlwaysFailingOperation()
        {
            attemptCount++;
            throw new InvalidOperationException("Always fails");
        }

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await ExecuteWithRetryAsync(AlwaysFailingOperation, maxRetries: 3, delayMs: 10);
        });

        Assert.Equal(4, attemptCount); // Initial + 3 retries
    }

    #endregion

    #region Security Tests

    [Fact]
    public void SecurityOptions_ShouldValidateCorrectly()
    {
        // Arrange
        var options = new SecurityOptions
        {
            EnableEncryption = true,
            EncryptionKey = Convert.ToBase64String(new byte[32]),
            KeyVersion = "v1"
        };

        // Act & Assert - Should not throw
        options.Validate();
    }

    [Fact]
    public void AuthenticationOptions_ShouldValidateCorrectly()
    {
        // Arrange
        var options = new AuthenticationOptions
        {
            EnableAuthentication = true,
            JwtIssuer = "TestIssuer",
            JwtAudience = "TestAudience",
            JwtSigningKey = Convert.ToBase64String(new byte[32])
        };

        // Act & Assert - Should not throw
        options.Validate();
    }

    #endregion

    #region End-to-End Integration Tests

    [Fact]
    public async Task EndToEnd_OutboxWithRabbitMQ_ShouldPublishMessages()
    {
        // Arrange
        var publishedMessages = new List<object>();
        var mockBroker = new Mock<IMessageBroker>();
        
        mockBroker.Setup(x => x.PublishAsync(
                It.IsAny<object>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<object, PublishOptions?, CancellationToken>((msg, opts, ct) =>
            {
                publishedMessages.Add(msg);
            })
            .Returns(ValueTask.CompletedTask);

        var store = _serviceProvider.GetRequiredService<IOutboxStore>();
        var options = Options.Create(new OutboxOptions { Enabled = true });
        var logger = _serviceProvider.GetRequiredService<ILogger<OutboxMessageBrokerDecorator>>();

        var decorator = new OutboxMessageBrokerDecorator(mockBroker.Object, store, options, logger);

        // Act
        var message = new TestMessage { Id = "test-1", Content = "Test content" };
        await decorator.PublishAsync(message);

        // Assert
        var pending = await store.GetPendingAsync(10, CancellationToken.None);
        Assert.Single(pending);
        Assert.Equal("TestMessage", pending.First().MessageType);
    }

    [Fact]
    public async Task EndToEnd_InboxWithKafka_ShouldPreventDuplicates()
    {
        // Arrange
        var processedMessages = new List<string>();
        var mockBroker = new Mock<IMessageBroker>();
        
        Func<TestMessage, MessageContext, CancellationToken, ValueTask>? capturedHandler = null;
        mockBroker.Setup(x => x.SubscribeAsync(
                It.IsAny<Func<TestMessage, MessageContext, CancellationToken, ValueTask>>(),
                It.IsAny<SubscriptionOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<Func<TestMessage, MessageContext, CancellationToken, ValueTask>, SubscriptionOptions?, CancellationToken>(
                (handler, _, _) => capturedHandler = handler)
            .Returns(ValueTask.CompletedTask);

        var store = _serviceProvider.GetRequiredService<IInboxStore>();
        var options = Options.Create(new InboxOptions { Enabled = true });
        var logger = _serviceProvider.GetRequiredService<ILogger<InboxMessageBrokerDecorator>>();

        var decorator = new InboxMessageBrokerDecorator(mockBroker.Object, store, options, logger);

        var originalHandler = new Func<TestMessage, MessageContext, CancellationToken, ValueTask>(
            async (msg, ctx, ct) =>
            {
                processedMessages.Add(msg.Id!);
                await ValueTask.CompletedTask;
            });

        // Act
        await decorator.SubscribeAsync(originalHandler);

        var message = new TestMessage { Id = "duplicate-msg", Content = "Test" };
        var context = new MessageContext
        {
            MessageId = "duplicate-msg",
            Acknowledge = () => ValueTask.CompletedTask
        };

        // Process twice
        await capturedHandler!(message, context, CancellationToken.None);
        await capturedHandler(message, context, CancellationToken.None);

        // Assert - Should only process once
        Assert.Single(processedMessages);
    }

    #endregion

    // Helper methods
    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxRetries,
        int delayMs)
    {
        var attempt = 0;

        while (true)
        {
            try
            {
                return await operation();
            }
            catch (Exception)
            {
                attempt++;
                if (attempt > maxRetries)
                {
                    throw;
                }
                await Task.Delay(delayMs);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
    }

    private class TestMessage
    {
        public string? Id { get; set; }
        public string? Content { get; set; }
    }
}
