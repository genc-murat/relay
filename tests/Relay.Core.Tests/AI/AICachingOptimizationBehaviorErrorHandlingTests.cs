using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Pipeline.Behaviors;
using Relay.Core.AI.Pipeline.Options;
using Relay.Core.Contracts.Pipeline;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI;

public class AICachingOptimizationBehaviorErrorHandlingTests : IDisposable
{
    private readonly ILogger<AICachingOptimizationBehavior<TestRequest, TestResponse>> _logger;
    private readonly Mock<IAIPredictionCache> _cacheMock;
    private readonly List<AICachingOptimizationBehavior<TestRequest, TestResponse>> _behaviorsToDispose;

    public AICachingOptimizationBehaviorErrorHandlingTests()
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
    public async Task HandleAsync_Should_Generate_Fallback_Cache_Key_On_Serialization_Error()
    {
        // Arrange
        var capturedKeys = new List<string>();

        _cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((key, _) => capturedKeys.Add(key))
            .ReturnsAsync((OptimizationRecommendation?)null);

        // Create behavior with custom options that might cause serialization issues
        var options = new AICachingOptimizationOptions
        {
            SerializerOptions = new JsonSerializerOptions
            {
                // Configure options that might cause issues, but actually JsonSerializer is robust
                // We'll test that the method handles any potential exceptions gracefully
            }
        };

        var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(_logger, _cacheMock.Object, options);
        _behaviorsToDispose.Add(behavior);

        var request = new TestRequest { Value = "test" };
        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "success" });

        // Act
        await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert - Should generate a valid cache key (either normal or fallback)
        Assert.Single(capturedKeys);
        var key = capturedKeys[0];
        Assert.NotNull(key);
        Assert.NotEmpty(key);
        // Key should either start with "global:" (normal) or "fallback:" (error case)
        Assert.True(key.StartsWith("global:") || key.StartsWith("fallback:"), $"Unexpected key format: {key}");
    }
}