using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Testing.Tests;

public class ScenarioTemplateTests
{
    [Fact]
    public async Task CqrsScenarioTemplate_ShouldExecuteCommandAndQuery()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new CqrsScenarioTemplate("Test CQRS Scenario", relay);

        var commandExecuted = false;
        var queryExecuted = false;

        relay.WithMockHandler<TestCommand, TestResponse>(builder =>
            builder.Returns((req) =>
            {
                commandExecuted = true;
                return new TestResponse { Result = "Command executed" };
            }));

        relay.WithMockHandler<TestQuery, TestResponse>(builder =>
            builder.Returns((req) =>
            {
                queryExecuted = true;
                return new TestResponse { Result = "Query executed" };
            }));

        template
            .SendCommand<TestCommand, TestResponse>(new TestCommand())
            .SendQuery<TestQuery, TestResponse>(new TestQuery())
            .VerifyState(async () =>
            {
                return commandExecuted && queryExecuted;
            });

        // Act
        var result = await template.ExecuteAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Test CQRS Scenario", result.ScenarioName);
        Assert.Equal(3, result.StepResults.Count);
        Assert.True(commandExecuted);
        Assert.True(queryExecuted);
    }

    [Fact]
    public async Task EventDrivenScenarioTemplate_ShouldPublishEventsAndVerifySideEffects()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new EventDrivenScenarioTemplate("Test Event-Driven Scenario", relay);

        var eventProcessed = false;

        relay.SetupNotificationHandler<TestEvent>((@event, ct) =>
        {
            eventProcessed = true;
            return ValueTask.CompletedTask;
        });

        template
            .PublishEvent(new TestEvent())
            .VerifySideEffects(async () => eventProcessed);

        // Act
        var result = await template.ExecuteAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Test Event-Driven Scenario", result.ScenarioName);
        Assert.Equal(2, result.StepResults.Count);
        Assert.True(eventProcessed);
    }

    [Fact]
    public async Task StreamingScenarioTemplate_ShouldHandleStreamingRequest()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new StreamingScenarioTemplate("Test Streaming Scenario", relay);

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
        }

        template
            .StreamRequest<TestStreamRequest, string>(new TestStreamRequest())
            .VerifyStream(async responses =>
            {
                return streamProcessed;
            });

        // Act
        var result = await template.ExecuteAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Test Streaming Scenario", result.ScenarioName);
        Assert.Equal(2, result.StepResults.Count);
        Assert.True(streamProcessed);
    }

    [Fact]
    public async Task ScenarioTemplate_WithSetupAndTeardown_ShouldExecuteInCorrectOrder()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new CqrsScenarioTemplate("Test Setup/Teardown", relay);

        var setupExecuted = false;
        var teardownExecuted = false;
        var commandExecuted = false;

        relay.WithMockHandler<TestCommand, TestResponse>(builder =>
            builder.Returns((req) =>
            {
                commandExecuted = true;
                return new TestResponse { Result = "Command executed" };
            }));

        template
            .WithSetup(async () => { setupExecuted = true; })
            .WithTeardown(async () => { teardownExecuted = true; });

        template.SendCommand<TestCommand, TestResponse>(new TestCommand());

        // Act
        var result = await template.ExecuteAsync();

        // Assert
        Assert.True(result.Success);
        Assert.True(setupExecuted);
        Assert.True(teardownExecuted);
        Assert.True(commandExecuted);
    }

    [Fact]
    public void ScenarioTemplate_WithNullParameters_ShouldThrowArgumentNullException()
    {
        var relay = new TestRelay();

        Assert.Throws<ArgumentException>(() => new CqrsScenarioTemplate("", relay));
        Assert.Throws<ArgumentNullException>(() => new CqrsScenarioTemplate("Test", null!));
        Assert.Throws<ArgumentNullException>(() => new CqrsScenarioTemplate("Test", relay).WithSetup(null!));
        Assert.Throws<ArgumentNullException>(() => new CqrsScenarioTemplate("Test", relay).WithTeardown(null!));
    }

    [Fact]
    public void CqrsScenarioTemplate_WithNullParameters_ShouldThrowArgumentNullException()
    {
        var relay = new TestRelay();
        var template = new CqrsScenarioTemplate("Test", relay);

        Assert.Throws<ArgumentNullException>(() => template.SendCommand<TestCommand, TestResponse>(null!));
        Assert.Throws<ArgumentNullException>(() => template.SendQuery<TestQuery, TestResponse>(null!));
        Assert.Throws<ArgumentNullException>(() => template.VerifyState(null!));
    }

    [Fact]
    public void EventDrivenScenarioTemplate_WithNullParameters_ShouldThrowArgumentNullException()
    {
        var relay = new TestRelay();
        var template = new EventDrivenScenarioTemplate("Test", relay);

        Assert.Throws<ArgumentNullException>(() => template.PublishEvent<TestEvent>(null!));
        Assert.Throws<ArgumentNullException>(() => template.PublishEvents<TestEvent>(null!));
        Assert.Throws<ArgumentNullException>(() => template.VerifySideEffects(null!));
        // Note: SendRequest with null is now allowed (validation deferred to execution time)
    }

    [Fact]
    public void StreamingScenarioTemplate_WithNullParameters_ShouldThrowArgumentNullException()
    {
        var relay = new TestRelay();
        var template = new StreamingScenarioTemplate("Test", relay);

        Assert.Throws<ArgumentNullException>(() => template.StreamRequest<TestStreamRequest, string>(null!));
        Assert.Throws<ArgumentNullException>(() => template.VerifyStream(null!));
        // Note: SendRequest with null is now allowed (validation deferred to execution time)
        Assert.Throws<ArgumentNullException>(() => template.PublishNotification<TestEvent>(null!));
    }

    // Test data classes
    private class TestCommand : IRequest<TestResponse> { }
    private class TestQuery : IRequest<TestResponse> { }
    private class TestResponse { public string Result { get; set; } = string.Empty; }
    private class TestEvent : INotification { }
    private class TestStreamRequest : IStreamRequest<string> { }
}