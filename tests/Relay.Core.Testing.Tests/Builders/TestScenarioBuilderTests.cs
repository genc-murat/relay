using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Testing.Tests.Builders;

public class TestScenarioBuilderTests
{
    private readonly Mock<IRelay> _mockRelay;
    private readonly TestScenario _scenario;
    private readonly TestScenarioBuilder _builder;

    public TestScenarioBuilderTests()
    {
        _mockRelay = new Mock<IRelay>();
        _scenario = new TestScenario();
        _builder = new TestScenarioBuilder(_scenario, _mockRelay.Object);
    }

    [Fact]
    public void SendRequest_AddsStepToScenario()
    {
        // Arrange
        var request = new TestRequest();

        // Act
        _builder.SendRequest(request);

        // Assert
        Assert.Single(_scenario.Steps);
        var step = _scenario.Steps[0];
        Assert.Equal("Send Request", step.Name);
        Assert.Equal(StepType.SendRequest, step.Type);
        Assert.Equal(request, step.Request);
    }

    [Fact]
    public void SendRequest_WithCustomStepName_AddsStepWithCustomName()
    {
        // Arrange
        var request = new TestRequest();
        var customName = "Custom Send";

        // Act
        _builder.SendRequest(request, customName);

        // Assert
        Assert.Single(_scenario.Steps);
        var step = _scenario.Steps[0];
        Assert.Equal(customName, step.Name);
    }

    [Fact]
    public void SendRequest_NullStepName_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestRequest();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _builder.SendRequest(request, null!));
        Assert.Contains("Step name cannot be empty", exception.Message);
    }

    [Fact]
    public void SendRequest_EmptyStepName_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestRequest();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _builder.SendRequest(request, ""));
        Assert.Contains("Step name cannot be empty", exception.Message);
    }

    [Fact]
    public void PublishNotification_AddsStepToScenario()
    {
        // Arrange
        var notification = new TestNotification();

        // Act
        _builder.PublishNotification(notification);

        // Assert
        Assert.Single(_scenario.Steps);
        var step = _scenario.Steps[0];
        Assert.Equal("Publish Notification", step.Name);
        Assert.Equal(StepType.PublishNotification, step.Type);
        Assert.Equal(notification, step.Notification);
    }

    [Fact]
    public void PublishNotification_WithCustomStepName_AddsStepWithCustomName()
    {
        // Arrange
        var notification = new TestNotification();
        var customName = "Custom Publish";

        // Act
        _builder.PublishNotification(notification, customName);

        // Assert
        Assert.Single(_scenario.Steps);
        var step = _scenario.Steps[0];
        Assert.Equal(customName, step.Name);
    }

    [Fact]
    public void PublishNotification_NullNotification_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _builder.PublishNotification((TestNotification)null!));
        Assert.Equal("notification", exception.ParamName);
    }

    [Fact]
    public void PublishNotification_NullStepName_ThrowsArgumentException()
    {
        // Arrange
        var notification = new TestNotification();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _builder.PublishNotification(notification, null!));
        Assert.Contains("Step name cannot be empty", exception.Message);
    }

    [Fact]
    public void StreamRequest_AddsStepToScenario()
    {
        // Arrange
        var request = new TestRequest();

        // Act
        _builder.StreamRequest(request);

        // Assert
        Assert.Single(_scenario.Steps);
        var step = _scenario.Steps[0];
        Assert.Equal("Stream Request", step.Name);
        Assert.Equal(StepType.StreamRequest, step.Type);
        Assert.Equal(request, step.StreamRequest);
    }

    [Fact]
    public void StreamRequest_WithCustomStepName_AddsStepWithCustomName()
    {
        // Arrange
        var request = new TestRequest();
        var customName = "Custom Stream";

        // Act
        _builder.StreamRequest(request, customName);

        // Assert
        Assert.Single(_scenario.Steps);
        var step = _scenario.Steps[0];
        Assert.Equal(customName, step.Name);
    }

    [Fact]
    public void StreamRequest_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _builder.StreamRequest((TestRequest)null!));
        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public void StreamRequest_NullStepName_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestRequest();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _builder.StreamRequest(request, null!));
        Assert.Contains("Step name cannot be empty", exception.Message);
    }

    [Fact]
    public void Verify_AddsStepToScenario()
    {
        // Arrange
        Func<Task<bool>> verificationFunc = () => Task.FromResult(true);

        // Act
        _builder.Verify(verificationFunc);

        // Assert
        Assert.Single(_scenario.Steps);
        var step = _scenario.Steps[0];
        Assert.Equal("Verify", step.Name);
        Assert.Equal(StepType.Verify, step.Type);
        Assert.Equal(verificationFunc, step.VerificationFunc);
    }

    [Fact]
    public void Verify_WithCustomStepName_AddsStepWithCustomName()
    {
        // Arrange
        Func<Task<bool>> verificationFunc = () => Task.FromResult(true);
        var customName = "Custom Verify";

        // Act
        _builder.Verify(verificationFunc, customName);

        // Assert
        Assert.Single(_scenario.Steps);
        var step = _scenario.Steps[0];
        Assert.Equal(customName, step.Name);
    }

    [Fact]
    public void Verify_NullVerificationFunc_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _builder.Verify(null!));
        Assert.Equal("verificationFunc", exception.ParamName);
    }

    [Fact]
    public void Verify_NullStepName_ThrowsArgumentException()
    {
        // Arrange
        Func<Task<bool>> verificationFunc = () => Task.FromResult(true);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _builder.Verify(verificationFunc, null!));
        Assert.Contains("Step name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Wait_AddsStepToScenario()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(1);

        // Act
        _builder.Wait(duration);

        // Assert
        Assert.Single(_scenario.Steps);
        var step = _scenario.Steps[0];
        Assert.Equal("Wait", step.Name);
        Assert.Equal(StepType.Wait, step.Type);
        Assert.Equal(duration, step.WaitTime);
    }

    [Fact]
    public void Wait_WithCustomStepName_AddsStepWithCustomName()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(1);
        var customName = "Custom Wait";

        // Act
        _builder.Wait(duration, customName);

        // Assert
        Assert.Single(_scenario.Steps);
        var step = _scenario.Steps[0];
        Assert.Equal(customName, step.Name);
    }

    [Fact]
    public void Wait_ZeroDuration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var duration = TimeSpan.Zero;

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _builder.Wait(duration));
        Assert.Equal("duration", exception.ParamName);
        Assert.Contains("Wait duration must be greater than zero", exception.Message);
    }

    [Fact]
    public void Wait_NegativeDuration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(-1);

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _builder.Wait(duration));
        Assert.Equal("duration", exception.ParamName);
    }

    [Fact]
    public void Wait_NullStepName_ThrowsArgumentException()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(1);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _builder.Wait(duration, null!));
        Assert.Contains("Step name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Methods_ReturnBuilderForChaining()
    {
        // Act
        var result = _builder.SendRequest(new TestRequest());

        // Assert
        Assert.Same(_builder, result);
    }

    // Test classes
    public class TestRequest
    {
    }

    public class TestNotification : INotification
    {
    }
}