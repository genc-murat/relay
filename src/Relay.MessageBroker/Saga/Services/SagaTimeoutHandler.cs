using Microsoft.Extensions.Logging;

using Relay.MessageBroker.Saga.Interfaces;
using Relay.MessageBroker.Saga.Persistence;

namespace Relay.MessageBroker.Saga.Services;

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
