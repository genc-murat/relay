using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.Engines;
using Relay.Core.AI.Analysis.Engines;
using Relay.Core.AI.Optimization.Data;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines
{
    public class DecisionBoundaryOptimizerTests
    {
        private readonly Mock<ILogger<DecisionBoundaryOptimizer>> _loggerMock;
        private readonly PatternRecognitionConfig _config;
        private readonly DecisionBoundaryOptimizer _optimizer;

        public DecisionBoundaryOptimizerTests()
        {
            _loggerMock = new Mock<ILogger<DecisionBoundaryOptimizer>>();
            _config = new PatternRecognitionConfig();
            _optimizer = new DecisionBoundaryOptimizer(_loggerMock.Object, _config);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DecisionBoundaryOptimizer(null!, _config));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Config_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DecisionBoundaryOptimizer(_loggerMock.Object, null!));
        }

        [Fact]
        public void UpdatePatterns_Should_Calculate_Optimal_Threshold()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePredictionResult(150, TimeSpan.FromMilliseconds(50)), // TP: predicted >100, actual needed optimization
                CreatePredictionResult(80, TimeSpan.Zero), // TN: predicted <=100, actual no optimization needed
                CreatePredictionResult(120, TimeSpan.FromMilliseconds(30)), // TP
                CreatePredictionResult(90, TimeSpan.Zero), // TN
                CreatePredictionResult(200, TimeSpan.Zero), // FP: predicted >100, but actual no optimization needed
                CreatePredictionResult(50, TimeSpan.FromMilliseconds(20)) // FN: predicted <=100, but actual needed optimization
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _optimizer.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(1, result);
            Assert.Single(_config.ExecutionTimeThresholds); // Should be updated to single optimal threshold
            Assert.True(_config.ExecutionTimeThresholds[0] > 0);
        }

        [Fact]
        public void UpdatePatterns_Should_Handle_Empty_Predictions()
        {
            // Arrange
            var predictions = Array.Empty<PredictionResult>();
            var analysis = new PatternAnalysisResult();

            // Act
            var result = _optimizer.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(1, result);
            Assert.Single(_config.ExecutionTimeThresholds);
        }

        [Fact]
        public void UpdatePatterns_Should_Handle_Exception_And_Return_Zero()
        {
            // Arrange
            var predictions = new PredictionResult[] { null! }; // This will cause exception
            var analysis = new PatternAnalysisResult();

            // Act
            var result = _optimizer.UpdatePatterns(predictions, analysis);

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
        public void CalculateThresholdMetrics_Should_Compute_Correct_Metrics()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePredictionResult(150, TimeSpan.FromMilliseconds(50)), // TP
                CreatePredictionResult(80, TimeSpan.Zero), // TN
                CreatePredictionResult(120, TimeSpan.FromMilliseconds(30)), // TP
                CreatePredictionResult(200, TimeSpan.Zero), // FP
                CreatePredictionResult(50, TimeSpan.FromMilliseconds(20)) // FN
            };

            // Act
            var metrics = GetCalculateThresholdMetricsMethod().Invoke(_optimizer, new object[] { predictions, 100 });

            // Assert
            Assert.IsType<ThresholdMetrics>(metrics);
            var thresholdMetrics = (ThresholdMetrics)metrics;
            Assert.Equal(100, thresholdMetrics.Threshold);
            Assert.Equal(2, thresholdMetrics.TruePositives);
            Assert.Equal(1, thresholdMetrics.FalsePositives);
            Assert.Equal(1, thresholdMetrics.TrueNegatives);
            Assert.Equal(1, thresholdMetrics.FalseNegatives);
            Assert.Equal(2.0/3.0, thresholdMetrics.Sensitivity); // 2/(2+1)
            Assert.Equal(0.5, thresholdMetrics.Specificity); // 1/(1+1)
            Assert.Equal(2.0/3.0, thresholdMetrics.Precision); // 2/(2+1)
            Assert.Equal(2.0/3.0, thresholdMetrics.Recall);
            Assert.Equal(0.6, thresholdMetrics.Accuracy); // (2+1)/5
        }

        private static PredictionResult CreatePredictionResult(double executionTimeMs, TimeSpan actualImprovement)
        {
            return new PredictionResult
            {
                RequestType = typeof(object),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                ActualImprovement = actualImprovement,
                Timestamp = DateTime.UtcNow,
                Metrics = new RequestExecutionMetrics
                {
                    AverageExecutionTime = TimeSpan.FromMilliseconds(executionTimeMs),
                    MemoryUsage = 0,
                    CpuUsage = 0,
                    TotalExecutions = 1,
                    SuccessfulExecutions = 1,
                    FailedExecutions = 0
                }
            };
        }

        private System.Reflection.MethodInfo GetCalculateThresholdMetricsMethod()
        {
            var method = typeof(DecisionBoundaryOptimizer).GetMethod("CalculateThresholdMetrics",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return method ?? throw new InvalidOperationException("Method not found");
        }
    }
}