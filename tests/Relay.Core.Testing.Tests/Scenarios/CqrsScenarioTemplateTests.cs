using System;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Testing.Tests;

/// <summary>
/// Comprehensive tests for CqrsScenarioTemplate class.
/// </summary>
public class CqrsScenarioTemplateTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var relay = new TestRelay();

        // Act
        var template = new CqrsScenarioTemplate("Test Scenario", relay);

        // Assert
        Assert.Equal("Test Scenario", template.ScenarioName);
        Assert.NotNull(template);
    }

    [Fact]
    public void Constructor_WithNullRelay_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CqrsScenarioTemplate("Test", null!));
    }

    [Fact]
    public void Constructor_WithEmptyScenarioName_ThrowsArgumentException()
    {
        // Arrange
        var relay = new TestRelay();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CqrsScenarioTemplate("", relay));
        Assert.Throws<ArgumentException>(() => new CqrsScenarioTemplate("   ", relay));
    }

    [Fact]
    public void SendCommand_WithValidCommand_ReturnsSelfForChaining()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new CqrsScenarioTemplate("Test Scenario", relay);
        var command = new TestCommand();

        // Act
        var result = template.SendCommand<TestCommand, TestResponse>(command, "Custom Command");

        // Assert
        Assert.Same(template, result); // Should return self for chaining
    }

    [Fact]
    public void SendCommand_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new CqrsScenarioTemplate("Test Scenario", relay);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => template.SendCommand<TestCommand, TestResponse>(null!));
    }

    [Fact]
    public void SendQuery_WithValidQuery_ReturnsSelfForChaining()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new CqrsScenarioTemplate("Test Scenario", relay);
        var query = new TestQuery();

        // Act
        var result = template.SendQuery<TestQuery, TestResponse>(query, "Custom Query");

        // Assert
        Assert.Same(template, result); // Should return self for chaining
    }

    [Fact]
    public void SendQuery_WithNullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new CqrsScenarioTemplate("Test Scenario", relay);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => template.SendQuery<TestQuery, TestResponse>(null!));
    }

    [Fact]
    public void VerifyState_WithValidVerification_ReturnsSelfForChaining()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new CqrsScenarioTemplate("Test Scenario", relay);
        Func<Task<bool>> verification = async () => await Task.FromResult(true);

        // Act
        var result = template.VerifyState(verification, "Custom Verification");

        // Assert
        Assert.Same(template, result); // Should return self for chaining
    }

    [Fact]
    public void VerifyState_WithNullVerification_ThrowsArgumentNullException()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new CqrsScenarioTemplate("Test Scenario", relay);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => template.VerifyState(null!));
    }

    [Fact]
    public void WaitForProcessing_WithValidDuration_ReturnsSelfForChaining()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new CqrsScenarioTemplate("Test Scenario", relay);
        var duration = TimeSpan.FromSeconds(2);

        // Act
        var result = template.WaitForProcessing(duration, "Custom Wait");

        // Assert
        Assert.Same(template, result); // Should return self for chaining
    }

    [Fact]
    public async Task ExecuteAsync_WithComplexScenario_ExecutesAllStepsInOrder()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new CqrsScenarioTemplate("Complex CQRS Scenario", relay);

        var commandExecuted = false;
        var queryExecuted = false;
        var verificationCalled = false;

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

        // Build complex scenario
        template
            .SendCommand<TestCommand, TestResponse>(new TestCommand(), "Send Command")
            .WaitForProcessing(TimeSpan.FromMilliseconds(100), "Wait for processing")
            .SendQuery<TestQuery, TestResponse>(new TestQuery(), "Send Query")
            .VerifyState(async () =>
            {
                verificationCalled = true;
                return commandExecuted && queryExecuted;
            }, "Verify state");

        // Act
        var result = await template.ExecuteAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Complex CQRS Scenario", result.ScenarioName);
        Assert.Equal(4, result.StepResults.Count);

        // Verify step names
        Assert.Equal("Send Command", result.StepResults[0].StepName);
        Assert.Equal("Wait for processing", result.StepResults[1].StepName);
        Assert.Equal("Send Query", result.StepResults[2].StepName);
        Assert.Equal("Verify state", result.StepResults[3].StepName);

        // Verify all steps succeeded
        foreach (var stepResult in result.StepResults)
        {
            Assert.True(stepResult.Success, $"Step '{stepResult.StepName}' failed: {stepResult.Error}");
        }

        // Verify side effects occurred
        Assert.True(commandExecuted);
        Assert.True(queryExecuted);
        Assert.True(verificationCalled);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailingVerification_FailsScenario()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new CqrsScenarioTemplate("Failing Scenario", relay);

        relay.WithMockHandler<TestCommand, TestResponse>(builder =>
            builder.Returns((req) => new TestResponse { Result = "Command executed" }));

        template
            .SendCommand<TestCommand, TestResponse>(new TestCommand())
            .VerifyState(async () => await Task.FromResult(false), "Always fails");

        // Act
        var result = await template.ExecuteAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Verification failed", result.Error);
        Assert.Equal(2, result.StepResults.Count);
        Assert.True(result.StepResults[0].Success); // Command should succeed
        Assert.False(result.StepResults[1].Success); // Verification should fail
    }

    [Fact]
    public void FluentChaining_WorksCorrectly()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new CqrsScenarioTemplate("Chaining Test", relay);

        // Act - Test that all methods return the template for chaining
        var result = template
            .SendCommand<TestCommand, TestResponse>(new TestCommand())
            .SendQuery<TestQuery, TestResponse>(new TestQuery())
            .WaitForProcessing(TimeSpan.FromSeconds(1))
            .VerifyState(async () => await Task.FromResult(true));

        // Assert
        Assert.Same(template, result);
    }

    [Fact]
    public async Task SendCommand_ExecutesSuccessfully()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new CqrsScenarioTemplate("Test Scenario", relay);

        var commandHandled = false;
        relay.WithMockHandler<TestCommand, TestResponse>(builder =>
            builder.Returns((req) =>
            {
                commandHandled = true;
                return new TestResponse { Result = "Success" };
            }));

        template.SendCommand<TestCommand, TestResponse>(new TestCommand());

        // Act
        var result = await template.ExecuteAsync();

        // Assert
        Assert.True(result.Success);
        Assert.True(commandHandled);
    }

    [Fact]
    public async Task SendQuery_ExecutesSuccessfully()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new CqrsScenarioTemplate("Test Scenario", relay);

        var queryHandled = false;
        relay.WithMockHandler<TestQuery, TestResponse>(builder =>
            builder.Returns((req) =>
            {
                queryHandled = true;
                return new TestResponse { Result = "Success" };
            }));

        template.SendQuery<TestQuery, TestResponse>(new TestQuery());

        // Act
        var result = await template.ExecuteAsync();

        // Assert
        Assert.True(result.Success);
        Assert.True(queryHandled);
    }

    [Fact]
    public async Task WaitForProcessing_DelaysExecution()
    {
        // Arrange
        var relay = new TestRelay();
        var template = new CqrsScenarioTemplate("Test Scenario", relay);

        var startTime = DateTime.UtcNow;
        var waitDuration = TimeSpan.FromMilliseconds(200);

        template.WaitForProcessing(waitDuration);

        // Act
        var result = await template.ExecuteAsync();
        var endTime = DateTime.UtcNow;

        // Assert
        Assert.True(result.Success);
        Assert.True((endTime - startTime) >= waitDuration, $"Wait duration was {(endTime - startTime).TotalMilliseconds}ms, expected at least {waitDuration.TotalMilliseconds}ms");
    }

    // Test data classes
    private class TestCommand : IRequest<TestResponse> { }
    private class TestQuery : IRequest<TestResponse> { }
    private class TestResponse { public string Result { get; set; } = string.Empty; }
}