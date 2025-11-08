using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Testing.Tests;

/// <summary>
/// Comprehensive tests for EventDrivenScenarioTemplate class.
/// </summary>
public class EventDrivenScenarioTemplateTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var relay = new TestRelay();

        // Act
        var template = new EventDrivenScenarioTemplate("Test Scenario", relay);

        // Assert
        Assert.Equal("Test Scenario", template.ScenarioName);
        Assert.NotNull(template);
    }

    [Fact]
    public void Constructor_WithNullRelay_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EventDrivenScenarioTemplate("Test", null!));
    }

    [Fact]
    public void Constructor_WithEmptyScenarioName_ThrowsArgumentException()
    {
        // Arrange
        var relay = new TestRelay();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new EventDrivenScenarioTemplate("", relay));
        Assert.Throws<ArgumentException>(() => new EventDrivenScenarioTemplate("   ", relay));
    }

    [Fact]
    public async Task PublishEvent_WithValidEvent_ReturnsSelfForChaining()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new EventDrivenScenarioTemplate("Test Scenario", relay);
        var testEvent = new TestEvent();

        // Act
        var result = template.PublishEvent(testEvent, "Custom Step Name");

        // Assert
        Assert.Same(template, result); // Should return self for chaining
    }

    [Fact]
    public void PublishEvent_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new EventDrivenScenarioTemplate("Test Scenario", relay);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => template.PublishEvent<TestEvent>(null!));
    }

    [Fact]
    public async Task PublishEvents_WithMultipleEvents_ReturnsSelfForChaining()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new EventDrivenScenarioTemplate("Test Scenario", relay);
        var event1 = new TestEvent();
        var event2 = new TestEvent();
        var event3 = new TestEvent();

        // Act
        var result = template.PublishEvents<TestEvent>(event1, event2, event3);

        // Assert
        Assert.Same(template, result); // Should return self for chaining
    }

    [Fact]
    public void PublishEvents_WithNullArray_ThrowsArgumentNullException()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new EventDrivenScenarioTemplate("Test Scenario", relay);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => template.PublishEvents<TestEvent>(null!));
    }

    [Fact]
    public void PublishEvents_WithNullEventInArray_ThrowsArgumentException()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new EventDrivenScenarioTemplate("Test Scenario", relay);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            template.PublishEvents<TestEvent>(new TestEvent(), null!, new TestEvent()));
        Assert.Contains("Event at index 1 cannot be null", exception.Message);
    }

    [Fact]
    public async Task VerifySideEffects_WithValidVerification_ReturnsSelfForChaining()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new EventDrivenScenarioTemplate("Test Scenario", relay);
        Func<Task<bool>> verification = async () => await Task.FromResult(true);

        // Act
        var result = template.VerifySideEffects(verification, "Custom Verification");

        // Assert
        Assert.Same(template, result); // Should return self for chaining
    }

    [Fact]
    public void VerifySideEffects_WithNullVerification_ThrowsArgumentNullException()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new EventDrivenScenarioTemplate("Test Scenario", relay);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => template.VerifySideEffects(null!));
    }

    [Fact]
    public async Task WaitForEventProcessing_WithValidDuration_ReturnsSelfForChaining()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new EventDrivenScenarioTemplate("Test Scenario", relay);
        var duration = TimeSpan.FromSeconds(2);

        // Act
        var result = template.WaitForEventProcessing(duration, "Custom Wait");

        // Assert
        Assert.Same(template, result); // Should return self for chaining
    }

    [Fact]
    public async Task SendRequest_WithValidRequest_ReturnsSelfForChaining()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new EventDrivenScenarioTemplate("Test Scenario", relay);
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
        var template = new EventDrivenScenarioTemplate("Test Scenario", relay);

        // Act & Assert
        var ex = Record.Exception(() => template.SendRequest<TestCommand, TestResponse>(null!));
        Assert.Null(ex);
    }

    [Fact]
    public async Task ExecuteAsync_WithComplexScenario_ExecutesAllStepsInOrder()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new EventDrivenScenarioTemplate("Complex Event Scenario", relay);

        var eventsPublished = new List<string>();
        var requestSent = false;
        var verificationCalled = false;

        relay.SetupNotificationHandler<TestEvent>((@event, ct) =>
        {
            eventsPublished.Add("Event1");
            return ValueTask.CompletedTask;
        });

        relay.SetupNotificationHandler<AnotherTestEvent>((@event, ct) =>
        {
            eventsPublished.Add("Event2");
            return ValueTask.CompletedTask;
        });

        relay.WithMockHandler<TestCommand, TestResponse>(builder =>
            builder.Returns((req) =>
            {
                requestSent = true;
                return new TestResponse { Result = "Request processed" };
            }));

        // Build complex scenario
        template
            .PublishEvent(new TestEvent(), "Publish First Event")
            .WaitForEventProcessing(TimeSpan.FromMilliseconds(100), "Wait for processing")
            .PublishEvents<AnotherTestEvent>(new AnotherTestEvent())
            .PublishEvents<TestEvent>(new TestEvent())
            .SendRequest<TestCommand, TestResponse>(new TestCommand(), "Send trigger request")
            .VerifySideEffects(async () =>
            {
                verificationCalled = true;
                return eventsPublished.Count == 3 && requestSent;
            }, "Verify all side effects");

        // Act
        var result = await template.ExecuteAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Complex Event Scenario", result.ScenarioName);
        Assert.Equal(6, result.StepResults.Count); // 5 steps + 1 verification

        // Verify step names
        Assert.Equal("Publish First Event", result.StepResults[0].StepName);
        Assert.Equal("Wait for processing", result.StepResults[1].StepName);
        Assert.Equal("Publish Event 1", result.StepResults[2].StepName);
        Assert.Equal("Publish Event 1", result.StepResults[3].StepName);
        Assert.Equal("Send trigger request", result.StepResults[4].StepName);
        Assert.Equal("Verify all side effects", result.StepResults[5].StepName);

        // Verify all steps succeeded
        foreach (var stepResult in result.StepResults)
        {
            Assert.True(stepResult.Success, $"Step '{stepResult.StepName}' failed: {stepResult.Error}");
        }

        // Verify side effects occurred
        Assert.Equal(3, eventsPublished.Count);
        Assert.Contains("Event1", eventsPublished);
        Assert.Contains("Event2", eventsPublished);
        Assert.True(requestSent);
        Assert.True(verificationCalled);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailingVerification_FailsScenario()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new EventDrivenScenarioTemplate("Failing Scenario", relay);

        template
            .PublishEvent(new TestEvent())
            .VerifySideEffects(async () => await Task.FromResult(false), "Always fails");

        // Act
        var result = await template.ExecuteAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Verification failed", result.Error);
        Assert.Equal(2, result.StepResults.Count);
        Assert.True(result.StepResults[0].Success); // Event publication should succeed
        Assert.False(result.StepResults[1].Success); // Verification should fail
    }

    [Fact]
    public async Task FluentChaining_WorksCorrectly()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new EventDrivenScenarioTemplate("Chaining Test", relay);

        // Act - Test that all methods return the template for chaining
        var result = template
            .PublishEvent(new TestEvent())
            .PublishEvents<AnotherTestEvent>(new AnotherTestEvent())
            .WaitForEventProcessing(TimeSpan.FromSeconds(1))
            .SendRequest<TestCommand, TestResponse>(new TestCommand())
            .VerifySideEffects(async () => await Task.FromResult(true));

        // Assert
        Assert.Same(template, result);
    }

    [Fact]
    public async Task PublishEvent_ExecutesSuccessfully()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new EventDrivenScenarioTemplate("Test Scenario", relay);

        var eventHandled = false;
        relay.SetupNotificationHandler<TestEvent>((@event, ct) =>
        {
            eventHandled = true;
            return ValueTask.CompletedTask;
        });

        template.PublishEvent(new TestEvent());

        // Act
        var result = await template.ExecuteAsync();

        // Assert
        Assert.True(result.Success);
        Assert.True(eventHandled);
    }

    [Fact]
    public async Task PublishEvents_MultipleEvents_ExecutesSuccessfully()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new EventDrivenScenarioTemplate("Test Scenario", relay);

        var eventsHandled = 0;
        relay.SetupNotificationHandler<TestEvent>((@event, ct) =>
        {
            eventsHandled++;
            return ValueTask.CompletedTask;
        });

        template.PublishEvents<TestEvent>(new TestEvent(), new TestEvent());

        // Act
        var result = await template.ExecuteAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, eventsHandled);
    }

    [Fact]
    public async Task WaitForEventProcessing_DelaysExecution()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new EventDrivenScenarioTemplate("Test Scenario", relay);

        var startTime = DateTime.UtcNow;
        var waitDuration = TimeSpan.FromMilliseconds(200);

        template.WaitForEventProcessing(waitDuration);

        // Act
        var result = await template.ExecuteAsync();
        var endTime = DateTime.UtcNow;

        // Assert
        Assert.True(result.Success);
        Assert.True((endTime - startTime) >= waitDuration, $"Wait duration was {(endTime - startTime).TotalMilliseconds}ms, expected at least {waitDuration.TotalMilliseconds}ms");
    }

    [Fact]
    public async Task SendRequest_ExecutesSuccessfully()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new EventDrivenScenarioTemplate("Test Scenario", relay);

        var requestHandled = false;
        relay.WithMockHandler<TestCommand, TestResponse>(builder =>
            builder.Returns((req) =>
            {
                requestHandled = true;
                return new TestResponse { Result = "Success" };
            }));

        template.SendRequest<TestCommand, TestResponse>(new TestCommand());

        // Act
        var result = await template.ExecuteAsync();

        // Assert
        Assert.True(result.Success);
        Assert.True(requestHandled);
    }

    // Test data classes
    private class TestEvent : INotification { }
    private class AnotherTestEvent : INotification { }
    private class TestCommand : IRequest<TestResponse> { }
    private class TestResponse { public string Result { get; set; } = string.Empty; }
}