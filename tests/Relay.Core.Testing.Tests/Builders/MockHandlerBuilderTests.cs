using System;
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
}