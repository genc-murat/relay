using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.Engines;
using Relay.Core.AI.Optimization.Data;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines
{
    public class CorrelationPatternUpdaterTests
    {
        private readonly Mock<ILogger<CorrelationPatternUpdater>> _loggerMock;
        private readonly PatternRecognitionConfig _config;
        private readonly CorrelationPatternUpdater _updater;

        public CorrelationPatternUpdaterTests()
        {
            _loggerMock = new Mock<ILogger<CorrelationPatternUpdater>>();
            _config = new PatternRecognitionConfig
            {
                MinimumCorrelationSuccessRate = 0.8,
                MinimumCorrelationCount = 3
            };
            _updater = new CorrelationPatternUpdater(_loggerMock.Object, _config);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CorrelationPatternUpdater(null!, _config));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Config_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CorrelationPatternUpdater(_loggerMock.Object, null!));
        }

        [Fact]
        public void UpdatePatterns_Should_Return_Zero_When_No_Correlations_Meet_Thresholds()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.Zero),
                CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.Zero),
                CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.Zero)
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void UpdatePatterns_Should_Return_Count_Of_Strong_Correlations()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(50)),
                CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(60)),
                CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(70)),
                CreatePredictionResult(typeof(int), OptimizationStrategy.Parallelization, TimeSpan.FromMilliseconds(30)),
                CreatePredictionResult(typeof(int), OptimizationStrategy.Parallelization, TimeSpan.FromMilliseconds(40)),
                CreatePredictionResult(typeof(int), OptimizationStrategy.Parallelization, TimeSpan.FromMilliseconds(10))
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(2, result); // Both string + caching and int + parallelization meet 0.8 success rate
        }

        [Fact]
        public void UpdatePatterns_Should_Filter_By_Minimum_Correlation_Count()
        {
            // Arrange
            _config.MinimumCorrelationCount = 5;
            var predictions = new[]
            {
                CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(50)),
                CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(60)),
                CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(70))
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(0, result); // Only 3 instances, below minimum count
        }

        [Fact]
        public void UpdatePatterns_Should_Filter_By_Minimum_Success_Rate()
        {
            // Arrange
            _config.MinimumCorrelationSuccessRate = 0.9;
            var predictions = new[]
            {
                CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(50)),
                CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(60)),
                CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.Zero),
                CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(70))
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(0, result); // 0.75 success rate, below 0.9 threshold
        }

        [Fact]
        public void UpdatePatterns_Should_Handle_Multiple_Strategies_Per_Prediction()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePredictionResultWithMultipleStrategies(typeof(string), 
                    new[] { OptimizationStrategy.Caching, OptimizationStrategy.Parallelization }, 
                    TimeSpan.FromMilliseconds(50)),
                CreatePredictionResultWithMultipleStrategies(typeof(string), 
                    new[] { OptimizationStrategy.Caching, OptimizationStrategy.Parallelization }, 
                    TimeSpan.FromMilliseconds(60)),
                CreatePredictionResultWithMultipleStrategies(typeof(string), 
                    new[] { OptimizationStrategy.Caching, OptimizationStrategy.Parallelization }, 
                    TimeSpan.FromMilliseconds(70))
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(2, result); // Both strategies have strong correlation with string type
        }

        [Fact]
        public void UpdatePatterns_Should_Handle_Empty_Predictions()
        {
            // Arrange
            var predictions = Array.Empty<PredictionResult>();
            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void UpdatePatterns_Should_Handle_Exception_And_Return_Zero()
        {
            // Arrange
            var predictions = new PredictionResult[] { null! };
            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(0, result);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void UpdatePatterns_Should_Log_Debug_Messages_For_Strong_Correlations()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(50)),
                CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(60)),
                CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(70))
            };

            var analysis = new PatternAnalysisResult();

            // Act
            _updater.UpdatePatterns(predictions, analysis);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void UpdatePatterns_Should_Handle_Different_Request_Types_With_Same_Strategy()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(50)),
                CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(60)),
                CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(70)),
                CreatePredictionResult(typeof(int), OptimizationStrategy.Caching, TimeSpan.Zero),
                CreatePredictionResult(typeof(int), OptimizationStrategy.Caching, TimeSpan.Zero),
                CreatePredictionResult(typeof(int), OptimizationStrategy.Caching, TimeSpan.Zero)
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(1, result); // Only string + caching meets success rate threshold
        }

        private static PredictionResult CreatePredictionResult(Type requestType, OptimizationStrategy strategy, TimeSpan actualImprovement)
        {
            return new PredictionResult
            {
                RequestType = requestType,
                PredictedStrategies = new[] { strategy },
                ActualImprovement = actualImprovement,
                Timestamp = DateTime.UtcNow,
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

        private static PredictionResult CreatePredictionResultWithMultipleStrategies(Type requestType, OptimizationStrategy[] strategies, TimeSpan actualImprovement)
        {
            return new PredictionResult
            {
                RequestType = requestType,
                PredictedStrategies = strategies,
                ActualImprovement = actualImprovement,
                Timestamp = DateTime.UtcNow,
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