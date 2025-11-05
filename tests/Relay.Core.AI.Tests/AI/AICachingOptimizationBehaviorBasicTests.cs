using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Pipeline.Behaviors;
using Relay.Core.AI.Pipeline.Options;
using Relay.Core.Contracts.Pipeline;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI;

public class AICachingOptimizationBehaviorBasicTests : IDisposable
{
    private readonly ILogger<AICachingOptimizationBehavior<TestRequest, TestResponse>> _logger;
    private readonly Mock<IAIPredictionCache> _cacheMock;
    private readonly List<AICachingOptimizationBehavior<TestRequest, TestResponse>> _behaviorsToDispose;

    public AICachingOptimizationBehaviorBasicTests()
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
}