using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Transactions;
using Xunit;

namespace Relay.Core.Tests.Transactions;

public class TransactionActivitySourceRecordMethodsTests
{
    private readonly TransactionActivitySource _activitySource;
    private readonly List<Activity> _capturedActivities;

    public TransactionActivitySourceRecordMethodsTests()
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

    private void ClearCapturedActivities()
    {
        _capturedActivities.Clear();
    }

    #region RecordTransactionSuccess Tests

    [Fact]
    public void RecordTransactionSuccess_WithNullActivity_Should_Not_Throw_Exception()
    {
        // Arrange
        Activity? activity = null;
        var context = new MockTransactionContext();
        var duration = TimeSpan.FromSeconds(1.5);

        // Act & Assert
        var exception = Record.Exception(() => _activitySource.RecordTransactionSuccess(activity, context, duration));
        Assert.Null(exception);
    }

    [Fact]
    public void RecordTransactionSuccess_WithValidActivity_Should_SetCorrectTagsAndStatus()
    {
        // Arrange
        ClearCapturedActivities();
        using var activity = _activitySource.StartTransactionActivity(
            "tx-success",
            "TestCommand",
            IsolationLevel.ReadCommitted,
            0,
            false);
        
        var context = new MockTransactionContext();
        var duration = TimeSpan.FromSeconds(1.5);

        Assert.NotNull(activity);
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);

        // Act
        _activitySource.RecordTransactionSuccess(activity, context, duration);

        // Assert
        var durationTag = activity.TagObjects.FirstOrDefault(t => t.Key == "transaction.duration_ms");
        var statusTag = activity.TagObjects.FirstOrDefault(t => t.Key == "transaction.status");
        
        Assert.NotNull(durationTag);
        Assert.NotNull(statusTag);
        Assert.Equal(1500.0, durationTag.Value); // 1.5 seconds in milliseconds
        Assert.Equal("committed", statusTag.Value);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
    }

    [Fact]
    public void RecordTransactionSuccess_WithValidActivityAndZeroDuration_Should_SetCorrectTags()
    {
        // Arrange
        ClearCapturedActivities();
        using var activity = _activitySource.StartTransactionActivity(
            "tx-success-zero",
            "TestCommand",
            IsolationLevel.ReadCommitted,
            0,
            false);
        
        var context = new MockTransactionContext();
        var duration = TimeSpan.Zero;

        Assert.NotNull(activity);

        // Act
        _activitySource.RecordTransactionSuccess(activity, context, duration);

        // Assert
        var durationTag = activity.TagObjects.FirstOrDefault(t => t.Key == "transaction.duration_ms");
        var statusTag = activity.TagObjects.FirstOrDefault(t => t.Key == "transaction.status");
        
        Assert.NotNull(durationTag);
        Assert.NotNull(statusTag);
        Assert.Equal(0.0, durationTag.Value);
        Assert.Equal("committed", statusTag.Value);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
    }

    [Fact]
    public void RecordTransactionSuccess_WithValidActivityAndLargeDuration_Should_SetCorrectTags()
    {
        // Arrange
        ClearCapturedActivities();
        using var activity = _activitySource.StartTransactionActivity(
            "tx-success-long",
            "TestCommand",
            IsolationLevel.ReadCommitted,
            0,
            false);
        
        var context = new MockTransactionContext();
        var duration = TimeSpan.FromMinutes(10); // 600,000 ms

        Assert.NotNull(activity);

        // Act
        _activitySource.RecordTransactionSuccess(activity, context, duration);

        // Assert
        var durationTag = activity.TagObjects.FirstOrDefault(t => t.Key == "transaction.duration_ms");
        var statusTag = activity.TagObjects.FirstOrDefault(t => t.Key == "transaction.status");
        
        Assert.NotNull(durationTag);
        Assert.NotNull(statusTag);
        Assert.Equal(600000.0, durationTag.Value);
        Assert.Equal("committed", statusTag.Value);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
    }

    #endregion

    #region RecordTransactionTimeout Tests

    [Fact]
    public void RecordTransactionTimeout_WithNullActivity_Should_Not_Throw_Exception()
    {
        // Arrange
        Activity? activity = null;
        var context = new MockTransactionContext();
        var exception = new TimeoutException("Transaction timed out");

        // Act & Assert
        var recordedException = Record.Exception(() => _activitySource.RecordTransactionTimeout(activity, context, exception));
        Assert.Null(recordedException);
    }

    [Fact]
    public void RecordTransactionTimeout_WithValidActivityAndException_Should_SetCorrectTagsAndRecordException()
    {
        // Arrange
        ClearCapturedActivities();
        using var activity = _activitySource.StartTransactionActivity(
            "tx-timeout",
            "TestCommand",
            IsolationLevel.ReadCommitted,
            0,
            false);
        
        var context = new MockTransactionContext();
        var exception = new TimeoutException("Transaction timed out");

        Assert.NotNull(activity);
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);

        // Act
        _activitySource.RecordTransactionTimeout(activity, context, exception);

        // Assert
        var statusTag = activity.TagObjects.FirstOrDefault(t => t.Key == "transaction.status");
        Assert.NotNull(statusTag);
        Assert.Equal("timeout", statusTag.Value);
        
        // Verify exception was recorded
        var exceptionEvents = activity.Events.Where(e => e.Name == "exception").ToList();
        Assert.Single(exceptionEvents);
        
        var exceptionEvent = exceptionEvents[0];
        Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.type" && tag.Value?.ToString().Contains("TimeoutException") == true);
        Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.message" && tag.Value?.ToString() == "Transaction timed out");
        
        // Status should be error because of the exception
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
    }

    [Fact]
    public void RecordTransactionTimeout_WithValidActivityAndDifferentExceptionType_Should_SetCorrectTagsAndRecordException()
    {
        // Arrange
        ClearCapturedActivities();
        using var activity = _activitySource.StartTransactionActivity(
            "tx-timeout-diff",
            "TestCommand",
            IsolationLevel.ReadCommitted,
            0,
            false);
        
        var context = new MockTransactionContext();
        var exception = new InvalidOperationException("Operation timed out");

        Assert.NotNull(activity);

        // Act
        _activitySource.RecordTransactionTimeout(activity, context, exception);

        // Assert
        var statusTag = activity.TagObjects.FirstOrDefault(t => t.Key == "transaction.status");
        Assert.NotNull(statusTag);
        Assert.Equal("timeout", statusTag.Value);
        
        // Verify exception was recorded
        var exceptionEvents = activity.Events.Where(e => e.Name == "exception").ToList();
        Assert.Single(exceptionEvents);
        
        var exceptionEvent = exceptionEvents[0];
        Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.type" && tag.Value?.ToString().Contains("InvalidOperationException") == true);
        Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.message" && tag.Value?.ToString() == "Operation timed out");
        
        // Status should be error because of the exception
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
    }

    [Fact]
    public void RecordTransactionTimeout_WithValidActivityAndNullException_Should_SetCorrectTags()
    {
        // Arrange
        ClearCapturedActivities();
        using var activity = _activitySource.StartTransactionActivity(
            "tx-timeout-null",
            "TestCommand",
            IsolationLevel.ReadCommitted,
            0,
            false);
        
        var context = new MockTransactionContext();
        Exception? exception = null;

        Assert.NotNull(activity);

        // Act & Assert
        var recordedException = Record.Exception(() => _activitySource.RecordTransactionTimeout(activity, context, exception));
        Assert.Null(recordedException);
        
        // Status tag should still be set to timeout even if exception is null
        var statusTag = activity.TagObjects.FirstOrDefault(t => t.Key == "transaction.status");
        Assert.NotNull(statusTag);
        Assert.Equal("timeout", statusTag.Value);
        
        // No exception events should be added if exception is null
        var exceptionEvents = activity.Events.Where(e => e.Name == "exception").ToList();
        Assert.Empty(exceptionEvents);
    }

    #endregion

    #region RecordTransactionRollback Tests

    [Fact]
    public void RecordTransactionRollback_WithNullActivity_Should_Not_Throw_Exception()
    {
        // Arrange
        Activity? activity = null;
        var context = new MockTransactionContext();
        var exception = new InvalidOperationException("Transaction rolled back due to error");

        // Act & Assert
        var recordedException = Record.Exception(() => _activitySource.RecordTransactionRollback(activity, context, exception));
        Assert.Null(recordedException);
    }

    [Fact]
    public void RecordTransactionRollback_WithValidActivityAndException_Should_SetCorrectTagsAndRecordException()
    {
        // Arrange
        ClearCapturedActivities();
        using var activity = _activitySource.StartTransactionActivity(
            "tx-rollback",
            "TestCommand",
            IsolationLevel.ReadCommitted,
            0,
            false);
        
        var context = new MockTransactionContext();
        var exception = new InvalidOperationException("Transaction rolled back due to error");

        Assert.NotNull(activity);
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);

        // Act
        _activitySource.RecordTransactionRollback(activity, context, exception);

        // Assert
        var statusTag = activity.TagObjects.FirstOrDefault(t => t.Key == "transaction.status");
        Assert.NotNull(statusTag);
        Assert.Equal("rolled_back", statusTag.Value);
        
        // Verify exception was recorded
        var exceptionEvents = activity.Events.Where(e => e.Name == "exception").ToList();
        Assert.Single(exceptionEvents);
        
        var exceptionEvent = exceptionEvents[0];
        Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.type" && tag.Value?.ToString().Contains("InvalidOperationException") == true);
        Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.message" && tag.Value?.ToString() == "Transaction rolled back due to error");
        
        // Status should be error because of the exception
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
    }

    [Fact]
    public void RecordTransactionRollback_WithValidActivityAndDifferentException_Should_SetCorrectTags()
    {
        // Arrange
        ClearCapturedActivities();
        using var activity = _activitySource.StartTransactionActivity(
            "tx-rollback-diff",
            "TestCommand",
            IsolationLevel.ReadCommitted,
            0,
            false);
        
        var context = new MockTransactionContext();
        var exception = new ArgumentException("Invalid argument caused rollback");

        Assert.NotNull(activity);

        // Act
        _activitySource.RecordTransactionRollback(activity, context, exception);

        // Assert
        var statusTag = activity.TagObjects.FirstOrDefault(t => t.Key == "transaction.status");
        Assert.NotNull(statusTag);
        Assert.Equal("rolled_back", statusTag.Value);
        
        // Verify exception was recorded
        var exceptionEvents = activity.Events.Where(e => e.Name == "exception").ToList();
        Assert.Single(exceptionEvents);
        
        var exceptionEvent = exceptionEvents[0];
        Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.type" && tag.Value?.ToString().Contains("ArgumentException") == true);
        Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.message" && tag.Value?.ToString() == "Invalid argument caused rollback");
        
        // Status should be error because of the exception
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
    }

    [Fact]
    public void RecordTransactionRollback_WithValidActivityAndNullException_Should_SetCorrectTags()
    {
        // Arrange
        ClearCapturedActivities();
        using var activity = _activitySource.StartTransactionActivity(
            "tx-rollback-null",
            "TestCommand",
            IsolationLevel.ReadCommitted,
            0,
            false);
        
        var context = new MockTransactionContext();
        Exception? exception = null;

        Assert.NotNull(activity);

        // Act & Assert
        var recordedException = Record.Exception(() => _activitySource.RecordTransactionRollback(activity, context, exception));
        Assert.Null(recordedException);
        
        // Status tag should still be set to rolled_back even if exception is null
        var statusTag = activity.TagObjects.FirstOrDefault(t => t.Key == "transaction.status");
        Assert.NotNull(statusTag);
        Assert.Equal("rolled_back", statusTag.Value);
        
        // No exception events should be added if exception is null
        var exceptionEvents = activity.Events.Where(e => e.Name == "exception").ToList();
        Assert.Empty(exceptionEvents);
    }

    #endregion

    #region RecordTransactionFailure Tests

    [Fact]
    public void RecordTransactionFailure_WithNullActivity_Should_Not_Throw_Exception()
    {
        // Arrange
        Activity? activity = null;
        var context = new MockTransactionContext();
        var exception = new InvalidOperationException("Transaction failed");

        // Act & Assert
        var recordedException = Record.Exception(() => _activitySource.RecordTransactionFailure(activity, context, exception));
        Assert.Null(recordedException);
    }

    [Fact]
    public void RecordTransactionFailure_WithValidActivityAndException_Should_SetCorrectTagsAndRecordException()
    {
        // Arrange
        ClearCapturedActivities();
        using var activity = _activitySource.StartTransactionActivity(
            "tx-failure",
            "TestCommand",
            IsolationLevel.ReadCommitted,
            0,
            false);
        
        var context = new MockTransactionContext();
        var exception = new InvalidOperationException("Transaction failed");

        Assert.NotNull(activity);
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);

        // Act
        _activitySource.RecordTransactionFailure(activity, context, exception);

        // Assert
        var statusTag = activity.TagObjects.FirstOrDefault(t => t.Key == "transaction.status");
        Assert.NotNull(statusTag);
        Assert.Equal("failed", statusTag.Value);
        
        // Verify exception was recorded
        var exceptionEvents = activity.Events.Where(e => e.Name == "exception").ToList();
        Assert.Single(exceptionEvents);
        
        var exceptionEvent = exceptionEvents[0];
        Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.type" && tag.Value?.ToString().Contains("InvalidOperationException") == true);
        Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.message" && tag.Value?.ToString() == "Transaction failed");
        
        // Status should be error because of the exception
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
    }

    [Fact]
    public void RecordTransactionFailure_WithValidActivityAndDifferentException_Should_SetCorrectTags()
    {
        // Arrange
        ClearCapturedActivities();
        using var activity = _activitySource.StartTransactionActivity(
            "tx-failure-diff",
            "TestCommand",
            IsolationLevel.ReadCommitted,
            0,
            false);
        
        var context = new MockTransactionContext();
        var exception = new DivideByZeroException("Division by zero caused failure");

        Assert.NotNull(activity);

        // Act
        _activitySource.RecordTransactionFailure(activity, context, exception);

        // Assert
        var statusTag = activity.TagObjects.FirstOrDefault(t => t.Key == "transaction.status");
        Assert.NotNull(statusTag);
        Assert.Equal("failed", statusTag.Value);
        
        // Verify exception was recorded
        var exceptionEvents = activity.Events.Where(e => e.Name == "exception").ToList();
        Assert.Single(exceptionEvents);
        
        var exceptionEvent = exceptionEvents[0];
        Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.type" && tag.Value?.ToString().Contains("DivideByZeroException") == true);
        Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.message" && tag.Value?.ToString() == "Division by zero caused failure");
        
        // Status should be error because of the exception
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
    }

    [Fact]
    public void RecordTransactionFailure_WithValidActivityAndNullException_Should_SetCorrectTags()
    {
        // Arrange
        ClearCapturedActivities();
        using var activity = _activitySource.StartTransactionActivity(
            "tx-failure-null",
            "TestCommand",
            IsolationLevel.ReadCommitted,
            0,
            false);
        
        var context = new MockTransactionContext();
        Exception? exception = null;

        Assert.NotNull(activity);

        // Act & Assert
        var recordedException = Record.Exception(() => _activitySource.RecordTransactionFailure(activity, context, exception));
        Assert.Null(recordedException);
        
        // Status tag should still be set to failed even if exception is null
        var statusTag = activity.TagObjects.FirstOrDefault(t => t.Key == "transaction.status");
        Assert.NotNull(statusTag);
        Assert.Equal("failed", statusTag.Value);
        
        // No exception events should be added if exception is null
        var exceptionEvents = activity.Events.Where(e => e.Name == "exception").ToList();
        Assert.Empty(exceptionEvents);
    }

    #endregion
}

public class MockTransactionContext : ITransactionContext
{
    public string TransactionId { get; set; } = "mock-tx-id";
    public bool IsActive { get; set; } = true;
    public int NestingLevel { get; set; } = 0;
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
    public bool IsReadOnly { get; set; } = false;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public Relay.Core.Transactions.IRelayDbTransaction? CurrentTransaction { get; set; } = null;
    
    public void Commit() { }
    public void Rollback() { }
    public void RollbackToSavepoint(string savepointName) { }
    public string CreateSavepoint(string name) => name;
    
    public Task<ISavepoint> CreateSavepointAsync(string name, CancellationToken cancellationToken = default) 
        => Task.FromResult<ISavepoint>(new MockSavepoint(name));
    
    public Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default) 
        => Task.CompletedTask;
}

public class MockSavepoint : ISavepoint
{
    public string Name { get; }
    public DateTime CreatedAt { get; }
    
    public MockSavepoint(string name)
    {
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }
    
    public Task RollbackAsync(CancellationToken cancellationToken = default) 
        => Task.CompletedTask;
    
    public Task ReleaseAsync(CancellationToken cancellationToken = default) 
        => Task.CompletedTask;
    
    public ValueTask DisposeAsync() 
        => new ValueTask();
}