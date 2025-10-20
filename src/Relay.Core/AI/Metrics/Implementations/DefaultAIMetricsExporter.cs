using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.AI.Metrics.Interfaces;
using Relay.Core.AI.Metrics.Builders;

namespace Relay.Core.AI.Metrics.Implementations;

/// <summary>
/// Default implementation of AI metrics exporter using modern design patterns.
/// Uses Strategy, Observer, Builder, Command, and Composite patterns for maintainability.
/// </summary>
internal class DefaultAIMetricsExporter : IAIMetricsExporter, IDisposable
{
    private readonly IAIMetricsExporter _innerExporter;
    private readonly ILogger<DefaultAIMetricsExporter> _logger;
    private bool _disposed = false;

    public DefaultAIMetricsExporter(ILogger<DefaultAIMetricsExporter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Use builder pattern to create the composite exporter
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddProvider(new LoggerProvider(logger));
        });

        var builder = new MetricsExporterBuilder(loggerFactory)
            .WithOpenTelemetry()
            .WithLogging()
            .WithAlerting();

        _innerExporter = builder.Build();

        _logger.LogInformation("AI Metrics Exporter initialized with modern design patterns");
    }

    public async ValueTask ExportMetricsAsync(AIModelStatistics statistics, CancellationToken cancellationToken = default)
    {
        await _innerExporter.ExportMetricsAsync(statistics, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        if (_innerExporter is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _logger.LogInformation("Default AI Metrics Exporter disposed");
    }

    /// <summary>
    /// Simple logger provider for the builder.
    /// </summary>
    private class LoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public LoggerProvider(Microsoft.Extensions.Logging.ILogger logger)
        {
            _logger = logger;
        }

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => _logger;

        public void Dispose() { }
    }
}
