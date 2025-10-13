namespace Relay.Core.Performance.BufferManagement;

/// <summary>
/// Performance metrics for buffer pool analysis
/// </summary>
public readonly record struct BufferPoolMetrics
{
    public long TotalRequests { get; init; }
    public long SmallPoolHits { get; init; }
    public long MediumPoolHits { get; init; }
    public long LargePoolHits { get; init; }
    public double SmallPoolEfficiency { get; init; }
    public double MediumPoolEfficiency { get; init; }
    public double LargePoolEfficiency { get; init; }

    public override string ToString()
    {
        return $"Total: {TotalRequests:N0}, " +
               $"Small: {SmallPoolHits:N0} ({SmallPoolEfficiency:P1}), " +
               $"Medium: {MediumPoolHits:N0} ({MediumPoolEfficiency:P1}), " +
               $"Large: {LargePoolHits:N0} ({LargePoolEfficiency:P1})";
    }
}
