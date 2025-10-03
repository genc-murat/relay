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
        // Compensate in reverse order
        executedSteps.Reverse();

        foreach (var step in executedSteps)
        {
            try
            {
                await step.CompensateAsync(data, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Log compensation failure but continue compensating other steps
                // In production, you might want to retry or alert operators
            }
        }
    }
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
