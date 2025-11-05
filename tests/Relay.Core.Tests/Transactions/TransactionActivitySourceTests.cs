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
            _activitySource = new TransactionActivitySource();
        }

        private IDisposable SetupActivityListener()
        {
            // Set up a listener to capture activities
            var listener = new ActivityListener
            {
                ShouldListenTo = (source) => source.Name == TransactionActivitySource.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
                ActivityStarted = activity => _capturedActivities.Add(activity)
            };
            ActivitySource.AddActivityListener(listener);
            return listener;
        }

        private void ClearCapturedActivities()
        {
            _capturedActivities.Clear();
        }

        [Fact]
        public void Constructor_Should_Initialize_ActivitySource()
        {
            // Arrange & Act
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var source = new TransactionActivitySource();

            // Assert
            Assert.NotNull(source);
        }

        [Fact]
        public void StartTransactionActivity_WithoutTimeout_Should_Create_Activity_Without_Timeout_Tag()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
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
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
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
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
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
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
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
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
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
        
        // Clean up all activities created in this test to avoid affecting other tests
        ClearCapturedActivities();
    }

        [Fact]
        public void StartTransactionActivity_MultipleCalls_Should_Create_Different_Activities()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
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

        // Assert - Filter to only activities with our expected transaction IDs
        var relevantActivities = _capturedActivities
            .Where(a => a.TagObjects.Any(t => t.Key == "transaction.id" && 
                       (t.Value?.ToString() == transactionId1 || t.Value?.ToString() == transactionId2)))
            .ToList();
            
        Assert.Equal(2, relevantActivities.Count);
        
        var capturedActivity1 = relevantActivities.FirstOrDefault(a => 
            a.TagObjects.Any(t => t.Key == "transaction.id" && t.Value?.ToString() == transactionId1));
        var capturedActivity2 = relevantActivities.FirstOrDefault(a => 
            a.TagObjects.Any(t => t.Key == "transaction.id" && t.Value?.ToString() == transactionId2));
            
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
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
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

        [Fact]
        public void StartSavepointActivity_WithAllParameters_Should_Create_Activity_With_Tags()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "tx-123";
        var savepointName = "sp_main";
        var operation = "Create";

        // Act
        using var activity = _activitySource.StartSavepointActivity(
            transactionId,
            savepointName,
            operation);

        // Assert
        var capturedActivity = _capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.Equal("relay.transaction.savepoint", capturedActivity.DisplayName);
        Assert.Equal(ActivityKind.Internal, capturedActivity.Kind);
        Assert.Equal(transactionId, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.id").Value);
        Assert.Equal(savepointName, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "savepoint.name").Value);
        Assert.Equal(operation, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "savepoint.operation").Value);
    }

        [Theory]
        [InlineData("Create")]
        [InlineData("Rollback")]
        [InlineData("Release")]
        [InlineData("CustomOperation")]
        public void StartSavepointActivity_WithDifferentOperations_Should_Create_Activity_With_Correct_Operation_Tag(string operation)
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "tx-456";
            var savepointName = "sp_test";

        // Act
        using var activity = _activitySource.StartSavepointActivity(
            transactionId,
            savepointName,
            operation);

            // Assert
            var capturedActivity = _capturedActivities.LastOrDefault();
            Assert.NotNull(capturedActivity);
            
            var operationTag = capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "savepoint.operation");
            Assert.True(operationTag.Key != null, $"savepoint.operation tag not found. Available tags: {string.Join(", ", capturedActivity.TagObjects.Select(t => $"{t.Key}={t.Value}"))}");
            Assert.Equal(operation, operationTag.Value);
    }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void StartSavepointActivity_WithInvalidTransactionId_Should_Handle_Gracefully(string transactionId)
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var savepointName = "sp_invalid";
        var operation = "Create";

        // Act
        using var activity = _activitySource.StartSavepointActivity(
            transactionId,
            savepointName,
            operation);

        // Assert
        var capturedActivity = _capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.Equal(transactionId, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.id").Value);
        Assert.Equal(savepointName, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "savepoint.name").Value);
        Assert.Equal(operation, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "savepoint.operation").Value);
    }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void StartSavepointActivity_WithInvalidSavepointName_Should_Handle_Gracefully(string savepointName)
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "tx-789";
        var operation = "Rollback";

        // Act
        using var activity = _activitySource.StartSavepointActivity(
            transactionId,
            savepointName,
            operation);

        // Assert
        var capturedActivity = _capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.Equal(transactionId, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.id").Value);
        Assert.Equal(savepointName, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "savepoint.name").Value);
        Assert.Equal(operation, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "savepoint.operation").Value);
    }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void StartSavepointActivity_WithInvalidOperation_Should_Handle_Gracefully(string operation)
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "tx-invalid-op";
        var savepointName = "sp_invalid_op";

        // Act
        using var activity = _activitySource.StartSavepointActivity(
            transactionId,
            savepointName,
            operation);

        // Assert
        var capturedActivity = _capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.Equal(transactionId, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.id").Value);
        Assert.Equal(savepointName, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "savepoint.name").Value);
        Assert.Equal(operation, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "savepoint.operation").Value);
    }

        [Fact]
        public void StartSavepointActivity_WithLongSavepointName_Should_Create_Activity_With_Correct_Tags()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "tx-long";
        var savepointName = "this_is_a_very_long_savepoint_name_that_tests_boundary_conditions";
        var operation = "Create";

        // Act
        using var activity = _activitySource.StartSavepointActivity(
            transactionId,
            savepointName,
            operation);

        // Assert
        var capturedActivity = _capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.Equal(transactionId, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.id").Value);
        Assert.Equal(savepointName, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "savepoint.name").Value);
        Assert.Equal(operation, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "savepoint.operation").Value);
    }

        [Fact]
        public void StartSavepointActivity_WithSpecialCharacters_Should_Create_Activity_With_Correct_Tags()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "tx-special";
        var savepointName = "sp_special-chars_123";
        var operation = "Create-Special";

        // Act
        using var activity = _activitySource.StartSavepointActivity(
            transactionId,
            savepointName,
            operation);

        // Assert
        var capturedActivity = _capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.Equal(transactionId, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.id").Value);
        Assert.Equal(savepointName, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "savepoint.name").Value);
        Assert.Equal(operation, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "savepoint.operation").Value);
    }

        [Fact]
        public void StartSavepointActivity_MultipleCalls_Should_Create_Different_Activities()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId1 = "tx-sp-1";
        var transactionId2 = "tx-sp-2";

        // Act
        using var activity1 = _activitySource.StartSavepointActivity(
            transactionId1,
            "sp1",
            "Create");

        using var activity2 = _activitySource.StartSavepointActivity(
            transactionId2,
            "sp2",
            "Rollback");

        // Assert
        Assert.Equal(2, _capturedActivities.Count);
        var capturedActivity1 = _capturedActivities[0];
        var capturedActivity2 = _capturedActivities[1];
        Assert.NotNull(capturedActivity1);
        Assert.NotNull(capturedActivity2);
        Assert.NotSame(capturedActivity1, capturedActivity2);
        Assert.Equal(transactionId1, capturedActivity1.TagObjects.FirstOrDefault(t => t.Key == "transaction.id").Value);
        Assert.Equal(transactionId2, capturedActivity2.TagObjects.FirstOrDefault(t => t.Key == "transaction.id").Value);
        Assert.Equal("sp1", capturedActivity1.TagObjects.FirstOrDefault(t => t.Key == "savepoint.name").Value);
        Assert.Equal("sp2", capturedActivity2.TagObjects.FirstOrDefault(t => t.Key == "savepoint.name").Value);
        Assert.Equal("Create", capturedActivity1.TagObjects.FirstOrDefault(t => t.Key == "savepoint.operation").Value);
        Assert.Equal("Rollback", capturedActivity2.TagObjects.FirstOrDefault(t => t.Key == "savepoint.operation").Value);
    }

        [Fact]
        public void StartSavepointActivity_Activity_Disposal_Should_Work_Correctly()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "tx-dispose-sp";

        // Act
        var activity = _activitySource.StartSavepointActivity(
            transactionId,
            "sp_dispose",
            "Release");

        // Assert
        var capturedActivity = _capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.False(capturedActivity.IsStopped);

        // Act
        activity.Dispose();

        // Assert
        Assert.True(capturedActivity.IsStopped);
    }

        [Fact]
        public void StartSavepointActivity_WithNumericSavepointName_Should_Create_Activity_With_Correct_Tags()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "tx-numeric";
        var savepointName = "sp_12345";
        var operation = "Create";

        // Act
        using var activity = _activitySource.StartSavepointActivity(
            transactionId,
            savepointName,
            operation);

        // Assert
        var capturedActivity = _capturedActivities.LastOrDefault();
        Assert.NotNull(capturedActivity);
        Assert.Equal(transactionId, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.id").Value);
        Assert.Equal(savepointName, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "savepoint.name").Value);
        Assert.Equal(operation, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "savepoint.operation").Value);
    }

        [Fact]
        public void StartRetryActivity_WithAllParameters_Should_Create_Activity_With_All_Tags()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "tx-retry-123";
            var requestType = "CreateOrderCommand";
            var attemptNumber = 2;
            var maxRetries = 5;
            var delayMs = 1000;

            // Act
            using var activity = _activitySource.StartRetryActivity(
                transactionId,
                requestType,
                attemptNumber,
                maxRetries,
                delayMs);

            // Assert
            var capturedActivity = _capturedActivities.LastOrDefault();
            Assert.NotNull(capturedActivity);
            Assert.Equal("relay.transaction.retry", capturedActivity.DisplayName);
            Assert.Equal(ActivityKind.Internal, capturedActivity.Kind);
            
            var transactionIdTag = capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.id");
            var requestTypeTag = capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.request_type");
            var attemptTag = capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.attempt");
            var maxAttemptsTag = capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.max_attempts");
            var delayTag = capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.delay_ms");
            
            Assert.NotNull(transactionIdTag);
            Assert.NotNull(requestTypeTag);
            Assert.NotNull(attemptTag);
            Assert.NotNull(maxAttemptsTag);
            Assert.NotNull(delayTag);
            
            Assert.Equal(transactionId, transactionIdTag.Value);
            Assert.Equal(requestType, requestTypeTag.Value);
            Assert.Equal(attemptNumber, attemptTag.Value);
            Assert.Equal(maxRetries, maxAttemptsTag.Value);
            Assert.Equal(delayMs, delayTag.Value);
        }

        [Theory]
        [InlineData(1, 3, 100)]
        [InlineData(2, 5, 500)]
        [InlineData(3, 10, 2000)]
        [InlineData(5, 5, 0)]
        [InlineData(1, 1, 50)]
        public void StartRetryActivity_WithDifferentRetryParameters_Should_Create_Activity_With_Correct_Tags(
            int attemptNumber, int maxRetries, int delayMs)
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "tx-params";
            var requestType = "TestCommand";

            // Act
            using var activity = _activitySource.StartRetryActivity(
                transactionId,
                requestType,
                attemptNumber,
                maxRetries,
                delayMs);

            // Assert
            var capturedActivity = _capturedActivities.LastOrDefault();
            Assert.NotNull(capturedActivity);
            
            var attemptTag = capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.attempt");
            var maxAttemptsTag = capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.max_attempts");
            var delayTag = capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.delay_ms");
            
            Assert.NotNull(attemptTag);
            Assert.NotNull(maxAttemptsTag);
            Assert.NotNull(delayTag);
            
            Assert.Equal(attemptNumber, attemptTag.Value);
            Assert.Equal(maxRetries, maxAttemptsTag.Value);
            Assert.Equal(delayMs, delayTag.Value);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void StartRetryActivity_WithInvalidTransactionId_Should_Handle_Gracefully(string transactionId)
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var requestType = "TestCommand";
            var attemptNumber = 1;
            var maxRetries = 3;
            var delayMs = 100;

            // Act
            using var activity = _activitySource.StartRetryActivity(
                transactionId,
                requestType,
                attemptNumber,
                maxRetries,
                delayMs);

            // Assert
            var capturedActivity = _capturedActivities.LastOrDefault();
            Assert.NotNull(capturedActivity);
            
            var transactionIdTag = capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.id");
            var requestTypeTag = capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.request_type");
            var attemptTag = capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.attempt");
            var maxAttemptsTag = capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.max_attempts");
            var delayTag = capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.delay_ms");
            
            Assert.NotNull(transactionIdTag);
            Assert.NotNull(requestTypeTag);
            Assert.NotNull(attemptTag);
            Assert.NotNull(maxAttemptsTag);
            Assert.NotNull(delayTag);
            
            Assert.Equal(transactionId, transactionIdTag.Value);
            Assert.Equal(requestType, requestTypeTag.Value);
            Assert.Equal(attemptNumber, attemptTag.Value);
            Assert.Equal(maxRetries, maxAttemptsTag.Value);
            Assert.Equal(delayMs, delayTag.Value);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void StartRetryActivity_WithInvalidRequestType_Should_Handle_Gracefully(string requestType)
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "tx-invalid-request";
            var attemptNumber = 1;
            var maxRetries = 3;
            var delayMs = 100;

            // Act
            using var activity = _activitySource.StartRetryActivity(
                transactionId,
                requestType,
                attemptNumber,
                maxRetries,
                delayMs);

            // Assert
            var capturedActivity = _capturedActivities.LastOrDefault();
            Assert.NotNull(capturedActivity);
            Assert.Equal(transactionId, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.id").Value);
            Assert.Equal(requestType, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.request_type").Value);
            Assert.Equal(attemptNumber, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.attempt").Value);
            Assert.Equal(maxRetries, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.max_attempts").Value);
            Assert.Equal(delayMs, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.delay_ms").Value);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public void StartRetryActivity_WithInvalidAttemptNumber_Should_Handle_Gracefully(int attemptNumber)
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "tx-invalid-attempt";
            var requestType = "TestCommand";
            var maxRetries = 3;
            var delayMs = 100;

            // Act
            using var activity = _activitySource.StartRetryActivity(
                transactionId,
                requestType,
                attemptNumber,
                maxRetries,
                delayMs);

            // Assert
            var capturedActivity = _capturedActivities.LastOrDefault();
            Assert.NotNull(capturedActivity);
            Assert.Equal(attemptNumber, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.attempt").Value);
            Assert.Equal(maxRetries, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.max_attempts").Value);
            Assert.Equal(delayMs, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.delay_ms").Value);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-5)]
        public void StartRetryActivity_WithInvalidMaxRetries_Should_Handle_Gracefully(int maxRetries)
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "tx-invalid-max";
            var requestType = "TestCommand";
            var attemptNumber = 1;
            var delayMs = 100;

            // Act
            using var activity = _activitySource.StartRetryActivity(
                transactionId,
                requestType,
                attemptNumber,
                maxRetries,
                delayMs);

            // Assert
            var capturedActivity = _capturedActivities.LastOrDefault();
            Assert.NotNull(capturedActivity);
            Assert.Equal(attemptNumber, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.attempt").Value);
            Assert.Equal(maxRetries, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.max_attempts").Value);
            Assert.Equal(delayMs, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.delay_ms").Value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        [InlineData(-5000)]
        public void StartRetryActivity_WithInvalidDelayMs_Should_Handle_Gracefully(int delayMs)
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "tx-invalid-delay";
            var requestType = "TestCommand";
            var attemptNumber = 1;
            var maxRetries = 3;

            // Act
            using var activity = _activitySource.StartRetryActivity(
                transactionId,
                requestType,
                attemptNumber,
                maxRetries,
                delayMs);

            // Assert
            var capturedActivity = _capturedActivities.LastOrDefault();
            Assert.NotNull(capturedActivity);
            Assert.Equal(attemptNumber, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.attempt").Value);
            Assert.Equal(maxRetries, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.max_attempts").Value);
            Assert.Equal(delayMs, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.delay_ms").Value);
        }

        [Fact]
        public void StartRetryActivity_WithZeroDelay_Should_Create_Activity_With_Zero_Delay_Tag()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "tx-zero-delay";
            var requestType = "TestCommand";
            var attemptNumber = 1;
            var maxRetries = 3;
            var delayMs = 0;

            // Act
            using var activity = _activitySource.StartRetryActivity(
                transactionId,
                requestType,
                attemptNumber,
                maxRetries,
                delayMs);

            // Assert
            var capturedActivity = _capturedActivities.LastOrDefault();
            Assert.NotNull(capturedActivity);
            Assert.Equal(delayMs, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.delay_ms").Value);
        }

        [Fact]
        public void StartRetryActivity_WithLargeValues_Should_Create_Activity_With_Correct_Tags()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "tx-large-values";
            var requestType = "TestCommand";
            var attemptNumber = 1000;
            var maxRetries = 5000;
            var delayMs = 300000; // 5 minutes

            // Act
            using var activity = _activitySource.StartRetryActivity(
                transactionId,
                requestType,
                attemptNumber,
                maxRetries,
                delayMs);

            // Assert
            var capturedActivity = _capturedActivities.LastOrDefault();
            Assert.NotNull(capturedActivity);
            Assert.Equal(attemptNumber, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.attempt").Value);
            Assert.Equal(maxRetries, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.max_attempts").Value);
            Assert.Equal(delayMs, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.delay_ms").Value);
        }

        [Fact]
        public void StartRetryActivity_WithLongTransactionId_Should_Create_Activity_With_Correct_Tags()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "this_is_a_very_long_transaction_id_that_tests_boundary_conditions_and_special_characters_12345";
            var requestType = "TestCommand";
            var attemptNumber = 1;
            var maxRetries = 3;
            var delayMs = 100;

            // Act
            using var activity = _activitySource.StartRetryActivity(
                transactionId,
                requestType,
                attemptNumber,
                maxRetries,
                delayMs);

            // Assert
            var capturedActivity = _capturedActivities.LastOrDefault();
            Assert.NotNull(capturedActivity);
            Assert.Equal(transactionId, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.id").Value);
            Assert.Equal(requestType, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.request_type").Value);
            Assert.Equal(attemptNumber, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.attempt").Value);
            Assert.Equal(maxRetries, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.max_attempts").Value);
            Assert.Equal(delayMs, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.delay_ms").Value);
        }

        [Fact]
        public void StartRetryActivity_WithSpecialCharacters_Should_Create_Activity_With_Correct_Tags()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "tx-special-chars_123-456";
            var requestType = "Command.With-Special_Chars123";
            var attemptNumber = 1;
            var maxRetries = 3;
            var delayMs = 100;

            // Act
            using var activity = _activitySource.StartRetryActivity(
                transactionId,
                requestType,
                attemptNumber,
                maxRetries,
                delayMs);

            // Assert
            var capturedActivity = _capturedActivities.LastOrDefault();
            Assert.NotNull(capturedActivity);
            Assert.Equal(transactionId, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.id").Value);
            Assert.Equal(requestType, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "transaction.request_type").Value);
            Assert.Equal(attemptNumber, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.attempt").Value);
            Assert.Equal(maxRetries, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.max_attempts").Value);
            Assert.Equal(delayMs, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.delay_ms").Value);
        }

        [Fact]
        public void StartRetryActivity_MultipleCalls_Should_Create_Different_Activities()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId1 = "tx-retry-1";
            var transactionId2 = "tx-retry-2";

            // Act
            using var activity1 = _activitySource.StartRetryActivity(
                transactionId1,
                "Command1",
                1,
                3,
                100);

            using var activity2 = _activitySource.StartRetryActivity(
                transactionId2,
                "Command2",
                2,
                5,
                500);

        // Assert
        Assert.Equal(2, _capturedActivities.Count);
        var capturedActivity1 = _capturedActivities[0];
        var capturedActivity2 = _capturedActivities[1];
        Assert.NotNull(capturedActivity1);
        Assert.NotNull(capturedActivity2);
        Assert.NotSame(capturedActivity1, capturedActivity2);
        
        var transactionId1Tag = capturedActivity1.TagObjects.FirstOrDefault(t => t.Key == "transaction.id");
        var transactionId2Tag = capturedActivity2.TagObjects.FirstOrDefault(t => t.Key == "transaction.id");
        
        Assert.NotNull(transactionId1Tag);
        Assert.NotNull(transactionId2Tag);
        
        Assert.Equal(transactionId1, transactionId1Tag.Value);
        Assert.Equal(transactionId2, transactionId2Tag.Value);
            Assert.Equal(1, capturedActivity1.TagObjects.FirstOrDefault(t => t.Key == "retry.attempt").Value);
            Assert.Equal(2, capturedActivity2.TagObjects.FirstOrDefault(t => t.Key == "retry.attempt").Value);
        }

        [Fact]
        public void StartRetryActivity_Activity_Disposal_Should_Work_Correctly()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "tx-retry-dispose";

            // Act
            var activity = _activitySource.StartRetryActivity(
                transactionId,
                "TestCommand",
                1,
                3,
                100);

            // Assert
            var capturedActivity = _capturedActivities.LastOrDefault();
            Assert.NotNull(capturedActivity);
            Assert.False(capturedActivity.IsStopped);

            // Act
            activity.Dispose();

            // Assert
            Assert.True(capturedActivity.IsStopped);
        }

        [Fact]
        public void StartRetryActivity_WithAttemptNumberGreaterThanMaxRetries_Should_Create_Activity_With_Correct_Tags()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "tx-attempt-greater";
            var requestType = "TestCommand";
            var attemptNumber = 5;
            var maxRetries = 3;
            var delayMs = 100;

            // Act
            using var activity = _activitySource.StartRetryActivity(
                transactionId,
                requestType,
                attemptNumber,
                maxRetries,
                delayMs);

            // Assert
            var capturedActivity = _capturedActivities.LastOrDefault();
            Assert.NotNull(capturedActivity);
            Assert.Equal(attemptNumber, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.attempt").Value);
            Assert.Equal(maxRetries, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.max_attempts").Value);
            Assert.Equal(delayMs, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.delay_ms").Value);
        }

        [Fact]
        public void StartRetryActivity_WithAttemptNumberEqualToMaxRetries_Should_Create_Activity_With_Correct_Tags()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            var transactionId = "tx-attempt-equal";
            var requestType = "TestCommand";
            var attemptNumber = 3;
            var maxRetries = 3;
            var delayMs = 100;

            // Act
            using var activity = _activitySource.StartRetryActivity(
                transactionId,
                requestType,
                attemptNumber,
                maxRetries,
                delayMs);

            // Assert
            var capturedActivity = _capturedActivities.LastOrDefault();
            Assert.NotNull(capturedActivity);
            Assert.Equal(attemptNumber, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.attempt").Value);
            Assert.Equal(maxRetries, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.max_attempts").Value);
            Assert.Equal(delayMs, capturedActivity.TagObjects.FirstOrDefault(t => t.Key == "retry.delay_ms").Value);
        }

        [Fact]
        public void SetTransactionStatus_WithNullActivity_Should_Not_Throw_Exception()
        {
            // Arrange
            Activity? activity = null;

            // Act & Assert
            var exception = Record.Exception(() => _activitySource.SetTransactionStatus(activity, true));
            Assert.Null(exception);
        }

        [Fact]
        public void SetTransactionStatus_WithNullActivityAndSuccessTrue_Should_Not_Throw_Exception()
        {
            // Arrange
            Activity? activity = null;

            // Act & Assert
            var exception = Record.Exception(() => _activitySource.SetTransactionStatus(activity, true));
            Assert.Null(exception);
        }

        [Fact]
        public void SetTransactionStatus_WithNullActivityAndSuccessFalse_Should_Not_Throw_Exception()
        {
            // Arrange
            Activity? activity = null;

            // Act & Assert
            var exception = Record.Exception(() => _activitySource.SetTransactionStatus(activity, false));
            Assert.Null(exception);
        }

        [Fact]
        public void SetTransactionStatus_WithNullActivityAndSuccessFalseAndErrorMessage_Should_Not_Throw_Exception()
        {
            // Arrange
            Activity? activity = null;
            string errorMessage = "Test error";

            // Act & Assert
            var exception = Record.Exception(() => _activitySource.SetTransactionStatus(activity, false, errorMessage));
            Assert.Null(exception);
        }

        [Fact]
        public void SetTransactionStatus_WithValidActivityAndSuccessTrue_Should_SetStatusToOk()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            using var activity = _activitySource.StartTransactionActivity(
                "tx-success",
                "TestCommand",
                IsolationLevel.ReadCommitted,
                0,
                false);
            
            Assert.NotNull(activity);
            Assert.Equal(ActivityStatusCode.Unset, activity.Status);

            // Act
            _activitySource.SetTransactionStatus(activity, true);

            // Assert
            Assert.Equal(ActivityStatusCode.Ok, activity.Status);
            Assert.Null(activity.StatusDescription);
        }

        [Fact]
        public void SetTransactionStatus_WithValidActivityAndSuccessTrue_AfterPreviousError_Should_ChangeStatusToOk()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            using var activity = _activitySource.StartTransactionActivity(
                "tx-change-status",
                "TestCommand",
                IsolationLevel.ReadCommitted,
                0,
                false);
            
            // First set to error
            _activitySource.SetTransactionStatus(activity, false, "Initial error");
            Assert.Equal(ActivityStatusCode.Error, activity.Status);

            // Act - change to success
            _activitySource.SetTransactionStatus(activity, true);

            // Assert
            Assert.Equal(ActivityStatusCode.Ok, activity.Status);
            Assert.Null(activity.StatusDescription);
        }

        [Fact]
        public void SetTransactionStatus_WithValidActivityAndSuccessFalse_Should_SetStatusToErrorWithDefaultMessage()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            using var activity = _activitySource.StartTransactionActivity(
                "tx-fail-default",
                "TestCommand",
                IsolationLevel.ReadCommitted,
                0,
                false);
            
            Assert.NotNull(activity);
            Assert.Equal(ActivityStatusCode.Unset, activity.Status);

            // Act
            _activitySource.SetTransactionStatus(activity, false);

            // Assert
            Assert.Equal(ActivityStatusCode.Error, activity.Status);
            Assert.Equal("Transaction failed", activity.StatusDescription);
        }

        [Fact]
        public void SetTransactionStatus_WithValidActivityAndSuccessFalse_AfterPreviousSuccess_Should_ChangeStatusToError()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            using var activity = _activitySource.StartTransactionActivity(
                "tx-change-to-error",
                "TestCommand",
                IsolationLevel.ReadCommitted,
                0,
                false);
            
            // First set to success
            _activitySource.SetTransactionStatus(activity, true);
            Assert.Equal(ActivityStatusCode.Ok, activity.Status);

            // Act - change to error
            _activitySource.SetTransactionStatus(activity, false);

            // Assert
            Assert.Equal(ActivityStatusCode.Error, activity.Status);
            Assert.Equal("Transaction failed", activity.StatusDescription);
        }

        [Fact]
        public void SetTransactionStatus_WithValidActivityAndSuccessFalseAndCustomErrorMessage_Should_SetStatusToErrorWithCustomMessage()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            using var activity = _activitySource.StartTransactionActivity(
                "tx-fail-custom",
                "TestCommand",
                IsolationLevel.ReadCommitted,
                0,
                false);
            
            var customErrorMessage = "Custom error occurred during transaction";
            
            Assert.NotNull(activity);
            Assert.Equal(ActivityStatusCode.Unset, activity.Status);

            // Act
            _activitySource.SetTransactionStatus(activity, false, customErrorMessage);

            // Assert
            Assert.Equal(ActivityStatusCode.Error, activity.Status);
            Assert.Equal(customErrorMessage, activity.StatusDescription);
        }

        [Fact]
        public void SetTransactionStatus_WithValidActivityAndSuccessFalseAndEmptyErrorMessage_Should_SetStatusToErrorWithEmptyMessage()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            using var activity = _activitySource.StartTransactionActivity(
                "tx-fail-empty",
                "TestCommand",
                IsolationLevel.ReadCommitted,
                0,
                false);
            
            var emptyErrorMessage = "";
            
            Assert.NotNull(activity);
            Assert.Equal(ActivityStatusCode.Unset, activity.Status);

            // Act
            _activitySource.SetTransactionStatus(activity, false, emptyErrorMessage);

            // Assert
            Assert.Equal(ActivityStatusCode.Error, activity.Status);
            Assert.Equal(emptyErrorMessage, activity.StatusDescription);
        }

        [Fact]
        public void SetTransactionStatus_WithValidActivityAndSuccessFalseAndNullErrorMessage_Should_SetStatusToErrorWithDefaultMessage()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            using var activity = _activitySource.StartTransactionActivity(
                "tx-fail-null",
                "TestCommand",
                IsolationLevel.ReadCommitted,
                0,
                false);
            
            string? nullErrorMessage = null;
            
            Assert.NotNull(activity);
            Assert.Equal(ActivityStatusCode.Unset, activity.Status);

            // Act
            _activitySource.SetTransactionStatus(activity, false, nullErrorMessage);

            // Assert
            Assert.Equal(ActivityStatusCode.Error, activity.Status);
            Assert.Equal("Transaction failed", activity.StatusDescription);
        }

        [Fact]
        public void SetTransactionStatus_WithValidActivityAndSuccessFalseAndWhitespaceErrorMessage_Should_SetStatusToErrorWithWhitespaceMessage()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            using var activity = _activitySource.StartTransactionActivity(
                "tx-fail-whitespace",
                "TestCommand",
                IsolationLevel.ReadCommitted,
                0,
                false);
            
            var whitespaceErrorMessage = "   ";
            
            Assert.NotNull(activity);
            Assert.Equal(ActivityStatusCode.Unset, activity.Status);

            // Act
            _activitySource.SetTransactionStatus(activity, false, whitespaceErrorMessage);

            // Assert
            Assert.Equal(ActivityStatusCode.Error, activity.Status);
            Assert.Equal(whitespaceErrorMessage, activity.StatusDescription);
        }

        [Fact]
        public void RecordException_WithNullActivity_Should_Not_Throw_Exception()
        {
            // Arrange
            Activity? activity = null;
            var exception = new InvalidOperationException("Test exception");

            // Act & Assert
            var ex = Record.Exception(() => _activitySource.RecordException(activity, exception));
            Assert.Null(ex);
        }

        [Fact]
        public void RecordException_WithNullActivityAndNullException_Should_Not_Throw_Exception()
        {
            // Arrange
            Activity? activity = null;
            Exception? exception = null;

            // Act & Assert
            var ex = Record.Exception(() => _activitySource.RecordException(activity, exception));
            Assert.Null(ex);
        }

        [Fact]
        public void RecordException_WithValidActivityAndException_Should_AddExceptionEventAndSetStatus()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            using var activity = _activitySource.StartTransactionActivity(
                "tx-exception",
                "TestCommand",
                IsolationLevel.ReadCommitted,
                0,
                false);
            
            var exception = new InvalidOperationException("Test operation failed");

            Assert.NotNull(activity);
            Assert.Equal(ActivityStatusCode.Unset, activity.Status);

            // Act
            _activitySource.RecordException(activity, exception);

            // Assert
            Assert.Equal(ActivityStatusCode.Error, activity.Status);
            Assert.Equal("Test operation failed", activity.StatusDescription);
            
            // Check that the exception event was added to the activity
            var exceptionEvents = activity.Events.Where(e => e.Name == "exception").ToList();
            Assert.Single(exceptionEvents);
            
            var exceptionEvent = exceptionEvents[0];
            Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.type" && tag.Value?.ToString().Contains("InvalidOperationException") == true);
            Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.message" && tag.Value?.ToString() == "Test operation failed");
            // Stack trace may be null in test environment, so we just check if it exists or not
            var stackTraceTag = exceptionEvent.Tags.FirstOrDefault(t => t.Key == "exception.stacktrace");
            // The stack trace tag should exist (even if its value is null)
            Assert.NotNull(stackTraceTag.Key);
        }

        [Fact]
        public void RecordException_WithValidActivityAndException_AfterPreviousError_Should_UpdateStatus()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            using var activity = _activitySource.StartTransactionActivity(
                "tx-exception-update",
                "TestCommand",
                IsolationLevel.ReadCommitted,
                0,
                false);
            
            var exception = new ArgumentException("New argument exception");

            // First set to error using SetTransactionStatus
            _activitySource.SetTransactionStatus(activity, false, "Initial error");
            Assert.Equal(ActivityStatusCode.Error, activity.Status);
            Assert.Equal("Initial error", activity.StatusDescription);

            // Act - record exception
            _activitySource.RecordException(activity, exception);

            // Assert that status remains as error but description is updated
            Assert.Equal(ActivityStatusCode.Error, activity.Status);
            Assert.Equal("New argument exception", activity.StatusDescription);
            
            // Check that the exception event was added
            var exceptionEvents = activity.Events.Where(e => e.Name == "exception").ToList();
            Assert.Single(exceptionEvents);
        }

        [Fact]
        public void RecordException_WithValidActivityAndNestedException_Should_AddExceptionEventWithInnerExceptionDetails()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            using var activity = _activitySource.StartTransactionActivity(
                "tx-nested-exception",
                "TestCommand",
                IsolationLevel.ReadCommitted,
                0,
                false);
            
            var innerException = new ArgumentException("Inner argument exception");
            var outerException = new InvalidOperationException("Outer operation failed", innerException);

            Assert.NotNull(activity);
            Assert.Equal(ActivityStatusCode.Unset, activity.Status);

            // Act
            _activitySource.RecordException(activity, outerException);

            // Assert
            Assert.Equal(ActivityStatusCode.Error, activity.Status);
            Assert.Equal("Outer operation failed", activity.StatusDescription);
            
            // Check that the exception event was added to the activity
            var exceptionEvents = activity.Events.Where(e => e.Name == "exception").ToList();
            Assert.Single(exceptionEvents);
            
            var exceptionEvent = exceptionEvents[0];
            Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.type" && tag.Value?.ToString().Contains("InvalidOperationException") == true);
            Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.message" && tag.Value?.ToString() == "Outer operation failed");
            // Stack trace may be null in test environment
            var stackTraceTag = exceptionEvent.Tags.FirstOrDefault(t => t.Key == "exception.stacktrace");
            // The stack trace tag should exist (even if its value is null)
            Assert.NotNull(stackTraceTag.Key);
        }

        [Fact]
        public void RecordException_WithValidActivityAndNullException_Should_Not_Throw_Exception()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            using var activity = _activitySource.StartTransactionActivity(
                "tx-null-exception",
                "TestCommand",
                IsolationLevel.ReadCommitted,
                0,
                false);
            
            Exception? exception = null;

            Assert.NotNull(activity);
            Assert.Equal(ActivityStatusCode.Unset, activity.Status);

            // Act & Assert
            var ex = Record.Exception(() => _activitySource.RecordException(activity, exception));
            Assert.Null(ex);
            
            // Activity status should remain unchanged since no exception was provided
            Assert.Equal(ActivityStatusCode.Unset, activity.Status);
        }

        [Fact]
        public void RecordException_WithValidActivityAndDifferentExceptionTypes_Should_RecordWithCorrectType()
        {
            // Arrange
            ClearCapturedActivities();
            using var listener = SetupActivityListener();
            using var activity = _activitySource.StartTransactionActivity(
                "tx-diff-exception",
                "TestCommand",
                IsolationLevel.ReadCommitted,
                0,
                false);
            
            var exception = new DivideByZeroException("Division by zero occurred");

            Assert.NotNull(activity);
            Assert.Equal(ActivityStatusCode.Unset, activity.Status);

            // Act
            _activitySource.RecordException(activity, exception);

            // Assert
            Assert.Equal(ActivityStatusCode.Error, activity.Status);
            Assert.Equal("Division by zero occurred", activity.StatusDescription);
            
            // Check that the exception event was added to the activity with correct type
            var exceptionEvents = activity.Events.Where(e => e.Name == "exception").ToList();
            Assert.Single(exceptionEvents);
            
            var exceptionEvent = exceptionEvents[0];
            Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.type" && tag.Value?.ToString().Contains("DivideByZeroException") == true);
            Assert.Contains(exceptionEvent.Tags, tag => tag.Key == "exception.message" && tag.Value?.ToString() == "Division by zero occurred");
            // Stack trace may be null in test environment
            var stackTraceTag = exceptionEvent.Tags.FirstOrDefault(t => t.Key == "exception.stacktrace");
            // The stack trace tag should exist (even if its value is null)
            Assert.NotNull(stackTraceTag.Key);
        }
}
