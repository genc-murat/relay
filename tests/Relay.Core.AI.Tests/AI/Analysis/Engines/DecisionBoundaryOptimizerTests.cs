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

        [Fact]
        public void UpdatePatterns_Should_Handle_Multiple_Thresholds_And_Select_Best()
        {
            // Arrange
            _config.ExecutionTimeThresholds = new[] { 50, 100, 150, 200 };
            var predictions = new[]
            {
                CreatePredictionResult(75, TimeSpan.FromMilliseconds(50)),  // TP for 50, TN for 100+
                CreatePredictionResult(125, TimeSpan.FromMilliseconds(30)), // TP for 100, TN for 150+
                CreatePredictionResult(175, TimeSpan.FromMilliseconds(20)), // TP for 150, TN for 200+
                CreatePredictionResult(225, TimeSpan.Zero),                // FP for all thresholds
                CreatePredictionResult(25, TimeSpan.FromMilliseconds(10))  // FN for all thresholds
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _optimizer.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(1, result);
            Assert.Single(_config.ExecutionTimeThresholds); // Should be updated to single optimal threshold
        }

        [Fact]
        public void UpdatePatterns_Should_Handle_All_True_Positives()
        {
            // Arrange
            _config.ExecutionTimeThresholds = new[] { 100 };
            var predictions = new[]
            {
                CreatePredictionResult(150, TimeSpan.FromMilliseconds(50)),
                CreatePredictionResult(120, TimeSpan.FromMilliseconds(30)),
                CreatePredictionResult(200, TimeSpan.FromMilliseconds(40))
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _optimizer.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(1, result);
            Assert.Single(_config.ExecutionTimeThresholds);
        }

        [Fact]
        public void UpdatePatterns_Should_Handle_All_True_Negatives()
        {
            // Arrange
            _config.ExecutionTimeThresholds = new[] { 100 };
            var predictions = new[]
            {
                CreatePredictionResult(50, TimeSpan.Zero),
                CreatePredictionResult(80, TimeSpan.Zero),
                CreatePredictionResult(25, TimeSpan.Zero)
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _optimizer.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(1, result);
            Assert.Single(_config.ExecutionTimeThresholds);
        }

        [Fact]
        public void UpdatePatterns_Should_Handle_All_False_Positives()
        {
            // Arrange
            _config.ExecutionTimeThresholds = new[] { 100 };
            var predictions = new[]
            {
                CreatePredictionResult(150, TimeSpan.Zero),
                CreatePredictionResult(120, TimeSpan.Zero),
                CreatePredictionResult(200, TimeSpan.Zero)
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _optimizer.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(1, result);
            Assert.Single(_config.ExecutionTimeThresholds);
        }

        [Fact]
        public void UpdatePatterns_Should_Handle_All_False_Negatives()
        {
            // Arrange
            _config.ExecutionTimeThresholds = new[] { 100 };
            var predictions = new[]
            {
                CreatePredictionResult(50, TimeSpan.FromMilliseconds(50)),
                CreatePredictionResult(80, TimeSpan.FromMilliseconds(30)),
                CreatePredictionResult(25, TimeSpan.FromMilliseconds(40))
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _optimizer.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(1, result);
            Assert.Single(_config.ExecutionTimeThresholds);
        }

        [Fact]
        public void UpdatePatterns_Should_Handle_Zero_Threshold()
        {
            // Arrange
            _config.ExecutionTimeThresholds = new[] { 0 };
            var predictions = new[]
            {
                CreatePredictionResult(50, TimeSpan.FromMilliseconds(50)),
                CreatePredictionResult(100, TimeSpan.Zero),
                CreatePredictionResult(25, TimeSpan.FromMilliseconds(30))
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _optimizer.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(1, result);
            Assert.Single(_config.ExecutionTimeThresholds);
        }

        [Fact]
        public void UpdatePatterns_Should_Handle_Very_High_Threshold()
        {
            // Arrange
            _config.ExecutionTimeThresholds = new[] { 10000 };
            var predictions = new[]
            {
                CreatePredictionResult(50, TimeSpan.FromMilliseconds(50)),
                CreatePredictionResult(100, TimeSpan.Zero),
                CreatePredictionResult(25, TimeSpan.FromMilliseconds(30))
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _optimizer.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(1, result);
            Assert.Single(_config.ExecutionTimeThresholds);
        }

        [Fact]
        public void UpdatePatterns_Should_Log_Debug_Message_For_Optimal_Threshold()
        {
            // Arrange
            _config.ExecutionTimeThresholds = new[] { 100 };
            var predictions = new[]
            {
                CreatePredictionResult(150, TimeSpan.FromMilliseconds(50)),
                CreatePredictionResult(80, TimeSpan.Zero)
            };

            var analysis = new PatternAnalysisResult();

            // Act
            _optimizer.UpdatePatterns(predictions, analysis);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Optimal execution time threshold")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void CalculateThresholdMetrics_Should_Handle_Empty_Predictions()
        {
            // Arrange
            var predictions = Array.Empty<PredictionResult>();

            // Act
            var metrics = GetCalculateThresholdMetricsMethod().Invoke(_optimizer, new object[] { predictions, 100 });

            // Assert
            Assert.IsType<ThresholdMetrics>(metrics);
            var thresholdMetrics = (ThresholdMetrics)metrics;
            Assert.Equal(100, thresholdMetrics.Threshold);
            Assert.Equal(0, thresholdMetrics.TruePositives);
            Assert.Equal(0, thresholdMetrics.FalsePositives);
            Assert.Equal(0, thresholdMetrics.TrueNegatives);
            Assert.Equal(0, thresholdMetrics.FalseNegatives);
            Assert.Equal(0, thresholdMetrics.Sensitivity);
            Assert.Equal(0, thresholdMetrics.Specificity);
            Assert.Equal(0, thresholdMetrics.Precision);
            Assert.Equal(0, thresholdMetrics.Recall);
            Assert.Equal(0, thresholdMetrics.Accuracy);
        }

        [Fact]
        public void CalculateThresholdMetrics_Should_Handle_Perfect_Classification()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePredictionResult(150, TimeSpan.FromMilliseconds(50)), // TP
                CreatePredictionResult(120, TimeSpan.FromMilliseconds(30)), // TP
                CreatePredictionResult(80, TimeSpan.Zero),                  // TN
                CreatePredictionResult(50, TimeSpan.Zero)                  // TN
            };

            // Act
            var metrics = GetCalculateThresholdMetricsMethod().Invoke(_optimizer, new object[] { predictions, 100 });

            // Assert
            Assert.IsType<ThresholdMetrics>(metrics);
            var thresholdMetrics = (ThresholdMetrics)metrics;
            Assert.Equal(100, thresholdMetrics.Threshold);
            Assert.Equal(2, thresholdMetrics.TruePositives);
            Assert.Equal(0, thresholdMetrics.FalsePositives);
            Assert.Equal(2, thresholdMetrics.TrueNegatives);
            Assert.Equal(0, thresholdMetrics.FalseNegatives);
            Assert.Equal(1.0, thresholdMetrics.Sensitivity);
            Assert.Equal(1.0, thresholdMetrics.Specificity);
            Assert.Equal(1.0, thresholdMetrics.Precision);
            Assert.Equal(1.0, thresholdMetrics.Recall);
            Assert.Equal(1.0, thresholdMetrics.Accuracy);
        }

        [Fact]
        public void CalculateThresholdMetrics_Should_Handle_Worst_Classification()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePredictionResult(150, TimeSpan.Zero), // FP
                CreatePredictionResult(120, TimeSpan.Zero), // FP
                CreatePredictionResult(80, TimeSpan.FromMilliseconds(50)),  // FN
                CreatePredictionResult(50, TimeSpan.FromMilliseconds(30))     // FN
            };

            // Act
            var metrics = GetCalculateThresholdMetricsMethod().Invoke(_optimizer, new object[] { predictions, 100 });

            // Assert
            Assert.IsType<ThresholdMetrics>(metrics);
            var thresholdMetrics = (ThresholdMetrics)metrics;
            Assert.Equal(100, thresholdMetrics.Threshold);
            Assert.Equal(0, thresholdMetrics.TruePositives);
            Assert.Equal(2, thresholdMetrics.FalsePositives);
            Assert.Equal(0, thresholdMetrics.TrueNegatives);
            Assert.Equal(2, thresholdMetrics.FalseNegatives);
            Assert.Equal(0, thresholdMetrics.Sensitivity);
            Assert.Equal(0, thresholdMetrics.Specificity);
            Assert.Equal(0, thresholdMetrics.Precision);
            Assert.Equal(0, thresholdMetrics.Recall);
            Assert.Equal(0, thresholdMetrics.Accuracy);
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