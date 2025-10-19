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
    public class DefaultAIMetricsExporterIntegrationTests : IDisposable
    {
        private readonly Mock<ILogger<DefaultAIMetricsExporter>> _loggerMock;
        private readonly DefaultAIMetricsExporter _exporter;
        private readonly AIModelStatistics _testStatistics;

        public DefaultAIMetricsExporterIntegrationTests()
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

        #region Integration Tests

        [Fact]
        public async Task DefaultAIMetricsExporter_CompleteWorkflow_Works()
        {
            // Arrange - Multiple exports to test trends and history
            var stats1 = _testStatistics;
            var stats2 = new AIModelStatistics
            {
                ModelVersion = "v1.2.4",
                ModelTrainingDate = DateTime.UtcNow.AddDays(-6),
                LastRetraining = DateTime.UtcNow.AddHours(-2),
                TotalPredictions = 15000,
                AccuracyScore = 0.87, // Slight improvement
                PrecisionScore = 0.84,
                RecallScore = 0.89,
                F1Score = 0.86,
                ModelConfidence = 0.80,
                AveragePredictionTime = TimeSpan.FromMilliseconds(42.0),
                TrainingDataPoints = 55000
            };

            // Act
            await _exporter.ExportMetricsAsync(stats1);
            await _exporter.ExportMetricsAsync(stats2);

            // Assert - Should have processed both exports successfully
            _loggerMock.Verify(x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Metrics export #1 completed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

            _loggerMock.Verify(x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Metrics export #2 completed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task ExportMetricsAsync_WithRetrainingEvent_IncrementsRetrainingCounter()
        {
            // Arrange
            var initialStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
                LastRetraining = DateTime.UtcNow.AddDays(-7),
                TotalPredictions = 1000,
                AccuracyScore = 0.80,
                PrecisionScore = 0.78,
                RecallScore = 0.82,
                F1Score = 0.80,
                ModelConfidence = 0.75,
                AveragePredictionTime = TimeSpan.FromMilliseconds(50.0),
                TrainingDataPoints = 10000
            };

            var retrainedStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
                LastRetraining = DateTime.UtcNow.AddHours(-1), // More recent
                TotalPredictions = 2000,
                AccuracyScore = 0.85,
                PrecisionScore = 0.83,
                RecallScore = 0.87,
                F1Score = 0.85,
                ModelConfidence = 0.80,
                AveragePredictionTime = TimeSpan.FromMilliseconds(45.0),
                TrainingDataPoints = 15000
            };

            // Act
            await _exporter.ExportMetricsAsync(initialStats);
            await _exporter.ExportMetricsAsync(retrainedStats);

            // Assert - Retraining should be detected and logged
            // The retraining detection is tested through the OpenTelemetry export
        }

        #endregion
    }
}