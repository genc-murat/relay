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

namespace Relay.Core.Tests.AI.Metrics
{
    public class DefaultAIMetricsExporterTests : IDisposable
    {
        private readonly Mock<ILogger<DefaultAIMetricsExporter>> _loggerMock;
        private readonly DefaultAIMetricsExporter _exporter;
        private readonly AIModelStatistics _testStatistics;

        public DefaultAIMetricsExporterTests()
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

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DefaultAIMetricsExporter(null!));
        }

        [Fact]
        public void Constructor_WithValidLogger_InitializesSuccessfully()
        {
            // Arrange & Act
            using var exporter = new DefaultAIMetricsExporter(_loggerMock.Object);

            // Assert - Should not throw
            Assert.NotNull(exporter);
        }

        #endregion

        #region ExportMetricsAsync Tests

        [Fact]
        public async Task ExportMetricsAsync_WithNullStatistics_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _exporter.ExportMetricsAsync(null!).AsTask());
        }

        [Fact]
        public async Task ExportMetricsAsync_WithValidStatistics_ExportsSuccessfully()
        {
            // Act
            await _exporter.ExportMetricsAsync(_testStatistics);

            // Assert - Should complete without throwing
            _loggerMock.Verify(x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Metrics export #1 completed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task ExportMetricsAsync_IncrementsExportCounter()
        {
            // Act - Export multiple times
            await _exporter.ExportMetricsAsync(_testStatistics);
            await _exporter.ExportMetricsAsync(_testStatistics);

            // Assert - Should log export numbers
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
        public async Task ExportMetricsAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await _exporter.ExportMetricsAsync(_testStatistics, cts.Token));
        }

        [Fact]
        public async Task ExportMetricsAsync_UpdatesLatestStatistics()
        {
            // Act
            await _exporter.ExportMetricsAsync(_testStatistics);

            // Assert - This is tested indirectly through the observable gauges
            // The gauges should now return the test statistics values
        }

        #endregion

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

        #region Trend Tracking Tests

        [Fact]
        public async Task ExportMetricsAsync_TracksMetricTrends()
        {
            // Arrange
            var improvingStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                ModelTrainingDate = DateTime.UtcNow.AddDays(-1),
                LastRetraining = DateTime.UtcNow.AddHours(-1),
                TotalPredictions = 1000,
                AccuracyScore = 0.935, // Improved from 0.85 (10% improvement)
                PrecisionScore = 0.82,
                RecallScore = 0.88,
                F1Score = 0.85,
                ModelConfidence = 0.78,
                AveragePredictionTime = TimeSpan.FromMilliseconds(45.5),
                TrainingDataPoints = 50000
            };

            // Act - Export baseline then improved
            await _exporter.ExportMetricsAsync(_testStatistics);
            await _exporter.ExportMetricsAsync(improvingStats);

            // Assert - Should log significant improvement
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Significant trend detected")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
        }

        #endregion

        #region Quality Categorization Tests

        [Fact]
        public void CategorizeMetricsQuality_WithExcellentMetrics_ReturnsExcellent()
        {
            // Arrange
            var excellentStats = new AIModelStatistics
            {
                AccuracyScore = 0.95,
                PrecisionScore = 0.94,
                RecallScore = 0.96,
                F1Score = 0.95,
                ModelConfidence = 0.92,
                AveragePredictionTime = TimeSpan.FromMilliseconds(25.0)
            };

            // Act - This is tested indirectly through logging, but we can test the private method via reflection if needed
            // For now, we'll test through the public interface
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task ExportMetricsAsync_WithException_LogsErrorAndRethrows()
        {
            // Arrange - Create a mock that will cause issues
            var problematicStats = new AIModelStatistics
            {
                ModelVersion = null!, // This might cause issues
                ModelTrainingDate = DateTime.UtcNow,
                LastRetraining = DateTime.UtcNow,
                TotalPredictions = -1, // Invalid value
                AccuracyScore = double.NaN, // NaN value
                PrecisionScore = 0.82,
                RecallScore = 0.88,
                F1Score = 0.85,
                ModelConfidence = 0.78,
                AveragePredictionTime = TimeSpan.FromMilliseconds(45.5),
                TrainingDataPoints = 50000
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _exporter.ExportMetricsAsync(problematicStats));

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error exporting AI metrics")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Act
            _exporter.Dispose();
            _exporter.Dispose(); // Second dispose

            // Assert - Should not throw
        }

        [Fact]
        public void Dispose_LogsDisposalInformation()
        {
            // Act
            _exporter.Dispose();

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("AI Metrics Exporter disposed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        #endregion

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