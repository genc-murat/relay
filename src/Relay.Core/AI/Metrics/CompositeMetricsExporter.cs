using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Relay.Core.AI
{
    /// <summary>
    /// Composite metrics exporter that combines multiple export strategies.
    /// Implements Decorator pattern to add functionality to base exporters.
    /// </summary>
    internal class CompositeMetricsExporter : IAIMetricsExporter, IDisposable
    {
        private readonly IEnumerable<IMetricsExportStrategy> _strategies;
        private readonly IMetricsValidator _validator;
        private readonly IMetricsTrendAnalyzer _trendAnalyzer;
        private readonly IMetricsAlertObserver? _alertObserver;
        private readonly ILogger _logger;
        private readonly ActivitySource _activitySource;

        private long _totalExports = 0;
        private DateTime _lastExportTime = DateTime.MinValue;
        private bool _disposed = false;

        public CompositeMetricsExporter(
            IEnumerable<IMetricsExportStrategy> strategies,
            IMetricsValidator validator,
            IMetricsTrendAnalyzer trendAnalyzer,
            IMetricsAlertObserver? alertObserver = null,
            ILogger? logger = null)
        {
            _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _trendAnalyzer = trendAnalyzer ?? throw new ArgumentNullException(nameof(trendAnalyzer));
            _alertObserver = alertObserver;

            // Create logger and activity source
            _logger = logger ?? NullLogger<CompositeMetricsExporter>.Instance;
            _activitySource = new ActivitySource("Relay.AI.CompositeExporter", "1.0.0");
        }

        public async ValueTask ExportMetricsAsync(AIModelStatistics statistics, CancellationToken cancellationToken = default)
        {
            if (statistics == null)
                throw new ArgumentNullException(nameof(statistics));

            cancellationToken.ThrowIfCancellationRequested();

            using var activity = _activitySource.StartActivity("CompositeExport", ActivityKind.Internal);

            var exportStartTime = DateTime.UtcNow;
            var exportNumber = Interlocked.Increment(ref _totalExports);
            _lastExportTime = exportStartTime;

            try
            {
                // Create and execute export command
                var command = new MetricsExportCommand(
                    statistics,
                    _validator,
                    _trendAnalyzer,
                    new CompositeExportStrategy(_strategies),
                    _alertObserver);

                await command.ExecuteAsync(activity, cancellationToken);

                // Record export activity
                activity?.SetTag("export.number", exportNumber);
                activity?.SetTag("export.duration_ms", (DateTime.UtcNow - exportStartTime).TotalMilliseconds);
                activity?.SetTag("model.version", statistics.ModelVersion);
                activity?.SetTag("model.accuracy", statistics.AccuracyScore);

                var exportDuration = (DateTime.UtcNow - exportStartTime).TotalMilliseconds;
                _logger.LogDebug("Metrics export #{ExportNumber} completed",
                    exportNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in composite metrics export");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            foreach (var strategy in _strategies.OfType<IDisposable>())
            {
                strategy.Dispose();
            }

            _activitySource?.Dispose();

            _logger.LogInformation(
                "Composite Metrics Exporter disposed. Total exports: {TotalExports}, Last export: {LastExportTime}",
                _totalExports,
                _lastExportTime);
        }

        /// <summary>
        /// Composite strategy that executes multiple strategies.
        /// </summary>
        private class CompositeExportStrategy : IMetricsExportStrategy
        {
            private readonly IEnumerable<IMetricsExportStrategy> _strategies;

            public string Name => "Composite";

            public CompositeExportStrategy(IEnumerable<IMetricsExportStrategy> strategies)
            {
                _strategies = strategies;
            }

            public async ValueTask ExportAsync(AIModelStatistics statistics, CancellationToken cancellationToken = default)
            {
                // Execute all strategies in parallel
                var tasks = _strategies.Select(strategy =>
                    strategy.ExportAsync(statistics, cancellationToken).AsTask());

                await Task.WhenAll(tasks);
            }
        }
    }
}