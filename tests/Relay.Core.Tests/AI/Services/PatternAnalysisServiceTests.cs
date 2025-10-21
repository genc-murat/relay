using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI.Optimization.Services;
using Relay.Core.AI.Optimization.Data;
using Relay.Core.AI.Optimization.Models;
using System;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.AI;

namespace Relay.Core.Tests.AI.Services
{
    public class PatternAnalysisServiceTests
    {
        private readonly Mock<ILogger<PatternAnalysisService>> _loggerMock;
        private readonly PatternAnalysisService _service;

        public PatternAnalysisServiceTests()
        {
            _loggerMock = new Mock<ILogger<PatternAnalysisService>>();
            _service = new PatternAnalysisService(_loggerMock.Object);
        }

        [Fact]
        public async Task AnalyzePatternsAsync_WithHighErrorRate_ReturnsCircuitBreakerStrategy()
        {
            // Arrange
            var requestType = typeof(TestRequest);
            var analysisData = new RequestAnalysisData();
            analysisData.AddMetrics(new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                TotalExecutions = 1000,
                SuccessfulExecutions = 800,
                FailedExecutions = 200,
                ConcurrentExecutions = 5,
                MemoryAllocated = 1024,
                CpuUsage = 0.5,
                DatabaseCalls = 2,
                ExternalApiCalls = 1,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(1),
                MemoryUsage = 512
            });

            var executionMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                TotalExecutions = 100,
                SuccessfulExecutions = 80,
                FailedExecutions = 20,
                ConcurrentExecutions = 5,
                MemoryAllocated = 1024,
                CpuUsage = 0.5,
                DatabaseCalls = 2,
                ExternalApiCalls = 1,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(1),
                MemoryUsage = 512
            };

            // Act
            var result = await _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics);

            // Assert
            Assert.Equal(OptimizationStrategy.CircuitBreaker, result.Strategy);
            Assert.True(result.ConfidenceScore > 0);
            Assert.True(result.Risk == Relay.Core.AI.RiskLevel.Low);
        }

        [Fact]
        public async Task AnalyzePatternsAsync_WithLongExecutionTime_ReturnsCachingStrategy()
        {
            // Arrange
            var requestType = typeof(TestRequest);
            var analysisData = new RequestAnalysisData();
            analysisData.AddMetrics(new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(2000),
                TotalExecutions = 100,
                SuccessfulExecutions = 95,
                FailedExecutions = 5,
                ConcurrentExecutions = 1,
                MemoryAllocated = 1024,
                CpuUsage = 0.5,
                DatabaseCalls = 2,
                ExternalApiCalls = 1,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(1),
                MemoryUsage = 512
            });

            var executionMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(2000),
                TotalExecutions = 100,
                SuccessfulExecutions = 95,
                FailedExecutions = 5,
                ConcurrentExecutions = 1,
                MemoryAllocated = 1024,
                CpuUsage = 0.5,
                DatabaseCalls = 2,
                ExternalApiCalls = 1,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(1),
                MemoryUsage = 512
            };

            // Act
            var result = await _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics);

            // Assert
            Assert.Equal(OptimizationStrategy.EnableCaching, result.Strategy);
            Assert.Contains("long execution time", result.Reasoning.ToLower());
        }

        [Fact]
        public async Task AnalyzePatternsAsync_WithNullParameters_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _service.AnalyzePatternsAsync(null!, new RequestAnalysisData(), new RequestExecutionMetrics()));

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _service.AnalyzePatternsAsync(typeof(TestRequest), null!, new RequestExecutionMetrics()));

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _service.AnalyzePatternsAsync(typeof(TestRequest), new RequestAnalysisData(), null!));
        }

        private class TestRequest { }
    }
}