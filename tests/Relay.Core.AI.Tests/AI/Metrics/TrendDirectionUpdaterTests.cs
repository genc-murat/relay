using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Metrics
{
    public class TrendDirectionUpdaterTests
    {
        private readonly Mock<ILogger<TrendDirectionUpdater>> _loggerMock;
        private readonly TrendDirectionUpdater _updater;

        public TrendDirectionUpdaterTests()
        {
            _loggerMock = new Mock<ILogger<TrendDirectionUpdater>>();
            _updater = new TrendDirectionUpdater(_loggerMock.Object);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TrendDirectionUpdater(null!));
        }

        [Fact]
        public void UpdateTrendDirections_StronglyIncreasing_ReturnsCorrectDirection()
        {
            // Arrange
            var currentMetrics = new Dictionary<string, double>
            {
                ["metric1"] = 120.0 // current > MA5
            };

            var movingAverages = new Dictionary<string, MovingAverageData>
            {
                ["metric1"] = new MovingAverageData
                {
                    MA5 = 100.0,  // MA5 > MA15 and MA5 > MA60
                    MA15 = 90.0,
                    MA60 = 80.0
                }
            };

            // Act
            var result = _updater.UpdateTrendDirections(currentMetrics, movingAverages);

            // Assert
            Assert.Equal(TrendDirection.StronglyIncreasing, result["metric1"]);
        }

        [Fact]
        public void UpdateTrendDirections_Increasing_ReturnsCorrectDirection()
        {
            // Arrange
            var currentMetrics = new Dictionary<string, double>
            {
                ["metric1"] = 110.0 // current > MA5
            };

            var movingAverages = new Dictionary<string, MovingAverageData>
            {
                ["metric1"] = new MovingAverageData
                {
                    MA5 = 100.0,  // MA5 > MA15 but MA5 <= MA60
                    MA15 = 90.0,
                    MA60 = 105.0
                }
            };

            // Act
            var result = _updater.UpdateTrendDirections(currentMetrics, movingAverages);

            // Assert
            Assert.Equal(TrendDirection.Increasing, result["metric1"]);
        }

        [Fact]
        public void UpdateTrendDirections_StronglyDecreasing_ReturnsCorrectDirection()
        {
            // Arrange
            var currentMetrics = new Dictionary<string, double>
            {
                ["metric1"] = 70.0 // current <= MA5
            };

            var movingAverages = new Dictionary<string, MovingAverageData>
            {
                ["metric1"] = new MovingAverageData
                {
                    MA5 = 80.0,   // MA5 <= MA15 and MA5 < MA60
                    MA15 = 85.0,
                    MA60 = 90.0
                }
            };

            // Act
            var result = _updater.UpdateTrendDirections(currentMetrics, movingAverages);

            // Assert
            Assert.Equal(TrendDirection.StronglyDecreasing, result["metric1"]);
        }

        [Fact]
        public void UpdateTrendDirections_Decreasing_ReturnsCorrectDirection()
        {
            // Arrange
            var currentMetrics = new Dictionary<string, double>
            {
                ["metric1"] = 75.0 // current <= MA5
            };

            var movingAverages = new Dictionary<string, MovingAverageData>
            {
                ["metric1"] = new MovingAverageData
                {
                    MA5 = 80.0,   // MA5 <= MA15 but MA5 >= MA60
                    MA15 = 85.0,
                    MA60 = 75.0
                }
            };

            // Act
            var result = _updater.UpdateTrendDirections(currentMetrics, movingAverages);

            // Assert
            Assert.Equal(TrendDirection.Decreasing, result["metric1"]);
        }

        [Fact]
        public void UpdateTrendDirections_Stable_ReturnsCorrectDirection()
        {
            // Arrange
            var currentMetrics = new Dictionary<string, double>
            {
                ["metric1"] = 95.0 // current <= MA5
            };

            var movingAverages = new Dictionary<string, MovingAverageData>
            {
                ["metric1"] = new MovingAverageData
                {
                    MA5 = 100.0,  // MA5 > MA15 but current <= MA5 (mixed signals)
                    MA15 = 90.0,
                    MA60 = 95.0
                }
            };

            // Act
            var result = _updater.UpdateTrendDirections(currentMetrics, movingAverages);

            // Assert
            Assert.Equal(TrendDirection.Stable, result["metric1"]);
        }

        [Fact]
        public void UpdateTrendDirections_MissingMovingAverage_SkipsMetric()
        {
            // Arrange
            var currentMetrics = new Dictionary<string, double>
            {
                ["metric1"] = 100.0,
                ["metric2"] = 200.0
            };

            var movingAverages = new Dictionary<string, MovingAverageData>
            {
                ["metric1"] = new MovingAverageData { MA5 = 90.0, MA15 = 85.0, MA60 = 80.0 }
                // metric2 missing
            };

            // Act
            var result = _updater.UpdateTrendDirections(currentMetrics, movingAverages);

            // Assert
            Assert.Single(result);
            Assert.Contains("metric1", result);
            Assert.DoesNotContain("metric2", result);
        }

        [Fact]
        public void UpdateTrendDirections_ExceptionHandling_ReturnsEmptyDictionary()
        {
            // Arrange
            var currentMetrics = new Dictionary<string, double>
            {
                ["metric1"] = 100.0
            };

            var movingAverages = new Dictionary<string, MovingAverageData>
            {
                ["metric1"] = null! // This will cause an exception
            };

            // Act
            var result = _updater.UpdateTrendDirections(currentMetrics, movingAverages);

            // Assert
            Assert.Empty(result);
            _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error detecting trend directions")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void UpdateTrendDirections_MultipleMetrics_ReturnsAllDirections()
        {
            // Arrange
            var currentMetrics = new Dictionary<string, double>
            {
                ["increasing"] = 120.0,
                ["decreasing"] = 70.0,
                ["stable"] = 95.0
            };

            var movingAverages = new Dictionary<string, MovingAverageData>
            {
                ["increasing"] = new MovingAverageData { MA5 = 100.0, MA15 = 90.0, MA60 = 80.0 }, // StronglyIncreasing
                ["decreasing"] = new MovingAverageData { MA5 = 80.0, MA15 = 85.0, MA60 = 90.0 },  // StronglyDecreasing
                ["stable"] = new MovingAverageData { MA5 = 100.0, MA15 = 90.0, MA60 = 95.0 }     // Stable
            };

            // Act
            var result = _updater.UpdateTrendDirections(currentMetrics, movingAverages);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(TrendDirection.StronglyIncreasing, result["increasing"]);
            Assert.Equal(TrendDirection.StronglyDecreasing, result["decreasing"]);
            Assert.Equal(TrendDirection.Stable, result["stable"]);
        }

        [Fact]
        public void UpdateTrendDirections_EmptyInputs_ReturnsEmptyDictionary()
        {
            // Arrange
            var currentMetrics = new Dictionary<string, double>();
            var movingAverages = new Dictionary<string, MovingAverageData>();

            // Act
            var result = _updater.UpdateTrendDirections(currentMetrics, movingAverages);

            // Assert
            Assert.Empty(result);
        }
    }
}