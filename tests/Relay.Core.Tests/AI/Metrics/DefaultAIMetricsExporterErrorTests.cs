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
    public class DefaultAIMetricsExporterErrorTests : IDisposable
    {
        private readonly Mock<ILogger<DefaultAIMetricsExporter>> _loggerMock;
        private readonly DefaultAIMetricsExporter _exporter;
        private readonly AIModelStatistics _testStatistics;

        public DefaultAIMetricsExporterErrorTests()
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
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error in composite metrics export")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        #endregion
    }
}