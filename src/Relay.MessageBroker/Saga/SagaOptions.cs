namespace Relay.MessageBroker.Saga;

/// <summary>
/// Configuration options for saga orchestration.
/// </summary>
public sealed class SagaOptions
{
    /// <summary>
    /// Gets or sets whether saga orchestration is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the default timeout for saga execution.
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets whether to automatically persist saga state.
    /// </summary>
    public bool AutoPersist { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval for persisting saga state.
    /// </summary>
    public TimeSpan PersistenceInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets whether to automatically retry failed steps.
    /// </summary>
    public bool AutoRetryFailedSteps { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed steps.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the retry delay.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets whether to use exponential backoff for retries.
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to automatically compensate on failure.
    /// </summary>
    public bool AutoCompensateOnFailure { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to continue compensation on error.
    /// </summary>
    public bool ContinueCompensationOnError { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout for each step.
    /// </summary>
    public TimeSpan? StepTimeout { get; set; }

    /// <summary>
    /// Gets or sets the timeout for compensation.
    /// </summary>
    public TimeSpan? CompensationTimeout { get; set; }

    /// <summary>
    /// Gets or sets whether to track saga execution metrics.
    /// </summary>
    public bool TrackMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to emit telemetry for saga execution.
    /// </summary>
    public bool EnableTelemetry { get; set; } = true;

    /// <summary>
    /// Gets or sets the action to execute when a saga completes.
    /// </summary>
    public Action<SagaCompletedEventArgs>? OnSagaCompleted { get; set; }

    /// <summary>
    /// Gets or sets the action to execute when a saga fails.
    /// </summary>
    public Action<SagaFailedEventArgs>? OnSagaFailed { get; set; }

    /// <summary>
    /// Gets or sets the action to execute when a saga is compensated.
    /// </summary>
    public Action<SagaCompensatedEventArgs>? OnSagaCompensated { get; set; }
}

/// <summary>
/// Event arguments for saga completed event.
/// </summary>
public sealed class SagaCompletedEventArgs
{
    /// <summary>
    /// Gets or sets the saga ID.
    /// </summary>
    public Guid SagaId { get; init; }

    /// <summary>
    /// Gets or sets the correlation ID.
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of steps executed.
    /// </summary>
    public int StepsExecuted { get; init; }

    /// <summary>
    /// Gets or sets the total execution duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets or sets the timestamp of completion.
    /// </summary>
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Event arguments for saga failed event.
/// </summary>
public sealed class SagaFailedEventArgs
{
    /// <summary>
    /// Gets or sets the saga ID.
    /// </summary>
    public Guid SagaId { get; init; }

    /// <summary>
    /// Gets or sets the correlation ID.
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the step that failed.
    /// </summary>
    public string FailedStep { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the exception that occurred.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets or sets the timestamp of failure.
    /// </summary>
    public DateTimeOffset FailedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the number of steps executed before failure.
    /// </summary>
    public int StepsExecutedBeforeFailure { get; init; }
}

/// <summary>
/// Event arguments for saga compensated event.
/// </summary>
public sealed class SagaCompensatedEventArgs
{
    /// <summary>
    /// Gets or sets the saga ID.
    /// </summary>
    public Guid SagaId { get; init; }

    /// <summary>
    /// Gets or sets the correlation ID.
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets whether compensation was successful.
    /// </summary>
    public bool CompensationSucceeded { get; init; }

    /// <summary>
    /// Gets or sets the number of steps compensated.
    /// </summary>
    public int StepsCompensated { get; init; }

    /// <summary>
    /// Gets or sets the timestamp of compensation.
    /// </summary>
    public DateTimeOffset CompensatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the original failure exception.
    /// </summary>
    public Exception? OriginalException { get; init; }
}
