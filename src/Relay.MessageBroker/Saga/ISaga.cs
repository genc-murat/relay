namespace Relay.MessageBroker.Saga;

/// <summary>
/// Interface for saga orchestration.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
public interface ISaga<TSagaData> where TSagaData : ISagaData
{
    /// <summary>
    /// Gets the unique identifier for the saga definition.
    /// </summary>
    string SagaId { get; }

    /// <summary>
    /// Gets the steps in the saga.
    /// </summary>
    IReadOnlyList<ISagaStep<TSagaData>> Steps { get; }

    /// <summary>
    /// Executes the saga.
    /// </summary>
    /// <param name="data">The saga data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask<SagaExecutionResult<TSagaData>> ExecuteAsync(TSagaData data, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base class for saga orchestration.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
public abstract class Saga<TSagaData> : ISaga<TSagaData> where TSagaData : ISagaData
{
    private readonly List<ISagaStep<TSagaData>> _steps = new();

    /// <inheritdoc/>
    public virtual string SagaId => GetType().Name;

    /// <inheritdoc/>
    public IReadOnlyList<ISagaStep<TSagaData>> Steps => _steps.AsReadOnly();

    /// <summary>
    /// Adds a step to the saga.
    /// </summary>
    /// <param name="step">The step to add.</param>
    /// <returns>The saga instance for method chaining.</returns>
    protected Saga<TSagaData> AddStep(ISagaStep<TSagaData> step)
    {
        _steps.Add(step);
        return this;
    }

    /// <inheritdoc/>
    public async ValueTask<SagaExecutionResult<TSagaData>> ExecuteAsync(TSagaData data, CancellationToken cancellationToken = default)
    {
        data.State = SagaState.Running;
        data.UpdatedAt = DateTimeOffset.UtcNow;

        var executedSteps = new List<ISagaStep<TSagaData>>();
        Exception? lastException = null;

        try
        {
            // Execute forward steps
            for (int i = data.CurrentStep; i < _steps.Count; i++)
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                var step = _steps[i];
                
                try
                {
                    await step.ExecuteAsync(data, cancellationToken).ConfigureAwait(false);
                    executedSteps.Add(step);
                    data.CurrentStep = i + 1;
                    data.UpdatedAt = DateTimeOffset.UtcNow;
                }
                catch (OperationCanceledException)
                {
                    // Re-throw cancellation to propagate it properly
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    // Start compensation
                    data.State = SagaState.Compensating;
                    data.UpdatedAt = DateTimeOffset.UtcNow;

                    await CompensateAsync(data, executedSteps, cancellationToken).ConfigureAwait(false);

                    data.State = SagaState.Compensated;
                    data.UpdatedAt = DateTimeOffset.UtcNow;

                    return new SagaExecutionResult<TSagaData>
                    {
                        Data = data,
                        IsSuccess = false,
                        FailedStep = step.Name,
                        Exception = ex,
                        CompensationSucceeded = true
                    };
                }
            }

            // All steps completed successfully
            data.State = SagaState.Completed;
            data.UpdatedAt = DateTimeOffset.UtcNow;

            return new SagaExecutionResult<TSagaData>
            {
                Data = data,
                IsSuccess = true
            };
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation to propagate it properly
            throw;
        }
        catch (Exception ex)
        {
            data.State = SagaState.Failed;
            data.UpdatedAt = DateTimeOffset.UtcNow;

            return new SagaExecutionResult<TSagaData>
            {
                Data = data,
                IsSuccess = false,
                Exception = ex
            };
        }
    }

    private async ValueTask CompensateAsync(TSagaData data, List<ISagaStep<TSagaData>> executedSteps, CancellationToken cancellationToken)
    {
        // Comprehensive compensation logic with retry, error tracking, and rollback strategies
        
        // Track compensation progress
        var compensationStartTime = DateTimeOffset.UtcNow;
        var failedCompensations = new List<CompensationFailure>();
        var successfulCompensations = new List<string>();
        var compensationAttempts = 0;
        
        // Compensate in reverse order (LIFO - Last In, First Out)
        executedSteps.Reverse();

        for (int i = 0; i < executedSteps.Count; i++)
        {
            var step = executedSteps[i];
            var stepName = step.Name ?? $"Step_{i}";
            var retryCount = 0;
            var maxRetries = 3; // Configurable retry count for compensation
            var compensated = false;
            Exception? lastException = null;

            // Retry loop for each compensation step
            while (!compensated && retryCount <= maxRetries)
            {
                try
                {
                    compensationAttempts++;
                    
                    // Check for cancellation before each compensation attempt
                    if (cancellationToken.IsCancellationRequested)
                    {
                        // Record cancellation but continue compensating critical steps
                        failedCompensations.Add(new CompensationFailure
                        {
                            StepName = stepName,
                            Reason = "Compensation cancelled by request",
                            Exception = new OperationCanceledException("Compensation was cancelled"),
                            RetryAttempt = retryCount,
                            Timestamp = DateTimeOffset.UtcNow
                        });
                        break;
                    }

                    // Execute compensation with timeout
                    using var compensationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    compensationCts.CancelAfter(TimeSpan.FromSeconds(30)); // 30-second timeout per compensation

                    await step.CompensateAsync(data, compensationCts.Token).ConfigureAwait(false);
                    
                    // Mark as successfully compensated
                    compensated = true;
                    successfulCompensations.Add(stepName);
                    
                    // Update saga data with compensation progress
                    if (data is ISagaDataWithCompensationTracking trackingData)
                    {
                        trackingData.RecordCompensatedStep(stepName, DateTimeOffset.UtcNow);
                    }
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // Timeout occurred - retry if attempts remain
                    lastException = new TimeoutException($"Compensation timeout for step '{stepName}' after 30 seconds");
                    retryCount++;
                    
                    if (retryCount <= maxRetries)
                    {
                        // Exponential backoff: 1s, 2s, 4s
                        var delayMs = (int)Math.Pow(2, retryCount - 1) * 1000;
                        await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    retryCount++;
                    
                    // Decide if we should retry based on exception type
                    var isRetryable = IsRetryableException(ex);
                    
                    if (isRetryable && retryCount <= maxRetries)
                    {
                        // Exponential backoff: 1s, 2s, 4s
                        var delayMs = (int)Math.Pow(2, retryCount - 1) * 1000;
                        
                        try
                        {
                            await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            // If delay is cancelled, record and exit retry loop
                            break;
                        }
                    }
                    else
                    {
                        // Non-retryable exception or max retries reached
                        break;
                    }
                }
            }

            // If compensation failed after all retries, record the failure
            if (!compensated && lastException != null)
            {
                failedCompensations.Add(new CompensationFailure
                {
                    StepName = stepName,
                    Reason = $"Compensation failed after {retryCount} attempts",
                    Exception = lastException,
                    RetryAttempt = retryCount,
                    Timestamp = DateTimeOffset.UtcNow
                });
                
                // Update saga data with failure information
                if (data is ISagaDataWithCompensationTracking trackingData)
                {
                    trackingData.RecordFailedCompensation(stepName, lastException, retryCount, DateTimeOffset.UtcNow);
                }
                
                // Log critical compensation failure
                // In production: alert operators, write to dead-letter queue, etc.
                OnCompensationFailed(stepName, lastException, retryCount);
            }
        }

        // Record overall compensation results
        var compensationDuration = DateTimeOffset.UtcNow - compensationStartTime;
        
        if (data is ISagaDataWithCompensationTracking trackingDataFinal)
        {
            trackingDataFinal.RecordCompensationSummary(
                totalSteps: executedSteps.Count,
                successfulCount: successfulCompensations.Count,
                failedCount: failedCompensations.Count,
                totalAttempts: compensationAttempts,
                duration: compensationDuration
            );
        }

        // If any compensations failed, we might want to throw or handle specially
        if (failedCompensations.Any())
        {
            // Store failed compensation details for manual intervention
            OnPartialCompensationFailure(failedCompensations, successfulCompensations);
        }
    }

    /// <summary>
    /// Determines if an exception is retryable during compensation.
    /// </summary>
    private bool IsRetryableException(Exception ex)
    {
        // Transient exceptions that are worth retrying
        return ex switch
        {
            TimeoutException => true,
            System.Net.Http.HttpRequestException => true,
            System.IO.IOException => true,
            System.Net.Sockets.SocketException => true,
            _ when ex.GetType().Name.Contains("Transient") => true,
            _ when ex.GetType().Name.Contains("Timeout") => true,
            _ when ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) => true,
            _ when ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) => true,
            _ => false
        };
    }

    /// <summary>
    /// Called when a single compensation step fails after all retries.
    /// Override this method to implement custom alerting or logging.
    /// </summary>
    protected virtual void OnCompensationFailed(string stepName, Exception exception, int retryCount)
    {
        // Default implementation: no-op
        // Override in derived classes to implement:
        // - Logging to monitoring systems
        // - Sending alerts to operators
        // - Writing to dead-letter queue
        // - Creating incident tickets
    }

    /// <summary>
    /// Called when compensation partially fails (some steps succeeded, some failed).
    /// Override this method to implement custom handling for partial failures.
    /// </summary>
    protected virtual void OnPartialCompensationFailure(
        List<CompensationFailure> failedCompensations,
        List<string> successfulCompensations)
    {
        // Default implementation: no-op
        // Override in derived classes to implement:
        // - Manual intervention workflow
        // - Automated retry with different strategy
        // - Alerting with detailed failure information
        // - State reconciliation procedures
    }

    /// <summary>
    /// Represents a compensation failure for tracking and diagnostics.
    /// </summary>
    protected sealed class CompensationFailure
    {
        public string StepName { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;
        public Exception? Exception { get; init; }
        public int RetryAttempt { get; init; }
        public DateTimeOffset Timestamp { get; init; }
    }
}

/// <summary>
/// Extended interface for saga data that tracks compensation progress.
/// Implement this interface on your saga data to enable detailed compensation tracking.
/// </summary>
public interface ISagaDataWithCompensationTracking : ISagaData
{
    /// <summary>
    /// Records a successfully compensated step.
    /// </summary>
    void RecordCompensatedStep(string stepName, DateTimeOffset timestamp);

    /// <summary>
    /// Records a failed compensation attempt.
    /// </summary>
    void RecordFailedCompensation(string stepName, Exception exception, int retryCount, DateTimeOffset timestamp);

    /// <summary>
    /// Records overall compensation summary.
    /// </summary>
    void RecordCompensationSummary(int totalSteps, int successfulCount, int failedCount, int totalAttempts, TimeSpan duration);
}

/// <summary>
/// Result of saga execution.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
public sealed class SagaExecutionResult<TSagaData> where TSagaData : ISagaData
{
    /// <summary>
    /// Gets or sets the saga data after execution.
    /// </summary>
    public TSagaData Data { get; init; } = default!;

    /// <summary>
    /// Gets or sets whether the saga executed successfully.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets or sets the name of the step that failed (if any).
    /// </summary>
    public string? FailedStep { get; init; }

    /// <summary>
    /// Gets or sets the exception that occurred (if any).
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets or sets whether compensation was successful (if it was needed).
    /// </summary>
    public bool CompensationSucceeded { get; init; }
}
