using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization
{
    public class OptimizationStrategyTests
    {
        private readonly ILogger _logger = NullLogger.Instance;

        [Fact]
        public void RequestAnalysisStrategy_AnalyzeRequestOperation_IsHandled()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024 // 1MB
                }
            };

            // Act
            var canHandle = strategy.CanHandle(context.Operation);

            // Assert
            Assert.True(canHandle);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_HighCpuUsage_ReturnsSIMDOptimizationRecommendation()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(150),
                    CpuUsage = 0.8, // High CPU
                    MemoryAllocated = 10 * 1024 * 1024 // 10MB
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("RequestAnalysis", result.StrategyName);
            Assert.IsType<OptimizationRecommendation>(result.Data);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.SIMDAcceleration, recommendation.Strategy);
            Assert.True(recommendation.ConfidenceScore > 0);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_HighMemory_ReturnsMemoryPoolingRecommendation()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 11 * 1024 * 1024 // More than 10MB threshold
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("RequestAnalysis", result.StrategyName);
            Assert.IsType<OptimizationRecommendation>(result.Data);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.MemoryPooling, recommendation.Strategy);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_HighConcurrentExecutions_ReturnsBatchingRecommendation()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024, // 1MB
                    ConcurrentExecutions = 15 // More than 10 threshold
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("RequestAnalysis", result.StrategyName);
            Assert.IsType<OptimizationRecommendation>(result.Data);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.BatchProcessing, recommendation.Strategy);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_HighDatabaseCalls_ReturnsResourcePoolingRecommendation()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024, // 1MB
                    ConcurrentExecutions = 5,
                    DatabaseCalls = 8 // More than 5 threshold
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("RequestAnalysis", result.StrategyName);
            Assert.IsType<OptimizationRecommendation>(result.Data);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.ResourcePooling, recommendation.Strategy);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_HighExternalApiCalls_ReturnsCircuitBreakerRecommendation()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024, // 1MB
                    ConcurrentExecutions = 5,
                    DatabaseCalls = 2,
                    ExternalApiCalls = 5 // More than 2 threshold
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("RequestAnalysis", result.StrategyName);
            Assert.IsType<OptimizationRecommendation>(result.Data);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.CircuitBreaker, recommendation.Strategy);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_Default_ReturnsCachingRecommendation()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024, // 1MB (less than 10MB threshold)
                    ConcurrentExecutions = 5,
                    DatabaseCalls = 2,
                    ExternalApiCalls = 1
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("RequestAnalysis", result.StrategyName);
            Assert.IsType<OptimizationRecommendation>(result.Data);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.Caching, recommendation.Strategy);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_HighSystemLoad_AdjustsRecommendation()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(150),
                    CpuUsage = 0.8, // High CPU - would normally suggest SIMD
                    MemoryAllocated = 10 * 1024 * 1024 // 10MB
                },
                SystemLoad = new SystemLoadMetrics // High system load
                {
                    CpuUtilization = 0.85, // High CPU utilization
                    MemoryUtilization = 0.7 // Not high enough to trigger adjustment on its own
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("RequestAnalysis", result.StrategyName);
            Assert.IsType<OptimizationRecommendation>(result.Data);
            var recommendation = (OptimizationRecommendation)result.Data!;
            // With high system load, the strategy should be adjusted from SIMD to Caching
            Assert.Equal(OptimizationStrategy.Caching, recommendation.Strategy);
            Assert.Contains("adjusted for high system load", recommendation.Reasoning);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_HighSystemLoad_AdjustsMemoryAndCpu()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(150),
                    CpuUsage = 0.8, // High CPU - would normally suggest SIMD
                    MemoryAllocated = 10 * 1024 * 1024 // 10MB
                },
                SystemLoad = new SystemLoadMetrics // High system load
                {
                    CpuUtilization = 0.7, // Not quite high enough to trigger adjustment alone
                    MemoryUtilization = 0.85 // High memory utilization
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("RequestAnalysis", result.StrategyName);
            Assert.IsType<OptimizationRecommendation>(result.Data);
            var recommendation = (OptimizationRecommendation)result.Data!;
            // With high system load, the strategy should be adjusted from SIMD to Caching
            Assert.Equal(OptimizationStrategy.Caching, recommendation.Strategy);
            Assert.Contains("adjusted for high system load", recommendation.Reasoning);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_InvalidContext_ReturnsErrorResult()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = null, // Invalid - null RequestType
                ExecutionMetrics = null // Invalid - null ExecutionMetrics
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("RequestAnalysis", result.StrategyName);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("Request type and execution metrics are required", result.ErrorMessage);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_ZeroExecutions_HasZeroConfidence()
        {
            // Test the edge case with zero executions which could cause division by zero in confidence calculation
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 0, // Zero executions
                    SuccessfulExecutions = 0,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success); // Should not throw with 0 executions
            Assert.Equal("RequestAnalysis", result.StrategyName);
            Assert.Equal(0.0, result.Confidence); // Confidence should be 0 due to no data
        }

        [Fact]
        public async Task RequestAnalysisStrategy_ConfidenceCalculation_LowSampleSize()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 10, // Low sample size
                    SuccessfulExecutions = 8, // 80% success rate
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            // Confidence should be based on low sample size (10/1000 = 0.01) and success rate (0.8)
            // So confidence should be (0.01 + 0.8) / 2 = 0.405
            Assert.InRange(result.Confidence, 0.35, 0.45);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_ConfidenceCalculation_HighSampleSize()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 1000, // High sample size
                    SuccessfulExecutions = 950, // 95% success rate
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            // Confidence should be based on high sample size (1000/1000 = 1.0) and success rate (0.95)
            // So confidence should be (1.0 + 0.95) / 2 = 0.975
            Assert.InRange(result.Confidence, 0.95, 1.0);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_ConfidenceCalculation_MaxSampleSize()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 2000, // Even higher than max (1000), should cap at 1.0 for sample confidence
                    SuccessfulExecutions = 1900, // 95% success rate
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            // Confidence should be based on capped sample size (1.0) and success rate (0.95)
            // So confidence should be (1.0 + 0.95) / 2 = 0.975
            Assert.InRange(result.Confidence, 0.95, 1.0);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_ExpectedImprovement_Caching()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                    CpuUsage = 0.2, // Low CPU, so should suggest caching
                    MemoryAllocated = 1024 * 1024, // 1MB
                    ConcurrentExecutions = 2,
                    DatabaseCalls = 2,
                    ExternalApiCalls = 1
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.Caching, recommendation.Strategy);
            Assert.Equal(0.3, recommendation.EstimatedGainPercentage); // 30% improvement for caching
        }

        [Fact]
        public async Task RequestAnalysisStrategy_ExpectedImprovement_BatchProcessing()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024, // 1MB
                    ConcurrentExecutions = 15, // High concurrent executions
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.BatchProcessing, recommendation.Strategy);
            Assert.Equal(0.4, recommendation.EstimatedGainPercentage); // 40% improvement for batch processing
        }

        [Fact]
        public async Task RequestAnalysisStrategy_ExpectedImprovement_SIMDAcceleration()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(150), // High execution time
                    CpuUsage = 0.9, // High CPU usage
                    MemoryAllocated = 10 * 1024 * 1024, // 10MB
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.SIMDAcceleration, recommendation.Strategy);
            Assert.Equal(0.5, recommendation.EstimatedGainPercentage); // 50% improvement for SIMD
        }

        [Fact]
        public async Task RequestAnalysisStrategy_ExpectedImprovement_MemoryPooling()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 15 * 1024 * 1024, // 15MB - high memory allocation
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.MemoryPooling, recommendation.Strategy);
            Assert.Equal(0.25, recommendation.EstimatedGainPercentage); // 25% improvement for memory pooling
        }

        [Fact]
        public async Task RequestAnalysisStrategy_ExpectedImprovement_ResourcePooling()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024, // 1MB
                    DatabaseCalls = 8 // High database calls
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.ResourcePooling, recommendation.Strategy);
            Assert.Equal(0.35, recommendation.EstimatedGainPercentage); // 35% improvement for resource pooling
        }

        [Fact]
        public async Task RequestAnalysisStrategy_ExpectedImprovement_CircuitBreaker()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024, // 1MB
                    ExternalApiCalls = 5 // High external API calls
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.CircuitBreaker, recommendation.Strategy);
            Assert.Equal(0.2, recommendation.EstimatedGainPercentage); // 20% improvement for circuit breaker
        }

        [Fact]
        public async Task RequestAnalysisStrategy_ResourceRequirements_Caching()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                    CpuUsage = 0.2, // Low CPU, so should suggest caching
                    MemoryAllocated = 1024 * 1024, // 1MB
                    ConcurrentExecutions = 2,
                    DatabaseCalls = 2,
                    ExternalApiCalls = 1
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.Caching, recommendation.Strategy);
            
            var resourceRequirements = (ResourceRequirements)recommendation.Parameters["resource_requirements"];
            Assert.Equal(50, resourceRequirements.MemoryMB);
            Assert.Equal(5, resourceRequirements.CpuPercent);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_ResourceRequirements_BatchProcessing()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024, // 1MB
                    ConcurrentExecutions = 15, // High concurrent executions
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.BatchProcessing, recommendation.Strategy);
            
            var resourceRequirements = (ResourceRequirements)recommendation.Parameters["resource_requirements"];
            Assert.Equal(20, resourceRequirements.MemoryMB);
            Assert.Equal(10, resourceRequirements.CpuPercent);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_ResourceRequirements_SIMDAcceleration()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(150), // High execution time
                    CpuUsage = 0.9, // High CPU usage
                    MemoryAllocated = 10 * 1024 * 1024, // 10MB
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.SIMDAcceleration, recommendation.Strategy);
            
            var resourceRequirements = (ResourceRequirements)recommendation.Parameters["resource_requirements"];
            Assert.Equal(10, resourceRequirements.MemoryMB);
            Assert.Equal(15, resourceRequirements.CpuPercent);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_ResourceRequirements_MemoryPooling()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 15 * 1024 * 1024, // 15MB - high memory allocation
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.MemoryPooling, recommendation.Strategy);
            
            var resourceRequirements = (ResourceRequirements)recommendation.Parameters["resource_requirements"];
            Assert.Equal(100, resourceRequirements.MemoryMB);
            Assert.Equal(5, resourceRequirements.CpuPercent);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_ResourceRequirements_ResourcePooling()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024, // 1MB
                    DatabaseCalls = 8 // High database calls
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.ResourcePooling, recommendation.Strategy);
            
            var resourceRequirements = (ResourceRequirements)recommendation.Parameters["resource_requirements"];
            Assert.Equal(30, resourceRequirements.MemoryMB);
            Assert.Equal(5, resourceRequirements.CpuPercent);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_ResourceRequirements_CircuitBreaker()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024, // 1MB
                    ExternalApiCalls = 5 // High external API calls
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.CircuitBreaker, recommendation.Strategy);
            
            var resourceRequirements = (ResourceRequirements)recommendation.Parameters["resource_requirements"];
            Assert.Equal(15, resourceRequirements.MemoryMB);
            Assert.Equal(2, resourceRequirements.CpuPercent);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_RiskLevel_Caching()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                    CpuUsage = 0.2, // Low CPU, so should suggest caching
                    MemoryAllocated = 1024 * 1024, // 1MB
                    ConcurrentExecutions = 2,
                    DatabaseCalls = 2,
                    ExternalApiCalls = 1
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.Caching, recommendation.Strategy);
            Assert.Equal(RiskLevel.Low, recommendation.Risk);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_RiskLevel_BatchProcessing()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024, // 1MB
                    ConcurrentExecutions = 15, // High concurrent executions
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.BatchProcessing, recommendation.Strategy);
            Assert.Equal(RiskLevel.Medium, recommendation.Risk);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_RiskLevel_SIMDAcceleration()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(150), // High execution time
                    CpuUsage = 0.9, // High CPU usage
                    MemoryAllocated = 10 * 1024 * 1024, // 10MB
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.SIMDAcceleration, recommendation.Strategy);
            Assert.Equal(RiskLevel.High, recommendation.Risk);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_RiskLevel_MemoryPooling()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 15 * 1024 * 1024, // 15MB - high memory allocation
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.MemoryPooling, recommendation.Strategy);
            Assert.Equal(RiskLevel.Medium, recommendation.Risk);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_RiskLevel_ResourcePooling()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024, // 1MB
                    DatabaseCalls = 8 // High database calls
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.ResourcePooling, recommendation.Strategy);
            Assert.Equal(RiskLevel.Low, recommendation.Risk);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_RiskLevel_CircuitBreaker()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024, // 1MB
                    ExternalApiCalls = 5 // High external API calls
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.CircuitBreaker, recommendation.Strategy);
            Assert.Equal(RiskLevel.Low, recommendation.Risk);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_Priority_Caching()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                    CpuUsage = 0.2, // Low CPU, so should suggest caching
                    MemoryAllocated = 1024 * 1024, // 1MB
                    ConcurrentExecutions = 2,
                    DatabaseCalls = 2,
                    ExternalApiCalls = 1
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.Caching, recommendation.Strategy);
            Assert.Equal(OptimizationPriority.Low, recommendation.Priority);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_Priority_BatchProcessing()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024, // 1MB
                    ConcurrentExecutions = 15, // High concurrent executions
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.BatchProcessing, recommendation.Strategy);
            Assert.Equal(OptimizationPriority.Medium, recommendation.Priority);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_Priority_SIMDAcceleration()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(150), // High execution time
                    CpuUsage = 0.9, // High CPU usage
                    MemoryAllocated = 10 * 1024 * 1024, // 10MB
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.SIMDAcceleration, recommendation.Strategy);
            Assert.Equal(OptimizationPriority.High, recommendation.Priority);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_Priority_MemoryPooling()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 15 * 1024 * 1024, // 15MB - high memory allocation
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.MemoryPooling, recommendation.Strategy);
            Assert.Equal(OptimizationPriority.Medium, recommendation.Priority);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_Priority_ResourcePooling()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024, // 1MB
                    DatabaseCalls = 8 // High database calls
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.ResourcePooling, recommendation.Strategy);
            Assert.Equal(OptimizationPriority.Low, recommendation.Priority);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_Priority_CircuitBreaker()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024, // 1MB
                    ExternalApiCalls = 5 // High external API calls
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.CircuitBreaker, recommendation.Strategy);
            Assert.Equal(OptimizationPriority.High, recommendation.Priority);
        }

        [Fact]
        public async Task RequestAnalysisStrategy_ExecutionMetricsWithAllThresholds_ReturnsCorrectStrategy()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(200), // High execution time
                    CpuUsage = 0.9, // High CPU - would trigger SIMD first
                    MemoryAllocated = 15 * 1024 * 1024, // 15MB - high memory
                    ConcurrentExecutions = 15, // High concurrent
                    DatabaseCalls = 8, // High DB calls
                    ExternalApiCalls = 5 // High external calls
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            var recommendation = (OptimizationRecommendation)result.Data!;
            // Since SIMD is first in the priority list, it should be returned
            Assert.Equal(OptimizationStrategy.SIMDAcceleration, recommendation.Strategy);
            // The other strategies should be in the parameters as alternatives
            var alternatives = (OptimizationStrategy[])recommendation.Parameters["alternative_strategies"];
            Assert.Contains(OptimizationStrategy.MemoryPooling, alternatives);
            Assert.Contains(OptimizationStrategy.BatchProcessing, alternatives);
            Assert.Contains(OptimizationStrategy.ResourcePooling, alternatives);
            Assert.Contains(OptimizationStrategy.CircuitBreaker, alternatives);
        }

        [Fact]
        public void RequestAnalysisStrategy_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RequestAnalysisStrategy(null!));
        }

        [Fact]
        public void BatchSizePredictionStrategy_PredictBatchSizeOperation_IsHandled()
        {
            // Arrange
            var options = new AIOptimizationOptions();
            var strategy = new BatchSizePredictionStrategy(_logger, options);
            var context = new OptimizationContext
            {
                Operation = "PredictBatchSize",
                RequestType = typeof(string),
                SystemLoad = new SystemLoadMetrics
                {
                    CpuUtilization = 0.5,
                    MemoryUtilization = 0.3,
                    ActiveConnections = 50,
                    QueuedRequestCount = 10
                }
            };

            // Act
            var canHandle = strategy.CanHandle(context.Operation);

            // Assert
            Assert.True(canHandle);
        }

        [Fact]
        public async Task BatchSizePredictionStrategy_HighCpuUtilization_ReturnsReducedBatchSize()
        {
            // Arrange
            var options = new AIOptimizationOptions { DefaultBatchSize = 100, MaxBatchSize = 1000 };
            var strategy = new BatchSizePredictionStrategy(_logger, options);
            var context = new OptimizationContext
            {
                Operation = "PredictBatchSize",
                RequestType = typeof(string),
                SystemLoad = new SystemLoadMetrics
                {
                    CpuUtilization = 0.8, // High CPU - should reduce batch size
                    MemoryUtilization = 0.2,
                    ActiveConnections = 100,
                    QueuedRequestCount = 5
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.IsType<int>(result.Data);
            var batchSize = (int)result.Data!;
            Assert.True(batchSize <= options.MaxBatchSize);
            Assert.True(batchSize > 0);
        }

        [Fact]
        public void CachingStrategy_OptimizeCachingOperation_IsHandled()
        {
            // Arrange
            var strategy = new CachingStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "OptimizeCaching",
                AccessPatterns = new[]
                {
                    new AccessPattern
                    {
                        RequestKey = "test",
                        AccessCount = 10,
                        AccessFrequency = 2.0,
                        DataVolatility = 0.1
                    }
                }
            };

            // Act
            var canHandle = strategy.CanHandle(context.Operation);

            // Assert
            Assert.True(canHandle);
        }

        [Fact]
        public async Task CachingStrategy_FrequentAccessPattern_ReturnsCachingRecommendation()
        {
            // Arrange
            var strategy = new CachingStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "OptimizeCaching",
                AccessPatterns = new[]
                {
                    new AccessPattern
                    {
                        RequestKey = "frequent",
                        AccessCount = 20,
                        AccessFrequency = 5.0,
                        DataVolatility = 0.1,
                        AverageExecutionTime = TimeSpan.FromMilliseconds(100)
                    }
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.IsType<OptimizationRecommendation>(result.Data);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.Caching, recommendation.Strategy);
        }

        [Fact]
        public void LearningStrategy_LearnFromResultsOperation_IsHandled()
        {
            // Arrange
            var strategy = new LearningStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "LearnFromResults",
                AppliedStrategies = new[]
                {
                    new AppliedOptimizationResult
                    {
                        Strategy = OptimizationStrategy.Caching,
                        Success = true,
                        ActualImprovement = TimeSpan.FromMilliseconds(50),
                        ExpectedImprovement = TimeSpan.FromMilliseconds(40)
                    }
                }
            };

            // Act
            var canHandle = strategy.CanHandle(context.Operation);

            // Assert
            Assert.True(canHandle);
        }

        [Fact]
        public async Task LearningStrategy_ShouldGenerateLearningInsights()
        {
            // Arrange
            var strategy = new LearningStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "LearnFromResults",
                AppliedStrategies = new[]
                {
                    new AppliedOptimizationResult
                    {
                        Strategy = OptimizationStrategy.Caching,
                        Success = true,
                        ActualImprovement = TimeSpan.FromMilliseconds(50),
                        ExpectedImprovement = TimeSpan.FromMilliseconds(40),
                        ConfidenceScore = 0.8
                    },
                    new AppliedOptimizationResult
                    {
                        Strategy = OptimizationStrategy.BatchProcessing,
                        Success = false,
                        ActualImprovement = TimeSpan.FromMilliseconds(10),
                        ExpectedImprovement = TimeSpan.FromMilliseconds(30),
                        ConfidenceScore = 0.6
                    }
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.IsType<OptimizationRecommendation>(result.Data);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Contains("preferred_strategies", recommendation.Parameters.Keys);
            Assert.Contains("avoid_strategies", recommendation.Parameters.Keys);
        }

        [Fact]
        public void SystemInsightsStrategy_ShouldHandleAnalyzeSystemInsightsOperation()
        {
            // Arrange
            var strategy = new SystemInsightsStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeSystemInsights",
                SystemLoad = new SystemLoadMetrics
                {
                    CpuUtilization = 0.7,
                    MemoryUtilization = 0.5,
                    ActiveConnections = 100,
                    QueuedRequestCount = 5
                }
            };

            // Act
            var canHandle = strategy.CanHandle(context.Operation);

            // Assert
            Assert.True(canHandle);
        }

        [Fact]
        public async Task SystemInsightsStrategy_ShouldReturnSystemOptimizationRecommendation()
        {
            // Arrange
            var strategy = new SystemInsightsStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeSystemInsights",
                SystemLoad = new SystemLoadMetrics
                {
                    CpuUtilization = 0.9, // High CPU
                    MemoryUtilization = 0.2,
                    ActiveConnections = 200,
                    QueuedRequestCount = 50
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.IsType<OptimizationRecommendation>(result.Data);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Contains("cpu_insights", recommendation.Parameters.Keys);
            Assert.Contains("memory_insights", recommendation.Parameters.Keys);
            Assert.Contains("connection_insights", recommendation.Parameters.Keys);
            Assert.Contains("queue_insights", recommendation.Parameters.Keys);
        }

        [Fact]
        public void AllStrategies_ShouldHaveUniqueNames()
        {
            // Arrange
            var strategies = new List<IOptimizationStrategy>
            {
                new RequestAnalysisStrategy(_logger),
                new BatchSizePredictionStrategy(_logger, new AIOptimizationOptions()),
                new CachingStrategy(_logger),
                new LearningStrategy(_logger),
                new SystemInsightsStrategy(_logger)
            };

            // Act
            var names = strategies.Select(s => s.Name).ToList();

            // Assert
            Assert.Equal(5, names.Distinct().Count());
            Assert.Equal(5, names.Count);
        }

        [Fact]
        public void AllStrategies_ShouldHaveValidPriorities()
        {
            // Arrange
            var strategies = new List<IOptimizationStrategy>
            {
                new RequestAnalysisStrategy(_logger),
                new BatchSizePredictionStrategy(_logger, new AIOptimizationOptions()),
                new CachingStrategy(_logger),
                new LearningStrategy(_logger),
                new SystemInsightsStrategy(_logger)
            };

            // Act & Assert
            foreach (var strategy in strategies)
            {
                Assert.True(strategy.Priority >= 0);
            }
        }
    }
}