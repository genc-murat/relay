using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Relay.MessageBroker.CircuitBreaker;
using Relay.MessageBroker.ConnectionPool;

namespace Relay.MessageBroker.HealthChecks;

/// <summary>
/// Extension methods for registering message broker health checks.
/// </summary>
public static class HealthCheckServiceCollectionExtensions
{
    /// <summary>
    /// Adds message broker health checks to the health checks builder.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The name of the health check. Defaults to "MessageBroker".</param>
    /// <param name="failureStatus">The health status to report when the check fails. Defaults to Unhealthy.</param>
    /// <param name="tags">Optional tags for the health check.</param>
    /// <param name="timeout">Optional timeout for the health check.</param>
    /// <param name="configureOptions">Optional action to configure health check options.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddMessageBrokerHealthChecks(
        this IHealthChecksBuilder builder,
        string? name = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null,
        Action<HealthCheckOptions>? configureOptions = null)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        // Configure health check options
        var options = new HealthCheckOptions();
        if (!string.IsNullOrWhiteSpace(name))
        {
            options.Name = name;
        }

        if (tags != null)
        {
            options.Tags = tags.ToArray();
        }

        configureOptions?.Invoke(options);
        options.Validate();

        // Register options
        builder.Services.Configure<HealthCheckOptions>(opts =>
        {
            opts.Interval = options.Interval;
            opts.ConnectivityTimeout = options.ConnectivityTimeout;
            opts.IncludeCircuitBreakerState = options.IncludeCircuitBreakerState;
            opts.IncludeConnectionPoolMetrics = options.IncludeConnectionPoolMetrics;
            opts.Name = options.Name;
            opts.Tags = options.Tags;
        });

        // Register the health check
        builder.Add(new HealthCheckRegistration(
            options.Name,
            sp =>
            {
                var broker = sp.GetRequiredService<IMessageBroker>();
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MessageBrokerHealthCheck>>();
                var healthCheckOptions = sp.GetRequiredService<IOptions<HealthCheckOptions>>();

                // Try to get optional dependencies
                var circuitBreaker = sp.GetService<ICircuitBreaker>();

                // Try to get connection pool metrics function
                Func<ConnectionPoolMetrics>? getMetrics = null;
                
                // Attempt to resolve connection pool for different connection types
                // This is a best-effort approach since we don't know the exact connection type
                try
                {
                    // Try common connection pool types
                    var poolTypes = new[]
                    {
                        typeof(IConnectionPool<>).MakeGenericType(typeof(object)),
                    };

                    foreach (var poolType in poolTypes)
                    {
                        var pool = sp.GetService(poolType);
                        if (pool != null)
                        {
                            var getMetricsMethod = poolType.GetMethod("GetMetrics");
                            if (getMetricsMethod != null)
                            {
                                getMetrics = () => (ConnectionPoolMetrics)getMetricsMethod.Invoke(pool, null)!;
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore errors when trying to resolve connection pool
                }

                return new MessageBrokerHealthCheck(
                    broker,
                    logger,
                    healthCheckOptions,
                    circuitBreaker,
                    getMetrics);
            },
            failureStatus,
            options.Tags,
            timeout));

        return builder;
    }

    /// <summary>
    /// Adds message broker health checks with a specific connection pool type.
    /// </summary>
    /// <typeparam name="TConnection">The connection type used by the connection pool.</typeparam>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The name of the health check. Defaults to "MessageBroker".</param>
    /// <param name="failureStatus">The health status to report when the check fails. Defaults to Unhealthy.</param>
    /// <param name="tags">Optional tags for the health check.</param>
    /// <param name="timeout">Optional timeout for the health check.</param>
    /// <param name="configureOptions">Optional action to configure health check options.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddMessageBrokerHealthChecks<TConnection>(
        this IHealthChecksBuilder builder,
        string? name = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null,
        Action<HealthCheckOptions>? configureOptions = null)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        // Configure health check options
        var options = new HealthCheckOptions();
        if (!string.IsNullOrWhiteSpace(name))
        {
            options.Name = name;
        }

        if (tags != null)
        {
            options.Tags = tags.ToArray();
        }

        configureOptions?.Invoke(options);
        options.Validate();

        // Register options
        builder.Services.Configure<HealthCheckOptions>(opts =>
        {
            opts.Interval = options.Interval;
            opts.ConnectivityTimeout = options.ConnectivityTimeout;
            opts.IncludeCircuitBreakerState = options.IncludeCircuitBreakerState;
            opts.IncludeConnectionPoolMetrics = options.IncludeConnectionPoolMetrics;
            opts.Name = options.Name;
            opts.Tags = options.Tags;
        });

        // Register the health check with typed connection pool
        builder.Add(new HealthCheckRegistration(
            options.Name,
            sp =>
            {
                var broker = sp.GetRequiredService<IMessageBroker>();
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MessageBrokerHealthCheck>>();
                var healthCheckOptions = sp.GetRequiredService<IOptions<HealthCheckOptions>>();

                // Try to get optional dependencies
                var circuitBreaker = sp.GetService<ICircuitBreaker>();
                var connectionPool = sp.GetService<IConnectionPool<TConnection>>();

                Func<ConnectionPoolMetrics>? getMetrics = null;
                if (connectionPool != null)
                {
                    getMetrics = () => connectionPool.GetMetrics();
                }

                return new MessageBrokerHealthCheck(
                    broker,
                    logger,
                    healthCheckOptions,
                    circuitBreaker,
                    getMetrics);
            },
            failureStatus,
            options.Tags,
            timeout));

        return builder;
    }
}
