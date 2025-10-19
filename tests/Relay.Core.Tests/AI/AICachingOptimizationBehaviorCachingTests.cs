using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.AI;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AICachingOptimizationBehaviorCachingTests : IDisposable
    {
        private readonly ILogger<AICachingOptimizationBehavior<TestRequest, TestResponse>> _logger;
        private readonly Mock<IAIPredictionCache> _cacheMock;
        private readonly List<AICachingOptimizationBehavior<TestRequest, TestResponse>> _behaviorsToDispose;

        public AICachingOptimizationBehaviorCachingTests()
        {
            _logger = NullLogger<AICachingOptimizationBehavior<TestRequest, TestResponse>>.Instance;
            _cacheMock = new Mock<IAIPredictionCache>();
            _behaviorsToDispose = new List<AICachingOptimizationBehavior<TestRequest, TestResponse>>();
        }

        public void Dispose()
        {
            _behaviorsToDispose.Clear();
        }

        [Fact]
        public async Task HandleAsync_Should_Apply_Dynamic_TTL()
        {
            // Arrange
            TimeSpan? capturedTtl = null;

            var cacheMock = new Mock<IAIPredictionCache>();
            cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OptimizationRecommendation?)null);

            cacheMock.Setup(c => c.SetCachedPredictionAsync(
                    It.IsAny<string>(),
                    It.IsAny<OptimizationRecommendation>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, OptimizationRecommendation, TimeSpan, CancellationToken>((_, __, ttl, ___) => capturedTtl = ttl)
                .Returns(ValueTask.CompletedTask);

            var options = new AICachingOptimizationOptions
            {
                DefaultCacheTtl = TimeSpan.FromMinutes(10),
                MinExecutionTimeForCaching = 5.0 // Set low threshold so it caches
            };

            // Use TestRequest instead of TestIntelligentCachingRequest to avoid MinAccessFrequency check
            var logger = NullLogger<AICachingOptimizationBehavior<TestRequest, TestResponse>>.Instance;
            var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(
                logger,
                cacheMock.Object,
                options);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = async () =>
            {
                await Task.Delay(15); // Ensure execution time is above threshold
                return new TestResponse { Result = "success" };
            };

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.NotNull(capturedTtl);
            Assert.True(capturedTtl.Value > TimeSpan.Zero);
            Assert.Equal(options.DefaultCacheTtl, capturedTtl.Value);
            cacheMock.Verify(c => c.SetCachedPredictionAsync(
                It.IsAny<string>(),
                It.IsAny<OptimizationRecommendation>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Track_Cache_Hits_And_Misses()
        {
            // Arrange
            var callCount = 0;
            OptimizationRecommendation? recommendation = null;

            _cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    // Return cached recommendation on second call
                    if (callCount == 1)
                    {
                        recommendation = new OptimizationRecommendation
                        {
                            Strategy = OptimizationStrategy.EnableCaching,
                            ConfidenceScore = 0.8,
                            EstimatedImprovement = TimeSpan.FromMilliseconds(50)
                        };
                        return null;
                    }
                    return recommendation;
                });

            var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(_logger, _cacheMock.Object);
            _behaviorsToDispose.Add(behavior);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = async () =>
            {
                await Task.Delay(15);
                return new TestResponse { Result = "success" };
            };

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None); // Miss
            await behavior.HandleAsync(request, next, CancellationToken.None); // Hit

            // Assert
            Assert.Equal(2, callCount);
            _cacheMock.Verify(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task HandleAsync_Should_Handle_Large_Response_Size()
        {
            // Arrange
            var cacheMock = new Mock<IAIPredictionCache>();
            cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OptimizationRecommendation?)null);

            var options = new AICachingOptimizationOptions
            {
                MaxCacheSizeBytes = 100,
                MinExecutionTimeForCaching = 5.0
            };

            var logger = NullLogger<AICachingOptimizationBehavior<TestSizedRequest, TestSizedResponse>>.Instance;
            var behavior = new AICachingOptimizationBehavior<TestSizedRequest, TestSizedResponse>(logger, cacheMock.Object, options);

            var request = new TestSizedRequest { Value = "test" };
            RequestHandlerDelegate<TestSizedResponse> next = async () =>
            {
                await Task.Delay(10);
                return new TestSizedResponse { Result = "success", Size = 1000 }; // Too large
            };

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert - Should not cache due to size
            cacheMock.Verify(c => c.SetCachedPredictionAsync(
                It.IsAny<string>(),
                It.IsAny<OptimizationRecommendation>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_Should_Create_Recommendation_With_Correct_Properties()
        {
            // Arrange
            OptimizationRecommendation? capturedRecommendation = null;

            _cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OptimizationRecommendation?)null);

            _cacheMock.Setup(c => c.SetCachedPredictionAsync(
                    It.IsAny<string>(),
                    It.IsAny<OptimizationRecommendation>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, OptimizationRecommendation, TimeSpan, CancellationToken>((_, rec, __, ___) =>
                    capturedRecommendation = rec)
                .Returns(ValueTask.CompletedTask);

            var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(_logger, _cacheMock.Object);
            _behaviorsToDispose.Add(behavior);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = async () =>
            {
                await Task.Delay(20);
                return new TestResponse { Result = "success" };
            };

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.NotNull(capturedRecommendation);
            Assert.Equal(OptimizationStrategy.EnableCaching, capturedRecommendation.Strategy);
            Assert.InRange(capturedRecommendation.ConfidenceScore, 0.0, 1.0);
            Assert.True(capturedRecommendation.EstimatedImprovement > TimeSpan.Zero);
            Assert.NotNull(capturedRecommendation.Reasoning);
            Assert.Equal(RiskLevel.Low, capturedRecommendation.Risk);
            Assert.Contains("HitRate", capturedRecommendation.Parameters.Keys);
        }

        [Fact]
        public async Task HandleAsync_Should_Apply_Dynamic_TTL_When_Enabled()
        {
            // Arrange
            TimeSpan? capturedTtl = null;

            var cacheMock = new Mock<IAIPredictionCache>();
            cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OptimizationRecommendation?)null);

            cacheMock.Setup(c => c.SetCachedPredictionAsync(
                    It.IsAny<string>(),
                    It.IsAny<OptimizationRecommendation>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, OptimizationRecommendation, TimeSpan, CancellationToken>((_, __, ttl, ___) => capturedTtl = ttl)
                .Returns(ValueTask.CompletedTask);

            var options = new AICachingOptimizationOptions
            {
                DefaultCacheTtl = TimeSpan.FromMinutes(10),
                MinExecutionTimeForCaching = 5.0
            };

            var logger = NullLogger<AICachingOptimizationBehavior<TestDynamicTtlRequest, TestResponse>>.Instance;
            var behavior = new AICachingOptimizationBehavior<TestDynamicTtlRequest, TestResponse>(
                logger,
                cacheMock.Object,
                options);

            var request = new TestDynamicTtlRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = async () =>
            {
                await Task.Delay(15);
                return new TestResponse { Result = "success" };
            };

            // Act - First call to establish baseline
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert - Should use dynamic TTL calculation (which should be > default since hit rate starts at 0)
            Assert.NotNull(capturedTtl);
            Assert.True(capturedTtl.Value > TimeSpan.Zero);
            // With hit rate = 0, adjusted TTL should equal base TTL (1.0 + 0.0 = 1.0)
            Assert.Equal(options.DefaultCacheTtl, capturedTtl.Value);
        }

        [Fact]
        public async Task HandleAsync_Should_Not_Cache_Large_Responses()
        {
            // Arrange
            _cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OptimizationRecommendation?)null);

            var options = new AICachingOptimizationOptions
            {
                MaxCacheSizeBytes = 1024 * 1024, // 1MB limit
                MinExecutionTimeForCaching = 5.0
            };

            var logger = NullLogger<AICachingOptimizationBehavior<TestLargeRequest, TestLargeResponse>>.Instance;
            var behavior = new AICachingOptimizationBehavior<TestLargeRequest, TestLargeResponse>(logger, _cacheMock.Object, options);

            var request = new TestLargeRequest { Value = "test" };
            RequestHandlerDelegate<TestLargeResponse> next = async () =>
            {
                await Task.Delay(10);
                return new TestLargeResponse { Result = "success" }; // 2MB size
            };

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert - Should not cache because response size exceeds limit
            _cacheMock.Verify(c => c.SetCachedPredictionAsync(
                It.IsAny<string>(),
                It.IsAny<OptimizationRecommendation>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}