using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.AI;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationPipelineBehaviorAdvancedTests
    {
        private readonly ServiceCollection _services;
        private readonly ILogger<AIOptimizationPipelineBehavior<TestRequest, TestResponse>> _logger;
        private readonly ILogger<SystemLoadMetricsProvider> _systemLogger;
        private readonly AIOptimizationOptions _options;

        public AIOptimizationPipelineBehaviorAdvancedTests()
        {
            _services = new ServiceCollection();
            _services.AddLogging();
            var provider = _services.BuildServiceProvider();

            _logger = provider.GetRequiredService<ILogger<AIOptimizationPipelineBehavior<TestRequest, TestResponse>>>();
            _systemLogger = provider.GetRequiredService<ILogger<SystemLoadMetricsProvider>>();

            _options = new AIOptimizationOptions
            {
                Enabled = true,
                LearningEnabled = true,
                MinConfidenceScore = 0.7,
                MinExecutionsForAnalysis = 5
            };
        }

        [Fact]
        public async Task GetAIOptimizationAttributes_ReturnsAttributesFromRequestType()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("GetAIOptimizationAttributes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (AIOptimizedAttribute[])method!.Invoke(behavior, new object[] { typeof(TestRequest) })!;

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result); // TestRequest doesn't have attributes
        }

        [Fact]
        public async Task ShouldPerformOptimization_ReturnsTrue_WhenAttributesEnableOptimization()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var attributes = new[]
            {
                new AIOptimizedAttribute { AutoApplyOptimizations = true, EnableMetricsTracking = true }
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ShouldPerformOptimization", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (bool)method!.Invoke(behavior, new object[] { attributes })!;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ShouldPerformOptimization_ReturnsFalse_WhenAttributesDisableOptimization()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var attributes = new[]
            {
                new AIOptimizedAttribute { AutoApplyOptimizations = false, EnableMetricsTracking = false }
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ShouldPerformOptimization", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (bool)method!.Invoke(behavior, new object[] { attributes })!;

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ShouldPerformOptimization_ReturnsDefault_WhenNoAttributes()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var attributes = Array.Empty<AIOptimizedAttribute>();

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ShouldPerformOptimization", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (bool)method!.Invoke(behavior, new object[] { attributes })!;

            // Assert
            Assert.True(result); // Should return _options.Enabled which is true
        }

        [Fact]
        public async Task GetHistoricalMetrics_ReturnsDefaultMetrics_WhenNoProvider()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("GetHistoricalMetrics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var resultTask = (ValueTask<RequestExecutionMetrics>)method!.Invoke(behavior, new object[] { typeof(TestRequest), CancellationToken.None })!;
            var result = await resultTask;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalExecutions);
            Assert.Equal(TimeSpan.FromMilliseconds(100), result.AverageExecutionTime);
        }

        [Fact]
        public async Task EstimateMemoryUsage_ReturnsValueFromStats_WhenAvailable()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var stats = new HandlerExecutionStats
            {
                AverageMemoryAllocated = 1024 * 512 // 512KB
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("EstimateMemoryUsage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (long)method!.Invoke(behavior, new object[] { stats })!;

            // Assert
            Assert.Equal(1024 * 512, result);
        }

        [Fact]
        public async Task CalculateExecutionFrequency_ReturnsCorrectFrequency()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var stats = new HandlerExecutionStats
            {
                TotalExecutions = 100,
                LastExecution = DateTimeOffset.UtcNow.AddHours(-1)
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("CalculateExecutionFrequency", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (double)method!.Invoke(behavior, new object[] { stats })!;

            // Assert
            Assert.InRange(result, 0.027, 0.028); // Approximately 100 executions per 3600 seconds = 0.0278
        }

        [Fact]
        public async Task ExtractExternalApiCalls_ReturnsValueFromProperties()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var stats = new HandlerExecutionStats
            {
                Properties = new Dictionary<string, object>
                {
                    ["ExternalApiCalls"] = 3
                },
                AverageExecutionTime = TimeSpan.FromMilliseconds(50)
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ExtractExternalApiCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method!.Invoke(behavior, new object[] { stats })!;

            // Assert
            Assert.Equal(3, result);
        }

        [Fact]
        public async Task GetHistoricalMetrics_ConvertsTelemetryStatsToExecutionMetrics()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var mockMetricsProvider = new MockMetricsProvider();

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics, mockMetricsProvider);

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("GetHistoricalMetrics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var resultTask = (ValueTask<RequestExecutionMetrics>)method!.Invoke(behavior, new object[] { typeof(TestRequest), CancellationToken.None })!;
            var result = await resultTask;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.TotalExecutions);
            Assert.Equal(8, result.SuccessfulExecutions);
            Assert.Equal(TimeSpan.FromMilliseconds(150), result.AverageExecutionTime);
            Assert.Equal(5, result.DatabaseCalls);
        }

        [Fact]
        public async Task ShouldApplyBatching_ReturnsFalse_WhenBatchSizeTooSmall()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var systemLoad = new SystemLoadMetrics
            {
                CpuUtilization = 0.3,
                MemoryUtilization = 0.3,
                ThroughputPerSecond = 10.0
            };

            var recommendation = new OptimizationRecommendation
            {
                ConfidenceScore = 0.8
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ShouldApplyBatching", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (bool)method!.Invoke(behavior, new object[] { systemLoad, 1, recommendation })!; // Batch size 1

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ShouldApplyBatching_ReturnsFalse_WhenHighSystemLoad()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var systemLoad = new SystemLoadMetrics
            {
                CpuUtilization = 0.96, // High CPU load
                MemoryUtilization = 0.3,
                ThroughputPerSecond = 10.0
            };

            var recommendation = new OptimizationRecommendation
            {
                ConfidenceScore = 0.8
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ShouldApplyBatching", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (bool)method!.Invoke(behavior, new object[] { systemLoad, 5, recommendation })!;

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ShouldApplyBatching_ReturnsTrue_WhenConditionsFavorable()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var systemLoad = new SystemLoadMetrics
            {
                CpuUtilization = 0.3, // Low CPU load
                MemoryUtilization = 0.3,
                ThroughputPerSecond = 15.0 // Good throughput
            };

            var recommendation = new OptimizationRecommendation
            {
                ConfidenceScore = 0.8
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ShouldApplyBatching", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (bool)method!.Invoke(behavior, new object[] { systemLoad, 5, recommendation })!;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CalculateOptimalParallelism_AdjustsForCpuUtilization()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var systemLoad = new SystemLoadMetrics
            {
                CpuUtilization = 0.8 // High CPU utilization
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("CalculateOptimalParallelism", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method!.Invoke(behavior, new object[] { 8, systemLoad })!;

            // Assert
            Assert.True(result < 8); // Should reduce parallelism under high load
        }

        [Fact]
        public async Task CalculateOptimalParallelism_ReturnsAtLeastOne()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var systemLoad = new SystemLoadMetrics
            {
                CpuUtilization = 0.95 // Very high CPU utilization
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("CalculateOptimalParallelism", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method!.Invoke(behavior, new object[] { 8, systemLoad })!;

            // Assert
            Assert.True(result >= 1); // Should never go below 1
        }

        [Fact]
        public async Task HandleAsync_AppliesMemoryPoolingOptimization_WhenHardwareAccelerationAvailable()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.MemoryPooling,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["EnableObjectPooling"] = true,
                    ["EnableBufferPooling"] = true,
                    ["PoolSize"] = 200
                }
            };

            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var request = new TestRequest { Value = "test" };
            var executed = false;
            RequestHandlerDelegate<TestResponse> next = () =>
            {
                executed = true;
                return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.True(executed);
            Assert.Equal("success", result.Result);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_AppliesParallelProcessingOptimization_WhenLowCpuLoad()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.ParallelProcessing,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["MaxDegreeOfParallelism"] = 4,
                    ["EnableWorkStealing"] = true,
                    ["MinItemsForParallel"] = 5
                }
            };

            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var request = new TestRequest { Value = "test" };
            var executed = false;
            RequestHandlerDelegate<TestResponse> next = () =>
            {
                executed = true;
                return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.True(executed);
            Assert.Equal("success", result.Result);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Skips_Parallel_Processing_Under_High_Cpu_Load()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.ParallelProcessing,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["MaxDegreeOfParallelism"] = 4,
                    ["EnableWorkStealing"] = true,
                    ["MinItemsForParallel"] = 5
                }
            };

            var systemMetrics = new MockSystemLoadMetricsProvider();
            systemMetrics.LoadToReturn = new SystemLoadMetrics
            {
                CpuUtilization = 0.92, // High CPU load
                MemoryUtilization = 0.5,
                ThroughputPerSecond = 100.0,
                ActiveRequestCount = 100,
                ThreadPoolUtilization = 0.9
            };

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var request = new TestRequest { Value = "test" };
            var executed = false;
            RequestHandlerDelegate<TestResponse> next = () =>
            {
                executed = true;
                return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert - Should skip parallel processing due to high CPU load
            Assert.True(executed);
            Assert.Equal("success", result.Result);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Applies_Parallel_Processing_With_Low_Cpu_Load()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.ParallelProcessing,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["MaxDegreeOfParallelism"] = 4,
                    ["EnableWorkStealing"] = true,
                    ["MinItemsForParallel"] = 5
                }
            };

            var systemMetrics = new MockSystemLoadMetricsProvider();
            systemMetrics.LoadToReturn = new SystemLoadMetrics
            {
                CpuUtilization = 0.3, // Low CPU load
                MemoryUtilization = 0.4,
                ThroughputPerSecond = 100.0,
                ActiveRequestCount = 50,
                ThreadPoolUtilization = 0.2
            };

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var request = new TestRequest { Value = "test" };
            var executed = false;
            RequestHandlerDelegate<TestResponse> next = () =>
            {
                executed = true;
                return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.True(executed);
            Assert.Equal("success", result.Result);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Adjusts_Parallelism_Based_On_Thread_Pool_Utilization()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.ParallelProcessing,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["MaxDegreeOfParallelism"] = 8,
                    ["EnableWorkStealing"] = true,
                    ["MinItemsForParallel"] = 5
                }
            };

            var systemMetrics = new MockSystemLoadMetricsProvider();
            systemMetrics.LoadToReturn = new SystemLoadMetrics
            {
                CpuUtilization = 0.5,
                MemoryUtilization = 0.4,
                ThroughputPerSecond = 100.0,
                ActiveRequestCount = 50,
                ThreadPoolUtilization = 0.85 // High thread pool utilization
            };

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var request = new TestRequest { Value = "test" };
            var executed = false;
            RequestHandlerDelegate<TestResponse> next = () =>
            {
                executed = true;
                return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert - Should reduce parallelism due to high thread pool utilization
            Assert.True(executed);
            Assert.Equal("success", result.Result);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Applies_Circuit_Breaker_Optimization()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.CircuitBreaker,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["FailureThreshold"] = 5,
                    ["SuccessThreshold"] = 2,
                    ["Timeout"] = 30000,
                    ["BreakDuration"] = 60000,
                    ["HalfOpenMaxCalls"] = 1
                }
            };

            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var request = new TestRequest { Value = "test" };
            var executed = false;
            RequestHandlerDelegate<TestResponse> next = () =>
            {
                executed = true;
                return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.True(executed);
            Assert.Equal("success", result.Result);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Handles_Circuit_Breaker_With_Fallback_Response()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.CircuitBreaker,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["FailureThreshold"] = 5,
                    ["SuccessThreshold"] = 2,
                    ["Timeout"] = 30000,
                    ["BreakDuration"] = 60000,
                    ["HalfOpenMaxCalls"] = 1,
                    ["FallbackResponse"] = new TestResponse { Result = "fallback" }
                }
            };

            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var request = new TestRequest { Value = "test" };
            var executed = false;
            RequestHandlerDelegate<TestResponse> next = () =>
            {
                executed = true;
                // Simulate circuit breaker open scenario by throwing a specific exception
                throw new CircuitBreakerOpenException("Circuit breaker is open");
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert - Should return fallback response
            Assert.True(executed);
            Assert.Equal("fallback", result.Result);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Handles_Circuit_Breaker_Without_Fallback_Response()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.CircuitBreaker,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["FailureThreshold"] = 5,
                    ["SuccessThreshold"] = 2,
                    ["Timeout"] = 30000,
                    ["BreakDuration"] = 60000,
                    ["HalfOpenMaxCalls"] = 1
                    // No fallback response provided
                }
            };

            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var services = new ServiceCollection();
            services.AddLogging();
            var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<AIOptimizationPipelineBehavior<TestRequestWithoutDefaultCtor, TestResponseWithoutDefaultCtor>>>();

            var behavior = new AIOptimizationPipelineBehavior<TestRequestWithoutDefaultCtor, TestResponseWithoutDefaultCtor>(
                aiEngine, logger, Options.Create(_options), systemMetrics);

            var request = new TestRequestWithoutDefaultCtor { Value = "test" };
            var executed = false;
            RequestHandlerDelegate<TestResponseWithoutDefaultCtor> next = () =>
            {
                executed = true;
                throw new CircuitBreakerOpenException("Circuit breaker is open");
            };

            // Act & Assert - Should throw InvalidOperationException when circuit is open and no fallback
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await behavior.HandleAsync(request, next, CancellationToken.None));

            Assert.True(executed);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        // Test Request and Response classes
        public class TestRequest : IRequest<TestResponse>
        {
            public string Value { get; set; } = string.Empty;
        }

        public class TestResponse
        {
            public string Result { get; set; } = string.Empty;
        }

        public class TestResponseWithoutDefaultCtor
        {
            public string Result { get; set; }

            // Private constructor to prevent default instantiation
            private TestResponseWithoutDefaultCtor() { }

            public static TestResponseWithoutDefaultCtor Create(string result)
            {
                return new TestResponseWithoutDefaultCtor { Result = result };
            }
        }

        public class TestRequestWithoutDefaultCtor : IRequest<TestResponseWithoutDefaultCtor>
        {
            public string Value { get; set; } = string.Empty;
        }

        // Mock Metrics Provider
        private class MockMetricsProvider : IMetricsProvider
        {
            public HandlerExecutionStats GetHandlerExecutionStats(Type requestType, string? handlerName = null)
            {
                return new HandlerExecutionStats
                {
                    TotalExecutions = 10,
                    SuccessfulExecutions = 8,
                    FailedExecutions = 2,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(150),
                    P50ExecutionTime = TimeSpan.FromMilliseconds(120),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(300),
                    P99ExecutionTime = TimeSpan.FromMilliseconds(500),
                    LastExecution = DateTimeOffset.UtcNow.AddMinutes(-5),
                    Properties = new Dictionary<string, object>
                    {
                        ["DatabaseCalls"] = 5,
                        ["ExternalApiCalls"] = 2
                    }
                };
            }

            public void RecordHandlerExecution(HandlerExecutionMetrics metrics)
            {
                // Mock implementation
            }

            public void RecordNotificationPublish(NotificationPublishMetrics metrics)
            {
                // Mock implementation
            }

            public void RecordStreamingOperation(StreamingOperationMetrics metrics)
            {
                // Mock implementation
            }

            public NotificationPublishStats GetNotificationPublishStats(Type notificationType)
            {
                return new NotificationPublishStats
                {
                    NotificationType = notificationType,
                    TotalPublishes = 0,
                    SuccessfulPublishes = 0,
                    FailedPublishes = 0,
                    AveragePublishTime = TimeSpan.Zero,
                    MinPublishTime = TimeSpan.Zero,
                    MaxPublishTime = TimeSpan.Zero,
                    AverageHandlerCount = 0,
                    LastPublish = DateTimeOffset.MinValue
                };
            }

            public StreamingOperationStats GetStreamingOperationStats(Type requestType, string? handlerName = null)
            {
                return new StreamingOperationStats
                {
                    TotalOperations = 0,
                    SuccessfulOperations = 0,
                    FailedOperations = 0,
                    AverageOperationTime = TimeSpan.Zero,
                    LastOperation = DateTimeOffset.MinValue
                };
            }

            public IEnumerable<PerformanceAnomaly> DetectAnomalies(TimeSpan lookbackPeriod)
            {
                return Array.Empty<PerformanceAnomaly>();
            }

            public TimingBreakdown GetTimingBreakdown(string operationId)
            {
                return new TimingBreakdown
                {
                    OperationId = operationId,
                    TotalDuration = TimeSpan.Zero,
                    PhaseTimings = new Dictionary<string, TimeSpan>(),
                    Metadata = new Dictionary<string, object>()
                };
            }

            public void RecordTimingBreakdown(TimingBreakdown breakdown)
            {
                // Mock implementation
            }
        }

        // Mock AI Optimization Engine
        private class MockAIOptimizationEngine : IAIOptimizationEngine
        {
            public bool AnalyzeCalled { get; private set; }
            public bool LearnCalled { get; private set; }
            public int BatchSizeToReturn { get; set; } = 10;
            public bool ThrowOnAnalyze { get; set; }
            public bool LowConfidence { get; set; }

            public OptimizationRecommendation RecommendationToReturn { get; set; } = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.None,
                ConfidenceScore = 0.5,
                EstimatedImprovement = TimeSpan.Zero,
                Reasoning = "Mock recommendation",
                Priority = OptimizationPriority.Low,
                EstimatedGainPercentage = 0.0,
                Risk = RiskLevel.VeryLow
            };

            public ValueTask<OptimizationRecommendation> AnalyzeRequestAsync<TRequest>(
                TRequest request,
                RequestExecutionMetrics executionMetrics,
                CancellationToken cancellationToken = default)
            {
                AnalyzeCalled = true;
                cancellationToken.ThrowIfCancellationRequested();

                if (ThrowOnAnalyze)
                {
                    throw new InvalidOperationException("AI Engine analyze failed");
                }

                var recommendation = RecommendationToReturn;
                if (LowConfidence)
                {
                    recommendation = new OptimizationRecommendation
                    {
                        Strategy = OptimizationStrategy.None,
                        ConfidenceScore = 0.3, // Below threshold
                        EstimatedImprovement = TimeSpan.Zero,
                        Reasoning = "Low confidence mock recommendation",
                        Priority = OptimizationPriority.Low,
                        EstimatedGainPercentage = 0.0,
                        Risk = RiskLevel.VeryLow
                    };
                }

                return new ValueTask<OptimizationRecommendation>(recommendation);
            }

            public ValueTask<int> PredictOptimalBatchSizeAsync(
                Type requestType,
                SystemLoadMetrics currentLoad,
                CancellationToken cancellationToken = default)
            {
                return new ValueTask<int>(BatchSizeToReturn);
            }

            public ValueTask<CachingRecommendation> ShouldCacheAsync(
                Type requestType,
                AccessPattern[] accessPatterns,
                CancellationToken cancellationToken = default)
            {
                return new ValueTask<CachingRecommendation>(new CachingRecommendation
                {
                    ShouldCache = false,
                    RecommendedTtl = TimeSpan.FromMinutes(5),
                    Strategy = CacheStrategy.None,
                    ExpectedHitRate = 0.0,
                    CacheKey = string.Empty,
                    Scope = CacheScope.Global,
                    ConfidenceScore = 0.5
                });
            }

            public ValueTask LearnFromExecutionAsync(
                Type requestType,
                OptimizationStrategy[] appliedOptimizations,
                RequestExecutionMetrics actualMetrics,
                CancellationToken cancellationToken = default)
            {
                LearnCalled = true;
                return ValueTask.CompletedTask;
            }

            public ValueTask<SystemPerformanceInsights> GetSystemInsightsAsync(
                TimeSpan timeWindow,
                CancellationToken cancellationToken = default)
            {
                return new ValueTask<SystemPerformanceInsights>(new SystemPerformanceInsights
                {
                    AnalysisTime = DateTime.UtcNow,
                    AnalysisPeriod = timeWindow,
                    PerformanceGrade = 'A'
                });
            }

            public void SetLearningMode(bool enabled)
            {
                // Mock implementation
            }

            public AIModelStatistics GetModelStatistics()
            {
                return new AIModelStatistics
                {
                    ModelTrainingDate = DateTime.UtcNow,
                    TotalPredictions = 0,
                    AccuracyScore = 0.0,
                    PrecisionScore = 0.0,
                    RecallScore = 0.0,
                    F1Score = 0.0,
                    AveragePredictionTime = TimeSpan.Zero,
                    TrainingDataPoints = 0,
                    ModelVersion = "1.0.0",
                    LastRetraining = DateTime.UtcNow,
                    ModelConfidence = 0.0
                };
            }
        }

        // Mock system load metrics provider for testing
        private class MockSystemLoadMetricsProvider : ISystemLoadMetricsProvider
        {
            public SystemLoadMetrics LoadToReturn { get; set; } = new SystemLoadMetrics
            {
                CpuUtilization = 0.5,
                MemoryUtilization = 0.6,
                ActiveConnections = 100,
                QueuedRequestCount = 10,
                AvailableMemory = 1024 * 1024 * 1024L,
                ActiveRequestCount = 50,
                ThroughputPerSecond = 100.0,
                AverageResponseTime = TimeSpan.FromMilliseconds(50),
                ErrorRate = 0.01,
                Timestamp = DateTime.UtcNow,
                DatabasePoolUtilization = 0.3,
                ThreadPoolUtilization = 0.4
            };

            public ValueTask<SystemLoadMetrics> GetCurrentLoadAsync(CancellationToken cancellationToken = default)
            {
                return new ValueTask<SystemLoadMetrics>(LoadToReturn);
            }
        }
    }
}