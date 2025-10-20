using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI.Metrics.Implementations;

namespace Relay.Core.Tests.AI.Metrics
{
    public class DefaultAIMetricsExporterAlertTests : IDisposable
    {
        private readonly Mock<ILogger<DefaultAIMetricsExporter>> _loggerMock;
        private readonly DefaultAIMetricsExporter _exporter;
        private readonly AIModelStatistics _testStatistics;

        public DefaultAIMetricsExporterAlertTests()
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

        #region Alert Tests

        [Fact]
        public async Task ExportMetricsAsync_WithLowAccuracy_GeneratesAlert()
        {
            // Arrange
            var lowAccuracyStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                ModelTrainingDate = DateTime.UtcNow.AddDays(-1),
                LastRetraining = DateTime.UtcNow.AddHours(-1),
                TotalPredictions = 1000,
                AccuracyScore = 0.60, // Below threshold
                PrecisionScore = 0.80,
                RecallScore = 0.85,
                F1Score = 0.82,
                ModelConfidence = 0.75,
                AveragePredictionTime = TimeSpan.FromMilliseconds(50.0),
                TrainingDataPoints = 10000
            };

            // Act
            await _exporter.ExportMetricsAsync(lowAccuracyStats);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("AI Model Performance Alerts Detected")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task ExportMetricsAsync_WithHighLatency_GeneratesAlert()
        {
            // Arrange
            var highLatencyStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                ModelTrainingDate = DateTime.UtcNow.AddDays(-1),
                LastRetraining = DateTime.UtcNow.AddHours(-1),
                TotalPredictions = 1000,
                AccuracyScore = 0.85,
                PrecisionScore = 0.82,
                RecallScore = 0.88,
                F1Score = 0.85,
                ModelConfidence = 0.78,
                AveragePredictionTime = TimeSpan.FromMilliseconds(150.0), // Above threshold
                TrainingDataPoints = 10000
            };

            // Act
            await _exporter.ExportMetricsAsync(highLatencyStats);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("AI Model Performance Alerts Detected")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task ExportMetricsAsync_WithStaleModel_GeneratesAlert()
        {
            // Arrange
            var staleModelStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                ModelTrainingDate = DateTime.UtcNow.AddDays(-60),
                LastRetraining = DateTime.UtcNow.AddDays(-35), // More than 30 days ago
                TotalPredictions = 1000,
                AccuracyScore = 0.85,
                PrecisionScore = 0.82,
                RecallScore = 0.88,
                F1Score = 0.85,
                ModelConfidence = 0.78,
                AveragePredictionTime = TimeSpan.FromMilliseconds(50.0),
                TrainingDataPoints = 10000
            };

            // Act
            await _exporter.ExportMetricsAsync(staleModelStats);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("AI Model Performance Alerts Detected")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task ExportMetricsAsync_WithGoodMetrics_NoAlertsGenerated()
        {
            // Act
            await _exporter.ExportMetricsAsync(_testStatistics);

            // Assert - Should not log alerts
            _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("AI Model Performance Alerts Detected")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
        }

        #endregion
    }
}