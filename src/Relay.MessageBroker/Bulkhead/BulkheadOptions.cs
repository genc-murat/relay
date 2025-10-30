namespace Relay.MessageBroker.Bulkhead;

/// <summary>
/// Configuration options for bulkhead pattern.
/// </summary>
public class BulkheadOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether bulkhead pattern is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent operations allowed.
    /// Default is 100.
    /// </summary>
    public int MaxConcurrentOperations { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of operations that can be queued when all slots are busy.
    /// Default is 1000.
    /// </summary>
    public int MaxQueuedOperations { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the timeout for acquiring a slot in the bulkhead.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan AcquisitionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Validates the bulkhead options.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when options are invalid.</exception>
    public void Validate()
    {
        if (Enabled)
        {
            if (MaxConcurrentOperations <= 0)
            {
                throw new InvalidOperationException("MaxConcurrentOperations must be greater than 0.");
            }

            if (MaxQueuedOperations < 0)
            {
                throw new InvalidOperationException("MaxQueuedOperations must be greater than or equal to 0.");
            }

            if (AcquisitionTimeout <= TimeSpan.Zero)
            {
                throw new InvalidOperationException("AcquisitionTimeout must be greater than zero.");
            }
        }
    }
}
