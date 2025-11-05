using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Pipeline.Behaviors;
using System;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI;

public class AICachingOptimizationBehaviorConstructorTests : IDisposable
{
    private readonly ILogger<AICachingOptimizationBehavior<TestRequest, TestResponse>> _logger;
    private readonly Mock<IAIPredictionCache> _cacheMock;
    private readonly List<AICachingOptimizationBehavior<TestRequest, TestResponse>> _behaviorsToDispose;

    public AICachingOptimizationBehaviorConstructorTests()
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
}