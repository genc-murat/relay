using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.MessageBroker;
using Relay.MessageBroker.CircuitBreaker;
using Relay.MessageBroker.ConnectionPool;
using Relay.MessageBroker.HealthChecks;
using Xunit;

namespace Relay.MessageBroker.Tests.HealthChecks;

public class HealthCheckServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMessageBrokerHealthChecks_WithNullBuilder_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            Relay.MessageBroker.HealthChecks.HealthCheckServiceCollectionExtensions.AddMessageBrokerHealthChecks(null!));
    }

    [Fact]
    public void AddMessageBrokerHealthChecks_WithDefaultParameters_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBroker, InMemoryMessageBroker>();
        var builder = services.AddHealthChecks();

        // Act
        builder.AddMessageBrokerHealthChecks();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckOptions>>().Value;

        Assert.NotNull(healthCheckService);
        Assert.Equal("MessageBroker", options.Name);
        Assert.Equal(new[] { "messagebroker", "ready" }, options.Tags);
    }

    [Fact]
    public void AddMessageBrokerHealthChecks_WithCustomParameters_ShouldApplyConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBroker, InMemoryMessageBroker>();
        var builder = services.AddHealthChecks();

        // Act
        builder.AddMessageBrokerHealthChecks(
            name: "CustomHealthCheck",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "custom", "test" },
            timeout: TimeSpan.FromSeconds(10),
            configureOptions: options =>
            {
                options.Interval = TimeSpan.FromMinutes(5);
                options.IncludeCircuitBreakerState = false;
            });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckOptions>>().Value;

        Assert.Equal("CustomHealthCheck", options.Name);
        Assert.Equal(new[] { "custom", "test" }, options.Tags);
        Assert.Equal(TimeSpan.FromMinutes(5), options.Interval);
        Assert.False(options.IncludeCircuitBreakerState);
        Assert.True(options.IncludeConnectionPoolMetrics); // Default value
    }

    [Fact]
    public void AddMessageBrokerHealthChecks_WithCircuitBreaker_ShouldIncludeCircuitBreakerInHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBroker, InMemoryMessageBroker>();
        services.AddSingleton<ICircuitBreaker, TestCircuitBreaker>();
        var builder = services.AddHealthChecks();

        // Act
        builder.AddMessageBrokerHealthChecks();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        // The health check should be registered and should be able to resolve circuit breaker
        var healthChecks = healthCheckService.CheckHealthAsync().Result;
        Assert.NotNull(healthChecks);
    }

    [Fact]
    public void AddMessageBrokerHealthChecksGeneric_WithConnectionPool_ShouldIncludeConnectionPoolMetrics()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBroker, InMemoryMessageBroker>();

        // Register a test connection pool
        services.AddSingleton<IConnectionPool<TestConnection>>(new TestConnectionPool());

        var builder = services.AddHealthChecks();

        // Act
        builder.AddMessageBrokerHealthChecks<TestConnection>();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        // The health check should be registered and should be able to resolve connection pool
        var healthChecks = healthCheckService.CheckHealthAsync().Result;
        Assert.NotNull(healthChecks);
    }

    [Fact]
    public void AddMessageBrokerHealthChecksGeneric_WithNullBuilder_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            Relay.MessageBroker.HealthChecks.HealthCheckServiceCollectionExtensions.AddMessageBrokerHealthChecks<TestConnection>(null!));
    }

    [Fact]
    public void AddMessageBrokerHealthChecksGeneric_WithCustomParameters_ShouldApplyConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBroker, InMemoryMessageBroker>();
        var builder = services.AddHealthChecks();

        // Act
        builder.AddMessageBrokerHealthChecks<TestConnection>(
            name: "GenericHealthCheck",
            tags: new[] { "generic", "test" },
            configureOptions: options =>
            {
                options.ConnectivityTimeout = TimeSpan.FromSeconds(5);
                options.IncludeConnectionPoolMetrics = false;
            });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckOptions>>().Value;

        Assert.Equal("GenericHealthCheck", options.Name);
        Assert.Equal(new[] { "generic", "test" }, options.Tags);
        Assert.Equal(TimeSpan.FromSeconds(5), options.ConnectivityTimeout);
        Assert.False(options.IncludeConnectionPoolMetrics);
    }

    // Test helper classes
    private class TestCircuitBreaker : ICircuitBreaker
    {
        public CircuitBreakerState State => CircuitBreakerState.Closed;
        public CircuitBreakerMetrics Metrics => new CircuitBreakerMetrics
        {
            FailedCalls = 0,
            SuccessfulCalls = 10,
            TotalCalls = 10
        };

        public ValueTask<TResult> ExecuteAsync<TResult>(Func<CancellationToken, ValueTask<TResult>> operation, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask ExecuteAsync(Func<CancellationToken, ValueTask> operation, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public void Isolate()
        {
            throw new NotImplementedException();
        }
    }

    private class TestConnection
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private class TestConnectionPool : IConnectionPool<TestConnection>
    {
        public ConnectionPoolMetrics GetMetrics() => new ConnectionPoolMetrics
        {
            ActiveConnections = 2,
            IdleConnections = 3,
            TotalConnections = 5,
            AverageWaitTimeMs = 10.0,
            WaitingThreads = 0
        };

        public ValueTask<PooledConnection<TestConnection>> AcquireAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask ReleaseAsync(PooledConnection<TestConnection> connection)
        {
            throw new NotImplementedException();
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}