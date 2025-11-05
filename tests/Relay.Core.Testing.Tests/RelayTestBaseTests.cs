using System.Threading.Tasks;
using Xunit;

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
}