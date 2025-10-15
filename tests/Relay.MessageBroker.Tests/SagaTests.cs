using Relay.MessageBroker.Saga;
using Relay.MessageBroker.Saga.Services;
using Moq;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class SagaTests
{
    [Fact]
    public async Task ExecuteAsync_AllStepsSucceed_ShouldCompleteSuccessfully()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-001",
            Amount = 100m
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.Equal(3, result.Data.CurrentStep); // All 3 steps completed
        Assert.True(result.Data.ReserveInventoryExecuted);
        Assert.True(result.Data.ProcessPaymentExecuted);
        Assert.True(result.Data.ShipOrderExecuted);
    }

    [Fact]
    public async Task ExecuteAsync_StepFails_ShouldCompensatePreviousSteps()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-002",
            Amount = 100m,
            FailAtStep = "ProcessPayment" // Fail at step 2
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaState.Compensated, result.Data.State);
        Assert.Equal("ProcessPayment", result.FailedStep);
        Assert.True(result.CompensationSucceeded);
        
        // First step executed and compensated
        Assert.True(result.Data.ReserveInventoryExecuted);
        Assert.True(result.Data.ReserveInventoryCompensated);
        
        // Second step failed, no compensation needed
        Assert.False(result.Data.ProcessPaymentExecuted);
        Assert.False(result.Data.ProcessPaymentCompensated);
        
        // Third step never executed
        Assert.False(result.Data.ShipOrderExecuted);
    }

    [Fact]
    public async Task ExecuteAsync_FirstStepFails_ShouldNotCompensateAnything()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-003",
            Amount = 100m,
            FailAtStep = "ReserveInventory" // Fail at first step
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaState.Compensated, result.Data.State);
        Assert.Equal("ReserveInventory", result.FailedStep);
        
        // No steps succeeded, nothing to compensate
        Assert.False(result.Data.ReserveInventoryExecuted);
        Assert.False(result.Data.ReserveInventoryCompensated);
    }

    [Fact]
    public async Task ExecuteAsync_LastStepFails_ShouldCompensateAllPreviousSteps()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-004",
            Amount = 100m,
            FailAtStep = "ShipOrder" // Fail at last step
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaState.Compensated, result.Data.State);
        Assert.Equal("ShipOrder", result.FailedStep);
        Assert.True(result.CompensationSucceeded);
        
        // All previous steps compensated
        Assert.True(result.Data.ReserveInventoryCompensated);
        Assert.True(result.Data.ProcessPaymentCompensated);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateTimestamps()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-005",
            Amount = 100m
        };
        var startTime = DateTimeOffset.UtcNow;

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.True((result.Data.CreatedAt - startTime).Duration() <= TimeSpan.FromSeconds(1));
        Assert.True(result.Data.UpdatedAt >= result.Data.CreatedAt);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExecuteStepsInOrder()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-006",
            Amount = 100m
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.Equal(new[] { "ReserveInventory", "ProcessPayment", "ShipOrder" }, result.Data.ExecutionOrder);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCompensateInReverseOrder()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-007",
            Amount = 100m,
            FailAtStep = "ShipOrder"
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        // When ShipOrder fails, only previously executed steps are compensated (in reverse order)
        Assert.Equal(new[] { "ProcessPayment-Compensation", "ReserveInventory-Compensation" }, result.Data.CompensationOrder);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-008",
            Amount = 100m
        };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await saga.ExecuteAsync(data, cts.Token));
    }

    [Fact]
    public void SagaId_ShouldReturnTypeName()
    {
        // Arrange
        var saga = new TestOrderSaga();

        // Act
        var sagaId = saga.SagaId;

        // Assert
        Assert.Equal("TestOrderSaga", sagaId);
    }

    [Fact]
    public void Steps_ShouldReturnAllAddedSteps()
    {
        // Arrange
        var saga = new TestOrderSaga();

        // Act
        var steps = saga.Steps;

        // Assert
        Assert.Equal(3, steps.Count);
        Assert.Equal("ReserveInventory", steps[0].Name);
        Assert.Equal("ProcessPayment", steps[1].Name);
        Assert.Equal("ShipOrder", steps[2].Name);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTrackCurrentStep()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-009",
            Amount = 100m,
            FailAtStep = "ProcessPayment"
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Failed at step 2, so CurrentStep should be 1 (0-indexed)
        Assert.Equal(1, result.Data.CurrentStep);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleTimes_ShouldResumeFromCurrentStep()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-010",
            Amount = 100m,
            CurrentStep = 1 // Resume from second step
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Data.ReserveInventoryExecuted); // First step skipped
        Assert.True(result.Data.ProcessPaymentExecuted); // Second step executed
        Assert.True(result.Data.ShipOrderExecuted); // Third step executed
    }

    [Fact]
    public async Task ExecuteAsync_WithAbortedState_ShouldExecuteFromCurrentStep()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-011",
            Amount = 100m,
            State = SagaState.Aborted,
            CurrentStep = 0 // Start from beginning despite aborted state
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Saga executes normally from CurrentStep, ignoring initial state
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.True(result.Data.ReserveInventoryExecuted);
        Assert.True(result.Data.ProcessPaymentExecuted);
        Assert.True(result.Data.ShipOrderExecuted);
    }

    [Fact]
    public async Task ExecuteAsync_WithCompletedState_ShouldExecuteFromCurrentStep()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-012",
            Amount = 100m,
            State = SagaState.Completed,
            CurrentStep = 0 // Start from beginning despite completed state
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Saga executes normally from CurrentStep, ignoring initial state
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.True(result.Data.ReserveInventoryExecuted);
        Assert.True(result.Data.ProcessPaymentExecuted);
        Assert.True(result.Data.ShipOrderExecuted);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedState_ShouldExecuteFromCurrentStep()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-013",
            Amount = 100m,
            State = SagaState.Failed,
            CurrentStep = 0 // Start from beginning despite failed state
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Saga executes normally from CurrentStep, ignoring initial state
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.True(result.Data.ReserveInventoryExecuted);
        Assert.True(result.Data.ProcessPaymentExecuted);
        Assert.True(result.Data.ShipOrderExecuted);
    }

    [Fact]
    public async Task ExecuteAsync_WithCompensatedState_ShouldExecuteFromCurrentStep()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-014",
            Amount = 100m,
            State = SagaState.Compensated,
            CurrentStep = 0 // Start from beginning despite compensated state
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Saga executes normally from CurrentStep, ignoring initial state
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.True(result.Data.ReserveInventoryExecuted);
        Assert.True(result.Data.ProcessPaymentExecuted);
        Assert.True(result.Data.ShipOrderExecuted);
    }

    [Fact]
    public async Task ExecuteAsync_CompensationFails_ShouldStillMarkAsCompensated()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-015",
            Amount = 100m,
            FailAtStep = "ProcessPayment",
            FailCompensationAtStep = "ReserveInventory" // Fail compensation of first step
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Even with compensation failure, state is set to Compensated
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaState.Compensated, result.Data.State);
        Assert.Equal("ProcessPayment", result.FailedStep);
        Assert.False(result.CompensationSucceeded); // Compensation failed

        // First step executed but compensation failed
        Assert.True(result.Data.ReserveInventoryExecuted);
        Assert.False(result.Data.ReserveInventoryCompensated);

        // Second step failed
        Assert.False(result.Data.ProcessPaymentExecuted);
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeout_ShouldTriggerCompensation()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-016",
            Amount = 100m,
            TimeoutAtStep = "ProcessPayment" // Simulate timeout during payment
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Timeout triggers compensation, resulting in Compensated state
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaState.Compensated, result.Data.State);
        Assert.Equal("ProcessPayment", result.FailedStep);
        Assert.True(result.CompensationSucceeded);

        // First step executed and should be compensated
        Assert.True(result.Data.ReserveInventoryExecuted);
        Assert.True(result.Data.ReserveInventoryCompensated);
    }

    [Fact]
    public async Task ExecuteAsync_EmptySaga_ShouldCompleteImmediately()
    {
        // Arrange
        var saga = new EmptySaga();
        var data = new EmptySagaData
        {
            CorrelationId = "EMPTY-001"
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.Equal(0, result.Data.CurrentStep);
    }

    [Fact]
    public async Task ExecuteAsync_SingleStepSaga_Success()
    {
        // Arrange
        var saga = new SingleStepSaga();
        var data = new SingleStepSagaData
        {
            CorrelationId = "SINGLE-001",
            Value = 42
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.Equal(1, result.Data.CurrentStep);
        Assert.True(result.Data.StepExecuted);
    }

    [Fact]
    public async Task ExecuteAsync_SingleStepSaga_Failure()
    {
        // Arrange
        var saga = new SingleStepSaga();
        var data = new SingleStepSagaData
        {
            CorrelationId = "SINGLE-002",
            Value = 42,
            ShouldFail = true
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaState.Compensated, result.Data.State);
        Assert.Equal("SingleStep", result.FailedStep);
        Assert.False(result.CompensationSucceeded); // No steps were successfully executed, so nothing to compensate
        Assert.False(result.Data.StepCompensated); // Step never executed, so no compensation
    }

    [Fact]
    public async Task ExecuteAsync_ConcurrentExecution_ShouldHandleProperly()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-017",
            Amount = 100m,
            State = SagaState.Running // Already running
        };

        // Act - Try to execute while already running
        var result = await saga.ExecuteAsync(data);

        // Assert - Should continue from current state
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
    }

    [Fact]
    public async Task ExecuteAsync_WithLargeNumberOfSteps_ShouldHandleCorrectly()
    {
        // Arrange
        var saga = new LargeSaga();
        var data = new LargeSagaData
        {
            CorrelationId = "LARGE-001"
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.Equal(10, result.Data.CurrentStep); // 10 steps completed
        Assert.Equal(10, result.Data.ExecutedSteps.Count);
    }

    [Fact]
    public async Task ExecuteAsync_WithStepSkipping_ShouldSkipSpecifiedSteps()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-018",
            Amount = 100m,
            SkipSteps = new[] { "ProcessPayment" } // Skip payment step
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.True(result.Data.ReserveInventoryExecuted);
        Assert.False(result.Data.ProcessPaymentExecuted); // Skipped
        Assert.True(result.Data.ShipOrderExecuted);
    }

    // SagaOptions Tests
    [Fact]
    public void SagaOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new SagaOptions();

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal(TimeSpan.FromMinutes(30), options.DefaultTimeout);
        Assert.True(options.AutoPersist);
        Assert.Equal(TimeSpan.FromSeconds(5), options.PersistenceInterval);
        Assert.False(options.AutoRetryFailedSteps);
        Assert.Equal(3, options.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(5), options.RetryDelay);
        Assert.True(options.UseExponentialBackoff);
        Assert.True(options.AutoCompensateOnFailure);
        Assert.True(options.ContinueCompensationOnError);
        Assert.Null(options.StepTimeout);
        Assert.Null(options.CompensationTimeout);
        Assert.True(options.TrackMetrics);
        Assert.True(options.EnableTelemetry);
        Assert.Null(options.OnSagaCompleted);
        Assert.Null(options.OnSagaFailed);
        Assert.Null(options.OnSagaCompensated);
    }

    [Fact]
    public void SagaOptions_CanBeConfigured()
    {
        // Arrange & Act
        var options = new SagaOptions
        {
            Enabled = false,
            DefaultTimeout = TimeSpan.FromHours(1),
            AutoPersist = false,
            PersistenceInterval = TimeSpan.FromMinutes(1),
            AutoRetryFailedSteps = true,
            MaxRetryAttempts = 5,
            RetryDelay = TimeSpan.FromSeconds(10),
            UseExponentialBackoff = false,
            AutoCompensateOnFailure = false,
            ContinueCompensationOnError = false,
            StepTimeout = TimeSpan.FromMinutes(5),
            CompensationTimeout = TimeSpan.FromMinutes(10),
            TrackMetrics = false,
            EnableTelemetry = false
        };

        // Assert
        Assert.False(options.Enabled);
        Assert.Equal(TimeSpan.FromHours(1), options.DefaultTimeout);
        Assert.False(options.AutoPersist);
        Assert.Equal(TimeSpan.FromMinutes(1), options.PersistenceInterval);
        Assert.True(options.AutoRetryFailedSteps);
        Assert.Equal(5, options.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(10), options.RetryDelay);
        Assert.False(options.UseExponentialBackoff);
        Assert.False(options.AutoCompensateOnFailure);
        Assert.False(options.ContinueCompensationOnError);
        Assert.Equal(TimeSpan.FromMinutes(5), options.StepTimeout);
        Assert.Equal(TimeSpan.FromMinutes(10), options.CompensationTimeout);
        Assert.False(options.TrackMetrics);
        Assert.False(options.EnableTelemetry);
    }

    [Fact]
    public void SagaOptions_EventHandlers_CanBeSet()
    {
        // Arrange
        var completedCalled = false;
        var failedCalled = false;
        var compensatedCalled = false;

        var options = new SagaOptions
        {
            OnSagaCompleted = args => completedCalled = true,
            OnSagaFailed = args => failedCalled = true,
            OnSagaCompensated = args => compensatedCalled = true
        };

        // Act
        options.OnSagaCompleted?.Invoke(new SagaCompletedEventArgs { SagaId = Guid.NewGuid() });
        options.OnSagaFailed?.Invoke(new SagaFailedEventArgs { SagaId = Guid.NewGuid() });
        options.OnSagaCompensated?.Invoke(new SagaCompensatedEventArgs { SagaId = Guid.NewGuid() });

        // Assert
        Assert.True(completedCalled);
        Assert.True(failedCalled);
        Assert.True(compensatedCalled);
    }

    // SagaState Transition Tests
    [Fact]
    public async Task ExecuteAsync_StateTransitions_FromRunningToCompleted()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-019",
            Amount = 100m,
            State = SagaState.Running
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_StateTransitions_FromRunningToCompensated()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-020",
            Amount = 100m,
            State = SagaState.Running,
            FailAtStep = "ProcessPayment"
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.Equal(SagaState.Compensated, result.Data.State);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_ResumeFromMiddleStep_Succeeds()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-021",
            Amount = 100m,
            State = SagaState.Compensating, // Initial state doesn't matter
            CurrentStep = 1 // Resume from ProcessPayment step
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Executes from CurrentStep, ignoring initial state
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.True(result.IsSuccess);
        Assert.False(result.Data.ReserveInventoryExecuted); // Skipped
        Assert.True(result.Data.ProcessPaymentExecuted); // Executed
        Assert.True(result.Data.ShipOrderExecuted); // Executed
    }

    // SagaMetricsCollector Tests
    [Fact]
    public void InMemorySagaMetricsCollector_RecordSagaStarted_IncrementsStarted()
    {
        // Arrange
        var logger = new Mock<ILogger<InMemorySagaMetricsCollector>>();
        var collector = new InMemorySagaMetricsCollector(logger.Object);
        var sagaId = Guid.NewGuid();
        var correlationId = "test-correlation";

        // Act
        collector.RecordSagaStarted("TestSaga", sagaId, correlationId);

        // Assert
        var metrics = collector.GetMetrics("TestSaga");
        Assert.Equal(1, metrics.TotalStarted);
        Assert.Equal(0, metrics.TotalCompleted);
        Assert.Equal(0, metrics.TotalFailed);
        Assert.Equal(0, metrics.TotalCompensated);
    }

    [Fact]
    public void InMemorySagaMetricsCollector_RecordSagaCompleted_IncrementsCompleted()
    {
        // Arrange
        var logger = new Mock<ILogger<InMemorySagaMetricsCollector>>();
        var collector = new InMemorySagaMetricsCollector(logger.Object);
        var sagaId = Guid.NewGuid();
        var duration = TimeSpan.FromSeconds(5);

        // Act
        collector.RecordSagaCompleted("TestSaga", sagaId, duration);

        // Assert
        var metrics = collector.GetMetrics("TestSaga");
        Assert.Equal(1, metrics.TotalCompleted);
        Assert.Equal(duration.TotalMilliseconds, metrics.AverageDurationMs);
    }

    [Fact]
    public void InMemorySagaMetricsCollector_RecordSagaFailed_IncrementsFailed()
    {
        // Arrange
        var logger = new Mock<ILogger<InMemorySagaMetricsCollector>>();
        var collector = new InMemorySagaMetricsCollector(logger.Object);
        var sagaId = Guid.NewGuid();
        var failedStep = "TestStep";
        var duration = TimeSpan.FromSeconds(3);

        // Act
        collector.RecordSagaFailed("TestSaga", sagaId, failedStep, duration);

        // Assert
        var metrics = collector.GetMetrics("TestSaga");
        Assert.Equal(1, metrics.TotalFailed);
        Assert.Equal(1, metrics.FailuresByStep[failedStep]);
    }

    [Fact]
    public void InMemorySagaMetricsCollector_RecordStepExecuted_TracksMetrics()
    {
        // Arrange
        var logger = new Mock<ILogger<InMemorySagaMetricsCollector>>();
        var collector = new InMemorySagaMetricsCollector(logger.Object);
        var stepName = "TestStep";
        var duration = TimeSpan.FromMilliseconds(100);

        // Act
        collector.RecordStepExecuted("TestSaga", stepName, duration, true);
        collector.RecordStepExecuted("TestSaga", stepName, duration, false);

        // Assert
        var metrics = collector.GetMetrics("TestSaga");
        Assert.True(metrics.StepMetrics.ContainsKey(stepName));
        var stepMetrics = metrics.StepMetrics[stepName];
        Assert.Equal(2, stepMetrics.TotalExecutions);
        Assert.Equal(1, stepMetrics.Successes);
        Assert.Equal(1, stepMetrics.Failures);
        Assert.Equal(50.0, stepMetrics.SuccessRate); // 50%
    }

    [Fact]
    public void InMemorySagaMetricsCollector_GetAllMetrics_ReturnsAllTypes()
    {
        // Arrange
        var logger = new Mock<ILogger<InMemorySagaMetricsCollector>>();
        var collector = new InMemorySagaMetricsCollector(logger.Object);

        // Act
        collector.RecordSagaStarted("SagaA", Guid.NewGuid(), "corr1");
        collector.RecordSagaStarted("SagaB", Guid.NewGuid(), "corr2");

        // Assert
        var allMetrics = collector.GetAllMetrics();
        Assert.Equal(2, allMetrics.Count);
        Assert.True(allMetrics.ContainsKey("SagaA"));
        Assert.True(allMetrics.ContainsKey("SagaB"));
    }

    [Fact]
    public void InMemorySagaMetricsCollector_Reset_ClearsAllMetrics()
    {
        // Arrange
        var logger = new Mock<ILogger<InMemorySagaMetricsCollector>>();
        var collector = new InMemorySagaMetricsCollector(logger.Object);
        collector.RecordSagaStarted("TestSaga", Guid.NewGuid(), "corr");

        // Act
        collector.Reset();

        // Assert
        var metrics = collector.GetMetrics("TestSaga");
        Assert.Equal(0, metrics.TotalStarted);
        var allMetrics = collector.GetAllMetrics();
        Assert.Empty(allMetrics);
    }

    // SagaPersistence Tests
    [Fact]
    public async Task InMemorySagaPersistence_SaveAndGetById_Works()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        var data = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "test-correlation",
            OrderId = "ORDER-022",
            Amount = 100m
        };

        // Act
        await persistence.SaveAsync(data);
        var retrieved = await persistence.GetByIdAsync(data.SagaId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(data.SagaId, retrieved!.SagaId);
        Assert.Equal(data.CorrelationId, retrieved.CorrelationId);
        Assert.Equal(data.OrderId, retrieved.OrderId);
        Assert.Equal(data.Amount, retrieved.Amount);
    }

    [Fact]
    public async Task InMemorySagaPersistence_SaveAndGetByCorrelationId_Works()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        var data = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "test-correlation",
            OrderId = "ORDER-023",
            Amount = 200m
        };

        // Act
        await persistence.SaveAsync(data);
        var retrieved = await persistence.GetByCorrelationIdAsync(data.CorrelationId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(data.SagaId, retrieved!.SagaId);
        Assert.Equal(data.CorrelationId, retrieved.CorrelationId);
    }

    [Fact]
    public async Task InMemorySagaPersistence_Delete_RemovesData()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        var data = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "test-correlation",
            OrderId = "ORDER-024",
            Amount = 150m
        };
        await persistence.SaveAsync(data);

        // Act
        await persistence.DeleteAsync(data.SagaId);
        var retrieved = await persistence.GetByIdAsync(data.SagaId);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task InMemorySagaPersistence_GetActiveSagas_ReturnsRunningAndCompensating()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        var runningData = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "running",
            State = SagaState.Running
        };
        var compensatingData = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "compensating",
            State = SagaState.Compensating
        };
        var completedData = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "completed",
            State = SagaState.Completed
        };

        await persistence.SaveAsync(runningData);
        await persistence.SaveAsync(compensatingData);
        await persistence.SaveAsync(completedData);

        // Act
        var activeSagas = new List<OrderSagaData>();
        await foreach (var saga in persistence.GetActiveSagasAsync())
        {
            activeSagas.Add(saga);
        }

        // Assert
        Assert.Equal(2, activeSagas.Count);
        Assert.Contains(activeSagas, s => s.SagaId == runningData.SagaId);
        Assert.Contains(activeSagas, s => s.SagaId == compensatingData.SagaId);
        Assert.DoesNotContain(activeSagas, s => s.SagaId == completedData.SagaId);
    }

    [Fact]
    public async Task InMemorySagaPersistence_GetByState_ReturnsCorrectState()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        var completedData = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "completed",
            State = SagaState.Completed
        };
        var failedData = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "failed",
            State = SagaState.Failed
        };

        await persistence.SaveAsync(completedData);
        await persistence.SaveAsync(failedData);

        // Act
        var completedSagas = new List<OrderSagaData>();
        await foreach (var saga in persistence.GetByStateAsync(SagaState.Completed))
        {
            completedSagas.Add(saga);
        }
        var failedSagas = new List<OrderSagaData>();
        await foreach (var saga in persistence.GetByStateAsync(SagaState.Failed))
        {
            failedSagas.Add(saga);
        }

        // Assert
        Assert.Single(completedSagas);
        Assert.Equal(completedData.SagaId, completedSagas[0].SagaId);
        Assert.Single(failedSagas);
        Assert.Equal(failedData.SagaId, failedSagas[0].SagaId);
    }

    [Fact]
    public async Task InMemorySagaPersistence_GetById_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();

        // Act
        var retrieved = await persistence.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task InMemorySagaPersistence_GetByCorrelationId_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();

        // Act
        var retrieved = await persistence.GetByCorrelationIdAsync("nonexistent");

        // Assert
        Assert.Null(retrieved);
    }
}

// Test implementation classes
public partial class OrderSagaData : SagaDataBase
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    // Test tracking properties
    public string? FailAtStep { get; set; }
    public bool ReserveInventoryExecuted { get; set; }
    public bool ReserveInventoryCompensated { get; set; }
    public bool ProcessPaymentExecuted { get; set; }
    public bool ProcessPaymentCompensated { get; set; }
    public bool ShipOrderExecuted { get; set; }
    public bool ShipOrderCompensated { get; set; }
    public List<string> ExecutionOrder { get; set; } = new();
    public List<string> CompensationOrder { get; set; } = new();
}

public class TestOrderSaga : Saga<OrderSagaData>
{
    public TestOrderSaga()
    {
        AddStep(new ReserveInventoryStep());
        AddStep(new ProcessPaymentStep());
        AddStep(new ShipOrderStep());
    }
}

public class ReserveInventoryStep : ISagaStep<OrderSagaData>
{
    public string Name => "ReserveInventory";

    public ValueTask ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        if (data.SkipSteps?.Contains(Name) == true)
        {
            return ValueTask.CompletedTask; // Skip this step
        }

        if (data.FailAtStep == Name)
        {
            throw new InvalidOperationException($"Failed at {Name}");
        }

        if (data.TimeoutAtStep == Name)
        {
            throw new TimeoutException($"Timeout at {Name}");
        }

        data.ReserveInventoryExecuted = true;
        data.ExecutionOrder.Add(Name);
        return ValueTask.CompletedTask;
    }

    public ValueTask CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        if (data.FailCompensationAtStep == Name)
        {
            throw new InvalidOperationException($"Compensation failed at {Name}");
        }

        data.ReserveInventoryCompensated = true;
        data.CompensationOrder.Add($"{Name}-Compensation");
        return ValueTask.CompletedTask;
    }
}

public class ProcessPaymentStep : ISagaStep<OrderSagaData>
{
    public string Name => "ProcessPayment";

    public ValueTask ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        if (data.SkipSteps?.Contains(Name) == true)
        {
            return ValueTask.CompletedTask; // Skip this step
        }

        if (data.FailAtStep == Name)
        {
            throw new InvalidOperationException($"Failed at {Name}");
        }

        if (data.TimeoutAtStep == Name)
        {
            throw new TimeoutException($"Timeout at {Name}");
        }

        data.ProcessPaymentExecuted = true;
        data.ExecutionOrder.Add(Name);
        return ValueTask.CompletedTask;
    }

    public ValueTask CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        if (data.FailCompensationAtStep == Name)
        {
            throw new InvalidOperationException($"Compensation failed at {Name}");
        }

        data.ProcessPaymentCompensated = true;
        data.CompensationOrder.Add($"{Name}-Compensation");
        return ValueTask.CompletedTask;
    }
}

// Enhanced OrderSagaData for additional test scenarios
public partial class OrderSagaData
{
    public string? FailCompensationAtStep { get; set; }
    public string? TimeoutAtStep { get; set; }
    public string[]? SkipSteps { get; set; }
}

// Additional test classes for edge cases

public class ShipOrderStep : ISagaStep<OrderSagaData>
{
    public string Name => "ShipOrder";

    public ValueTask ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        if (data.FailAtStep == Name)
        {
            throw new InvalidOperationException($"Failed at {Name}");
        }

        if (data.TimeoutAtStep == Name)
        {
            throw new TimeoutException($"Timeout at {Name}");
        }

        data.ShipOrderExecuted = true;
        data.ExecutionOrder.Add(Name);
        return ValueTask.CompletedTask;
    }

    public ValueTask CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        data.ShipOrderCompensated = true;
        data.CompensationOrder.Add($"{Name}-Compensation");
        return ValueTask.CompletedTask;
    }
}

// Additional test classes for edge cases

public class EmptySagaData : SagaDataBase
{
    public string CorrelationId { get; set; } = string.Empty;
}

public class EmptySaga : Saga<EmptySagaData>
{
    // No steps added - empty saga
}

public class SingleStepSagaData : SagaDataBase
{
    public string CorrelationId { get; set; } = string.Empty;
    public int Value { get; set; }
    public bool ShouldFail { get; set; }
    public bool StepExecuted { get; set; }
    public bool StepCompensated { get; set; }
}

public class SingleStepSaga : Saga<SingleStepSagaData>
{
    public SingleStepSaga()
    {
        AddStep(new SingleStep());
    }
}

public class SingleStep : ISagaStep<SingleStepSagaData>
{
    public string Name => "SingleStep";

    public ValueTask ExecuteAsync(SingleStepSagaData data, CancellationToken cancellationToken = default)
    {
        if (data.ShouldFail)
        {
            throw new InvalidOperationException("Step failed as requested");
        }

        data.StepExecuted = true;
        return ValueTask.CompletedTask;
    }

    public ValueTask CompensateAsync(SingleStepSagaData data, CancellationToken cancellationToken = default)
    {
        data.StepCompensated = true;
        return ValueTask.CompletedTask;
    }
}

public class LargeSagaData : SagaDataBase
{
    public string CorrelationId { get; set; } = string.Empty;
    public List<string> ExecutedSteps { get; set; } = new();
}

public class LargeSaga : Saga<LargeSagaData>
{
    public LargeSaga()
    {
        // Add 10 steps
        for (int i = 1; i <= 10; i++)
        {
            AddStep(new NumberedStep(i));
        }
    }
}

public class NumberedStep : ISagaStep<LargeSagaData>
{
    private readonly int _number;

    public NumberedStep(int number)
    {
        _number = number;
    }

    public string Name => $"Step{_number}";

    public ValueTask ExecuteAsync(LargeSagaData data, CancellationToken cancellationToken = default)
    {
        data.ExecutedSteps.Add(Name);
        return ValueTask.CompletedTask;
    }

    public ValueTask CompensateAsync(LargeSagaData data, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}

// Enhanced OrderSagaData for additional test scenarios

