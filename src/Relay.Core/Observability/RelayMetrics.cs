using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;

namespace Relay.Core.Observability
{
    /// <summary>
    /// Comprehensive metrics collection for Relay framework operations.
    /// Provides detailed observability with OpenTelemetry integration.
    /// </summary>
    public static class RelayMetrics
    {
        private static readonly string InstrumentationName = "Relay.Core";
        private static readonly string InstrumentationVersion = "1.0.0";

        public static readonly Meter Meter = new(InstrumentationName, InstrumentationVersion);

        // Request Processing Metrics
        public static readonly Counter<long> RequestsTotal = Meter.CreateCounter<long>(
            "relay_requests_total",
            "requests", 
            "Total number of requests processed");

        public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
            "relay_request_duration_ms",
            "milliseconds",
            "Duration of request processing");

        public static readonly Counter<long> RequestErrors = Meter.CreateCounter<long>(
            "relay_request_errors_total",
            "errors",
            "Total number of request processing errors");

        // Handler Metrics
        public static readonly Counter<long> HandlersExecuted = Meter.CreateCounter<long>(
            "relay_handlers_executed_total",
            "handlers",
            "Total number of handlers executed");

        public static readonly Histogram<double> HandlerExecutionTime = Meter.CreateHistogram<double>(
            "relay_handler_execution_duration_ms",
            "milliseconds", 
            "Time spent executing handlers");

        // Cache Metrics
        public static readonly Counter<long> CacheHits = Meter.CreateCounter<long>(
            "relay_cache_hits_total",
            "hits",
            "Total cache hits");

        public static readonly Counter<long> CacheMisses = Meter.CreateCounter<long>(
            "relay_cache_misses_total",
            "misses",
            "Total cache misses");

        // Performance Metrics
        public static readonly UpDownCounter<long> ActiveRequests = Meter.CreateUpDownCounter<long>(
            "relay_active_requests",
            "requests",
            "Currently active requests being processed");

        /// <summary>
        /// Records request processing metrics.
        /// </summary>
        public static IDisposable TrackRequest(string requestType, string? handlerName = null)
        {
            var tags = new TagList
            {
                { "request_type", requestType },
                { "handler_name", handlerName ?? "default" }
            };

            RequestsTotal.Add(1, tags);
            ActiveRequests.Add(1, tags);

            return new RequestTracker(requestType, handlerName, tags);
        }

        /// <summary>
        /// Records cache operation metrics.
        /// </summary>
        public static void RecordCacheHit(string cacheKey, string requestType)
        {
            var tags = new TagList
            {
                { "cache_key", cacheKey },
                { "request_type", requestType }
            };
            CacheHits.Add(1, tags);
        }

        /// <summary>
        /// Records cache miss metrics.
        /// </summary>
        public static void RecordCacheMiss(string cacheKey, string requestType)
        {
            var tags = new TagList
            {
                { "cache_key", cacheKey },
                { "request_type", requestType }
            };
            CacheMisses.Add(1, tags);
        }

        // Nested tracker classes for proper resource management
        private sealed class RequestTracker : IDisposable
        {
            private readonly TagList _tags;
            private readonly long _startTimestamp;
            private bool _disposed;

            public RequestTracker(string requestType, string? handlerName, TagList tags)
            {
                _tags = tags;
                _startTimestamp = Stopwatch.GetTimestamp();
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                var duration = Stopwatch.GetElapsedTime(_startTimestamp).TotalMilliseconds;
                RequestDuration.Record(duration, _tags);
                ActiveRequests.Add(-1, _tags);
            }
        }
    }
}