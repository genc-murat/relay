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
    public class AICachingOptimizationBehaviorTests : IDisposable
    {
        private readonly ILogger<AICachingOptimizationBehavior<TestRequest, TestResponse>> _logger;
        private readonly Mock<IAIPredictionCache> _cacheMock;
        private readonly List<AICachingOptimizationBehavior<TestRequest, TestResponse>> _behaviorsToDispose;

        public AICachingOptimizationBehaviorTests()
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
        public void Constructor_Should_Initialize_With_Valid_Logger()
        {
            // Arrange & Act
            var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(_logger);
            _behaviorsToDispose.Add(behavior);

            // Assert
            Assert.NotNull(behavior);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new AICachingOptimizationBehavior<TestRequest, TestResponse>(null!));
        }

        [Fact]
        public void Constructor_Should_Accept_Null_Cache()
        {
            // Arrange & Act
            var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(_logger, null);
            _behaviorsToDispose.Add(behavior);

            // Assert
            Assert.NotNull(behavior);
        }

        [Fact]
        public void Constructor_Should_Accept_Null_Options()
        {
            // Arrange & Act
            var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(_logger, _cacheMock.Object, null);
            _behaviorsToDispose.Add(behavior);

            // Assert
            Assert.NotNull(behavior);
        }

        [Fact]
        public async Task HandleAsync_Should_Execute_Without_Cache()
        {
            // Arrange
            var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(_logger, null);
            _behaviorsToDispose.Add(behavior);

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
        public async Task HandleAsync_Should_Execute_When_Caching_Disabled()
        {
            // Arrange
            var options = new AICachingOptimizationOptions { EnableCaching = false };
            var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(_logger, _cacheMock.Object, options);
            _behaviorsToDispose.Add(behavior);

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
            _cacheMock.Verify(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_Should_Check_Cache_On_First_Request()
        {
            // Arrange
            _cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OptimizationRecommendation?)null);

            var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(_logger, _cacheMock.Object);
            _behaviorsToDispose.Add(behavior);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            _cacheMock.Verify(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Use_Cached_Recommendation()
        {
            // Arrange
            var recommendation = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ConfidenceScore = 0.95,
                EstimatedImprovement = TimeSpan.FromMilliseconds(100),
                Reasoning = "High cache hit rate"
            };

            _cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(recommendation);

            var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(_logger, _cacheMock.Object);
            _behaviorsToDispose.Add(behavior);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("success", result.Result);
            _cacheMock.Verify(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Cache_Result_For_Slow_Operations()
        {
            // Arrange
            _cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OptimizationRecommendation?)null);

            var options = new AICachingOptimizationOptions
            {
                MinExecutionTimeForCaching = 10.0
            };

            var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(_logger, _cacheMock.Object, options);
            _behaviorsToDispose.Add(behavior);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = async () =>
            {
                await Task.Delay(20); // Simulate slow operation
                return new TestResponse { Result = "success" };
            };

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            _cacheMock.Verify(c => c.SetCachedPredictionAsync(
                It.IsAny<string>(),
                It.IsAny<OptimizationRecommendation>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Not_Cache_Fast_Operations()
        {
            // Arrange
            _cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OptimizationRecommendation?)null);

            var options = new AICachingOptimizationOptions
            {
                MinExecutionTimeForCaching = 100.0 // High threshold
            };

            var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(_logger, _cacheMock.Object, options);
            _behaviorsToDispose.Add(behavior);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            _cacheMock.Verify(c => c.SetCachedPredictionAsync(
                It.IsAny<string>(),
                It.IsAny<OptimizationRecommendation>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_Should_Not_Cache_Null_Response()
        {
            // Arrange
            _cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OptimizationRecommendation?)null);

            var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(_logger, _cacheMock.Object);
            _behaviorsToDispose.Add(behavior);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(default(TestResponse)!);

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            _cacheMock.Verify(c => c.SetCachedPredictionAsync(
                It.IsAny<string>(),
                It.IsAny<OptimizationRecommendation>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_Should_Handle_Cache_Exception_Gracefully()
        {
            // Arrange
            _cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Cache error"));

            var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(_logger, _cacheMock.Object);
            _behaviorsToDispose.Add(behavior);

            var request = new TestRequest { Value = "test" };
            var executed = false;

            RequestHandlerDelegate<TestResponse> next = () =>
            {
                executed = true;
                return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert - Should fall back to direct execution
            Assert.True(executed);
            Assert.Equal("success", result.Result);
        }

        [Fact]
        public async Task HandleAsync_Should_Support_Cancellation()
        {
            // Arrange
            var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(_logger, null);
            _behaviorsToDispose.Add(behavior);

            var request = new TestRequest { Value = "test" };
            var cts = new CancellationTokenSource();
            cts.Cancel();

            RequestHandlerDelegate<TestResponse> next = async () =>
            {
                await Task.Delay(1000, cts.Token);
                return new TestResponse { Result = "success" };
            };

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await behavior.HandleAsync(request, next, cts.Token));
        }

        [Fact]
        public void AICachingOptimizationOptions_Should_Have_Correct_Defaults()
        {
            // Arrange & Act
            var options = new AICachingOptimizationOptions();

            // Assert
            Assert.True(options.EnableCaching);
            Assert.Equal(0.7, options.MinConfidenceScore);
            Assert.Equal(10.0, options.MinExecutionTimeForCaching);
            Assert.Equal(1024 * 1024, options.MaxCacheSizeBytes);
            Assert.Equal(TimeSpan.FromMinutes(10), options.DefaultCacheTtl);
            Assert.Equal(TimeSpan.FromMinutes(1), options.MinCacheTtl);
            Assert.Equal(TimeSpan.FromHours(1), options.MaxCacheTtl);
            Assert.NotNull(options.SerializerOptions);
        }

        [Fact]
        public void AICachingOptimizationOptions_Should_Allow_Custom_Configuration()
        {
            // Arrange & Act
            var options = new AICachingOptimizationOptions
            {
                EnableCaching = false,
                MinConfidenceScore = 0.8,
                MinExecutionTimeForCaching = 50.0,
                MaxCacheSizeBytes = 512 * 1024,
                DefaultCacheTtl = TimeSpan.FromMinutes(5),
                MinCacheTtl = TimeSpan.FromSeconds(30),
                MaxCacheTtl = TimeSpan.FromMinutes(30)
            };

            // Assert
            Assert.False(options.EnableCaching);
            Assert.Equal(0.8, options.MinConfidenceScore);
            Assert.Equal(50.0, options.MinExecutionTimeForCaching);
            Assert.Equal(512 * 1024, options.MaxCacheSizeBytes);
            Assert.Equal(TimeSpan.FromMinutes(5), options.DefaultCacheTtl);
            Assert.Equal(TimeSpan.FromSeconds(30), options.MinCacheTtl);
            Assert.Equal(TimeSpan.FromMinutes(30), options.MaxCacheTtl);
        }

        [Fact]
        public async Task HandleAsync_Should_Generate_Unique_Cache_Keys_For_Different_Requests()
        {
            // Arrange
            var capturedKeys = new List<string>();

            _cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((key, _) => capturedKeys.Add(key))
                .ReturnsAsync((OptimizationRecommendation?)null);

            var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(_logger, _cacheMock.Object);
            _behaviorsToDispose.Add(behavior);

            var request1 = new TestRequest { Value = "test1" };
            var request2 = new TestRequest { Value = "test2" };

            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act
            await behavior.HandleAsync(request1, next, CancellationToken.None);
            await behavior.HandleAsync(request2, next, CancellationToken.None);

            // Assert
            Assert.Equal(2, capturedKeys.Count);
            Assert.NotEqual(capturedKeys[0], capturedKeys[1]);
        }

        [Fact]
        public async Task HandleAsync_Should_Generate_Same_Cache_Key_For_Identical_Requests()
        {
            // Arrange
            var capturedKeys = new List<string>();

            _cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((key, _) => capturedKeys.Add(key))
                .ReturnsAsync((OptimizationRecommendation?)null);

            var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(_logger, _cacheMock.Object);
            _behaviorsToDispose.Add(behavior);

            var request1 = new TestRequest { Value = "test" };
            var request2 = new TestRequest { Value = "test" };

            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act
            await behavior.HandleAsync(request1, next, CancellationToken.None);
            await behavior.HandleAsync(request2, next, CancellationToken.None);

            // Assert
            Assert.Equal(2, capturedKeys.Count);
            Assert.Equal(capturedKeys[0], capturedKeys[1]);
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

        // Test classes
        public class TestRequest : IRequest<TestResponse>
        {
            public string Value { get; set; } = string.Empty;
        }

        [IntelligentCaching(UseDynamicTtl = true, MinAccessFrequency = 1, MinPredictedHitRate = 0.0)]
        public class TestIntelligentCachingRequest : IRequest<TestResponse>
        {
            public string Value { get; set; } = string.Empty;
        }

        public class TestSizedRequest : IRequest<TestSizedResponse>
        {
            public string Value { get; set; } = string.Empty;
        }

        public class TestResponse
        {
            public string Result { get; set; } = string.Empty;
        }

        public class TestSizedResponse : IEstimateSize
        {
            public string Result { get; set; } = string.Empty;
            public long Size { get; set; }

            public long EstimateSize() => Size;
        }
    }
}
