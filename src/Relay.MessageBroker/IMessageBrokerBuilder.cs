using Microsoft.Extensions.DependencyInjection;
using Relay.MessageBroker.Backpressure;
using Relay.MessageBroker.Batch;
using Relay.MessageBroker.Bulkhead;
using Relay.MessageBroker.ConnectionPool;
using Relay.MessageBroker.Deduplication;
using Relay.MessageBroker.DistributedTracing;
using Relay.MessageBroker.HealthChecks;
using Relay.MessageBroker.Inbox;
using Relay.MessageBroker.Outbox;
using Relay.MessageBroker.PoisonMessage;
using Relay.MessageBroker.RateLimit;
using Relay.MessageBroker.Security;

namespace Relay.MessageBroker;

/// <summary>
/// Fluent builder interface for configuring message broker patterns and features.
/// </summary>
public interface IMessageBrokerBuilder
{
    /// <summary>
    /// Gets the service collection.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Adds the Outbox pattern for reliable message publishing.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithOutbox(Action<OutboxOptions>? configure = null);

    /// <summary>
    /// Adds the Inbox pattern for idempotent message processing.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithInbox(Action<InboxOptions>? configure = null);

    /// <summary>
    /// Adds connection pooling for improved performance.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithConnectionPool(Action<ConnectionPoolOptions>? configure = null);

    /// <summary>
    /// Adds batch processing for high-volume scenarios.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithBatching(Action<BatchOptions>? configure = null);

    /// <summary>
    /// Adds message deduplication.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithDeduplication(Action<DeduplicationOptions>? configure = null);

    /// <summary>
    /// Adds health checks for monitoring.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithHealthChecks(Action<HealthCheckOptions>? configure = null);

    /// <summary>
    /// Adds metrics and telemetry.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithMetrics();

    /// <summary>
    /// Adds distributed tracing.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithDistributedTracing(Action<DistributedTracingOptions>? configure = null);

    /// <summary>
    /// Adds message encryption.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithEncryption(Action<SecurityOptions>? configure = null);

    /// <summary>
    /// Adds authentication and authorization.
    /// </summary>
    /// <param name="configureAuth">Optional authentication configuration action.</param>
    /// <param name="configureAuthz">Optional authorization configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithAuthentication(
        Action<AuthenticationOptions>? configureAuth = null,
        Action<AuthorizationOptions>? configureAuthz = null);

    /// <summary>
    /// Adds rate limiting.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithRateLimit(Action<RateLimitOptions>? configure = null);

    /// <summary>
    /// Adds bulkhead pattern for resource isolation.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithBulkhead(Action<BulkheadOptions>? configure = null);

    /// <summary>
    /// Adds poison message handling.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithPoisonMessageHandling(Action<PoisonMessageOptions>? configure = null);

    /// <summary>
    /// Adds backpressure management.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithBackpressure(Action<BackpressureOptions>? configure = null);

    /// <summary>
    /// Builds and registers all configured components.
    /// </summary>
    /// <returns>The service collection.</returns>
    IServiceCollection Build();
}
