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

namespace Relay.Core.Tests.AI.Metrics
{
    public class DefaultAIMetricsExporterLoggingTests : IDisposable
    {
        private readonly Mock<ILogger<DefaultAIMetricsExporter>> _loggerMock;
        private readonly DefaultAIMetricsExporter _exporter;
        private readonly AIModelStatistics _testStatistics;

        public DefaultAIMetricsExporterLoggingTests()
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

        #region Structured Logging Tests

        [Fact]
        public async Task ExportMetricsAsync_LogsStructuredMetrics_WithExcellentQuality()
        {
            // Arrange
            var excellentStats = new AIModelStatistics
            {
                ModelVersion = "v2.0.0",
                ModelTrainingDate = DateTime.UtcNow.AddDays(-1),
                LastRetraining = DateTime.UtcNow.AddHours(-1),
                TotalPredictions = 50000,
                AccuracyScore = 0.95,
                PrecisionScore = 0.94,
                RecallScore = 0.96,
                F1Score = 0.95,
                ModelConfidence = 0.92,
                AveragePredictionTime = TimeSpan.FromMilliseconds(25.0),
                TrainingDataPoints = 100000
            };

            // Act
            await _exporter.ExportMetricsAsync(excellentStats);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Quality: Excellent")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task ExportMetricsAsync_LogsStructuredMetrics_WithPoorQuality()
        {
            // Arrange
            var poorStats = new AIModelStatistics
            {
                ModelVersion = "v0.5.0",
                ModelTrainingDate = DateTime.UtcNow.AddDays(-30),
                LastRetraining = DateTime.UtcNow.AddDays(-15),
                TotalPredictions = 1000,
                AccuracyScore = 0.55,
                PrecisionScore = 0.52,
                RecallScore = 0.58,
                F1Score = 0.55,
                ModelConfidence = 0.45,
                AveragePredictionTime = TimeSpan.FromMilliseconds(150.0),
                TrainingDataPoints = 5000
            };

            // Act
            await _exporter.ExportMetricsAsync(poorStats);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Quality: Poor")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        #endregion
    }
}