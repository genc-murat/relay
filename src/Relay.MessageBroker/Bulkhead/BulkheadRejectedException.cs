namespace Relay.MessageBroker.Bulkhead;

/// <summary>
/// Exception thrown when a bulkhead rejects an operation due to resource exhaustion.
/// </summary>
public class BulkheadRejectedException : Exception
{
    /// <summary>
    /// Gets the number of active operations at the time of rejection.
    /// </summary>
    public int ActiveOperations { get; }

    /// <summary>
    /// Gets the number of queued operations at the time of rejection.
    /// </summary>
    public int QueuedOperations { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkheadRejectedException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public BulkheadRejectedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkheadRejectedException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="activeOperations">The number of active operations at the time of rejection.</param>
    /// <param name="queuedOperations">The number of queued operations at the time of rejection.</param>
    public BulkheadRejectedException(string message, int activeOperations, int queuedOperations)
        : base(message)
    {
        ActiveOperations = activeOperations;
        QueuedOperations = queuedOperations;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkheadRejectedException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public BulkheadRejectedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
