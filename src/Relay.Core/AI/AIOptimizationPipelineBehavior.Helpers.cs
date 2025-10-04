using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Telemetry;

namespace Relay.Core.AI
{
    /// <summary>
    /// Partial class containing helper methods for AI optimization pipeline behavior.
    /// </summary>
    public sealed partial class AIOptimizationPipelineBehavior<TRequest, TResponse>
    {
        private AIOptimizedAttribute[] GetAIOptimizationAttributes(Type requestType)
        {
            var attributes = new List<AIOptimizedAttribute>();

            // Check request type attributes
            attributes.AddRange(requestType.GetCustomAttributes<AIOptimizedAttribute>());

            // Discover and check handler type attributes
            var handlerType = FindHandlerType(requestType);
            if (handlerType != null)
            {
                // Check handler class attributes
                attributes.AddRange(handlerType.GetCustomAttributes<AIOptimizedAttribute>());

                // Check handler method attributes (HandleAsync)
                var handlerMethod = FindHandlerMethod(handlerType, requestType);
                if (handlerMethod != null)
                {
                    attributes.AddRange(handlerMethod.GetCustomAttributes<AIOptimizedAttribute>());
                }
            }

            return attributes.ToArray();
        }

        private Type? FindHandlerType(Type requestType)
        {
            // Determine response type
            Type? responseType = null;
            Type handlerInterfaceType;

            // Check if request implements IRequest<TResponse>
            var requestInterface = requestType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

            if (requestInterface != null)
            {
                responseType = requestInterface.GetGenericArguments()[0];
                handlerInterfaceType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
            }
            else if (typeof(IRequest).IsAssignableFrom(requestType))
            {
                // Request without response
                handlerInterfaceType = typeof(IRequestHandler<>).MakeGenericType(requestType);
            }
            else
            {
                // Check if it's a stream request
                var streamInterface = requestType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamRequest<>));

                if (streamInterface != null)
                {
                    responseType = streamInterface.GetGenericArguments()[0];
                    handlerInterfaceType = typeof(IStreamHandler<,>).MakeGenericType(requestType, responseType);
                }
                else
                {
                    return null;
                }
            }

            // Search for handler implementation in loaded assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !a.ReflectionOnly);

            foreach (var assembly in assemblies)
            {
                try
                {
                    var handlerTypes = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && handlerInterfaceType.IsAssignableFrom(t));

                    var handler = handlerTypes.FirstOrDefault();
                    if (handler != null)
                    {
                        return handler;
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip assemblies that can't be loaded
                    continue;
                }
            }

            return null;
        }

        private MethodInfo? FindHandlerMethod(Type handlerType, Type requestType)
        {
            // Find the HandleAsync method
            var methods = handlerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == "HandleAsync" && m.GetParameters().Length >= 1);

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length > 0 &&
                    (parameters[0].ParameterType == requestType ||
                     parameters[0].ParameterType.IsAssignableFrom(requestType)))
                {
                    return method;
                }
            }

            return null;
        }

        private bool ShouldPerformOptimization(AIOptimizedAttribute[] attributes)
        {
            if (attributes.Length == 0)
                return _options.Enabled; // Default behavior when no specific attributes

            return attributes.Any(attr => attr.EnableMetricsTracking || attr.AutoApplyOptimizations);
        }

        private async ValueTask<RequestExecutionMetrics> GetHistoricalMetrics(Type requestType, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            // Try to get historical metrics from the metrics provider
            if (_metricsProvider != null)
            {
                try
                {
                    var stats = _metricsProvider.GetHandlerExecutionStats(requestType);

                    if (stats != null && stats.TotalExecutions > 0)
                    {
                        // Convert telemetry stats to AI execution metrics
                        return new RequestExecutionMetrics
                        {
                            AverageExecutionTime = stats.AverageExecutionTime,
                            MedianExecutionTime = stats.P50ExecutionTime,
                            P95ExecutionTime = stats.P95ExecutionTime,
                            P99ExecutionTime = stats.P99ExecutionTime,
                            TotalExecutions = stats.TotalExecutions,
                            SuccessfulExecutions = stats.SuccessfulExecutions,
                            FailedExecutions = stats.FailedExecutions,
                            MemoryAllocated = EstimateMemoryUsage(stats),
                            ConcurrentExecutions = EstimateConcurrentExecutions(stats),
                            LastExecution = stats.LastExecution.DateTime,
                            SamplePeriod = CalculateSamplePeriod(stats),
                            CpuUsage = EstimateCpuUsage(stats),
                            MemoryUsage = EstimateMemoryUsage(stats),
                            DatabaseCalls = ExtractDatabaseCalls(stats),
                            ExternalApiCalls = ExtractExternalApiCalls(stats)
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to retrieve historical metrics for {RequestType}", requestType.Name);
                }
            }

            // Return default metrics if no historical data is available
            return new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                MedianExecutionTime = TimeSpan.FromMilliseconds(80),
                P95ExecutionTime = TimeSpan.FromMilliseconds(200),
                P99ExecutionTime = TimeSpan.FromMilliseconds(500),
                TotalExecutions = 0, // Indicate no historical data
                SuccessfulExecutions = 0,
                FailedExecutions = 0,
                MemoryAllocated = 1024 * 512, // 512KB default
                ConcurrentExecutions = 1,
                LastExecution = DateTime.UtcNow.AddMinutes(-1),
                SamplePeriod = TimeSpan.FromHours(1),
                CpuUsage = 0.3,
                MemoryUsage = 1024 * 1024 * 100, // 100MB default
                DatabaseCalls = 0,
                ExternalApiCalls = 0
            };
        }

        private long EstimateMemoryUsage(HandlerExecutionStats stats)
        {
            // First, check if we have actual memory allocation data
            if (stats.AverageMemoryAllocated > 0)
            {
                return stats.AverageMemoryAllocated;
            }

            if (stats.TotalMemoryAllocated > 0 && stats.TotalExecutions > 0)
            {
                return stats.TotalMemoryAllocated / stats.TotalExecutions;
            }

            // Check if memory data is available in properties
            if (stats.Properties.TryGetValue("AverageMemoryBytes", out var memObj) && memObj is long avgMem)
            {
                return avgMem;
            }

            if (stats.Properties.TryGetValue("MemoryPerExecution", out var memPerExecObj) && memPerExecObj is long memPerExec)
            {
                return memPerExec;
            }

            // Fall back to estimation based on execution patterns
            return EstimateMemoryFromExecutionPatterns(stats);
        }

        private long EstimateMemoryFromExecutionPatterns(HandlerExecutionStats stats)
        {
            // Base memory estimate using execution time as a proxy
            var avgMs = stats.AverageExecutionTime.TotalMilliseconds;
            var baseEstimate = (long)(avgMs * 1024 * 5); // 5KB per millisecond as baseline

            // Adjust based on execution time variance (higher variance = more memory allocations)
            var executionTimeVariance = CalculateExecutionTimeVariance(stats);
            var varianceFactor = 1.0 + (executionTimeVariance * 0.5); // Up to 50% increase for high variance

            // Adjust based on failure rate (failed executions often allocate more memory for exceptions)
            var failureRate = stats.TotalExecutions > 0
                ? (double)stats.FailedExecutions / stats.TotalExecutions
                : 0.0;
            var failureFactor = 1.0 + (failureRate * 0.3); // Up to 30% increase for high failure rate

            // Adjust based on execution frequency (high frequency may benefit from object pooling)
            var executionFrequency = CalculateExecutionFrequency(stats);
            var frequencyFactor = executionFrequency > 10
                ? 0.8 // 20% reduction for high-frequency handlers (likely using pooling)
                : 1.0;

            // Calculate final estimate with all adjustments
            var estimate = (long)(baseEstimate * varianceFactor * failureFactor * frequencyFactor);

            // Apply reasonable bounds (min 1KB, max 100MB per execution)
            return Math.Clamp(estimate, 1024, 100 * 1024 * 1024);
        }

        private double CalculateExecutionTimeVariance(HandlerExecutionStats stats)
        {
            // Calculate coefficient of variation (CV) as a measure of variance
            // CV = standard deviation / mean
            // We approximate std dev using percentile spread

            var mean = stats.AverageExecutionTime.TotalMilliseconds;
            if (mean <= 0)
                return 0.0;

            // Approximate standard deviation using percentile range
            // For normal distribution: P95 - P50 ≈ 1.645 * std dev
            var p95ToP50Spread = stats.P95ExecutionTime.TotalMilliseconds - stats.P50ExecutionTime.TotalMilliseconds;
            var approximateStdDev = p95ToP50Spread / 1.645;

            var coefficientOfVariation = approximateStdDev / mean;

            // Normalize to 0-1 range (CV > 1 is very high variance)
            return Math.Clamp(coefficientOfVariation, 0.0, 1.0);
        }

        private double CalculateExecutionFrequency(HandlerExecutionStats stats)
        {
            // Calculate executions per second
            var timeSinceLastExecution = DateTime.UtcNow - stats.LastExecution.DateTime;
            var totalSeconds = Math.Max(1.0, timeSinceLastExecution.TotalSeconds);

            return stats.TotalExecutions / totalSeconds;
        }

        private int EstimateConcurrentExecutions(HandlerExecutionStats stats)
        {
            // Estimate concurrent executions based on total executions and timeframe
            // This is a simplified calculation
            var executionsPerSecond = stats.TotalExecutions / Math.Max(1, (DateTime.UtcNow - stats.LastExecution.DateTime).TotalSeconds);
            var avgExecutionSeconds = stats.AverageExecutionTime.TotalSeconds;
            return Math.Max(1, (int)(executionsPerSecond * avgExecutionSeconds));
        }

        private TimeSpan CalculateSamplePeriod(HandlerExecutionStats stats)
        {
            // Calculate the sample period based on last execution time
            var timeSinceLastExecution = DateTime.UtcNow - stats.LastExecution.DateTime;
            return timeSinceLastExecution > TimeSpan.Zero ? timeSinceLastExecution : TimeSpan.FromHours(1);
        }

        private double EstimateCpuUsage(HandlerExecutionStats stats)
        {
            // Estimate CPU usage based on execution time patterns
            // Higher P99/P50 ratio suggests CPU contention
            var p99ToP50Ratio = stats.P99ExecutionTime.TotalMilliseconds / Math.Max(1, stats.P50ExecutionTime.TotalMilliseconds);

            if (p99ToP50Ratio > 5.0)
                return 0.8; // High CPU usage
            else if (p99ToP50Ratio > 3.0)
                return 0.5; // Medium CPU usage
            else
                return 0.3; // Low CPU usage
        }

        private int ExtractDatabaseCalls(HandlerExecutionStats stats)
        {
            // Try to extract database call information from properties
            if (stats.Properties.TryGetValue("DatabaseCalls", out var dbCallsObj))
            {
                if (dbCallsObj is int dbCalls)
                    return dbCalls;
                if (dbCallsObj is long dbCallsLong)
                    return (int)dbCallsLong;
                if (dbCallsObj is double dbCallsDouble)
                    return (int)dbCallsDouble;
            }

            if (stats.Properties.TryGetValue("AvgDatabaseCalls", out var avgDbCallsObj))
            {
                if (avgDbCallsObj is double avgDbCalls)
                    return (int)Math.Round(avgDbCalls);
                if (avgDbCallsObj is int avgDbCallsInt)
                    return avgDbCallsInt;
            }

            // Estimate based on execution time if no data available
            // Longer execution times might indicate database operations
            if (stats.AverageExecutionTime.TotalMilliseconds > 100)
                return (int)(stats.AverageExecutionTime.TotalMilliseconds / 50); // Rough estimate: 1 DB call per 50ms

            return 0;
        }

        private int ExtractExternalApiCalls(HandlerExecutionStats stats)
        {
            // Try to extract external API call information from properties
            if (stats.Properties.TryGetValue("ExternalApiCalls", out var apiCallsObj))
            {
                if (apiCallsObj is int apiCalls)
                    return apiCalls;
                if (apiCallsObj is long apiCallsLong)
                    return (int)apiCallsLong;
                if (apiCallsObj is double apiCallsDouble)
                    return (int)apiCallsDouble;
            }

            if (stats.Properties.TryGetValue("AvgExternalApiCalls", out var avgApiCallsObj))
            {
                if (avgApiCallsObj is double avgApiCalls)
                    return (int)Math.Round(avgApiCalls);
                if (avgApiCallsObj is int avgApiCallsInt)
                    return avgApiCallsInt;
            }

            if (stats.Properties.TryGetValue("HttpCalls", out var httpCallsObj))
            {
                if (httpCallsObj is int httpCalls)
                    return httpCalls;
                if (httpCallsObj is long httpCallsLong)
                    return (int)httpCallsLong;
            }

            // Estimate based on execution time patterns
            // High P99/P50 ratio might indicate external API calls with variable latency
            var p99ToP50Ratio = stats.P99ExecutionTime.TotalMilliseconds / Math.Max(1, stats.P50ExecutionTime.TotalMilliseconds);
            if (p99ToP50Ratio > 4.0 && stats.AverageExecutionTime.TotalMilliseconds > 200)
                return 1; // Likely has external API calls

            return 0;
        }

        private void RecordCustomOptimizationMetrics(
            Type requestType,
            TimeSpan duration,
            CustomOptimizationStatistics stats,
            CustomOptimizationContext context)
        {
            if (_metricsProvider != null)
            {
                try
                {
                    var metrics = new HandlerExecutionMetrics
                    {
                        RequestType = requestType,
                        Duration = duration,
                        Success = true,
                        Timestamp = DateTimeOffset.UtcNow,
                        Properties = new Dictionary<string, object>
                        {
                            ["OptimizationType"] = context.OptimizationType,
                            ["OptimizationLevel"] = context.OptimizationLevel,
                            ["ActionsApplied"] = stats.OptimizationActionsApplied,
                            ["ActionsSucceeded"] = stats.ActionsSucceeded,
                            ["ActionsFailed"] = stats.ActionsFailed,
                            ["OverallEffectiveness"] = stats.OverallEffectiveness,
                            ["EnableProfiling"] = context.EnableProfiling,
                            ["EnableTracing"] = context.EnableTracing,
                            ["CustomParameterCount"] = context.CustomParameters.Count
                        }
                    };

                    // Add custom parameters to metrics
                    foreach (var param in context.CustomParameters)
                    {
                        metrics.Properties[$"Param_{param.Key}"] = param.Value?.ToString() ?? "null";
                    }

                    _metricsProvider.RecordHandlerExecution(metrics);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to record custom optimization metrics");
                }
            }
        }
    }
}
