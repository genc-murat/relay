using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Relay.MessageBroker.Saga.Services;

/// <summary>
/// Background service that monitors and handles saga timeouts.
/// Automatically compensates sagas that have exceeded their timeout duration.
/// </summary>
public class SagaTimeoutService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SagaTimeoutService> _logger;
    private readonly TimeSpan _checkInterval;
    private readonly TimeSpan _defaultTimeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaTimeoutService"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for creating scopes.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="checkInterval">Interval between timeout checks (default: 30 seconds).</param>
    /// <param name="defaultTimeout">Default timeout for sagas without explicit timeout (default: 5 minutes).</param>
    public SagaTimeoutService(
        IServiceProvider serviceProvider,
        ILogger<SagaTimeoutService> logger,
        TimeSpan? checkInterval = null,
        TimeSpan? defaultTimeout = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _checkInterval = checkInterval ?? TimeSpan.FromSeconds(30);
        _defaultTimeout = defaultTimeout ?? TimeSpan.FromMinutes(5);
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Saga Timeout Service started. Check interval: {CheckInterval}, Default timeout: {DefaultTimeout}",
            _checkInterval,
            _defaultTimeout);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndHandleTimeoutsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking saga timeouts");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown during delay
                break;
            }
        }

        _logger.LogInformation("Saga Timeout Service stopped");
    }

    /// <summary>
    /// Checks for timed-out sagas and triggers compensation.
    /// </summary>
    private async Task CheckAndHandleTimeoutsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var timeoutHandlers = scope.ServiceProvider.GetServices<ISagaTimeoutHandler>();

        var timedOutCount = 0;
        var checkedCount = 0;

        foreach (var handler in timeoutHandlers)
        {
            try
            {
                var result = await handler.CheckAndHandleTimeoutsAsync(_defaultTimeout, cancellationToken);
                checkedCount += result.CheckedCount;
                timedOutCount += result.TimedOutCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in timeout handler {HandlerType}", handler.GetType().Name);
            }
        }

        if (timedOutCount > 0)
        {
            _logger.LogWarning(
                "Found {TimedOutCount} timed-out sagas out of {CheckedCount} checked sagas",
                timedOutCount,
                checkedCount);
        }
        else if (checkedCount > 0)
        {
            _logger.LogDebug("Checked {CheckedCount} sagas, no timeouts found", checkedCount);
        }
    }
}

/// <summary>
/// Interface for saga timeout handlers.
/// Implement this for each saga type that supports timeout handling.
/// </summary>
public interface ISagaTimeoutHandler
{
    /// <summary>
    /// Checks for and handles timed-out sagas.
    /// </summary>
    /// <param name="defaultTimeout">Default timeout duration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing count of checked and timed-out sagas.</returns>
    ValueTask<SagaTimeoutCheckResult> CheckAndHandleTimeoutsAsync(
        TimeSpan defaultTimeout,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic implementation of saga timeout handler.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
public class SagaTimeoutHandler<TSagaData> : ISagaTimeoutHandler
    where TSagaData : ISagaData, new()
{
    private readonly ISagaPersistence<TSagaData> _persistence;
    private readonly ISaga<TSagaData>? _saga;
    private readonly ILogger<SagaTimeoutHandler<TSagaData>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaTimeoutHandler{TSagaData}"/> class.
    /// </summary>
    public SagaTimeoutHandler(
        ISagaPersistence<TSagaData> persistence,
        ILogger<SagaTimeoutHandler<TSagaData>> logger,
        ISaga<TSagaData>? saga = null)
    {
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _saga = saga;
    }

    /// <inheritdoc/>
    public async ValueTask<SagaTimeoutCheckResult> CheckAndHandleTimeoutsAsync(
        TimeSpan defaultTimeout,
        CancellationToken cancellationToken = default)
    {
        var checkedCount = 0;
        var timedOutCount = 0;
        var now = DateTimeOffset.UtcNow;

        try
        {
            // Get all active (Running or Compensating) sagas
            await foreach (var sagaData in _persistence.GetActiveSagasAsync(cancellationToken))
            {
                checkedCount++;

                // Determine timeout duration
                var timeout = GetSagaTimeout(sagaData) ?? defaultTimeout;
                var timeSinceUpdate = now - sagaData.UpdatedAt;

                // Check if saga has timed out
                if (timeSinceUpdate > timeout)
                {
                    timedOutCount++;

                    _logger.LogWarning(
                        "Saga {SagaId} (CorrelationId: {CorrelationId}) has timed out. " +
                        "Time since last update: {TimeSinceUpdate}, Timeout: {Timeout}, State: {State}",
                        sagaData.SagaId,
                        sagaData.CorrelationId,
                        timeSinceUpdate,
                        timeout,
                        sagaData.State);

                    try
                    {
                        // Handle timeout based on current state
                        if (sagaData.State == SagaState.Running)
                        {
                            // Trigger compensation for running saga
                            await HandleRunningSagaTimeoutAsync(sagaData, cancellationToken);
                        }
                        else if (sagaData.State == SagaState.Compensating)
                        {
                            // Mark as failed if compensation itself has timed out
                            await HandleCompensatingSagaTimeoutAsync(sagaData, cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to handle timeout for saga {SagaId}",
                            sagaData.SagaId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking saga timeouts for type {SagaType}", typeof(TSagaData).Name);
        }

        return new SagaTimeoutCheckResult
        {
            CheckedCount = checkedCount,
            TimedOutCount = timedOutCount
        };
    }

    /// <summary>
    /// Gets the timeout duration for a saga.
    /// Can be overridden in saga data via metadata.
    /// </summary>
    private TimeSpan? GetSagaTimeout(TSagaData sagaData)
    {
        // Check if saga data has custom timeout in metadata
        if (sagaData.Metadata.TryGetValue("Timeout", out var timeoutValue))
        {
            if (timeoutValue is TimeSpan timeout)
                return timeout;

            if (timeoutValue is int timeoutSeconds)
                return TimeSpan.FromSeconds(timeoutSeconds);

            if (timeoutValue is string timeoutString && TimeSpan.TryParse(timeoutString, out var parsedTimeout))
                return parsedTimeout;
        }

        // Check if saga data implements ISagaDataWithTimeout
        if (sagaData is ISagaDataWithTimeout sagaWithTimeout)
        {
            return sagaWithTimeout.Timeout;
        }

        return null;
    }

    /// <summary>
    /// Handles timeout for a running saga by triggering compensation.
    /// </summary>
    private async ValueTask HandleRunningSagaTimeoutAsync(TSagaData sagaData, CancellationToken cancellationToken)
    {
        // Update saga state to indicate timeout
        sagaData.State = SagaState.Compensating;
        sagaData.UpdatedAt = DateTimeOffset.UtcNow;
        sagaData.Metadata["TimedOut"] = true;
        sagaData.Metadata["TimeoutReason"] = "Saga execution exceeded timeout duration";

        // Save the updated state
        await _persistence.SaveAsync(sagaData, cancellationToken);

        _logger.LogInformation(
            "Saga {SagaId} marked for compensation due to timeout",
            sagaData.SagaId);

        // If we have saga instance, trigger compensation
        if (_saga != null)
        {
            try
            {
                // Execute saga (which will trigger compensation since state is Compensating)
                var result = await _saga.ExecuteAsync(sagaData, cancellationToken);

                if (result.CompensationSucceeded)
                {
                    _logger.LogInformation(
                        "Saga {SagaId} successfully compensated after timeout",
                        sagaData.SagaId);
                }
                else
                {
                    _logger.LogError(
                        "Saga {SagaId} compensation failed after timeout: {Error}",
                        sagaData.SagaId,
                        result.Exception?.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error compensating timed-out saga {SagaId}",
                    sagaData.SagaId);

                // Mark as failed
                sagaData.State = SagaState.Failed;
                sagaData.UpdatedAt = DateTimeOffset.UtcNow;
                await _persistence.SaveAsync(sagaData, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Handles timeout for a saga that is already compensating.
    /// </summary>
    private async ValueTask HandleCompensatingSagaTimeoutAsync(TSagaData sagaData, CancellationToken cancellationToken)
    {
        // If compensation itself has timed out, mark saga as failed
        sagaData.State = SagaState.Failed;
        sagaData.UpdatedAt = DateTimeOffset.UtcNow;
        sagaData.Metadata["CompensationTimedOut"] = true;
        sagaData.Metadata["FailureReason"] = "Saga compensation exceeded timeout duration";

        await _persistence.SaveAsync(sagaData, cancellationToken);

        _logger.LogError(
            "Saga {SagaId} marked as failed because compensation timed out",
            sagaData.SagaId);
    }
}

/// <summary>
/// Extended interface for saga data that supports custom timeout configuration.
/// </summary>
public interface ISagaDataWithTimeout : ISagaData
{
    /// <summary>
    /// Gets the timeout duration for this saga.
    /// </summary>
    TimeSpan Timeout { get; }
}

/// <summary>
/// Result of saga timeout check operation.
/// </summary>
public readonly record struct SagaTimeoutCheckResult
{
    /// <summary>
    /// Gets the number of sagas checked.
    /// </summary>
    public int CheckedCount { get; init; }

    /// <summary>
    /// Gets the number of timed-out sagas found.
    /// </summary>
    public int TimedOutCount { get; init; }
}
