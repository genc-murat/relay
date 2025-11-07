using System;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.Engines;
using Relay.Core.AI.Optimization.Data;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines
{
    public class TemporalPatternUpdaterTests
    {
        private readonly Mock<ILogger<TemporalPatternUpdater>> _loggerMock;
        private readonly TemporalPatternUpdater _updater;

        public TemporalPatternUpdaterTests()
        {
            _loggerMock = new Mock<ILogger<TemporalPatternUpdater>>();
            _updater = new TemporalPatternUpdater(_loggerMock.Object);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new TemporalPatternUpdater(null!));
        }

        [Fact]
        public void UpdatePatterns_Should_Group_By_Hour_Correctly()
        {
            // Arrange
            var baseTime = new DateTime(2023, 1, 1, 10, 0, 0);
            var predictions = new[]
            {
                CreatePredictionResult(baseTime.AddHours(0), TimeSpan.FromMilliseconds(50)),   // 10:00
                CreatePredictionResult(baseTime.AddHours(0), TimeSpan.Zero),                  // 10:00
                CreatePredictionResult(baseTime.AddHours(1), TimeSpan.FromMilliseconds(30)),   // 11:00
                CreatePredictionResult(baseTime.AddHours(1), TimeSpan.FromMilliseconds(40)),   // 11:00
                CreatePredictionResult(baseTime.AddHours(2), TimeSpan.Zero)                    // 12:00
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(4, result); // 2 hours + 2 days
            
            // Verify all success rates are 100%
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("100.00")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once());
        }

        [Fact]
        public void UpdatePatterns_Should_Handle_All_Failed_Predictions()
        {
            // Arrange
            var baseTime = new DateTime(2023, 1, 1, 10, 0, 0);
            var predictions = new[]
            {
                CreatePredictionResult(baseTime, TimeSpan.Zero),
                CreatePredictionResult(baseTime.AddHours(1), TimeSpan.Zero),
                CreatePredictionResult(baseTime.AddDays(1), TimeSpan.Zero)
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(4, result); // 2 hours + 2 days
            
            // Verify all success rates are 0%
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("0.00")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeast(3));
        }

        private static PredictionResult CreatePredictionResult(DateTime timestamp, TimeSpan actualImprovement)
        {
            return new PredictionResult
            {
                RequestType = typeof(object),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                ActualImprovement = actualImprovement,
                Timestamp = timestamp,
                Metrics = new RequestExecutionMetrics
                {
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                    MemoryUsage = 1024,
                    CpuUsage = 50,
                    TotalExecutions = 1,
                    SuccessfulExecutions = 1,
                    FailedExecutions = 0
                }
            };
        }
    }
}