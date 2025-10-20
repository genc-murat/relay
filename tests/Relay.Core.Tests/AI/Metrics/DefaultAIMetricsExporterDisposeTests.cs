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
    public class DefaultAIMetricsExporterDisposeTests : IDisposable
    {
        private readonly Mock<ILogger<DefaultAIMetricsExporter>> _loggerMock;
        private readonly DefaultAIMetricsExporter _exporter;
        private readonly AIModelStatistics _testStatistics;

        public DefaultAIMetricsExporterDisposeTests()
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
    }
}