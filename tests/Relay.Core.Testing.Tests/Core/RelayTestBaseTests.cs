using System;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Testing.Tests;

public class RelayTestBaseTests : RelayTestBase
{
    [Fact]
    public void TestRelay_IsInitialized()
    {
        // TestRelay should be initialized by base class
        Assert.NotNull(TestRelay);
        Assert.NotNull(Services);
    }

    [Fact]
    public async Task RunScenarioAsync_ExecutesScenarioSuccessfully()
    {
        // Arrange & Act
        var result = await RunScenarioAsync("TestScenario", builder =>
        {
            builder.SendRequest(new TestRequest());
        });

        // Assert
        AssertScenarioSuccess(result);
    }

    [Fact]
    public async Task RunScenarioAsync_WithFailingStep_FailsScenario()
    {
        // Arrange & Act
        var result = await RunScenarioAsync("FailingScenario", builder =>
        {
            builder.Verify(async () =>
            {
                throw new System.InvalidOperationException("Test failure");
            });
        });

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task RunScenarioAsync_WithPublishNotificationStep_ExecutesSuccessfully()
    {
        // Arrange & Act
        var result = await RunScenarioAsync("PublishScenario", builder =>
        {
            builder.PublishNotification(new TestNotification());
        });

        // Assert
        AssertScenarioSuccess(result);
    }

    // [Fact]
    // public async Task RunScenarioAsync_WithStreamRequestStep_ExecutesSuccessfully()
    // {
    //     // Arrange & Act
    //     var result = await RunScenarioAsync("StreamScenario", builder =>
    //     {
    //         builder.StreamRequest(new TestStreamRequest());
    //     });

    //     // Assert
    //     AssertScenarioSuccess(result);
    // }

    [Fact]
    public async Task RunScenarioAsync_WithWaitStep_ExecutesSuccessfully()
    {
        // Arrange & Act
        var result = await RunScenarioAsync("WaitScenario", builder =>
        {
            builder.Wait(System.TimeSpan.FromMilliseconds(1));
        });

        // Assert
        AssertScenarioSuccess(result);
    }

    [Fact]
    public async Task RunScenarioAsync_WithSuccessfulVerifyStep_ExecutesSuccessfully()
    {
        // Arrange & Act
        var result = await RunScenarioAsync("VerifySuccessScenario", builder =>
        {
            builder.Verify(async () => true);
        });

        // Assert
        AssertScenarioSuccess(result);
    }

    [Fact]
    public void AssertScenarioSuccess_WithSuccessfulResult_Passes()
    {
        // Arrange
        var result = new ScenarioResult
        {
            Success = true
        };

        // Act & Assert - Should not throw
        AssertScenarioSuccess(result);
    }

    [Fact]
    public void AssertScenarioFailure_WithFailedResult_Passes()
    {
        // Arrange
        var result = new ScenarioResult
        {
            Success = false,
            Error = "Failed"
        };

        // Act & Assert - Should not throw
        AssertScenarioFailure<System.InvalidOperationException>(result);
    }

    public class TestRequest : Relay.Core.Contracts.Requests.IRequest
    {
        public string? Value { get; set; }
    }

    public class TestNotification : INotification
    {
    }

    public class TestStreamRequest : IStreamRequest<object>
    {
    }

    public class RelayTestBaseVirtualMethodTests : RelayTestBase
{
    private bool _configureTestRelayCalled;
    private bool _onTestInitializedCalled;
    private bool _onTestCleanupCalled;

    protected override void ConfigureTestRelay(TestRelay testRelay)
    {
        _configureTestRelayCalled = true;
        base.ConfigureTestRelay(testRelay);
    }

    protected override Task OnTestInitializedAsync()
    {
        _onTestInitializedCalled = true;
        return base.OnTestInitializedAsync();
    }

    protected override Task OnTestCleanupAsync()
    {
        _onTestCleanupCalled = true;
        return base.OnTestCleanupAsync();
    }

    [Fact]
    public void VirtualMethods_AreCalledDuringInitialization()
    {
        // Initialization happens in IAsyncLifetime, so by the time this test runs, they should be called
        Assert.True(_configureTestRelayCalled);
        Assert.True(_onTestInitializedCalled);
    }

    [Fact]
    public async Task VirtualMethods_AreCalledDuringCleanup()
    {
        // Force cleanup
        await DisposeAsync();

        Assert.True(_onTestCleanupCalled);
    }
}
}