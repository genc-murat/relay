using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Relay.Core.Telemetry;

namespace Relay.Core.Tests.Telemetry;

/// <summary>
/// Test class for DefaultMetricsProvider with access to protected methods
/// </summary>
public class TestableDefaultMetricsProvider : DefaultMetricsProvider
{
    public TestableDefaultMetricsProvider(ILogger<DefaultMetricsProvider>? logger = null)
        : base(logger)
    {
    }

    public new IEnumerable<List<HandlerExecutionMetrics>> GetHandlerExecutionsSnapshot(DateTimeOffset cutoff)
    {
        return base.GetHandlerExecutionsSnapshot(cutoff);
    }

    public new IEnumerable<List<StreamingOperationMetrics>> GetStreamingOperationsSnapshot(DateTimeOffset cutoff)
    {
        return base.GetStreamingOperationsSnapshot(cutoff);
    }
}

// Test classes
public class TestRequest<T> { }
public class TestStreamRequest<T> { }