using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIValidationFrameworkResultsTests
    {
        private readonly ILogger<AIValidationFramework> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly AIValidationFramework _validationFramework;

        public AIValidationFrameworkResultsTests()
        {
            _logger = NullLogger<AIValidationFramework>.Instance;
            _options = new AIOptimizationOptions
            {
                MinConfidenceScore = 0.7,
                MaxAutomaticOptimizationRisk = RiskLevel.Medium,
                EnableAutomaticOptimization = true
            };
            _validationFramework = new AIValidationFramework(_logger, _options);
        }

        #region ValidateOptimizationResultsAsync Tests

        [Fact]
        public async Task ValidateOptimizationResultsAsync_WithImprovement_ShouldReturnSuccessful()
        {
            // Arrange
            var beforeMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(200),
                TotalExecutions = 100,
                SuccessfulExecutions = 90,
                MemoryAllocated = 1024
            };

            var afterMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100), // 50% improvement
                TotalExecutions = 100,
                SuccessfulExecutions = 95,
                MemoryAllocated = 512
            };

            var strategies = new[] { OptimizationStrategy.EnableCaching };

            // Act
            var result = await _validationFramework.ValidateOptimizationResultsAsync(
                strategies,
                beforeMetrics,
                afterMetrics,
                CancellationToken.None);

            // Assert
            Assert.True(result.WasSuccessful);
            Assert.True(result.OverallImprovement > 0);
            Assert.Single(result.StrategyResults);
            Assert.True(result.StrategyResults[0].WasSuccessful);
        }

        [Fact]
        public async Task ValidateOptimizationResultsAsync_WithNoImprovement_ShouldReturnUnsuccessful()
        {
            // Arrange
            var beforeMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                TotalExecutions = 100,
                SuccessfulExecutions = 90,
                MemoryAllocated = 512
            };

            var afterMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(150), // Worse
                TotalExecutions = 100,
                SuccessfulExecutions = 85,
                MemoryAllocated = 1024
            };

            var strategies = new[] { OptimizationStrategy.BatchProcessing };

            // Act
            var result = await _validationFramework.ValidateOptimizationResultsAsync(
                strategies,
                beforeMetrics,
                afterMetrics,
                CancellationToken.None);

            // Assert
            Assert.False(result.WasSuccessful);
            Assert.True(result.OverallImprovement < 0);
        }

        [Fact]
        public async Task ValidateOptimizationResultsAsync_WithMultipleStrategies_ShouldValidateAll()
        {
            // Arrange
            var beforeMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(300),
                TotalExecutions = 100,
                SuccessfulExecutions = 80,
                MemoryAllocated = 2048
            };

            var afterMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                TotalExecutions = 100,
                SuccessfulExecutions = 95,
                MemoryAllocated = 512
            };

            var strategies = new[]
            {
                OptimizationStrategy.EnableCaching,
                OptimizationStrategy.MemoryPooling,
                OptimizationStrategy.BatchProcessing
            };

            // Act
            var result = await _validationFramework.ValidateOptimizationResultsAsync(
                strategies,
                beforeMetrics,
                afterMetrics,
                CancellationToken.None);

            // Assert
            Assert.True(result.WasSuccessful);
            Assert.Equal(3, result.StrategyResults.Length);
            Assert.All(result.StrategyResults, sr => Assert.True(sr.WasSuccessful));
        }

        [Fact]
        public async Task ValidateOptimizationResultsAsync_ShouldCalculatePerformanceGain()
        {
            // Arrange
            var beforeMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(200),
                TotalExecutions = 100,
                SuccessfulExecutions = 90,
                MemoryAllocated = 1024
            };

            var afterMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                TotalExecutions = 100,
                SuccessfulExecutions = 90,
                MemoryAllocated = 1024
            };

            var strategies = new[] { OptimizationStrategy.EnableCaching };

            // Act
            var result = await _validationFramework.ValidateOptimizationResultsAsync(
                strategies,
                beforeMetrics,
                afterMetrics,
                CancellationToken.None);

            // Assert
            Assert.True(result.WasSuccessful);
            Assert.Equal(0.5, result.StrategyResults[0].PerformanceGain, 2); // 50% improvement
        }

        #endregion
    }
}