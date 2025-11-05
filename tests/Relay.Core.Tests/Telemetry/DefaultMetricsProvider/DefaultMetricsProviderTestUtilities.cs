using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Relay.Core.Telemetry;
using DefaultMetricsProvider = Relay.Core.Telemetry.DefaultMetricsProvider;

namespace Relay.Core.Tests.Telemetry;

/// <summary>
/// Test class for DefaultMetricsProvider with access to protected methods
using Relay.Core.Testing;
/// </summary>
public class TestableDefaultMetricsProvider(ILogger<DefaultMetricsProvider>? logger = null) : DefaultMetricsProvider(logger)
{
    public new int MaxRecordsPerHandler => base.MaxRecordsPerHandler;
    public new int MaxTimingBreakdowns => base.MaxTimingBreakdowns;

    public new IEnumerable<List<HandlerExecutionMetrics>> GetHandlerExecutionsSnapshot(DateTimeOffset cutoff)
    {
        return base.GetHandlerExecutionsSnapshot(cutoff);
    }

    public new IEnumerable<List<StreamingOperationMetrics>> GetStreamingOperationsSnapshot(DateTimeOffset cutoff)
    {
        return base.GetStreamingOperationsSnapshot(cutoff);
    }

    public static TimeSpan GetPercentileInternal(List<TimeSpan> sortedDurations, double percentile)
    {
        return GetPercentile(sortedDurations, percentile);
    }

    public static TimeSpan GetPercentile(List<TimeSpan> sortedDurations, double percentile)
    {
        if (sortedDurations.Count == 0) return TimeSpan.Zero;
        if (sortedDurations.Count == 1) return sortedDurations[0];

        var index = (int)Math.Ceiling(sortedDurations.Count * percentile) - 1;
        index = Math.Max(0, Math.Min(index, sortedDurations.Count - 1));
        return sortedDurations[index];
    }
}

// Test classes
public class TestRequest<T> { }
public class TestStreamRequest<T> { }
