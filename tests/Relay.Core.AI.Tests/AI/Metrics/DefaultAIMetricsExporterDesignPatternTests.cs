using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Metrics.Builders;
using Relay.Core.AI.Metrics.Commands;
using Relay.Core.AI.Metrics.Implementations;
using Relay.Core.AI.Metrics.Interfaces;
using Relay.Core.AI.Metrics.Strategies;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Metrics
{
    public class DefaultAIMetricsExporterDesignPatternTests : IDisposable
    {
        private readonly Mock<ILogger<DefaultAIMetricsExporter>> _loggerMock;
        private readonly DefaultAIMetricsExporter _exporter;
        private readonly AIModelStatistics _testStatistics;

        public DefaultAIMetricsExporterDesignPatternTests()
        {
            _loggerMock = new Mock<ILogger<DefaultAIMetricsExporter>>();
            _exporter = new DefaultAIMetricsExporter(_loggerMock.Object);

            _testStatistics = new AIModelStatistics
            {
                ModelVersion = "v1.2.3",
                ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
                LastRetraining = DateTime.UtcNow.AddDays(-1),
                TotalPredictions = 10000,
                AccuracyScore = 0.85,
                PrecisionScore = 0.82,
                RecallScore = 0.88,
                F1Score = 0.85,
                ModelConfidence = 0.78,
                AveragePredictionTime = TimeSpan.FromMilliseconds(45.5),
                TrainingDataPoints = 50000
            };
        }

        public void Dispose()
        {
            _exporter.Dispose();
        }

        #region Design Pattern Tests

        [Fact]
        public void Builder_Pattern_Creates_Exporter_With_Custom_Configuration()
        {
            // Arrange
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

            // Act
            var builder = new MetricsExporterBuilder(loggerFactory)
                .WithOpenTelemetry("Custom.Metrics", "2.0.0")
                .WithoutLogging()
                .WithAlerting();

            var exporter = builder.Build();

            // Assert
            Assert.NotNull(exporter);
            Assert.IsAssignableFrom<IAIMetricsExporter>(exporter);
        }

        [Fact]
        public void Strategy_Pattern_Allows_Custom_Export_Strategies()
        {
            // Arrange
            var mockStrategy = new Mock<IMetricsExportStrategy>();
            mockStrategy.Setup(x => x.Name).Returns("MockStrategy");

            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

            // Act
            var builder = new MetricsExporterBuilder(loggerFactory)
                .WithoutOpenTelemetry()
                .WithoutLogging()
                .WithoutAlerting()
                .AddStrategy(mockStrategy.Object);

            var exporter = builder.Build();

            // Assert
            Assert.NotNull(exporter);
        }

        [Fact]
        public async Task Command_Pattern_Executes_Export_Operation()
        {
            // Arrange
            var validator = new DefaultMetricsValidator();
            var trendAnalyzer = new DefaultMetricsTrendAnalyzer(_loggerMock.Object);
            var mockStrategy = new Mock<IMetricsExportStrategy>();
            mockStrategy.Setup(x => x.Name).Returns("TestStrategy");

            var command = new MetricsExportCommand(
                _testStatistics,
                validator,
                trendAnalyzer,
                mockStrategy.Object);

            // Act
            await command.ExecuteAsync();

            // Assert
            mockStrategy.Verify(x => x.ExportAsync(_testStatistics, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Observer_Pattern_Notifies_Alert_Observers()
        {
            // Arrange
            var alertObserverMock = new Mock<IMetricsAlertObserver>();
            var lowAccuracyStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                ModelTrainingDate = DateTime.UtcNow.AddDays(-1),
                LastRetraining = DateTime.UtcNow.AddHours(-1),
                TotalPredictions = 1000,
                AccuracyScore = 0.50, // Below threshold
                PrecisionScore = 0.80,
                RecallScore = 0.85,
                F1Score = 0.82,
                ModelConfidence = 0.75,
                AveragePredictionTime = TimeSpan.FromMilliseconds(50.0),
                TrainingDataPoints = 10000
            };

            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

            var builder = new MetricsExporterBuilder(loggerFactory)
                .WithAlertObserver(alertObserverMock.Object);

            var exporter = builder.Build();

            // Act
            await exporter.ExportMetricsAsync(lowAccuracyStats);

            // Assert
            alertObserverMock.Verify(x => x.OnAlertsDetected(It.IsAny<IReadOnlyList<string>>(), lowAccuracyStats), Times.Once);
        }

        [Fact]
        public void Composite_Pattern_Combines_Multiple_Strategies()
        {
            // Arrange
            var strategy1Mock = new Mock<IMetricsExportStrategy>();
            strategy1Mock.Setup(x => x.Name).Returns("Strategy1");

            var strategy2Mock = new Mock<IMetricsExportStrategy>();
            strategy2Mock.Setup(x => x.Name).Returns("Strategy2");

            var strategies = new[] { strategy1Mock.Object, strategy2Mock.Object };
            var validator = new DefaultMetricsValidator();
            var trendAnalyzer = new DefaultMetricsTrendAnalyzer(_loggerMock.Object);

            // Act
            var compositeExporter = new CompositeMetricsExporter(strategies, validator, trendAnalyzer);

            // Assert
            Assert.NotNull(compositeExporter);
            Assert.IsAssignableFrom<IAIMetricsExporter>(compositeExporter);
        }

        [Fact]
        public async Task Validator_Interface_Validates_Statistics()
        {
            // Arrange
            var validator = new DefaultMetricsValidator();
            var invalidStats = new AIModelStatistics
            {
                ModelVersion = null!, // Invalid
                AccuracyScore = 1.5 // Invalid
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => validator.Validate(invalidStats));
            Assert.Contains("Model version cannot be null", exception.Message);
        }

        [Fact]
        public async Task Trend_Analyzer_Interface_Analyzes_Trends()
        {
            // Arrange
            var trendAnalyzer = new DefaultMetricsTrendAnalyzer(_loggerMock.Object);

            // Act
            await trendAnalyzer.AnalyzeTrendsAsync(_testStatistics);

            // Assert - Should complete without throwing
            _loggerMock.Verify(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtMostOnce); // May or may not log
        }

        [Fact]
        public async Task OpenTelemetry_Strategy_Exports_To_Metrics()
        {
            // Arrange
            var strategy = new OpenTelemetryMetricsExportStrategy("Test.Metrics", "1.0.0");

            // Act
            await strategy.ExportAsync(_testStatistics);

            // Assert
            Assert.Equal("OpenTelemetry", strategy.Name);
        }

        [Fact]
        public async Task Logging_Strategy_Logs_Structured_Metrics()
        {
            // Arrange
            var strategy = new LoggingMetricsExportStrategy(_loggerMock.Object);

            // Act
            await strategy.ExportAsync(_testStatistics);

            // Assert
            Assert.Equal("Logging", strategy.Name);
            _loggerMock.Verify(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("AI Model Statistics Export")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task Alerting_Strategy_Generates_Alerts_For_Poor_Metrics()
        {
            // Arrange
            var strategy = new AlertingMetricsExportStrategy(_loggerMock.Object);
            var poorStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                ModelTrainingDate = DateTime.UtcNow.AddDays(-1),
                LastRetraining = DateTime.UtcNow.AddHours(-1),
                TotalPredictions = 1000,
                AccuracyScore = 0.50, // Below threshold
                PrecisionScore = 0.80,
                RecallScore = 0.85,
                F1Score = 0.82,
                ModelConfidence = 0.75,
                AveragePredictionTime = TimeSpan.FromMilliseconds(50.0),
                TrainingDataPoints = 10000
            };

            // Act
            await strategy.ExportAsync(poorStats);

            // Assert
            Assert.Equal("Alerting", strategy.Name);
            _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("AI Model Performance Alerts Detected")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void Builder_Fluent_API_Works_Correctly()
        {
            // Arrange & Act
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

            var exporter = new MetricsExporterBuilder(loggerFactory)
                .WithOpenTelemetry("Test.Metrics", "1.0.0")
                .WithLogging()
                .WithAlerting()
                .WithValidator(new DefaultMetricsValidator())
                .WithTrendAnalyzer(new DefaultMetricsTrendAnalyzer(loggerFactory.CreateLogger("Test")))
                .Build();

            // Assert
            Assert.NotNull(exporter);
        }

        [Fact]
        public async Task Composite_Exporter_Executes_All_Strategies_In_Parallel()
        {
            // Arrange
            var strategy1Mock = new Mock<IMetricsExportStrategy>();
            strategy1Mock.Setup(x => x.Name).Returns("Strategy1");
            strategy1Mock.Setup(x => x.ExportAsync(It.IsAny<AIModelStatistics>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var strategy2Mock = new Mock<IMetricsExportStrategy>();
            strategy2Mock.Setup(x => x.Name).Returns("Strategy2");
            strategy2Mock.Setup(x => x.ExportAsync(It.IsAny<AIModelStatistics>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var strategies = new[] { strategy1Mock.Object, strategy2Mock.Object };
            var validator = new DefaultMetricsValidator();
            var trendAnalyzer = new DefaultMetricsTrendAnalyzer(_loggerMock.Object);

            var exporter = new CompositeMetricsExporter(strategies, validator, trendAnalyzer);

            // Act
            await exporter.ExportMetricsAsync(_testStatistics);

            // Assert
            strategy1Mock.Verify(x => x.ExportAsync(_testStatistics, It.IsAny<CancellationToken>()), Times.Once);
            strategy2Mock.Verify(x => x.ExportAsync(_testStatistics, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion
    }
}