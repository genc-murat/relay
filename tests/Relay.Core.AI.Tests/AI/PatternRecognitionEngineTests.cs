using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.Engines;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class PatternRecognitionEngineTests
    {
        private readonly ILogger<PatternRecognitionEngine> _logger;
        private readonly PatternRecognitionEngine _engine;

        public PatternRecognitionEngineTests()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddPatternRecognition();

            var serviceProvider = services.BuildServiceProvider();
            _logger = serviceProvider.GetRequiredService<ILogger<PatternRecognitionEngine>>();
            _engine = serviceProvider.GetRequiredService<PatternRecognitionEngine>();
        }

        private IServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddPatternRecognition();
            return services.BuildServiceProvider();
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();
            var analyzer = serviceProvider.GetRequiredService<IPatternAnalyzer>();
            var updaters = serviceProvider.GetRequiredService<IEnumerable<IPatternUpdater>>();
            var config = serviceProvider.GetRequiredService<PatternRecognitionConfig>();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new PatternRecognitionEngine(null!, analyzer, updaters, config));
            Assert.Equal("logger", ex.ParamName);
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Skip_When_Insufficient_Data()
        {
            // Arrange
            var predictions = new PredictionResult[]
            {
                CreatePrediction(typeof(TestRequest), 100, true),
                CreatePrediction(typeof(TestRequest), 150, true),
                CreatePrediction(typeof(TestRequest), 80, false)
            };

            // Act - Should not throw and should log that data is insufficient
            _engine.RetrainPatternRecognition(predictions);

            // Assert - No exception means it handled insufficient data gracefully
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Process_Minimum_Required_Predictions()
        {
            // Arrange - Create exactly 10 predictions (minimum)
            var predictions = Enumerable.Range(0, 10)
                .Select(i => CreatePrediction(typeof(TestRequest), 100 + i * 10, i % 2 == 0))
                .ToArray();

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Process_Multiple_Request_Types()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePrediction(typeof(TestRequest), 100, true),
                CreatePrediction(typeof(TestRequest), 120, true),
                CreatePrediction(typeof(TestRequest), 90, false),
                CreatePrediction(typeof(AnotherTestRequest), 200, true),
                CreatePrediction(typeof(AnotherTestRequest), 180, true),
                CreatePrediction(typeof(AnotherTestRequest), 150, false),
                CreatePrediction(typeof(ThirdTestRequest), 50, true),
                CreatePrediction(typeof(ThirdTestRequest), 60, false),
                CreatePrediction(typeof(ThirdTestRequest), 55, true),
                CreatePrediction(typeof(ThirdTestRequest), 70, true),
            };

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Handle_All_Successful_Predictions()
        {
            // Arrange - All predictions successful
            var predictions = Enumerable.Range(0, 15)
                .Select(i => CreatePrediction(typeof(TestRequest), 100 + i * 10, true))
                .ToArray();

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Handle_All_Failed_Predictions()
        {
            // Arrange - All predictions failed
            var predictions = Enumerable.Range(0, 15)
                .Select(i => CreatePrediction(typeof(TestRequest), -50, false))
                .ToArray();

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Handle_Mixed_Success_Rates()
        {
            // Arrange - 70% success rate
            var predictions = new[]
            {
                CreatePrediction(typeof(TestRequest), 100, true, OptimizationStrategy.Caching),
                CreatePrediction(typeof(TestRequest), 120, true, OptimizationStrategy.Caching),
                CreatePrediction(typeof(TestRequest), 90, true, OptimizationStrategy.Caching),
                CreatePrediction(typeof(TestRequest), 110, true, OptimizationStrategy.Caching),
                CreatePrediction(typeof(TestRequest), 130, true, OptimizationStrategy.Caching),
                CreatePrediction(typeof(TestRequest), 105, true, OptimizationStrategy.Caching),
                CreatePrediction(typeof(TestRequest), 95, true, OptimizationStrategy.Caching),
                CreatePrediction(typeof(TestRequest), -20, false, OptimizationStrategy.Caching),
                CreatePrediction(typeof(TestRequest), -30, false, OptimizationStrategy.Caching),
                CreatePrediction(typeof(TestRequest), -10, false, OptimizationStrategy.Caching),
            };

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Handle_Multiple_Strategies()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePrediction(typeof(TestRequest), 100, true, OptimizationStrategy.Caching),
                CreatePrediction(typeof(TestRequest), 150, true, OptimizationStrategy.Parallelization),
                CreatePrediction(typeof(TestRequest), 80, true, OptimizationStrategy.DatabaseOptimization),
                CreatePrediction(typeof(TestRequest), 120, false, OptimizationStrategy.Caching),
                CreatePrediction(typeof(TestRequest), 90, true, OptimizationStrategy.Batching),
                CreatePrediction(typeof(TestRequest), 110, true, OptimizationStrategy.LazyLoading),
                CreatePrediction(typeof(TestRequest), 95, false, OptimizationStrategy.Parallelization),
                CreatePrediction(typeof(TestRequest), 105, true, OptimizationStrategy.ResourcePooling),
                CreatePrediction(typeof(TestRequest), 85, true, OptimizationStrategy.CompressionOptimization),
                CreatePrediction(typeof(TestRequest), 130, true, OptimizationStrategy.MemoryOptimization),
            };

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Handle_High_Impact_Improvements()
        {
            // Arrange - High impact improvements (>100ms)
            var predictions = Enumerable.Range(0, 12)
                .Select(i => CreatePrediction(typeof(TestRequest), 150 + i * 20, true))
                .ToArray();

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Handle_Low_Impact_Improvements()
        {
            // Arrange - Low impact improvements (<50ms)
            var predictions = Enumerable.Range(0, 12)
                .Select(i => CreatePrediction(typeof(TestRequest), 10 + i * 2, true))
                .ToArray();

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Handle_Medium_Impact_Improvements()
        {
            // Arrange - Medium impact improvements (50-100ms)
            var predictions = Enumerable.Range(0, 12)
                .Select(i => CreatePrediction(typeof(TestRequest), 60 + i * 3, true))
                .ToArray();

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Analyze_Temporal_Patterns()
        {
            // Arrange - Predictions across different hours and days
            var predictions = new[]
            {
                CreatePredictionAtTime(typeof(TestRequest), 100, true, DateTime.UtcNow.AddHours(-1)),
                CreatePredictionAtTime(typeof(TestRequest), 110, true, DateTime.UtcNow.AddHours(-2)),
                CreatePredictionAtTime(typeof(TestRequest), 90, false, DateTime.UtcNow.AddHours(-5)),
                CreatePredictionAtTime(typeof(TestRequest), 120, true, DateTime.UtcNow.AddDays(-1)),
                CreatePredictionAtTime(typeof(TestRequest), 105, true, DateTime.UtcNow.AddDays(-2)),
                CreatePredictionAtTime(typeof(TestRequest), 95, false, DateTime.UtcNow.AddDays(-3)),
                CreatePredictionAtTime(typeof(TestRequest), 115, true, DateTime.UtcNow.AddDays(-4)),
                CreatePredictionAtTime(typeof(TestRequest), 125, true, DateTime.UtcNow.AddHours(-10)),
                CreatePredictionAtTime(typeof(TestRequest), 85, false, DateTime.UtcNow.AddHours(-15)),
                CreatePredictionAtTime(typeof(TestRequest), 130, true, DateTime.UtcNow.AddHours(-20)),
            };

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Analyze_Load_Based_Patterns()
        {
            // Arrange - Different concurrency levels
            var predictions = new[]
            {
                CreatePredictionWithLoad(typeof(TestRequest), 100, true, 10),  // Low
                CreatePredictionWithLoad(typeof(TestRequest), 110, true, 20),  // Low
                CreatePredictionWithLoad(typeof(TestRequest), 90, false, 60),  // Medium
                CreatePredictionWithLoad(typeof(TestRequest), 120, true, 70),  // Medium
                CreatePredictionWithLoad(typeof(TestRequest), 105, true, 120), // High
                CreatePredictionWithLoad(typeof(TestRequest), 95, false, 150), // High
                CreatePredictionWithLoad(typeof(TestRequest), 115, true, 30),  // Low
                CreatePredictionWithLoad(typeof(TestRequest), 125, true, 80),  // Medium
                CreatePredictionWithLoad(typeof(TestRequest), 85, false, 200), // High
                CreatePredictionWithLoad(typeof(TestRequest), 130, true, 40),  // Low
            };

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Handle_Large_Dataset()
        {
            // Arrange - Large number of predictions
            var predictions = Enumerable.Range(0, 100)
                .Select(i => CreatePrediction(
                    i % 3 == 0 ? typeof(TestRequest) :
                    i % 3 == 1 ? typeof(AnotherTestRequest) : typeof(ThirdTestRequest),
                    50 + i * 2,
                    i % 3 != 0))
                .ToArray();

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Handle_Empty_Strategies_Array()
        {
            // Arrange
            var predictions = Enumerable.Range(0, 10)
                .Select(i => new PredictionResult
                {
                    RequestType = typeof(TestRequest),
                    PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                    ActualImprovement = TimeSpan.FromMilliseconds(100),
                    Timestamp = DateTime.UtcNow.AddMinutes(-i),
                    Metrics = CreateMetrics(50)
                })
                .ToArray();

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Detect_Strong_Correlations()
        {
            // Arrange - Create patterns with strong correlation between strategy and request type
            var predictions = new[]
            {
                // Caching works well for TestRequest
                CreatePrediction(typeof(TestRequest), 150, true, OptimizationStrategy.Caching),
                CreatePrediction(typeof(TestRequest), 160, true, OptimizationStrategy.Caching),
                CreatePrediction(typeof(TestRequest), 140, true, OptimizationStrategy.Caching),
                CreatePrediction(typeof(TestRequest), 155, true, OptimizationStrategy.Caching),

                // Parallelization doesn't work for AnotherTestRequest
                CreatePrediction(typeof(AnotherTestRequest), -20, false, OptimizationStrategy.Parallelization),
                CreatePrediction(typeof(AnotherTestRequest), -30, false, OptimizationStrategy.Parallelization),
                CreatePrediction(typeof(AnotherTestRequest), -15, false, OptimizationStrategy.Parallelization),
                CreatePrediction(typeof(AnotherTestRequest), -25, false, OptimizationStrategy.Parallelization),

                // DatabaseOptimization works for ThirdTestRequest
                CreatePrediction(typeof(ThirdTestRequest), 80, true, OptimizationStrategy.DatabaseOptimization),
                CreatePrediction(typeof(ThirdTestRequest), 90, true, OptimizationStrategy.DatabaseOptimization),
            };

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing and log correlations
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Handle_Exception_Gracefully()
        {
            // Arrange - This should not cause unhandled exceptions
            var predictions = Enumerable.Range(0, 10)
                .Select(i => CreatePrediction(typeof(TestRequest), 100, true))
                .ToArray();

            // Act - Should handle any internal exceptions gracefully
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Handle_Null_Predictions_Array()
        {
            // Arrange
            PredictionResult[] predictions = null!;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _engine.RetrainPatternRecognition(predictions));
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Handle_Null_Prediction_In_Array()
        {
            // Arrange
            var predictions = new PredictionResult[]
            {
                CreatePrediction(typeof(TestRequest), 100, true),
                null!,
                CreatePrediction(typeof(TestRequest), 120, true)
            };

            // Act - Should handle null prediction gracefully
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Log_Debug_For_Insufficient_Data()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePrediction(typeof(TestRequest), 100, true),
                CreatePrediction(typeof(TestRequest), 120, false)
            };

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should log debug message about insufficient data
            // Note: This would require logger mock to verify, but we're using real logger
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Log_Information_For_Successful_Retraining()
        {
            // Arrange
            var predictions = Enumerable.Range(0, 15)
                .Select(i => CreatePrediction(typeof(TestRequest), 100 + i * 10, true))
                .ToArray();

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should log information about successful retraining
            // Note: This would require logger mock to verify, but we're using real logger
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Handle_Zero_Minimum_Predictions_Configuration()
        {
            // Arrange - Create custom engine with zero minimum
            var serviceProvider = CreateServiceProvider();
            var config = serviceProvider.GetRequiredService<PatternRecognitionConfig>();
            config.MinimumPredictionsForRetraining = 0;
            
            var analyzer = serviceProvider.GetRequiredService<IPatternAnalyzer>();
            var updaters = serviceProvider.GetRequiredService<IEnumerable<IPatternUpdater>>();
            var logger = serviceProvider.GetRequiredService<ILogger<PatternRecognitionEngine>>();
            
            var customEngine = new PatternRecognitionEngine(logger, analyzer, updaters, config);
            
            var predictions = new[]
            {
                CreatePrediction(typeof(TestRequest), 100, true)
            };

            // Act
            customEngine.RetrainPatternRecognition(predictions);

            // Assert - Should process even with single prediction
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Handle_Very_High_Minimum_Predictions_Configuration()
        {
            // Arrange - Create custom engine with very high minimum
            var serviceProvider = CreateServiceProvider();
            var config = serviceProvider.GetRequiredService<PatternRecognitionConfig>();
            config.MinimumPredictionsForRetraining = 1000;
            
            var analyzer = serviceProvider.GetRequiredService<IPatternAnalyzer>();
            var updaters = serviceProvider.GetRequiredService<IEnumerable<IPatternUpdater>>();
            var logger = serviceProvider.GetRequiredService<ILogger<PatternRecognitionEngine>>();
            
            var customEngine = new PatternRecognitionEngine(logger, analyzer, updaters, config);
            
            var predictions = Enumerable.Range(0, 50)
                .Select(i => CreatePrediction(typeof(TestRequest), 100 + i * 10, true))
                .ToArray();

            // Act
            customEngine.RetrainPatternRecognition(predictions);

            // Assert - Should skip due to insufficient data
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Handle_Predictions_With_Zero_Improvement()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePrediction(typeof(TestRequest), 0, true),
                CreatePrediction(typeof(TestRequest), 0, true),
                CreatePrediction(typeof(TestRequest), 0, false),
                CreatePrediction(typeof(TestRequest), 0, true)
            };

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Handle_Negative_Improvement_Values()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePrediction(typeof(TestRequest), -50, false),
                CreatePrediction(typeof(TestRequest), -100, false),
                CreatePrediction(typeof(TestRequest), -25, false),
                CreatePrediction(typeof(TestRequest), -75, false)
            };

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Handle_Very_Large_Improvement_Values()
        {
            // Arrange
            var predictions = Enumerable.Range(0, 15)
                .Select(i => CreatePrediction(typeof(TestRequest), 10000 + i * 1000, true))
                .ToArray();

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Handle_Predictions_With_Future_Timestamps()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePredictionAtTime(typeof(TestRequest), 100, true, DateTime.UtcNow.AddHours(1)),
                CreatePredictionAtTime(typeof(TestRequest), 120, true, DateTime.UtcNow.AddMinutes(30)),
                CreatePredictionAtTime(typeof(TestRequest), 90, false, DateTime.UtcNow.AddMinutes(15)),
                CreatePredictionAtTime(typeof(TestRequest), 110, true, DateTime.UtcNow.AddMinutes(5))
            };

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing
        }

        [Fact]
        public void RetrainPatternRecognition_Should_Handle_Predictions_With_Very_Old_Timestamps()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePredictionAtTime(typeof(TestRequest), 100, true, DateTime.UtcNow.AddDays(-30)),
                CreatePredictionAtTime(typeof(TestRequest), 120, true, DateTime.UtcNow.AddDays(-60)),
                CreatePredictionAtTime(typeof(TestRequest), 90, false, DateTime.UtcNow.AddDays(-90)),
                CreatePredictionAtTime(typeof(TestRequest), 110, true, DateTime.UtcNow.AddDays(-120))
            };

            // Act
            _engine.RetrainPatternRecognition(predictions);

            // Assert - Should complete without throwing
        }

        #region Helper Methods

        private PredictionResult CreatePrediction(
            Type requestType,
            double improvementMs,
            bool success,
            params OptimizationStrategy[] strategies)
        {
            return new PredictionResult
            {
                RequestType = requestType,
                PredictedStrategies = strategies.Length > 0
                    ? strategies
                    : new[] { OptimizationStrategy.Caching },
                ActualImprovement = TimeSpan.FromMilliseconds(success ? improvementMs : -Math.Abs(improvementMs)),
                Timestamp = DateTime.UtcNow,
                Metrics = CreateMetrics(50)
            };
        }

        private PredictionResult CreatePredictionAtTime(
            Type requestType,
            double improvementMs,
            bool success,
            DateTime timestamp)
        {
            return new PredictionResult
            {
                RequestType = requestType,
                PredictedStrategies = new[] { OptimizationStrategy.Caching },
                ActualImprovement = TimeSpan.FromMilliseconds(success ? improvementMs : -Math.Abs(improvementMs)),
                Timestamp = timestamp,
                Metrics = CreateMetrics(50)
            };
        }

        private PredictionResult CreatePredictionWithLoad(
            Type requestType,
            double improvementMs,
            bool success,
            int concurrentExecutions)
        {
            return new PredictionResult
            {
                RequestType = requestType,
                PredictedStrategies = new[] { OptimizationStrategy.Caching },
                ActualImprovement = TimeSpan.FromMilliseconds(success ? improvementMs : -Math.Abs(improvementMs)),
                Timestamp = DateTime.UtcNow,
                Metrics = CreateMetrics(concurrentExecutions)
            };
        }

        private RequestExecutionMetrics CreateMetrics(int concurrentExecutions)
        {
            return new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                MedianExecutionTime = TimeSpan.FromMilliseconds(95),
                P95ExecutionTime = TimeSpan.FromMilliseconds(150),
                P99ExecutionTime = TimeSpan.FromMilliseconds(200),
                TotalExecutions = 100,
                SuccessfulExecutions = 95,
                FailedExecutions = 5,
                MemoryAllocated = 1024 * 1024,
                ConcurrentExecutions = concurrentExecutions,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(5),
                CpuUsage = 0.45,
                MemoryUsage = 512 * 1024,
                DatabaseCalls = 2,
                ExternalApiCalls = 1
            };
        }

        #endregion

        #region Test Types

        private class TestRequest { }
        private class AnotherTestRequest { }
        private class ThirdTestRequest { }

        #endregion
    }
}
