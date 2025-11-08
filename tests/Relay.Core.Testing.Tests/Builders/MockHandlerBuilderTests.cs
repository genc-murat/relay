using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Testing.Tests;

/// <summary>
/// Test request and response types for mock handler testing
/// </summary>
public class MockTestRequest : IRequest<MockTestResponse>
{
    public string? Data { get; set; }
}

public class MockTestResponse
{
    public string? Result { get; set; }
}

public class MockVoidRequest : IRequest
{
    public string? Data { get; set; }
}

public class MockHandlerBuilderTests
{
    [Fact]
    public async Task Returns_WithValue_ReturnsConfiguredResponse()
    {
        // Arrange
        var relay = new TestRelay();
        var expectedResponse = new MockTestResponse { Result = "test" };

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
            builder.Returns(expectedResponse));

        // Act
        var actualResponse = await relay.SendAsync(new MockTestRequest());

        // Assert
        Assert.Equal(expectedResponse, actualResponse);
    }

    [Fact]
    public async Task Returns_WithFactory_ReturnsFactoryResult()
    {
        // Arrange
        var relay = new TestRelay();
        var request = new MockTestRequest { Data = "input" };

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
            builder.Returns(r => new MockTestResponse { Result = r.Data + "_processed" }));

        // Act
        var response = await relay.SendAsync(request);

        // Assert
        Assert.Equal("input_processed", response.Result);
    }

    [Fact]
    public async Task Returns_WithAsyncFactory_ReturnsFactoryResult()
    {
        // Arrange
        var relay = new TestRelay();
        var request = new MockTestRequest { Data = "async" };

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
            builder.Returns(async (r, ct) =>
            {
                await Task.Delay(1, ct); // Simulate async work
                return new MockTestResponse { Result = r.Data + "_async" };
            }).Delays(TimeSpan.FromMilliseconds(1))); // Force async execution

        // Act
        var response = await relay.SendAsync(request);

        // Assert
        Assert.Equal("async_async", response.Result);
    }

    [Fact]
    public async Task Throws_WithException_ThrowsConfiguredException()
    {
        // Arrange
        var relay = new TestRelay();
        var expectedException = new InvalidOperationException("Test exception");

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
            builder.Throws(expectedException));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await relay.SendAsync(new MockTestRequest()));
    }

    [Fact]
    public async Task Throws_WithFactory_ThrowsFactoryResult()
    {
        // Arrange
        var relay = new TestRelay();
        var request = new MockTestRequest { Data = "error_data" };

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
            builder.Throws(r => new ArgumentException($"Error for {r.Data}")));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await relay.SendAsync(request));
    }

    [Fact]
    public async Task Delays_WithTimeSpan_DelaysExecution()
    {
        // Arrange
        var relay = new TestRelay();
        var delay = TimeSpan.FromMilliseconds(50);

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
            builder.Returns(new MockTestResponse { Result = "delayed" })
                   .Delays(delay));

        // Act
        var startTime = DateTime.UtcNow;
        await relay.SendAsync(new MockTestRequest());
        var endTime = DateTime.UtcNow;

        // Assert
        var actualDelay = endTime - startTime;
        Assert.True(actualDelay >= delay, $"Expected delay of at least {delay.TotalMilliseconds}ms, but was {actualDelay.TotalMilliseconds}ms");
    }

    [Fact]
    public async Task ReturnsInSequence_WithMultipleValues_ReturnsInOrder()
    {
        // Arrange
        var relay = new TestRelay();
        var response1 = new MockTestResponse { Result = "first" };
        var response2 = new MockTestResponse { Result = "second" };
        var response3 = new MockTestResponse { Result = "third" };

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
        {
            builder.Returns(response1);
            builder.Returns(response2);
            builder.Returns(response3);
        });

        // Act & Assert
        var firstResult = await relay.SendAsync(new MockTestRequest());
        Assert.Equal("first", firstResult.Result);
        var secondResult = await relay.SendAsync(new MockTestRequest());
        Assert.Equal("second", secondResult.Result);
        var thirdResult = await relay.SendAsync(new MockTestRequest());
        Assert.Equal("third", thirdResult.Result);
        // Should cycle back to first response
        var fourthResult = await relay.SendAsync(new MockTestRequest());
        Assert.Equal("first", fourthResult.Result);
    }

    [Fact]
    public async Task ThrowsInSequence_WithMultipleExceptions_ThrowsInOrder()
    {
        // Arrange
        var relay = new TestRelay();
        var exception1 = new InvalidOperationException("first");
        var exception2 = new InvalidOperationException("second");

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
            builder.ThrowsInSequence<InvalidOperationException>(exception1, exception2));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await relay.SendAsync(new MockTestRequest()));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await relay.SendAsync(new MockTestRequest()));
        // Should cycle back to first exception
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await relay.SendAsync(new MockTestRequest()));
    }

    [Fact]
    public async Task Verifier_ShouldHaveBeenCalled_RecordsCalls()
    {
        // Arrange
        var relay = new TestRelay();
        HandlerVerifier<MockTestRequest, MockTestResponse>? verifier = null;

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
        {
            builder.Returns(new MockTestResponse { Result = "test" });
            verifier = builder.Verifier;
        });

        // Act
        await relay.SendAsync(new MockTestRequest());
        await relay.SendAsync(new MockTestRequest());

        // Assert
        Assert.NotNull(verifier);
        verifier.ShouldHaveBeenCalled(2);
    }

    [Fact]
    public async Task Verifier_ShouldHaveBeenCalledWith_MatchesPredicate()
    {
        // Arrange
        var relay = new TestRelay();
        HandlerVerifier<MockTestRequest, MockTestResponse>? verifier = null;

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
        {
            builder.Returns(new MockTestResponse { Result = "test" });
            verifier = builder.Verifier;
        });

        var request1 = new MockTestRequest { Data = "match" };
        var request2 = new MockTestRequest { Data = "no_match" };

        // Act
        await relay.SendAsync(request1);
        await relay.SendAsync(request2);

        // Assert
        Assert.NotNull(verifier);
        verifier.ShouldHaveBeenCalledWith(r => r.Data == "match");
    }

    [Fact]
    public async Task Verifier_ShouldHaveBeenCalledInOrder_ValidatesOrder()
    {
        // Arrange
        var relay = new TestRelay();
        HandlerVerifier<MockTestRequest, MockTestResponse>? verifier = null;

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
        {
            builder.Returns(new MockTestResponse { Result = "test" });
            verifier = builder.Verifier;
        });

        var request1 = new MockTestRequest { Data = "first" };
        var request2 = new MockTestRequest { Data = "second" };

        // Act
        await relay.SendAsync(request1);
        await relay.SendAsync(request2);

        // Assert
        Assert.NotNull(verifier);
        verifier.ShouldHaveBeenCalledInOrder(request1, request2);
    }

    [Fact]
    public async Task Verifier_GetRequest_ReturnsCorrectRequest()
    {
        // Arrange
        var relay = new TestRelay();
        HandlerVerifier<MockTestRequest, MockTestResponse>? verifier = null;

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
        {
            builder.Returns(new MockTestResponse { Result = "test" });
            verifier = builder.Verifier;
        });

        var request1 = new MockTestRequest { Data = "first" };
        var request2 = new MockTestRequest { Data = "second" };

        // Act
        await relay.SendAsync(request1);
        await relay.SendAsync(request2);

        // Assert
        Assert.NotNull(verifier);
        Assert.Equal(request1, verifier.GetRequest(0));
        Assert.Equal(request2, verifier.GetRequest(1));
    }

    [Fact]
    public async Task Verifier_ShouldNotHaveBeenCalled_WhenNoCalls()
    {
        // Arrange
        var relay = new TestRelay();
        HandlerVerifier<MockTestRequest, MockTestResponse>? verifier = null;

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
        {
            builder.Returns(new MockTestResponse { Result = "test" });
            verifier = builder.Verifier;
        });

        // Act & Assert (no calls made)
        Assert.NotNull(verifier);
        verifier.ShouldNotHaveBeenCalled();
    }

    [Fact]
    public async Task Verifier_ShouldHaveBeenCalledAtLeast_ValidatesMinimumCalls()
    {
        // Arrange
        var relay = new TestRelay();
        HandlerVerifier<MockTestRequest, MockTestResponse>? verifier = null;

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
        {
            builder.Returns(new MockTestResponse { Result = "test" });
            verifier = builder.Verifier;
        });

        // Act
        await relay.SendAsync(new MockTestRequest());
        await relay.SendAsync(new MockTestRequest());

        // Assert
        Assert.NotNull(verifier);
        verifier.ShouldHaveBeenCalledAtLeast(1);
        verifier.ShouldHaveBeenCalledAtLeast(2);
    }

    [Fact]
    public async Task Verifier_ShouldHaveBeenCalledAtMost_ValidatesMaximumCalls()
    {
        // Arrange
        var relay = new TestRelay();
        HandlerVerifier<MockTestRequest, MockTestResponse>? verifier = null;

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
        {
            builder.Returns(new MockTestResponse { Result = "test" });
            verifier = builder.Verifier;
        });

        // Act
        await relay.SendAsync(new MockTestRequest());

        // Assert
        Assert.NotNull(verifier);
        verifier.ShouldHaveBeenCalledAtMost(1);
        verifier.ShouldHaveBeenCalledAtMost(2);
    }



    [Fact]
    public async Task Build_WithoutBehavior_ThrowsException()
    {
        // Arrange
        var builder = new MockHandlerBuilder<MockTestRequest, MockTestResponse>();
        var handler = builder.Build();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler(new MockTestRequest(), CancellationToken.None));
        Assert.Contains("No behavior configured", exception.Message);
    }

    [Fact]
    public void Delays_WithoutPriorBehavior_ThrowsException()
    {
        // Arrange
        var builder = new MockHandlerBuilder<MockTestRequest, MockTestResponse>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => builder.Delays(TimeSpan.FromSeconds(1)));
        Assert.Contains("Must configure a return or throw behavior", exception.Message);
    }

    [Fact]
    public async Task Returns_WithAsyncFactory_WithoutDelay_ThrowsException()
    {
        // Arrange
        var relay = new TestRelay();

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
            builder.Returns(async (r, ct) => new MockTestResponse { Result = "async" }));
            // Note: No .Delays() call, so it should execute synchronously and throw

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await relay.SendAsync(new MockTestRequest()));
        Assert.Contains("Async factory cannot be executed synchronously", exception.Message);
    }

    [Fact]
    public async Task SynchronousExecutionPath_WithoutDelay_UsesExecuteBehavior()
    {
        // Arrange
        var relay = new TestRelay();

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
            builder.Returns(new MockTestResponse { Result = "sync" }));
            // No delay, so should use synchronous execution path

        // Act
        var response = await relay.SendAsync(new MockTestRequest());

        // Assert
        Assert.Equal("sync", response.Result);
    }

    [Fact]
    public async Task CancellationToken_IsPropagated_InAsyncOperations()
    {
        // Arrange
        var relay = new TestRelay();
        var cts = new CancellationTokenSource();

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
            builder.Returns(async (r, ct) =>
            {
                // Simulate checking cancellation token
                ct.ThrowIfCancellationRequested();
                await Task.Delay(10, ct);
                return new MockTestResponse { Result = "cancelled_check" };
            }).Delays(TimeSpan.FromMilliseconds(1))); // Force async execution

        // Act & Assert
        cts.Cancel();
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await relay.SendAsync(new MockTestRequest(), cts.Token));
    }

    [Fact]
    public async Task BehaviorIndexing_HandlesLargeCallCounts()
    {
        // Arrange
        var relay = new TestRelay();
        var responses = new[] { "first", "second", "third" };

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
        {
            foreach (var response in responses)
            {
                builder.Returns(new MockTestResponse { Result = response });
            }
        });

        // Act - Make more calls than behaviors to test modulo operation
        var results = new List<string>();
        for (int i = 0; i < responses.Length * 2 + 1; i++)
        {
            var response = await relay.SendAsync(new MockTestRequest());
            results.Add(response.Result!);
        }

        // Assert - Should cycle through responses
        Assert.Equal("first", results[0]);
        Assert.Equal("second", results[1]);
        Assert.Equal("third", results[2]);
        Assert.Equal("first", results[3]); // Cycle back
        Assert.Equal("second", results[4]);
        Assert.Equal("third", results[5]);
        Assert.Equal("first", results[6]); // One more cycle
    }

    [Fact]
    public void Delays_WithPriorBehavior_SetsDelayOnLastBehavior()
    {
        // Arrange
        var builder = new MockHandlerBuilder<MockTestRequest, MockTestResponse>();
        var delay = TimeSpan.FromSeconds(1);

        // Act - Add behavior first, then delay
        builder.Returns(new MockTestResponse { Result = "test" });
        var result = builder.Delays(delay);

        // Assert
        Assert.Equal(builder, result); // Returns builder for chaining
        // Verify delay was set on the behavior using reflection
        var behaviorField = typeof(MockHandlerBuilder<MockTestRequest, MockTestResponse>)
            .GetField("_behaviors", BindingFlags.NonPublic | BindingFlags.Instance);
        var behaviors = behaviorField?.GetValue(builder) as List<MockHandlerBehavior<MockTestRequest, MockTestResponse>>;
        Assert.NotNull(behaviors);
        Assert.Single(behaviors);
        Assert.Equal(delay, behaviors[0].Delay);
    }

    [Fact]
    public async Task Build_WithoutDelay_UsesSynchronousExecution()
    {
        // Arrange
        var builder = new MockHandlerBuilder<MockTestRequest, MockTestResponse>();
        builder.Returns(new MockTestResponse { Result = "sync" });
        var handler = builder.Build();

        // Act - Call without delay to ensure ExecuteBehavior path
        var response = await handler(new MockTestRequest(), CancellationToken.None);

        // Assert
        Assert.Equal("sync", response.Result);
    }

    [Fact]
    public async Task ExecuteBehavior_WithInvalidBehaviorType_ThrowsException()
    {
        // Arrange - Need to create a behavior with invalid type using reflection
        var builder = new MockHandlerBuilder<MockTestRequest, MockTestResponse>();
        builder.Returns(new MockTestResponse { Result = "sync" }); // Add a behavior so Build() doesn't throw
        var handler = builder.Build();

        // Use reflection to create a behavior with invalid MockBehaviorType
        var behaviorField = typeof(MockHandlerBuilder<MockTestRequest, MockTestResponse>)
            .GetField("_behaviors", BindingFlags.NonPublic | BindingFlags.Instance);
        var behaviors = behaviorField?.GetValue(builder) as List<MockHandlerBehavior<MockTestRequest, MockTestResponse>>;

        // Create behavior with invalid type (e.g., cast -1 to MockBehaviorType)
        var invalidBehavior = new MockHandlerBehavior<MockTestRequest, MockTestResponse>
        {
            BehaviorType = (MockBehaviorType)(-1), // Invalid enum value
            Response = new MockTestResponse { Result = "invalid" },
            Delay = TimeSpan.FromTicks(1) // Ensure async execution path
        };
        behaviors?.Clear();
        behaviors?.Add(invalidBehavior);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler(new MockTestRequest(), CancellationToken.None));
        Assert.Contains("Unknown behavior type", exception.Message);
    }

    [Fact]
    public async Task ExecuteBehaviorAsync_WithInvalidBehaviorType_ThrowsException()
    {
        // Arrange - Similar to above but force async execution with delay
        var builder = new MockHandlerBuilder<MockTestRequest, MockTestResponse>();
        builder.Returns(new MockTestResponse { Result = "test" }).Delays(TimeSpan.FromTicks(1));
        var handler = builder.Build();

        // Use reflection to modify the behavior type to invalid value
        var behaviorField = typeof(MockHandlerBuilder<MockTestRequest, MockTestResponse>)
            .GetField("_behaviors", BindingFlags.NonPublic | BindingFlags.Instance);
        var behaviors = behaviorField?.GetValue(builder) as List<MockHandlerBehavior<MockTestRequest, MockTestResponse>>;

        if (behaviors?.Count > 0)
        {
            behaviors[0].BehaviorType = (MockBehaviorType)(-1); // Invalid enum value
        }

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler(new MockTestRequest(), CancellationToken.None));
        Assert.Contains("Unknown behavior type", exception.Message);
    }

    [Fact]
    public async Task ExecuteBehaviorAsync_WithReturnBehavior_UsesAsyncPath()
    {
        // Arrange - Force async execution with delay for Return behavior
        var relay = new TestRelay();
        var expectedResponse = new MockTestResponse { Result = "async_return" };

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
            builder.Returns(expectedResponse).Delays(TimeSpan.FromTicks(1)));

        // Act
        var response = await relay.SendAsync(new MockTestRequest());

        // Assert
        Assert.Equal(expectedResponse, response);
    }

    [Fact]
    public async Task ExecuteBehaviorAsync_WithReturnFactoryBehavior_UsesAsyncPath()
    {
        // Arrange - Force async execution with delay for ReturnFactory behavior
        var relay = new TestRelay();

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
            builder.Returns(r => new MockTestResponse { Result = r.Data + "_factory" })
                   .Delays(TimeSpan.FromTicks(1)));

        // Act
        var response = await relay.SendAsync(new MockTestRequest { Data = "async" });

        // Assert
        Assert.Equal("async_factory", response.Result);
    }

    [Fact]
    public async Task ExecuteBehaviorAsync_WithThrowFactoryBehavior_UsesAsyncPath()
    {
        // Arrange - Force async execution with delay for ThrowFactory behavior
        var relay = new TestRelay();

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
            builder.Throws(r => new ArgumentException($"Error for {r.Data}"))
                   .Delays(TimeSpan.FromTicks(1)));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await relay.SendAsync(new MockTestRequest { Data = "async_error" }));
        Assert.Contains("Error for async_error", exception.Message);
    }

    [Fact]
    public async Task ExecuteBehaviorAsync_WithThrowBehavior_UsesAsyncPath()
    {
        // Arrange - Force async execution with delay for Throw behavior
        var relay = new TestRelay();
        var expectedException = new InvalidOperationException("Async throw test");

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
            builder.Throws(expectedException).Delays(TimeSpan.FromTicks(1)));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await relay.SendAsync(new MockTestRequest()));
        Assert.Equal(expectedException, exception);
    }

    [Fact]
    public async Task ReturnsInSequence_Method_UsesConvenienceApi()
    {
        // Arrange
        var relay = new TestRelay();
        var response1 = new MockTestResponse { Result = "first" };
        var response2 = new MockTestResponse { Result = "second" };
        var response3 = new MockTestResponse { Result = "third" };

        relay.WithMockHandler<MockTestRequest, MockTestResponse>(builder =>
            builder.ReturnsInSequence(response1, response2, response3));

        // Act & Assert
        var firstResult = await relay.SendAsync(new MockTestRequest());
        Assert.Equal("first", firstResult.Result);
        var secondResult = await relay.SendAsync(new MockTestRequest());
        Assert.Equal("second", secondResult.Result);
        var thirdResult = await relay.SendAsync(new MockTestRequest());
        Assert.Equal("third", thirdResult.Result);
        // Should cycle back to first response
        var fourthResult = await relay.SendAsync(new MockTestRequest());
        Assert.Equal("first", fourthResult.Result);
    }
}