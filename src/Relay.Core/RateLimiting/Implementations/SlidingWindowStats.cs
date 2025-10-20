using System;

namespace Relay.Core.RateLimiting.Implementations;

/// <summary>
/// Statistics for sliding window rate limiter
/// </summary>
public readonly record struct SlidingWindowStats
{
    public int CurrentCount { get; init; }
    public int Limit { get; init; }
    public int Remaining { get; init; }
    public TimeSpan WindowDuration { get; init; }
    public int CurrentWindowRequests { get; init; }
    public int PreviousWindowRequests { get; init; }

    public override string ToString()
    {
        return $"Used: {CurrentCount}/{Limit} (Current: {CurrentWindowRequests}, Previous: {PreviousWindowRequests}), " +
               $"Remaining: {Remaining}, Window: {WindowDuration.TotalSeconds}s";
    }
}
