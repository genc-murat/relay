using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Relay.Core.Transactions;
using Xunit;

namespace Relay.Core.Tests.Transactions;

public class TransactionActivitySourceStartTransactionActivityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void StartTransactionActivity_WithVariousNestingLevels_Should_Create_Activity_With_Correct_Tags(int nestingLevel)
    {
        // Arrange
        var capturedActivities = new List<Activity>();
        
        // Set up a listener to capture activities
        using var listener = new ActivityListener
        {
            ShouldListenTo = (source) => source.Name == TransactionActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);
        
        var activitySource = new TransactionActivitySource();

        // Act
        using var activity = activitySource.StartTransactionActivity(
            "tx-nesting",
            "TestCommand",
            IsolationLevel.ReadCommitted,
            nestingLevel,
            false);

        // Assert
        var capturedActivity = capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        
        // Get the specific tag we're looking for
        var nestingTag = capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.nesting_level");
        Assert.NotNull(nestingTag.Value);
        Assert.Equal(nestingLevel, nestingTag.Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void StartTransactionActivity_WithVariousReadOnlyValues_Should_Create_Activity_With_Correct_Tags(bool isReadOnly)
    {
        // Arrange
        var capturedActivities = new List<Activity>();
        
        // Set up a listener to capture activities
        using var listener = new ActivityListener
        {
            ShouldListenTo = (source) => source.Name == TransactionActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);
        
        var activitySource = new TransactionActivitySource();

        // Act
        using var activity = activitySource.StartTransactionActivity(
            "tx-readonly",
            "TestCommand",
            IsolationLevel.ReadCommitted,
            0,
            isReadOnly);

        // Assert
        var capturedActivity = capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.Equal(isReadOnly, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.is_readonly").Value);
    }

    [Fact]
    public void StartTransactionActivity_WithMaxTimeoutValue_Should_Create_Activity_With_Correct_Tags()
    {
        // Arrange
        var capturedActivities = new List<Activity>();
        
        // Set up a listener to capture activities
        using var listener = new ActivityListener
        {
            ShouldListenTo = (source) => source.Name == TransactionActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);
        
        var activitySource = new TransactionActivitySource();
        var timeoutSeconds = int.MaxValue;

        // Act
        using var activity = activitySource.StartTransactionActivity(
            "tx-max-timeout",
            "TestCommand",
            IsolationLevel.ReadCommitted,
            0,
            false,
            timeoutSeconds);

        // Assert
        var capturedActivity = capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.Equal(timeoutSeconds, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.timeout_seconds").Value);
    }

    [Fact]
    public void StartTransactionActivity_WithVeryLargeTimeout_Should_Create_Activity_With_Correct_Tags()
    {
        // Arrange
        var capturedActivities = new List<Activity>();
        
        // Set up a listener to capture activities
        using var listener = new ActivityListener
        {
            ShouldListenTo = (source) => source.Name == TransactionActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);
        
        var activitySource = new TransactionActivitySource();
        var timeoutSeconds = 999999;

        // Act
        using var activity = activitySource.StartTransactionActivity(
            "tx-large-timeout",
            "TestCommand",
            IsolationLevel.ReadCommitted,
            0,
            false,
            timeoutSeconds);

        // Assert
        var capturedActivity = capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.Equal(timeoutSeconds, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.timeout_seconds").Value);
    }

    [Fact]
    public void StartTransactionActivity_WithZeroTimeout_Should_Create_Activity_With_Zero_Timeout_Tag()
    {
        // Arrange
        var capturedActivities = new List<Activity>();
        
        // Set up a listener to capture activities
        using var listener = new ActivityListener
        {
            ShouldListenTo = (source) => source.Name == TransactionActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);
        
        var activitySource = new TransactionActivitySource();
        var timeoutSeconds = 0;

        // Act
        using var activity = activitySource.StartTransactionActivity(
            "tx-zero-timeout",
            "TestCommand",
            IsolationLevel.ReadCommitted,
            0,
            false,
            timeoutSeconds);

        // Assert
        var capturedActivity = capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.Equal(timeoutSeconds, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.timeout_seconds").Value);
    }

    [Fact]
    public void StartTransactionActivity_WithConfiguration_TimeoutZero_Should_Create_Activity_Without_Timeout_Tag()
    {
        // Arrange
        var capturedActivities = new List<Activity>();
        
        // Set up a listener to capture activities
        using var listener = new ActivityListener
        {
            ShouldListenTo = (source) => source.Name == TransactionActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);
        
        var activitySource = new TransactionActivitySource();
        var requestType = "TestCommand";
        var configuration = new TransactionConfiguration(
            IsolationLevel.ReadCommitted,
            TimeSpan.Zero,  // Zero timeout should result in null timeoutSeconds
            false);

        // Act
        using var activity = activitySource.StartTransactionActivity(requestType, configuration);

        // Assert
        var capturedActivity = capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.Equal(requestType, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.request_type").Value);
        Assert.Null(capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.timeout_seconds").Value);
    }

    [Fact]
    public void StartTransactionActivity_WithConfiguration_TimeoutInfiniteTimeSpan_Should_Create_Activity_Without_Timeout_Tag()
    {
        // Arrange
        var capturedActivities = new List<Activity>();
        
        // Set up a listener to capture activities
        using var listener = new ActivityListener
        {
            ShouldListenTo = (source) => source.Name == TransactionActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);
        
        var activitySource = new TransactionActivitySource();
        var requestType = "TestCommand";
        var configuration = new TransactionConfiguration(
            IsolationLevel.ReadCommitted,
            System.Threading.Timeout.InfiniteTimeSpan,  // Infinite timeout should result in null timeoutSeconds
            false);

        // Act
        using var activity = activitySource.StartTransactionActivity(requestType, configuration);

        // Assert
        var capturedActivity = capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.Equal(requestType, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.request_type").Value);
        Assert.Null(capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.timeout_seconds").Value);
    }

    [Fact]
    public void StartTransactionActivity_WithConfiguration_NegativeTimeout_Should_Create_Activity_Without_Timeout_Tag()
    {
        // Arrange
        var capturedActivities = new List<Activity>();
        
        // Set up a listener to capture activities
        using var listener = new ActivityListener
        {
            ShouldListenTo = (source) => source.Name == TransactionActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);
        
        var activitySource = new TransactionActivitySource();
        var requestType = "TestCommand";
        var configuration = new TransactionConfiguration(
            IsolationLevel.ReadCommitted,
            TimeSpan.FromSeconds(-5),  // Negative timeout will result in null timeoutSeconds
            false);

        // Act
        using var activity = activitySource.StartTransactionActivity(requestType, configuration);

        // Assert
        var capturedActivity = capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.Equal(requestType, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.request_type").Value);
        Assert.Null(capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.timeout_seconds").Value);
    }

    [Fact]
    public void StartTransactionActivity_WithConfiguration_NegativeTimeoutSeconds_Should_Create_Activity_With_Negative_Timeout_Tag()
    {
        // Arrange
        var capturedActivities = new List<Activity>();
        
        // Set up a listener to capture activities
        using var listener = new ActivityListener
        {
            ShouldListenTo = (source) => source.Name == TransactionActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);
        
        var activitySource = new TransactionActivitySource();
        var requestType = "TestCommand";
        var transactionId = "tx-123";
        var timeoutSeconds = -10;

        // Act
        using var activity = activitySource.StartTransactionActivity(
            transactionId,
            requestType,
            IsolationLevel.ReadCommitted,
            0,
            false,
            timeoutSeconds);

        // Assert
        var capturedActivity = capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.Equal(requestType, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.request_type").Value);
        Assert.Equal(timeoutSeconds, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.timeout_seconds").Value);
    }

    [Fact]
    public void StartTransactionActivity_WithAllPossibleIsolationLevels_Should_Create_Activity_With_Correct_Tags()
    {
        // Arrange
        var capturedActivities = new List<Activity>();
        
        // Set up a listener to capture activities
        using var listener = new ActivityListener
        {
            ShouldListenTo = (source) => source.Name == TransactionActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);
        
        var activitySource = new TransactionActivitySource();
        var isolationLevels = Enum.GetValues<IsolationLevel>();

        // Act & Assert
        foreach (var isolationLevel in isolationLevels)
        {
            using var activity = activitySource.StartTransactionActivity(
                $"tx-{isolationLevel}",
                "TestCommand",
                isolationLevel,
                0,
                false);

            // Assert
            var capturedActivity = capturedActivities.LastOrDefault();
            Assert.NotNull(capturedActivity);
            Assert.Equal(isolationLevel.ToString(), capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.isolation_level").Value);
            
            // Clear the list for next iteration (or keep track separately)
            capturedActivities.Clear();
        }
    }
}