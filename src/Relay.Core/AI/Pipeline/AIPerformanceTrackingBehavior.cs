using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Pipeline behavior for tracking AI-related performance metrics.
    /// </summary>
    internal class AIPerformanceTrackingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<AIPerformanceTrackingBehavior<TRequest, TResponse>> _logger;
        private readonly IAIMetricsExporter? _metricsExporter;

        public AIPerformanceTrackingBehavior(
            ILogger<AIPerformanceTrackingBehavior<TRequest, TResponse>> logger,
            IAIMetricsExporter? metricsExporter = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metricsExporter = metricsExporter;
        }

        public async ValueTask<TResponse> HandleAsync(
            TRequest request, 
            RequestHandlerDelegate<TResponse> next, 
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestType = typeof(TRequest).Name;

            try
            {
                _logger.LogDebug("AI Performance tracking started for request: {RequestType}", requestType);

                var response = await next();

                stopwatch.Stop();

                _logger.LogDebug("AI Performance tracking completed for request: {RequestType} in {ElapsedMs}ms",
                    requestType, stopwatch.ElapsedMilliseconds);

                // Track successful execution
                TrackPerformanceMetrics(requestType, stopwatch.Elapsed, success: true);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogWarning(ex, "AI Performance tracking detected error for request: {RequestType} after {ElapsedMs}ms",
                    requestType, stopwatch.ElapsedMilliseconds);

                // Track failed execution
                TrackPerformanceMetrics(requestType, stopwatch.Elapsed, success: false);

                throw;
            }
        }

        private void TrackPerformanceMetrics(string requestType, TimeSpan elapsed, bool success)
        {
            // In a production environment, this would:
            // 1. Aggregate metrics in a sliding window
            // 2. Calculate percentiles (P50, P95, P99)
            // 3. Track error rates and success rates
            // 4. Store metrics for ML model training
            // 5. Trigger alerts on anomalies

            _logger.LogTrace("Performance metric tracked: {RequestType}, Duration: {Duration}ms, Success: {Success}",
                requestType, elapsed.TotalMilliseconds, success);
        }
    }
}
