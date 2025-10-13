using Relay.CLI.Migration;
using Xunit;

namespace Relay.CLI.Tests.Migration;

public class MigrationStatusTests
{
    [Fact]
    public void MigrationStatus_HasNotStartedValue()
    {
        // Arrange & Act
        var status = MigrationStatus.NotStarted;

        // Assert
        Assert.Equal(MigrationStatus.NotStarted, status);
    }

    [Fact]
    public void MigrationStatus_HasInProgressValue()
    {
        // Arrange & Act
        var status = MigrationStatus.InProgress;

        // Assert
        Assert.Equal(MigrationStatus.InProgress, status);
    }

    [Fact]
    public void MigrationStatus_HasSuccessValue()
    {
        // Arrange & Act
        var status = MigrationStatus.Success;

        // Assert
        Assert.Equal(MigrationStatus.Success, status);
    }

    [Fact]
    public void MigrationStatus_HasFailedValue()
    {
        // Arrange & Act
        var status = MigrationStatus.Failed;

        // Assert
        Assert.Equal(MigrationStatus.Failed, status);
    }

    [Fact]
    public void MigrationStatus_HasPreviewValue()
    {
        // Arrange & Act
        var status = MigrationStatus.Preview;

        // Assert
        Assert.Equal(MigrationStatus.Preview, status);
    }

    [Fact]
    public void MigrationStatus_HasCancelledValue()
    {
        // Arrange & Act
        var status = MigrationStatus.Cancelled;

        // Assert
        Assert.Equal(MigrationStatus.Cancelled, status);
    }

    [Fact]
    public void MigrationStatus_CanBeCompared()
    {
        // Arrange
        var status1 = MigrationStatus.Success;
        var status2 = MigrationStatus.Success;
        var status3 = MigrationStatus.Failed;

        // Assert
        Assert.Equal(status1, status2);
        Assert.NotEqual(status1, status3);
    }

    [Fact]
    public void MigrationStatus_CanBeUsedInSwitchExpression()
    {
        // Arrange & Act
        var result = MigrationStatus.Preview switch
        {
            MigrationStatus.NotStarted => "not started",
            MigrationStatus.InProgress => "in progress",
            MigrationStatus.Success => "success",
            MigrationStatus.Failed => "failed",
            MigrationStatus.Preview => "preview",
            MigrationStatus.Cancelled => "cancelled",
            _ => "unknown"
        };

        // Assert
        Assert.Equal("preview", result);
    }

    [Fact]
    public void MigrationStatus_CancelledCanBeUsedInSwitchExpression()
    {
        // Arrange & Act
        var result = MigrationStatus.Cancelled switch
        {
            MigrationStatus.NotStarted => "not started",
            MigrationStatus.InProgress => "in progress",
            MigrationStatus.Success => "success",
            MigrationStatus.Failed => "failed",
            MigrationStatus.Preview => "preview",
            MigrationStatus.Cancelled => "cancelled",
            _ => "unknown"
        };

        // Assert
        Assert.Equal("cancelled", result);
    }
}
