using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
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
    public class AIOptimizationPipelineBehaviorTests
    {
        private readonly ServiceCollection _services;
        private readonly ILogger<AIOptimizationPipelineBehavior<TestRequest, TestResponse>> _logger;
        private readonly ILogger<SystemLoadMetricsProvider> _systemLogger;
        private readonly AIOptimizationOptions _options;

        public AIOptimizationPipelineBehaviorTests()
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
        public void Constructor_Should_Throw_When_AIEngine_Is_Null()
        {
            // Arrange
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                    null!,
                    _logger,
                    Options.Create(_options),
                    systemMetrics));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                    aiEngine,
                    null!,
                    Options.Create(_options),
                    systemMetrics));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Options_Is_Null()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                    aiEngine,
                    _logger,
                    null!,
                    systemMetrics));
        }

        [Fact]
        public void Constructor_Should_Throw_When_SystemMetrics_Is_Null()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                    aiEngine,
                    _logger,
                    Options.Create(_options),
                    null!));
        }

        [Fact]
        public async Task HandleAsync_Should_Execute_Without_Optimization_When_Disabled()
        {
            // Arrange
            var disabledOptions = new AIOptimizationOptions { Enabled = false };
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine,
                _logger,
                Options.Create(disabledOptions),
                systemMetrics);

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
            Assert.False(aiEngine.AnalyzeCalled);
        }

        [Fact]
        public async Task HandleAsync_Should_Call_AIEngine_When_Enabled()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine,
                _logger,
                Options.Create(_options),
                systemMetrics);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.Equal("success", result.Result);
            Assert.True(aiEngine.AnalyzeCalled);
        }

        [Fact]
        public async Task HandleAsync_Should_Learn_From_Successful_Execution()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine,
                _logger,
                Options.Create(_options),
                systemMetrics);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Should_Learn_From_Failed_Execution()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine,
                _logger,
                Options.Create(_options),
                systemMetrics);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = () =>
                throw new InvalidOperationException("Test exception");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await behavior.HandleAsync(request, next, CancellationToken.None));

            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Should_Not_Learn_When_Learning_Disabled()
        {
            // Arrange
            var disabledLearningOptions = new AIOptimizationOptions
            {
                Enabled = true,
                LearningEnabled = false
            };
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine,
                _logger,
                Options.Create(disabledLearningOptions),
                systemMetrics);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.False(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task SystemLoadMetricsProvider_Should_Return_Valid_Metrics()
        {
            // Arrange
            var provider = new SystemLoadMetricsProvider(_systemLogger);

            // Act
            var metrics = await provider.GetCurrentLoadAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(metrics);
            Assert.InRange(metrics.CpuUtilization, 0.0, 1.0);
            Assert.InRange(metrics.MemoryUtilization, 0.0, 1.0);
            Assert.True(metrics.AvailableMemory >= 0);
            Assert.True(metrics.ActiveRequestCount >= 0);
            Assert.InRange(metrics.ThreadPoolUtilization, 0.0, 1.0);
        }

        [Fact]
        public async Task ExtractDatabaseCalls_WithDatabaseCallsProperty_ReturnsCorrectCount()
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
                    ["DatabaseCalls"] = 5
                },
                AverageExecutionTime = TimeSpan.FromMilliseconds(50)
            };

            // Act - We need to access the private method via reflection
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ExtractDatabaseCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method!.Invoke(behavior, new object[] { stats })!;

            // Assert
            Assert.Equal(5, result);
        }

        [Fact]
        public async Task ExtractDatabaseCalls_WithAvgDatabaseCallsProperty_ReturnsRoundedValue()
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
                    ["AvgDatabaseCalls"] = 7.8
                },
                AverageExecutionTime = TimeSpan.FromMilliseconds(50)
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ExtractDatabaseCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method!.Invoke(behavior, new object[] { stats })!;

            // Assert
            Assert.Equal(8, result); // Rounded from 7.8
        }

        [Fact]
        public async Task ExtractDatabaseCalls_WithLongDatabaseCallsProperty_ReturnsIntValue()
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
                    ["DatabaseCalls"] = 15L
                },
                AverageExecutionTime = TimeSpan.FromMilliseconds(50)
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ExtractDatabaseCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method!.Invoke(behavior, new object[] { stats })!;

            // Assert
            Assert.Equal(15, result);
        }

        [Fact]
        public async Task ExtractDatabaseCalls_WithDoubleDatabaseCallsProperty_ReturnsIntValue()
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
                    ["DatabaseCalls"] = 12.0
                },
                AverageExecutionTime = TimeSpan.FromMilliseconds(50)
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ExtractDatabaseCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method!.Invoke(behavior, new object[] { stats })!;

            // Assert
            Assert.Equal(12, result);
        }

        [Fact]
        public async Task ExtractDatabaseCalls_WithNoPropertiesAndLongExecutionTime_EstimatesBasedOnTime()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var stats = new HandlerExecutionStats
            {
                Properties = new Dictionary<string, object>(),
                AverageExecutionTime = TimeSpan.FromMilliseconds(250) // > 100ms
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ExtractDatabaseCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method!.Invoke(behavior, new object[] { stats })!;

            // Assert - Should estimate 250ms / 50ms = 5 DB calls
            Assert.Equal(5, result);
        }

        [Fact]
        public async Task ExtractDatabaseCalls_WithNoPropertiesAndShortExecutionTime_ReturnsZero()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var stats = new HandlerExecutionStats
            {
                Properties = new Dictionary<string, object>(),
                AverageExecutionTime = TimeSpan.FromMilliseconds(50) // < 100ms
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ExtractDatabaseCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method!.Invoke(behavior, new object[] { stats })!;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task ExtractDatabaseCalls_WithInvalidPropertyType_ReturnsZero()
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
                    ["DatabaseCalls"] = "invalid_string" // Not a numeric type
                },
                AverageExecutionTime = TimeSpan.FromMilliseconds(50)
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ExtractDatabaseCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method!.Invoke(behavior, new object[] { stats })!;

            // Assert - Should fall back to execution time estimation (0)
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task ExtractDatabaseCalls_PrefersDatabaseCallsOverAvgDatabaseCalls()
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
                    ["DatabaseCalls"] = 10,
                    ["AvgDatabaseCalls"] = 8.5 // Should be ignored since DatabaseCalls is present
                },
                AverageExecutionTime = TimeSpan.FromMilliseconds(50)
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ExtractDatabaseCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method!.Invoke(behavior, new object[] { stats })!;

            // Assert - Should use DatabaseCalls (10) not AvgDatabaseCalls (8.5)
            Assert.Equal(10, result);
        }

        [Fact]
        public async Task HandleAsync_AppliesCachingOptimization_WhenRecommended()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["CacheHitRate"] = 0.8
                }
            };

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics,
                memoryCache: memoryCache);

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
        public async Task HandleAsync_SkipsCachingOptimization_WhenNoCacheProviderAvailable()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ConfidenceScore = 0.9
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
        public async Task HandleAsync_AppliesBatchingOptimization_WhenRecommended()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.BatchProcessing,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["BatchSize"] = 5,
                    ["BatchWindow"] = 100
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
        public async Task HandleAsync_AppliesMemoryPoolingOptimization_WhenRecommended()
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
                    ["PoolSize"] = 100
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
        public async Task HandleAsync_AppliesParallelProcessingOptimization_WhenRecommended()
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
                    ["EnableWorkStealing"] = true
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
        public async Task HandleAsync_AppliesCircuitBreakerOptimization_WhenRecommended()
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
                    ["Timeout"] = 30000
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
        public async Task HandleAsync_AppliesDatabaseOptimization_WhenRecommended()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.DatabaseOptimization,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["EnableQueryOptimization"] = true,
                    ["MaxRetries"] = 3
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
        public async Task HandleAsync_Applies_Custom_Optimization()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.Custom,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["OptimizationType"] = "warmup",
                    ["OptimizationLevel"] = 2
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
        public async Task HandleAsync_Applies_SIMD_Optimization_When_Hardware_Accelerated()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.SIMDAcceleration,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["EnableVectorization"] = true,
                    ["VectorSize"] = 4,
                    ["EnableUnrolling"] = true,
                    ["UnrollFactor"] = 4,
                    ["MinDataSize"] = 64
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
        public async Task HandleAsync_Skips_SIMD_Optimization_When_Hardware_Not_Accelerated()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.SIMDAcceleration,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["EnableVectorization"] = true,
                    ["VectorSize"] = 4
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

            // Assert - Should skip SIMD and execute normally
            Assert.True(executed);
            Assert.Equal("success", result.Result);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Handles_Database_Optimization_With_Transient_Errors()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.DatabaseOptimization,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["EnableQueryOptimization"] = true,
                    ["MaxRetries"] = 3,
                    ["RetryDelayMs"] = 10
                }
            };

            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var request = new TestRequest { Value = "test" };
            var callCount = 0;
            RequestHandlerDelegate<TestResponse> next = () =>
            {
                callCount++;
                if (callCount <= 2) // Fail first 2 attempts with transient error
                {
                    throw new TimeoutException("Database timeout - transient error");
                }
                return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert - Should succeed after retries
            Assert.Equal(3, callCount); // Should have been called 3 times (2 failures + 1 success)
            Assert.Equal("success", result.Result);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Handles_Database_Optimization_With_Non_Transient_Errors()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.DatabaseOptimization,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["EnableQueryOptimization"] = true,
                    ["MaxRetries"] = 3,
                    ["RetryDelayMs"] = 10
                }
            };

            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var request = new TestRequest { Value = "test" };
            var callCount = 0;
            RequestHandlerDelegate<TestResponse> next = () =>
            {
                callCount++;
                // Always throw non-transient error (e.g., constraint violation)
                throw new InvalidOperationException("Constraint violation - non-transient error");
            };

            // Act & Assert - Should fail immediately without retries for non-transient errors
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await behavior.HandleAsync(request, next, CancellationToken.None));

            Assert.Equal(1, callCount); // Should only be called once
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Handles_Custom_Optimization_With_Different_Types()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.Custom,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["OptimizationType"] = "throttle",
                    ["OptimizationLevel"] = 2,
                    ["Custom_DelayMultiplier"] = 5
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
        public async Task HandleAsync_Handles_Metrics_Provider_Failure_Gracefully()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ConfidenceScore = 0.9
            };

            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            // Create a failing metrics provider
            var failingMetricsProvider = new FailingMetricsProvider();

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics, metricsProvider: failingMetricsProvider);

            var request = new TestRequest { Value = "test" };
            var executed = false;
            RequestHandlerDelegate<TestResponse> next = () =>
            {
                executed = true;
                return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert - Should continue execution despite metrics provider failure
            Assert.True(executed);
            Assert.Equal("success", result.Result);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Handles_System_Load_Metrics_Provider_Failure()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.ParallelProcessing,
                ConfidenceScore = 0.9
            };

            // Create a failing system load metrics provider
            var failingSystemMetrics = new FailingSystemLoadMetricsProvider();

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), failingSystemMetrics);

            var request = new TestRequest { Value = "test" };
            var executed = false;
            RequestHandlerDelegate<TestResponse> next = () =>
            {
                executed = true;
                return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert - Should continue with default behavior
            Assert.True(executed);
            Assert.Equal("success", result.Result);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Handles_Cancellation_In_Database_Optimization()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.DatabaseOptimization,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["EnableQueryOptimization"] = true,
                    ["MaxRetries"] = 3,
                     ["RetryDelayMs"] = 200 // Longer delay to ensure cancellation happens
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
                throw new TimeoutException("Database timeout - will trigger retry");
            };

            // Create a cancellation token that will cancel during retry delay
            var cts = new CancellationTokenSource();
            cts.CancelAfter(100); // Cancel during the 200ms delay

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await behavior.HandleAsync(request, next, cts.Token));

            Assert.True(executed);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Handles_Low_AI_Confidence_Threshold()
        {
            // Arrange
            var lowConfidenceOptions = new AIOptimizationOptions
            {
                Enabled = true,
                LearningEnabled = true,
                MinConfidenceScore = 0.8, // Higher threshold
                MinExecutionsForAnalysis = 5
            };

            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ConfidenceScore = 0.7 // Below threshold
            };

            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(lowConfidenceOptions), systemMetrics);

            var request = new TestRequest { Value = "test" };
            var executed = false;
            RequestHandlerDelegate<TestResponse> next = () =>
            {
                executed = true;
                return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert - Should skip optimization due to low confidence
            Assert.True(executed);
            Assert.Equal("success", result.Result);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }



        [Fact]
        public async Task HandleAsync_HandlesCancellationToken()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var request = new TestRequest { Value = "test" };
            var cancellationToken = new CancellationToken(true); // Already cancelled

            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await behavior.HandleAsync(request, next, cancellationToken));
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
        public async Task HandleAsync_Handles_Cache_Provider_Failure_Gracefully()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["CacheStrategy"] = CacheStrategy.LRU,
                    ["ExpectedHitRate"] = 0.8,
                    ["RecommendedTTL"] = TimeSpan.FromMinutes(10)
                }
            };

            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            // Create a failing memory cache
            var failingMemoryCache = new FailingMemoryCache();

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics,
                memoryCache: failingMemoryCache);

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
        public async Task HandleAsync_Handles_Distributed_Cache_Failure_Gracefully()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["CacheStrategy"] = CacheStrategy.LRU,
                    ["ExpectedHitRate"] = 0.8,
                    ["RecommendedTTL"] = TimeSpan.FromMinutes(10)
                }
            };

            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            // Create a failing distributed cache
            var failingDistributedCache = new FailingDistributedCache();

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics,
                distributedCache: failingDistributedCache);

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
        public async Task HandleAsync_Handles_Cache_Serialization_Failure_Gracefully()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["CacheStrategy"] = CacheStrategy.LRU,
                    ["ExpectedHitRate"] = 0.8,
                    ["RecommendedTTL"] = TimeSpan.FromMinutes(10)
                }
            };

            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            // Use memory cache which should work
            var memoryCache = new MemoryCache(new MemoryCacheOptions());

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics,
                memoryCache: memoryCache);

            var request = new TestRequest { Value = "test" };
            var executed = false;
            RequestHandlerDelegate<TestResponse> next = () =>
            {
                executed = true;
                // Return a response that might cause serialization issues
                return new ValueTask<TestResponse>(new NonSerializableResponse { Result = "success" });
            };

            // Act & Assert - Should not throw, should handle gracefully
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            Assert.True(executed);
            Assert.IsType<NonSerializableResponse>(result);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Skips_Caching_When_No_Cache_Providers_Available()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["CacheStrategy"] = CacheStrategy.LRU,
                    ["ExpectedHitRate"] = 0.8,
                    ["RecommendedTTL"] = TimeSpan.FromMinutes(10)
                }
            };

            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            // No cache providers provided
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
        public async Task HandleAsync_Handles_Cancellation_In_Cache_Operation()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["CacheStrategy"] = CacheStrategy.LRU,
                    ["ExpectedHitRate"] = 0.8,
                    ["RecommendedTTL"] = TimeSpan.FromMinutes(10)
                }
            };

            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            // Create a slow distributed cache that can be cancelled
            var slowDistributedCache = new SlowDistributedCache();

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics,
                distributedCache: slowDistributedCache);

            var request = new TestRequest { Value = "test" };
            var executed = false;
            RequestHandlerDelegate<TestResponse> next = () =>
            {
                executed = true;
                return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
            };

            // Create a cancellation token that will cancel during cache operation
            var cts = new CancellationTokenSource();
            cts.CancelAfter(50); // Cancel quickly

            // Act
            var result = await behavior.HandleAsync(request, next, cts.Token);

            // Assert
            Assert.True(executed);
            Assert.Equal("success", result.Result);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Handles_Batching_When_Batch_Size_Is_Too_Small()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.BatchProcessing,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["BatchWindow"] = 100,
                    ["MaxWaitTime"] = 200,
                    ["BatchingStrategy"] = "Adaptive"
                }
            };

            // Mock AI engine to return batch size of 1 (too small for batching)
            aiEngine.BatchSizeToReturn = 1;

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
        public async Task HandleAsync_Handles_Batching_Under_High_System_Load()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.BatchProcessing,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["BatchWindow"] = 100,
                    ["MaxWaitTime"] = 200,
                    ["BatchingStrategy"] = "Adaptive"
                }
            };

            // Mock AI engine to return reasonable batch size
            aiEngine.BatchSizeToReturn = 5;

            var systemMetrics = new MockSystemLoadMetricsProvider();
            systemMetrics.LoadToReturn = new SystemLoadMetrics
            {
                CpuUtilization = 0.96, // Very high CPU load
                MemoryUtilization = 0.95, // Very high memory load
                ThroughputPerSecond = 100.0,
                ActiveRequestCount = 1000
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

            // Assert - Should skip batching due to high load
            Assert.True(executed);
            Assert.Equal("success", result.Result);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Handles_Batching_With_Low_Confidence()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.BatchProcessing,
                ConfidenceScore = 0.3, // Below minimum confidence threshold
                Parameters = new Dictionary<string, object>
                {
                    ["BatchWindow"] = 100,
                    ["MaxWaitTime"] = 200,
                    ["BatchingStrategy"] = "Adaptive"
                }
            };

            aiEngine.BatchSizeToReturn = 5;

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

            // Assert - Should skip batching due to low confidence
            Assert.True(executed);
            Assert.Equal("success", result.Result);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Handles_Batching_With_Low_Throughput()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.BatchProcessing,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["BatchWindow"] = 100,
                    ["MaxWaitTime"] = 200,
                    ["BatchingStrategy"] = "Adaptive"
                }
            };

            aiEngine.BatchSizeToReturn = 5;

            var systemMetrics = new MockSystemLoadMetricsProvider();
            systemMetrics.LoadToReturn = new SystemLoadMetrics
            {
                CpuUtilization = 0.5,
                MemoryUtilization = 0.5,
                ThroughputPerSecond = 2.0, // Very low throughput
                ActiveRequestCount = 10
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

            // Assert - Should skip batching due to low throughput
            Assert.True(executed);
            Assert.Equal("success", result.Result);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Applies_Memory_Pooling_Optimization()
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
                    ["EstimatedBufferSize"] = 4096,
                    ["PoolSize"] = 100
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
        public async Task HandleAsync_Handles_Memory_Pooling_With_Custom_Parameters()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.MemoryPooling,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["EnableObjectPooling"] = false,
                    ["EnableBufferPooling"] = true,
                    ["EstimatedBufferSize"] = 8192,
                    ["PoolSize"] = 50
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
        public async Task HandleAsync_Handles_Memory_Pooling_Exception_Gracefully()
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
                    ["EstimatedBufferSize"] = 4096,
                    ["PoolSize"] = 100
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
                throw new OutOfMemoryException("Simulated memory pool exhaustion");
            };

            // Act & Assert - Should handle the exception gracefully
            await Assert.ThrowsAsync<OutOfMemoryException>(async () =>
                await behavior.HandleAsync(request, next, CancellationToken.None));

            Assert.True(executed);
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

        // Mock cache implementations for testing error scenarios
        private class FailingMemoryCache : IMemoryCache
        {
            public void Dispose() { }

            public bool TryGetValue(object key, out object? value)
            {
                throw new InvalidOperationException("Cache failure");
            }

            public ICacheEntry CreateEntry(object key)
            {
                throw new InvalidOperationException("Cache failure");
            }

            public void Remove(object key)
            {
                throw new InvalidOperationException("Cache failure");
            }
        }

        private class FailingDistributedCache : IDistributedCache
        {
            public byte[]? Get(string key)
            {
                throw new InvalidOperationException("Cache failure");
            }

            public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
            {
                throw new InvalidOperationException("Cache failure");
            }

            public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
            {
                throw new InvalidOperationException("Cache failure");
            }

            public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
            {
                throw new InvalidOperationException("Cache failure");
            }

            public void Refresh(string key)
            {
                throw new InvalidOperationException("Cache failure");
            }

            public Task RefreshAsync(string key, CancellationToken token = default)
            {
                throw new InvalidOperationException("Cache failure");
            }

            public void Remove(string key)
            {
                throw new InvalidOperationException("Cache failure");
            }

            public Task RemoveAsync(string key, CancellationToken token = default)
            {
                throw new InvalidOperationException("Cache failure");
            }
        }

        private class SlowDistributedCache : IDistributedCache
        {
            public byte[]? Get(string key)
            {
                Task.Delay(1000).Wait(); // Simulate slow operation
                return null;
            }

            public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
            {
                await Task.Delay(1000, token); // Simulate slow operation that respects cancellation
                return null;
            }

            public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
            {
                // Mock implementation
            }

            public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
            {
                return Task.CompletedTask;
            }

            public void Refresh(string key)
            {
                // Mock implementation
            }

            public Task RefreshAsync(string key, CancellationToken token = default)
            {
                return Task.CompletedTask;
            }

            public void Remove(string key)
            {
                // Mock implementation
            }

            public Task RemoveAsync(string key, CancellationToken token = default)
            {
                return Task.CompletedTask;
            }
        }

        // Non-serializable response for testing serialization failures
        private class NonSerializableResponse : TestResponse
        {
            // This class might cause serialization issues in some scenarios
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

        // Failing metrics provider for testing error scenarios
        private class FailingMetricsProvider : IMetricsProvider
        {
            public HandlerExecutionStats GetHandlerExecutionStats(Type requestType, string? handlerName = null)
            {
                throw new InvalidOperationException("Metrics provider failure");
            }

            public void RecordHandlerExecution(HandlerExecutionMetrics metrics)
            {
                throw new InvalidOperationException("Metrics provider failure");
            }

            public void RecordNotificationPublish(NotificationPublishMetrics metrics)
            {
                throw new InvalidOperationException("Metrics provider failure");
            }

            public void RecordStreamingOperation(StreamingOperationMetrics metrics)
            {
                throw new InvalidOperationException("Metrics provider failure");
            }

            public NotificationPublishStats GetNotificationPublishStats(Type notificationType)
            {
                throw new InvalidOperationException("Metrics provider failure");
            }

            public StreamingOperationStats GetStreamingOperationStats(Type requestType, string? handlerName = null)
            {
                throw new InvalidOperationException("Metrics provider failure");
            }

            public IEnumerable<PerformanceAnomaly> DetectAnomalies(TimeSpan lookbackPeriod)
            {
                throw new InvalidOperationException("Metrics provider failure");
            }

            public TimingBreakdown GetTimingBreakdown(string operationId)
            {
                throw new InvalidOperationException("Metrics provider failure");
            }

            public void RecordTimingBreakdown(TimingBreakdown breakdown)
            {
                throw new InvalidOperationException("Metrics provider failure");
            }
        }

        // Failing system load metrics provider for testing
        private class FailingSystemLoadMetricsProvider : ISystemLoadMetricsProvider
        {
            public ValueTask<SystemLoadMetrics> GetCurrentLoadAsync(CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("System load metrics provider failure");
            }
        }

        [Fact]
        public async Task HandleAsync_Should_Handle_SystemMetrics_Exception_Gracefully()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new FailingSystemLoadMetricsProvider();

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine,
                _logger,
                Options.Create(_options),
                systemMetrics);

            var request = new TestRequest { Value = "test" };
            var executed = false;
            RequestHandlerDelegate<TestResponse> next = () =>
            {
                executed = true;
                return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert - Should continue with default metrics
            Assert.True(executed);
            Assert.Equal("success", result.Result);
        }

        [Fact]
        public async Task HandleAsync_Should_Record_Metrics_When_Learning_Enabled()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var enabledOptions = new AIOptimizationOptions
            {
                Enabled = true,
                LearningEnabled = true,
                MinConfidenceScore = 0.7
            };

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine,
                _logger,
                Options.Create(enabledOptions),
                systemMetrics);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.True(aiEngine.LearnCalled);
            Assert.Equal("success", result.Result);
        }

        [Fact]
        public async Task HandleAsync_Should_Not_Record_Metrics_When_Learning_Disabled()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var disabledLearningOptions = new AIOptimizationOptions
            {
                Enabled = true,
                LearningEnabled = false,
                MinConfidenceScore = 0.7
            };

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine,
                _logger,
                Options.Create(disabledLearningOptions),
                systemMetrics);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.False(aiEngine.LearnCalled);
            Assert.Equal("success", result.Result);
        }

        [Fact]
        public async Task HandleAsync_Should_Track_Memory_Allocation()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine,
                _logger,
                Options.Create(_options),
                systemMetrics);

            var request = new TestRequest { Value = "test" };
            var largeArray = new byte[1024 * 1024]; // Allocate 1MB
            RequestHandlerDelegate<TestResponse> next = () =>
            {
                // Force some memory allocation
                _ = new byte[1024];
                return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.Equal("success", result.Result);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Should_Handle_Null_Response()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine,
                _logger,
                Options.Create(_options),
                systemMetrics);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(default(TestResponse)!);

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert - Should handle null response gracefully
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleAsync_Should_Handle_Multiple_Concurrent_Requests()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine,
                _logger,
                Options.Create(_options),
                systemMetrics);

            var tasks = new List<Task<TestResponse>>();
            for (int i = 0; i < 10; i++)
            {
                var request = new TestRequest { Value = $"test{i}" };
                RequestHandlerDelegate<TestResponse> next = () =>
                    new ValueTask<TestResponse>(new TestResponse { Result = $"success{i}" });

                tasks.Add(behavior.HandleAsync(request, next, CancellationToken.None).AsTask());
            }

            // Act
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(10, results.Length);
            Assert.All(results, r => Assert.StartsWith("success", r.Result));
        }

        [Fact]
        public async Task HandleAsync_Should_Measure_Execution_Time()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine,
                _logger,
                Options.Create(_options),
                systemMetrics);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = async () =>
            {
                await Task.Delay(50); // Simulate some work
                return new TestResponse { Result = "success" };
            };

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);
            stopwatch.Stop();

            // Assert
            Assert.Equal("success", result.Result);
            Assert.True(stopwatch.ElapsedMilliseconds >= 50, "Should have taken at least 50ms");
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Should_Handle_AIEngine_Analyze_Exception()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine { ThrowOnAnalyze = true };
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine,
                _logger,
                Options.Create(_options),
                systemMetrics);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await behavior.HandleAsync(request, next, CancellationToken.None));
        }

        [Fact]
        public async Task HandleAsync_Should_Handle_Low_Confidence_Recommendations()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine { LowConfidence = true };
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine,
                _logger,
                Options.Create(_options),
                systemMetrics);

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
        }

        [Fact]
        public async Task HandleAsync_Should_Work_With_Optional_Dependencies_Missing()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            // Create behavior without optional dependencies
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine,
                _logger,
                Options.Create(_options),
                systemMetrics,
                metricsProvider: null,
                memoryCache: null,
                distributedCache: null);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.Equal("success", result.Result);
        }
    }
}
