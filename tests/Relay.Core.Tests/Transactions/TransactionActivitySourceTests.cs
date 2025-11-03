using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Relay.Core.Transactions;
using Xunit;

namespace Relay.Core.Tests.Transactions;

public class TransactionActivitySourceTests
{
    private readonly TransactionActivitySource _activitySource;
    private readonly List<Activity> _capturedActivities;

    public TransactionActivitySourceTests()
    {
        _capturedActivities = new List<Activity>();
        
        // Set up a listener to capture activities
        ActivitySource.AddActivityListener(new ActivityListener
        {
            ShouldListenTo = (source) => source.Name == TransactionActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => _capturedActivities.Add(activity)
        });
        
        _activitySource = new TransactionActivitySource();
    }

    [Fact]
    public void Constructor_Should_Initialize_ActivitySource()
    {
        // Arrange & Act
        var source = new TransactionActivitySource();

        // Assert
        Assert.NotNull(source);
    }

    [Fact]
    public void StartTransactionActivity_WithAllParameters_Should_Create_Activity_With_Tags()
    {
        // Arrange
        var transactionId = "tx-123";
        var requestType = "CreateOrderCommand";
        var isolationLevel = IsolationLevel.ReadCommitted;
        var nestingLevel = 0;
        var isReadOnly = false;
        var timeoutSeconds = 30;

        // Act
        using var activity = _activitySource.StartTransactionActivity(
            transactionId,
            requestType,
            isolationLevel,
            nestingLevel,
            isReadOnly,
            timeoutSeconds);

        // Assert
        var capturedActivity = _capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.Equal("relay.transaction", capturedActivity.DisplayName);
        Assert.Equal(ActivityKind.Internal, capturedActivity.Kind);
        Assert.Equal(transactionId, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.id").Value);
        Assert.Equal(requestType, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.request_type").Value);
        Assert.Equal(isolationLevel.ToString(), capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.isolation_level").Value);
        Assert.Equal(nestingLevel, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.nesting_level").Value);
        Assert.Equal(isReadOnly, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.is_readonly").Value);
        Assert.Equal(timeoutSeconds, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.timeout_seconds").Value);
    }

    [Fact]
    public void StartTransactionActivity_WithoutTimeout_Should_Create_Activity_Without_Timeout_Tag()
    {
        // Arrange
        var transactionId = "tx-456";
        var requestType = "UpdateProductCommand";
        var isolationLevel = IsolationLevel.Serializable;
        var nestingLevel = 1;
        var isReadOnly = true;

        // Act
        using var activity = _activitySource.StartTransactionActivity(
            transactionId,
            requestType,
            isolationLevel,
            nestingLevel,
            isReadOnly);

        // Assert
        var capturedActivity = _capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.Equal(transactionId, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.id").Value);
        Assert.Equal(requestType, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.request_type").Value);
        Assert.Equal(isolationLevel.ToString(), capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.isolation_level").Value);
        Assert.Equal(nestingLevel, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.nesting_level").Value);
        Assert.Equal(isReadOnly, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.is_readonly").Value);
        Assert.Null(capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.timeout_seconds").Value);
    }

    [Fact]
    public void StartTransactionActivity_WithConfiguration_Should_Create_Activity_With_Configuration_Values()
    {
        // Arrange
        var requestType = "DeleteUserCommand";
        var configuration = new TransactionConfiguration(
            IsolationLevel.RepeatableRead,
            TimeSpan.FromSeconds(45),
            false);

        // Act
        using var activity = _activitySource.StartTransactionActivity(requestType, configuration);

        // Assert
        var capturedActivity = _capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.Equal("relay.transaction", capturedActivity.DisplayName);
        Assert.Equal(requestType, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.request_type").Value);
        Assert.Equal(configuration.IsolationLevel.ToString(), capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.isolation_level").Value);
        Assert.Equal(0, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.nesting_level").Value);
        Assert.Equal(configuration.IsReadOnly, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.is_readonly").Value);
        Assert.Equal(45, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.timeout_seconds").Value);
        Assert.NotNull(capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.id").Value);
    }

    [Fact]
    public void StartTransactionActivity_WithConfiguration_NoTimeout_Should_Create_Activity_Without_Timeout_Tag()
    {
        // Arrange
        var requestType = "ReadProductQuery";
        var configuration = new TransactionConfiguration(
            IsolationLevel.ReadCommitted,
            System.Threading.Timeout.InfiniteTimeSpan,
            true);

        // Act
        using var activity = _activitySource.StartTransactionActivity(requestType, configuration);

        // Assert
        var capturedActivity = _capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.Equal(requestType, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.request_type").Value);
        Assert.Equal(configuration.IsolationLevel.ToString(), capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.isolation_level").Value);
        Assert.Equal(configuration.IsReadOnly, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.is_readonly").Value);
        Assert.Null(capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.timeout_seconds").Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void StartTransactionActivity_WithInvalidTransactionId_Should_Handle_Gracefully(string transactionId)
    {
        // Arrange
        var requestType = "TestCommand";
        var isolationLevel = IsolationLevel.ReadCommitted;
        var nestingLevel = 0;
        var isReadOnly = false;

        // Act
        using var activity = _activitySource.StartTransactionActivity(
            transactionId,
            requestType,
            isolationLevel,
            nestingLevel,
            isReadOnly);

        // Assert
        var capturedActivity = _capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.Equal(transactionId, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.id").Value);
        Assert.Equal(requestType, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.request_type").Value);
        Assert.Equal(nestingLevel, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.nesting_level").Value);
    }

    [Fact]
    public void StartTransactionActivity_WithAllIsolationLevels_Should_Create_Activity_With_Correct_Tags()
    {
        // Arrange
        var isolationLevels = Enum.GetValues<IsolationLevel>();

        foreach (var isolationLevel in isolationLevels)
        {
            // Act
            using var activity = _activitySource.StartTransactionActivity(
                "tx-isolation",
                "TestCommand",
                isolationLevel,
                0,
                false);

            // Assert
            var capturedActivity = _capturedActivities.LastOrDefault();
            Assert.NotNull(capturedActivity);
            Assert.Equal(isolationLevel.ToString(), capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.isolation_level").Value);
        }
    }

    [Fact]
    public void StartTransactionActivity_MultipleCalls_Should_Create_Different_Activities()
    {
        // Arrange
        var transactionId1 = "tx-1";
        var transactionId2 = "tx-2";

        // Act
        using var activity1 = _activitySource.StartTransactionActivity(
            transactionId1,
            "Command1",
            IsolationLevel.ReadCommitted,
            0,
            false);

        using var activity2 = _activitySource.StartTransactionActivity(
            transactionId2,
            "Command2",
            IsolationLevel.Serializable,
            1,
            true);

        // Assert
        Assert.Equal(2, _capturedActivities.Count);
        var capturedActivity1 = _capturedActivities[0];
        var capturedActivity2 = _capturedActivities[1];
        Assert.NotNull(capturedActivity1);
        Assert.NotNull(capturedActivity2);
        Assert.NotSame(capturedActivity1, capturedActivity2);
        Assert.Equal(transactionId1, capturedActivity1.TagObjects.FirstOrDefault(t => t.Key == "transaction.id").Value);
        Assert.Equal(transactionId2, capturedActivity2.TagObjects.FirstOrDefault(t => t.Key == "transaction.id").Value);
    }

    [Fact]
    public void StartTransactionActivity_Activity_Disposal_Should_Work_Correctly()
    {
        // Arrange
        var transactionId = "tx-dispose";

        // Act
        var activity = _activitySource.StartTransactionActivity(
            transactionId,
            "TestCommand",
            IsolationLevel.ReadCommitted,
            0,
            false);

        // Assert
        var capturedActivity = _capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.False(capturedActivity.IsStopped);

        // Act
        activity.Dispose();

        // Assert
        Assert.True(capturedActivity.IsStopped);
    }
}