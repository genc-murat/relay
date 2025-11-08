using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Testing.Tests;

/// <summary>
/// Comprehensive tests for StreamingScenarioTemplate class.
/// </summary>
public class StreamingScenarioTemplateTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var relay = new TestRelay();

        // Act
        var template = new StreamingScenarioTemplate("Test Scenario", relay);

        // Assert
        Assert.Equal("Test Scenario", template.ScenarioName);
        Assert.NotNull(template);
    }

    [Fact]
    public void Constructor_WithNullRelay_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StreamingScenarioTemplate("Test", null!));
    }

    [Fact]
    public void Constructor_WithEmptyScenarioName_ThrowsArgumentException()
    {
        // Arrange
        var relay = new TestRelay();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new StreamingScenarioTemplate("", relay));
        Assert.Throws<ArgumentException>(() => new StreamingScenarioTemplate("   ", relay));
    }

    [Fact]
    public async Task StreamRequest_WithValidRequest_ReturnsSelfForChaining()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new StreamingScenarioTemplate("Test Scenario", relay);
        var request = new TestStreamRequest();

        // Act
        var result = template.StreamRequest<TestStreamRequest, string>(request, "Custom Stream");

        // Assert
        Assert.Same(template, result); // Should return self for chaining
    }

    [Fact]
    public void StreamRequest_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new StreamingScenarioTemplate("Test Scenario", relay);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => template.StreamRequest<TestStreamRequest, string>(null!));
    }

    [Fact]
    public async Task VerifyStream_WithValidVerification_ReturnsSelfForChaining()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new StreamingScenarioTemplate("Test Scenario", relay);
        Func<IReadOnlyList<object>, Task<bool>> verification = async (responses) => await Task.FromResult(true);

        // Act
        var result = template.VerifyStream(verification, "Custom Verification");

        // Assert
        Assert.Same(template, result); // Should return self for chaining
    }

    [Fact]
    public void VerifyStream_WithNullVerification_ThrowsArgumentNullException()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new StreamingScenarioTemplate("Test Scenario", relay);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => template.VerifyStream(null!));
    }

    [Fact]
    public async Task WaitForStreamCompletion_WithValidDuration_ReturnsSelfForChaining()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new StreamingScenarioTemplate("Test Scenario", relay);
        var duration = TimeSpan.FromSeconds(2);

        // Act
        var result = template.WaitForStreamCompletion(duration, "Custom Wait");

        // Assert
        Assert.Same(template, result); // Should return self for chaining
    }

    [Fact]
    public async Task SendRequest_WithValidRequest_ReturnsSelfForChaining()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new StreamingScenarioTemplate("Test Scenario", relay);
        var request = new TestCommand();

        // Act
        var result = template.SendRequest<TestCommand, TestResponse>(request, "Custom Request");

        // Assert
        Assert.Same(template, result); // Should return self for chaining
    }

    [Fact]
    public void SendRequest_WithNullRequest_DoesNotThrow()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new StreamingScenarioTemplate("Test Scenario", relay);

        // Act & Assert
        var ex = Record.Exception(() => template.SendRequest<TestCommand, TestResponse>(null!));
        Assert.Null(ex);
    }

    [Fact]
    public async Task PublishNotification_WithValidNotification_ReturnsSelfForChaining()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new StreamingScenarioTemplate("Test Scenario", relay);
        var notification = new TestEvent();

        // Act
        var result = template.PublishNotification(notification, "Custom Notification");

        // Assert
        Assert.Same(template, result); // Should return self for chaining
    }

    [Fact]
    public void PublishNotification_WithNullNotification_ThrowsArgumentNullException()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new StreamingScenarioTemplate("Test Scenario", relay);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => template.PublishNotification<TestEvent>(null!));
    }

    [Fact]
    public async Task ExecuteAsync_WithStreamingRequest_ExecutesSuccessfully()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new StreamingScenarioTemplate("Streaming Test", relay);

        var streamProcessed = false;
        relay.SetupStreamHandler<TestStreamRequest, string>((request, ct) =>
        {
            streamProcessed = true;
            return AsyncEnumerable();
        });

        async IAsyncEnumerable<string> AsyncEnumerable()
        {
            yield return "Item 1";
            yield return "Item 2";
            yield return "Item 3";
        }

        template.StreamRequest<TestStreamRequest, string>(new TestStreamRequest());

        // Act
        var result = await template.ExecuteAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Streaming Test", result.ScenarioName);
        Assert.Equal(1, result.StepResults.Count);
        Assert.True(result.StepResults[0].Success);
        Assert.True(streamProcessed);
    }

    [Fact]
    public async Task ExecuteAsync_WithComplexStreamingScenario_ExecutesAllSteps()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new StreamingScenarioTemplate("Complex Streaming Scenario", relay);

        var streamProcessed = false;
        var requestSent = false;
        var notificationPublished = false;
        var verificationCalled = false;

        relay.SetupStreamHandler<TestStreamRequest, string>((request, ct) =>
        {
            streamProcessed = true;
            return AsyncEnumerable();
        });

        async IAsyncEnumerable<string> AsyncEnumerable()
        {
            yield return "Stream Item 1";
            yield return "Stream Item 2";
        }

        relay.WithMockHandler<TestCommand, TestResponse>(builder =>
            builder.Returns((req) =>
            {
                requestSent = true;
                return new TestResponse { Result = "Request processed" };
            }));

        relay.SetupNotificationHandler<TestEvent>((@event, ct) =>
        {
            notificationPublished = true;
            return ValueTask.CompletedTask;
        });

        // Build complex scenario
        template
            .SendRequest<TestCommand, TestResponse>(new TestCommand(), "Send initial request")
            .PublishNotification(new TestEvent(), "Publish event")
            .WaitForStreamCompletion(TimeSpan.FromMilliseconds(100), "Wait for stream setup")
            .StreamRequest<TestStreamRequest, string>(new TestStreamRequest(), "Stream data")
            .VerifyStream(async (responses) =>
            {
                verificationCalled = true;
                // Note: Current implementation doesn't pass actual responses
                return streamProcessed && requestSent && notificationPublished;
            }, "Verify streaming results");

        // Act
        var result = await template.ExecuteAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Complex Streaming Scenario", result.ScenarioName);
        Assert.Equal(5, result.StepResults.Count);

        // Verify step names
        Assert.Equal("Send initial request", result.StepResults[0].StepName);
        Assert.Equal("Publish event", result.StepResults[1].StepName);
        Assert.Equal("Wait for stream setup", result.StepResults[2].StepName);
        Assert.Equal("Stream data", result.StepResults[3].StepName);
        Assert.Equal("Verify streaming results", result.StepResults[4].StepName);

        // Verify all steps succeeded
        foreach (var stepResult in result.StepResults)
        {
            Assert.True(stepResult.Success, $"Step '{stepResult.StepName}' failed: {stepResult.Error}");
        }

        // Verify side effects
        Assert.True(streamProcessed);
        Assert.True(requestSent);
        Assert.True(notificationPublished);
        Assert.True(verificationCalled);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailingStreamVerification_FailsScenario()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new StreamingScenarioTemplate("Failing Stream Scenario", relay);

        relay.SetupStreamHandler<TestStreamRequest, string>((request, ct) => AsyncEnumerable());

        async IAsyncEnumerable<string> AsyncEnumerable()
        {
            yield return "Item";
        }

        template
            .StreamRequest<TestStreamRequest, string>(new TestStreamRequest())
            .VerifyStream(async (responses) => await Task.FromResult(false), "Always fails");

        // Act
        var result = await template.ExecuteAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Verification failed", result.Error);
        Assert.Equal(2, result.StepResults.Count);
        Assert.True(result.StepResults[0].Success); // Streaming should succeed
        Assert.False(result.StepResults[1].Success); // Verification should fail
    }

    [Fact]
    public async Task FluentChaining_WorksCorrectly()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new StreamingScenarioTemplate("Chaining Test", relay);

        // Act - Test that all methods return the template for chaining
        var result = template
            .SendRequest<TestCommand, TestResponse>(new TestCommand())
            .PublishNotification(new TestEvent())
            .WaitForStreamCompletion(TimeSpan.FromSeconds(1))
            .StreamRequest<TestStreamRequest, string>(new TestStreamRequest())
            .VerifyStream(async (responses) => await Task.FromResult(true));

        // Assert
        Assert.Same(template, result);
    }

    [Fact]
    public async Task StreamRequest_WithMultipleStreamRequests_ExecutesSuccessfully()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new StreamingScenarioTemplate("Multiple Streams Test", relay);

        var streamCallCount = 0;

        relay.SetupStreamHandler<TestStreamRequest, string>((request, ct) =>
        {
            streamCallCount++;
            return StreamItems();
        });

        async IAsyncEnumerable<string> StreamItems()
        {
            yield return $"Item {streamCallCount}-1";
            yield return $"Item {streamCallCount}-2";
        }

        template
            .StreamRequest<TestStreamRequest, string>(new TestStreamRequest(), "First stream")
            .StreamRequest<TestStreamRequest, string>(new TestStreamRequest(), "Second stream");

        // Act
        var result = await template.ExecuteAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.StepResults.Count);
        Assert.Equal(2, streamCallCount); // Handler should be called twice
    }

    [Fact]
    public async Task WaitForStreamCompletion_DelaysExecution()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new StreamingScenarioTemplate("Wait Test", relay);

        var startTime = DateTime.UtcNow;
        var waitDuration = TimeSpan.FromMilliseconds(180); // Reduced to allow for system timing variations

        template.WaitForStreamCompletion(waitDuration);

        // Act
        var result = await template.ExecuteAsync();
        var endTime = DateTime.UtcNow;

        // Assert
        Assert.True(result.Success);
        Assert.True((endTime - startTime) >= waitDuration, $"Wait duration was {(endTime - startTime).TotalMilliseconds}ms, expected at least {waitDuration.TotalMilliseconds}ms");
    }

    // Test data classes
    private class TestStreamRequest : IStreamRequest<string> { }
    private class AnotherStreamRequest : IStreamRequest<int> { }
    private class TestCommand : IRequest<TestResponse> { }
    private class TestResponse { public string Result { get; set; } = string.Empty; }
    private class TestEvent : INotification { }
}