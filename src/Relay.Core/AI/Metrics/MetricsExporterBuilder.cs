using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Builder for creating configured metrics exporters.
    /// </summary>
    public class MetricsExporterBuilder
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly List<IMetricsExportStrategy> _strategies = new();
        private IMetricsValidator? _validator;
        private IMetricsTrendAnalyzer? _trendAnalyzer;
        private IMetricsAlertObserver? _alertObserver;
        private string _meterName = "Relay.AI.Metrics";
        private string _meterVersion = "1.0.0";
        private bool _enableOpenTelemetry = true;
        private bool _enableLogging = true;
        private bool _enableAlerting = true;

        public MetricsExporterBuilder(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public MetricsExporterBuilder WithOpenTelemetry(string meterName = "Relay.AI.Metrics", string version = "1.0.0")
        {
            _meterName = meterName;
            _meterVersion = version;
            _enableOpenTelemetry = true;
            return this;
        }

        public MetricsExporterBuilder WithoutOpenTelemetry()
        {
            _enableOpenTelemetry = false;
            return this;
        }

        public MetricsExporterBuilder WithLogging()
        {
            _enableLogging = true;
            return this;
        }

        public MetricsExporterBuilder WithoutLogging()
        {
            _enableLogging = false;
            return this;
        }

        public MetricsExporterBuilder WithAlerting()
        {
            _enableAlerting = true;
            return this;
        }

        public MetricsExporterBuilder WithoutAlerting()
        {
            _enableAlerting = false;
            return this;
        }

        public MetricsExporterBuilder WithValidator(IMetricsValidator validator)
        {
            _validator = validator;
            return this;
        }

        public MetricsExporterBuilder WithTrendAnalyzer(IMetricsTrendAnalyzer trendAnalyzer)
        {
            _trendAnalyzer = trendAnalyzer;
            return this;
        }

        public MetricsExporterBuilder WithAlertObserver(IMetricsAlertObserver observer)
        {
            _alertObserver = observer;
            return this;
        }

        public MetricsExporterBuilder AddStrategy(IMetricsExportStrategy strategy)
        {
            _strategies.Add(strategy);
            return this;
        }

        public IAIMetricsExporter Build()
        {
            var logger = _loggerFactory.CreateLogger<DefaultAIMetricsExporter>();

            // Create default components if not provided
            var validator = _validator ?? new DefaultMetricsValidator();
            var trendAnalyzer = _trendAnalyzer ?? new DefaultMetricsTrendAnalyzer(logger);

            // Add default strategies
            if (_enableOpenTelemetry)
            {
                _strategies.Add(new OpenTelemetryMetricsExportStrategy(_meterName, _meterVersion));
            }

            if (_enableLogging)
            {
                _strategies.Add(new LoggingMetricsExportStrategy(logger));
            }

            if (_enableAlerting)
            {
                _strategies.Add(new AlertingMetricsExportStrategy(logger));
            }

            // Create composite exporter
            return new CompositeMetricsExporter(_strategies, validator, trendAnalyzer, _alertObserver);
        }
    }
}