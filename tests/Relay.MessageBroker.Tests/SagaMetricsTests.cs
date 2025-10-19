
using Microsoft.Extensions.Logging.Abstractions;
using Relay.MessageBroker.Saga;
using Relay.MessageBroker.Saga.Services;
using Moq;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class SagaMetricsTests
{
    [Fact]
    public void SagaMetricsCollector_RecordSagaStarted_ShouldIncrementStartedCount()
    {
        // Arrange
        var logger = NullLogger<InMemorySagaMetricsCollector>.Instance;
        var collector = new InMemorySagaMetricsCollector(logger);

        // Act
        collector.RecordSagaStarted("OrderSaga", Guid.NewGuid(), "CORR-001");
        collector.RecordSagaStarted("OrderSaga", Guid.NewGuid(), "CORR-002");

        // Assert
        var metrics = collector.GetMetrics("OrderSaga");
        Assert.Equal(2, metrics.TotalStarted);
    }

    [Fact]
    public void SagaMetricsCollector_RecordSagaCompleted_ShouldCalculateSuccessRate()
    {
        // Arrange
        var logger = NullLogger<InMemorySagaMetricsCollector>.Instance;
        var collector = new InMemorySagaMetricsCollector(logger);
        var sagaType = "OrderSaga";

        // Act - Record 3 completions and 1 failure
        for (int i = 0; i < 3; i++)
        {
            var sagaId = Guid.NewGuid();
            collector.RecordSagaStarted(sagaType, sagaId, $"CORR-{i}");
            collector.RecordSagaCompleted(sagaType, sagaId, TimeSpan.FromMilliseconds(100));
        }

        var failedSagaId = Guid.NewGuid();
        collector.RecordSagaStarted(sagaType, failedSagaId, "CORR-FAIL");
        collector.RecordSagaFailed(sagaType, failedSagaId, "Step2", TimeSpan.FromMilliseconds(100));

        // Assert
        var metrics = collector.GetMetrics(sagaType);
        Assert.Equal(4, metrics.TotalStarted);
        Assert.Equal(3, metrics.TotalCompleted);
        Assert.Equal(1, metrics.TotalFailed);
        Assert.InRange(metrics.SuccessRate, 74.9, 75.1); // 3 out of 4 = 75%
    }

    [Fact]
    public void SagaMetricsCollector_RecordSagaFailed_ShouldTrackFailuresByStep()
    {
        // Arrange
        var logger = NullLogger<InMemorySagaMetricsCollector>.Instance;
        var collector = new InMemorySagaMetricsCollector(logger);
        var sagaType = "OrderSaga";

        // Act - Record failures in different steps
        collector.RecordSagaFailed(sagaType, Guid.NewGuid(), "CreateOrder", TimeSpan.FromMilliseconds(100));
        collector.RecordSagaFailed(sagaType, Guid.NewGuid(), "CreateOrder", TimeSpan.FromMilliseconds(100));
        collector.RecordSagaFailed(sagaType, Guid.NewGuid(), "ProcessPayment", TimeSpan.FromMilliseconds(100));

        // Assert
        var metrics = collector.GetMetrics(sagaType);
        Assert.True(metrics.FailuresByStep.ContainsKey("CreateOrder"));
        Assert.Equal(2, metrics.FailuresByStep["CreateOrder"]);
        Assert.Equal(1, metrics.FailuresByStep["ProcessPayment"]);
    }

    [Fact]
    public void SagaMetricsCollector_RecordSagaCompensated_ShouldTrackCompensationMetrics()
    {
        // Arrange
        var logger = NullLogger<InMemorySagaMetricsCollector>.Instance;
        var collector = new InMemorySagaMetricsCollector(logger);
        var sagaType = "OrderSaga";

        // Act
        collector.RecordSagaCompensated(sagaType, Guid.NewGuid(), 3, TimeSpan.FromMilliseconds(150));
        collector.RecordSagaCompensated(sagaType, Guid.NewGuid(), 5, TimeSpan.FromMilliseconds(250));

        // Assert
        var metrics = collector.GetMetrics(sagaType);
        Assert.Equal(2, metrics.TotalCompensated);
        Assert.InRange(metrics.AverageCompensationDurationMs, 199.9, 200.1); // (150 + 250) / 2
        Assert.InRange(metrics.AverageStepsCompensated, 3.9, 4.1); // (3 + 5) / 2
    }

    [Fact]
    public void SagaMetricsCollector_RecordSagaTimedOut_ShouldIncrementTimeoutCount()
    {
        // Arrange
        var logger = NullLogger<InMemorySagaMetricsCollector>.Instance;
        var collector = new InMemorySagaMetricsCollector(logger);
        var sagaType = "OrderSaga";

        // Act
        collector.RecordSagaTimedOut(sagaType, Guid.NewGuid(), SagaState.Running);
        collector.RecordSagaTimedOut(sagaType, Guid.NewGuid(), SagaState.Compensating);

        // Assert
        var metrics = collector.GetMetrics(sagaType);
        Assert.Equal(2, metrics.TotalTimedOut);
    }

    [Fact]
    public void SagaMetricsCollector_CalculatePercentiles_ShouldReturnCorrectValues()
    {
        // Arrange
        var logger = NullLogger<InMemorySagaMetricsCollector>.Instance;
        var collector = new InMemorySagaMetricsCollector(logger);
        var sagaType = "OrderSaga";

        // Act - Record sagas with known durations: 100, 200, 300, 400, 500 ms
        var durations = new[] { 100, 200, 300, 400, 500 };
        foreach (var duration in durations)
        {
            var sagaId = Guid.NewGuid();
            collector.RecordSagaStarted(sagaType, sagaId, Guid.NewGuid().ToString());
            collector.RecordSagaCompleted(sagaType, sagaId, TimeSpan.FromMilliseconds(duration));
        }

        // Assert
        var metrics = collector.GetMetrics(sagaType);
        Assert.InRange(metrics.P50DurationMs, 299.9, 300.1); // Median
        Assert.InRange(metrics.P95DurationMs, 499.9, 500.1); // 95th percentile
        Assert.InRange(metrics.P99DurationMs, 499.9, 500.1); // 99th percentile
        Assert.InRange(metrics.AverageDurationMs, 299.9, 300.1); // (100+200+300+400+500)/5
    }

    [Fact]
    public void SagaMetricsCollector_RecordStepExecuted_ShouldTrackStepMetrics()
    {
        // Arrange
        var logger = NullLogger<InMemorySagaMetricsCollector>.Instance;
        var collector = new InMemorySagaMetricsCollector(logger);
        var sagaType = "OrderSaga";
        var stepName = "CreateOrder";

        // Act - Record 3 successful and 1 failed execution
        collector.RecordStepExecuted(sagaType, stepName, TimeSpan.FromMilliseconds(100), success: true);
        collector.RecordStepExecuted(sagaType, stepName, TimeSpan.FromMilliseconds(200), success: true);
        collector.RecordStepExecuted(sagaType, stepName, TimeSpan.FromMilliseconds(300), success: true);
        collector.RecordStepExecuted(sagaType, stepName, TimeSpan.FromMilliseconds(400), success: false);

        // Assert
        var metrics = collector.GetMetrics(sagaType);
        Assert.True(metrics.StepMetrics.ContainsKey(stepName));

        var stepMetrics = metrics.StepMetrics[stepName];
        Assert.Equal(4, stepMetrics.TotalExecutions);
        Assert.Equal(3, stepMetrics.Successes);
        Assert.Equal(1, stepMetrics.Failures);
        Assert.InRange(stepMetrics.SuccessRate, 74.9, 75.1); // 3 out of 4 = 75%
        Assert.InRange(stepMetrics.AverageDurationMs, 249.9, 250.1); // (100+200+300+400)/4
    }

    [Fact]
    public void SagaMetricsCollector_GetAllMetrics_ShouldReturnAllSagaTypes()
    {
        // Arrange
        var logger = NullLogger<InMemorySagaMetricsCollector>.Instance;
        var collector = new InMemorySagaMetricsCollector(logger);

        // Act
        collector.RecordSagaStarted("OrderSaga", Guid.NewGuid(), "CORR-001");
        collector.RecordSagaStarted("PaymentSaga", Guid.NewGuid(), "CORR-002");
        collector.RecordSagaStarted("ShippingSaga", Guid.NewGuid(), "CORR-003");

        // Assert
        var allMetrics = collector.GetAllMetrics();
        Assert.Equal(3, allMetrics.Count);
        Assert.True(allMetrics.ContainsKey("OrderSaga"));
        Assert.True(allMetrics.ContainsKey("PaymentSaga"));
        Assert.True(allMetrics.ContainsKey("ShippingSaga"));
    }

    [Fact]
    public void SagaMetricsCollector_Reset_ShouldClearAllMetrics()
    {
        // Arrange
        var logger = NullLogger<InMemorySagaMetricsCollector>.Instance;
        var collector = new InMemorySagaMetricsCollector(logger);
        var sagaType = "OrderSaga";

        collector.RecordSagaStarted(sagaType, Guid.NewGuid(), "CORR-001");
        collector.RecordSagaCompleted(sagaType, Guid.NewGuid(), TimeSpan.FromMilliseconds(100));
        collector.RecordSagaFailed(sagaType, Guid.NewGuid(), "Step1", TimeSpan.FromMilliseconds(100));

        // Act
        collector.Reset();

        // Assert
        var metrics = collector.GetMetrics(sagaType);
        Assert.Equal(0, metrics.TotalStarted);
        Assert.Equal(0, metrics.TotalCompleted);
        Assert.Equal(0, metrics.TotalFailed);
    }

    [Fact]
    public void SagaMetricsCollector_EmptyMetrics_ShouldReturnZeroValues()
    {
        // Arrange
        var logger = NullLogger<InMemorySagaMetricsCollector>.Instance;
        var collector = new InMemorySagaMetricsCollector(logger);

        // Act
        var metrics = collector.GetMetrics("NonExistentSaga");

        // Assert
        Assert.Equal("NonExistentSaga", metrics.SagaType);
        Assert.Equal(0, metrics.TotalStarted);
        Assert.Equal(0, metrics.TotalCompleted);
        Assert.Equal(0, metrics.TotalFailed);
        Assert.Equal(0, metrics.SuccessRate);
        Assert.Equal(0, metrics.AverageDurationMs);
    }

    [Fact]
    public void SagaMetricsCollector_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var logger = NullLogger<InMemorySagaMetricsCollector>.Instance;
        var collector = new InMemorySagaMetricsCollector(logger);
        var sagaType = "OrderSaga";
        var iterations = 100;

        // Act - Simulate concurrent access from multiple threads
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < iterations; j++)
                {
                    var sagaId = Guid.NewGuid();
                    collector.RecordSagaStarted(sagaType, sagaId, $"CORR-{j}");
                    collector.RecordSagaCompleted(sagaType, sagaId, TimeSpan.FromMilliseconds(100));
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var metrics = collector.GetMetrics(sagaType);
        Assert.Equal(iterations * 10, metrics.TotalStarted); // 100 * 10 = 1000
        Assert.Equal(iterations * 10, metrics.TotalCompleted);
    }

    [Fact]
    public void SagaMetrics_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var metrics = new SagaMetrics
        {
            SagaType = "OrderSaga",
            TotalStarted = 100,
            TotalCompleted = 80,
            TotalFailed = 15,
            TotalCompensated = 5,
            TotalTimedOut = 3,
            SuccessRate = 80.0,
            AverageDurationMs = 250.5
        };

        // Act
        var result = metrics.ToString();

        // Assert
        Assert.Contains("OrderSaga", result);
        Assert.Contains("Started=100", result);
        Assert.Contains("Completed=80", result);
        Assert.Contains("Failed=15", result);
        Assert.Contains("Compensated=5", result);
        Assert.Contains("TimedOut=3", result);
        Assert.Contains("SuccessRate=80.0%", result);
        Assert.Contains("AvgDuration=250ms", result);
    }

    [Fact]
    public void StepMetrics_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var stepMetrics = new StepMetricsData
        {
            TotalExecutions = 100,
            Successes = 95,
            Failures = 5,
            SuccessRate = 95.0,
            AverageDurationMs = 125.5
        };

        // Act
        var result = stepMetrics.ToString();

        // Assert
        Assert.Contains("Executions=100", result);
        Assert.Contains("Successes=95", result);
        Assert.Contains("Failures=5", result);
        Assert.Contains("SuccessRate=95.0%", result);
        Assert.Contains("AvgDuration=", result); // Just check it has this field, rounding may vary
        Assert.True(result.Contains("125ms") || result.Contains("126ms")); // Accept either due to rounding
    }

    [Fact]
    public void SagaMetricsCollector_MixedOutcomes_ShouldTrackAllCorrectly()
    {
        // Arrange
        var logger = NullLogger<InMemorySagaMetricsCollector>.Instance;
        var collector = new InMemorySagaMetricsCollector(logger);
        var sagaType = "OrderSaga";

        // Act - Create a realistic scenario with mixed outcomes
        // 5 successful sagas
        for (int i = 0; i < 5; i++)
        {
            var sagaId = Guid.NewGuid();
            collector.RecordSagaStarted(sagaType, sagaId, $"SUCCESS-{i}");
            collector.RecordStepExecuted(sagaType, "CreateOrder", TimeSpan.FromMilliseconds(50), true);
            collector.RecordStepExecuted(sagaType, "ProcessPayment", TimeSpan.FromMilliseconds(100), true);
            collector.RecordSagaCompleted(sagaType, sagaId, TimeSpan.FromMilliseconds(150));
        }

        // 2 failed sagas
        for (int i = 0; i < 2; i++)
        {
            var sagaId = Guid.NewGuid();
            collector.RecordSagaStarted(sagaType, sagaId, $"FAILED-{i}");
            collector.RecordStepExecuted(sagaType, "CreateOrder", TimeSpan.FromMilliseconds(50), true);
            collector.RecordStepExecuted(sagaType, "ProcessPayment", TimeSpan.FromMilliseconds(100), false);
            collector.RecordSagaFailed(sagaType, sagaId, "ProcessPayment", TimeSpan.FromMilliseconds(150));
        }

        // 1 compensated saga
        var compensatedId = Guid.NewGuid();
        collector.RecordSagaStarted(sagaType, compensatedId, "COMPENSATED-1");
        collector.RecordSagaCompensated(sagaType, compensatedId, 2, TimeSpan.FromMilliseconds(200));

        // 1 timed out saga
        var timedOutId = Guid.NewGuid();
        collector.RecordSagaStarted(sagaType, timedOutId, "TIMEOUT-1");
        collector.RecordSagaTimedOut(sagaType, timedOutId, SagaState.Running);

        // Assert
        var metrics = collector.GetMetrics(sagaType);

        // Saga counts
        Assert.Equal(9, metrics.TotalStarted);
        Assert.Equal(5, metrics.TotalCompleted);
        Assert.Equal(2, metrics.TotalFailed);
        Assert.Equal(1, metrics.TotalCompensated);
        Assert.Equal(1, metrics.TotalTimedOut);

        // Success rate (5 completed out of 8 finished = 62.5%)
        Assert.InRange(metrics.SuccessRate, 62.4, 62.6);

        // Step metrics
        Assert.True(metrics.StepMetrics.ContainsKey("CreateOrder"));
        Assert.Equal(7, metrics.StepMetrics["CreateOrder"].TotalExecutions); // 5 + 2
        Assert.Equal(7, metrics.StepMetrics["CreateOrder"].Successes);

        Assert.True(metrics.StepMetrics.ContainsKey("ProcessPayment"));
        Assert.Equal(7, metrics.StepMetrics["ProcessPayment"].TotalExecutions);
        Assert.Equal(5, metrics.StepMetrics["ProcessPayment"].Successes);
        Assert.Equal(2, metrics.StepMetrics["ProcessPayment"].Failures);

        // Failure tracking
        Assert.True(metrics.FailuresByStep.ContainsKey("ProcessPayment"));
        Assert.Equal(2, metrics.FailuresByStep["ProcessPayment"]);
    }

    // Additional metrics tests from SagaTests.cs

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
}


