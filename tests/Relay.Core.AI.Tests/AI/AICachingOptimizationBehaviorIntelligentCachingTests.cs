using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Pipeline.Behaviors;
using Relay.Core.Contracts.Pipeline;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AICachingOptimizationBehaviorIntelligentCachingTests
    {
        private readonly ILogger<AICachingOptimizationBehavior<TestRequest, TestResponse>> _logger;
        private readonly Mock<IAIPredictionCache> _cacheMock;

        public AICachingOptimizationBehaviorIntelligentCachingTests()
        {
            _logger = NullLogger<AICachingOptimizationBehavior<TestRequest, TestResponse>>.Instance;
            _cacheMock = new Mock<IAIPredictionCache>();
        }

        [Fact]
        public async Task HandleAsync_Should_Not_Cache_When_IntelligentCaching_Disabled()
        {
            // Arrange
            _cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OptimizationRecommendation?)null);

            var logger = NullLogger<AICachingOptimizationBehavior<TestIntelligentCachingDisabledRequest, TestResponse>>.Instance;
            var behavior = new AICachingOptimizationBehavior<TestIntelligentCachingDisabledRequest, TestResponse>(logger, _cacheMock.Object);

            var request = new TestIntelligentCachingDisabledRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = async () =>
            {
                await Task.Delay(20);
                return new TestResponse { Result = "success" };
            };

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert - Should not attempt to cache because EnableAIAnalysis is false
            _cacheMock.Verify(c => c.SetCachedPredictionAsync(
                It.IsAny<string>(),
                It.IsAny<OptimizationRecommendation>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_Should_Not_Cache_Below_MinAccessFrequency()
        {
            // Arrange
            _cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OptimizationRecommendation?)null);

            var logger = NullLogger<AICachingOptimizationBehavior<TestHighMinAccessFrequencyRequest, TestResponse>>.Instance;
            var behavior = new AICachingOptimizationBehavior<TestHighMinAccessFrequencyRequest, TestResponse>(logger, _cacheMock.Object);

            var request = new TestHighMinAccessFrequencyRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = async () =>
            {
                await Task.Delay(20);
                return new TestResponse { Result = "success" };
            };

            // Act - Call 5 times (below MinAccessFrequency of 10)
            for (int i = 0; i < 5; i++)
            {
                await behavior.HandleAsync(request, next, CancellationToken.None);
            }

            // Assert - Should not cache because access frequency is below threshold
            _cacheMock.Verify(c => c.SetCachedPredictionAsync(
                It.IsAny<string>(),
                It.IsAny<OptimizationRecommendation>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_Should_Not_Cache_Below_MinPredictedHitRate()
        {
            // Arrange
            _cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OptimizationRecommendation?)null);

            var logger = NullLogger<AICachingOptimizationBehavior<TestHighMinHitRateRequest, TestResponse>>.Instance;
            var behavior = new AICachingOptimizationBehavior<TestHighMinHitRateRequest, TestResponse>(logger, _cacheMock.Object);

            var request = new TestHighMinHitRateRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = async () =>
            {
                await Task.Delay(20);
                return new TestResponse { Result = "success" };
            };

            // Act - Call 15 times to build attempt count, all misses so hit rate = 0 < 0.8
            for (int i = 0; i < 15; i++)
            {
                await behavior.HandleAsync(request, next, CancellationToken.None);
            }

            // Assert - Should not cache because hit rate is below threshold
            _cacheMock.Verify(c => c.SetCachedPredictionAsync(
                It.IsAny<string>(),
                It.IsAny<OptimizationRecommendation>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}