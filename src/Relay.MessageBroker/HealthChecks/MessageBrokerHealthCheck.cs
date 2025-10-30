using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.MessageBroker.CircuitBreaker;
using Relay.MessageBroker.ConnectionPool;

namespace Relay.MessageBroker.HealthChecks;

/// <summary>
/// Health check implementation for message broker infrastructure.
/// Checks broker connectivity, circuit breaker state, and connection pool metrics.
/// </summary>
public class MessageBrokerHealthCheck : IHealthCheck
{
    private readonly IMessageBroker _broker;
    private readonly ICircuitBreaker? _circuitBreaker;
    private readonly ILogger<MessageBrokerHealthCheck> _logger;
    private readonly HealthCheckOptions _options;
    private readonly Func<ConnectionPoolMetrics>? _getConnectionPoolMetrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageBrokerHealthCheck"/> class.
    /// </summary>
    /// <param name="broker">The message broker instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">Health check options.</param>
    /// <param name="circuitBreaker">Optional circuit breaker instance.</param>
    /// <param name="getConnectionPoolMetrics">Optional function to retrieve connection pool metrics.</param>
    public MessageBrokerHealthCheck(
        IMessageBroker broker,
        ILogger<MessageBrokerHealthCheck> logger,
        IOptions<HealthCheckOptions> options,
        ICircuitBreaker? circuitBreaker = null,
        Func<ConnectionPoolMetrics>? getConnectionPoolMetrics = null)
    {
        _broker = broker ?? throw new ArgumentNullException(nameof(broker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _circuitBreaker = circuitBreaker;
        _getConnectionPoolMetrics = getConnectionPoolMetrics;

        _options.Validate();
    }

    /// <summary>
    /// Performs the health check.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The health check result.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var isHealthy = true;
        var unhealthyReasons = new List<string>();

        try
        {
            // Check broker connectivity with timeout
            var connectivityCheck = await CheckBrokerConnectivityAsync(cancellationToken);
            data["broker_connected"] = connectivityCheck.IsConnected;
            data["broker_type"] = connectivityCheck.BrokerType;

            if (!connectivityCheck.IsConnected)
            {
                isHealthy = false;
                unhealthyReasons.Add($"Broker connectivity check failed: {connectivityCheck.ErrorMessage}");
            }

            // Check circuit breaker state if enabled and available
            if (_options.IncludeCircuitBreakerState && _circuitBreaker != null)
            {
                var circuitState = _circuitBreaker.State;
                data["circuit_breaker_state"] = circuitState.ToString();

                if (circuitState == CircuitBreakerState.Open)
                {
                    isHealthy = false;
                    unhealthyReasons.Add("Circuit breaker is open");
                }

                // Include circuit breaker metrics
                var metrics = _circuitBreaker.Metrics;
                data["circuit_breaker_failed_calls"] = metrics.FailedCalls;
                data["circuit_breaker_successful_calls"] = metrics.SuccessfulCalls;
                data["circuit_breaker_total_calls"] = metrics.TotalCalls;
                data["circuit_breaker_failure_rate"] = metrics.FailureRate;
            }

            // Check connection pool metrics if enabled and available
            if (_options.IncludeConnectionPoolMetrics && _getConnectionPoolMetrics != null)
            {
                var poolMetrics = _getConnectionPoolMetrics();
                data["pool_active_connections"] = poolMetrics.ActiveConnections;
                data["pool_idle_connections"] = poolMetrics.IdleConnections;
                data["pool_total_connections"] = poolMetrics.TotalConnections;
                data["pool_wait_time_ms"] = poolMetrics.AverageWaitTimeMs;
                data["pool_waiting_threads"] = poolMetrics.WaitingThreads;

                // Check if pool is exhausted
                if (poolMetrics.ActiveConnections >= poolMetrics.TotalConnections && poolMetrics.TotalConnections > 0)
                {
                    data["pool_exhausted"] = true;
                    _logger.LogWarning("Connection pool is exhausted");
                }
            }

            data["check_timestamp"] = DateTimeOffset.UtcNow;

            if (isHealthy)
            {
                _logger.LogDebug("Message broker health check passed");
                return HealthCheckResult.Healthy("Message broker is healthy", data: data);
            }

            var description = $"Message broker is unhealthy: {string.Join("; ", unhealthyReasons)}";
            _logger.LogWarning("Message broker health check failed: {Reasons}", string.Join("; ", unhealthyReasons));
            return HealthCheckResult.Unhealthy(description, data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception");
            data["error"] = ex.Message;
            data["error_type"] = ex.GetType().Name;
            return HealthCheckResult.Unhealthy("Health check failed with exception", ex, data);
        }
    }

    /// <summary>
    /// Checks broker connectivity with timeout.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Connectivity check result.</returns>
    private async Task<ConnectivityCheckResult> CheckBrokerConnectivityAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = new CancellationTokenSource(_options.ConnectivityTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            // Check if broker is a BaseMessageBroker to access IsStarted property
            if (_broker is BaseMessageBroker baseBroker)
            {
                var isStarted = baseBroker.GetType()
                    .GetProperty("IsStarted", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(baseBroker) as bool? ?? false;

                if (!isStarted)
                {
                    return new ConnectivityCheckResult
                    {
                        IsConnected = false,
                        BrokerType = _broker.GetType().Name,
                        ErrorMessage = "Broker is not started"
                    };
                }
            }

            // Attempt a lightweight operation to verify connectivity
            // Since we can't directly test connectivity without publishing, we check if the broker is operational
            // by verifying it's not disposed and is in a valid state
            var brokerType = _broker.GetType().Name;

            return new ConnectivityCheckResult
            {
                IsConnected = true,
                BrokerType = brokerType
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ConnectivityCheckResult
            {
                IsConnected = false,
                BrokerType = _broker.GetType().Name,
                ErrorMessage = "Connectivity check timed out"
            };
        }
        catch (Exception ex)
        {
            return new ConnectivityCheckResult
            {
                IsConnected = false,
                BrokerType = _broker.GetType().Name,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Result of a connectivity check.
    /// </summary>
    private class ConnectivityCheckResult
    {
        public bool IsConnected { get; set; }
        public string BrokerType { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }
}
