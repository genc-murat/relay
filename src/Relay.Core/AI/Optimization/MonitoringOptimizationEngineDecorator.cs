using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization
{
    /// <summary>
    /// Decorator that adds performance monitoring.
    /// </summary>
    public class MonitoringOptimizationEngineDecorator : OptimizationEngineDecorator
    {
        private readonly Dictionary<string, List<TimeSpan>> _performanceHistory = new();

        public MonitoringOptimizationEngineDecorator(
            OptimizationEngineTemplate innerEngine,
            ILogger logger,
            OptimizationStrategyFactory strategyFactory,
            IEnumerable<IPerformanceObserver> observers)
            : base(innerEngine, logger, strategyFactory, observers)
        {
        }

        public override async ValueTask<StrategyExecutionResult> OptimizeAsync(
            OptimizationContext context,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            _logger.LogInformation("Starting monitored optimization for operation: {Operation}", context.Operation);

            var result = await base.OptimizeAsync(context, cancellationToken);

            var totalTime = DateTime.UtcNow - startTime;

            // Record performance metrics
            RecordPerformanceMetrics(context.Operation, totalTime);

            _logger.LogInformation("Completed monitored optimization in {Time}ms (Success: {Success})",
                totalTime.TotalMilliseconds, result.Success);

            // Check for performance degradation
            if (IsPerformanceDegraded(context.Operation, totalTime))
            {
                _logger.LogWarning("Performance degradation detected for operation: {Operation}", context.Operation);

                // Notify observers
                var alert = new PerformanceAlert
                {
                    AlertType = "PerformanceDegradation",
                    Message = $"Performance degraded for operation {context.Operation}",
                    Severity = AlertSeverity.Medium,
                    Data = new { Operation = context.Operation, ExecutionTime = totalTime }
                };

                await NotifyPerformanceAlertAsync(alert, cancellationToken);
            }

            return result;
        }

        protected override async ValueTask<StrategyExecutionResult> ExecuteOptimizationAsync(
            OptimizationContext context,
            IEnumerable<IOptimizationStrategy> strategies,
            CancellationToken cancellationToken)
        {
            return await _innerEngine.OptimizeAsync(context, cancellationToken);
        }

        protected override bool ValidateContext(OptimizationContext context)
        {
            return _innerEngine.GetType().GetMethod("ValidateContext")?
                .Invoke(_innerEngine, new object[] { context }) as bool? ?? true;
        }

        private void RecordPerformanceMetrics(string operation, TimeSpan executionTime)
        {
            if (!_performanceHistory.ContainsKey(operation))
            {
                _performanceHistory[operation] = new List<TimeSpan>();
            }

            _performanceHistory[operation].Add(executionTime);

            // Keep only last 100 measurements
            if (_performanceHistory[operation].Count > 100)
            {
                _performanceHistory[operation].RemoveAt(0);
            }
        }

        private bool IsPerformanceDegraded(string operation, TimeSpan currentTime)
        {
            if (!_performanceHistory.ContainsKey(operation) || _performanceHistory[operation].Count < 5)
            {
                return false; // Not enough data
            }

            var recentTimes = _performanceHistory[operation].Skip(Math.Max(0, _performanceHistory[operation].Count - 10)).ToArray();
            var averageTime = recentTimes.Average(t => t.TotalMilliseconds);
            var currentMs = currentTime.TotalMilliseconds;

            // Consider degraded if current time is 50% worse than average
            return currentMs > averageTime * 1.5;
        }

        private async ValueTask NotifyPerformanceAlertAsync(PerformanceAlert alert, CancellationToken cancellationToken)
        {
            foreach (var observer in base._observers)
            {
                try
                {
                    await observer.OnPerformanceThresholdExceededAsync(alert);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error notifying observer of performance alert");
                }
            }
        }
    }
}