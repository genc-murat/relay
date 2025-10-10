using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.MessageBroker.Saga;
using Relay.MessageBroker.Saga.Services;
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
        metrics.TotalStarted.Should().Be(2);
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
        metrics.TotalStarted.Should().Be(4);
        metrics.TotalCompleted.Should().Be(3);
        metrics.TotalFailed.Should().Be(1);
        metrics.SuccessRate.Should().BeApproximately(75.0, 0.1); // 3 out of 4 = 75%
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
        metrics.FailuresByStep.Should().ContainKey("CreateOrder");
        metrics.FailuresByStep["CreateOrder"].Should().Be(2);
        metrics.FailuresByStep["ProcessPayment"].Should().Be(1);
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
        metrics.TotalCompensated.Should().Be(2);
        metrics.AverageCompensationDurationMs.Should().BeApproximately(200.0, 0.1); // (150 + 250) / 2
        metrics.AverageStepsCompensated.Should().BeApproximately(4.0, 0.1); // (3 + 5) / 2
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
        metrics.TotalTimedOut.Should().Be(2);
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
        metrics.P50DurationMs.Should().BeApproximately(300.0, 0.1); // Median
        metrics.P95DurationMs.Should().BeApproximately(500.0, 0.1); // 95th percentile
        metrics.P99DurationMs.Should().BeApproximately(500.0, 0.1); // 99th percentile
        metrics.AverageDurationMs.Should().BeApproximately(300.0, 0.1); // (100+200+300+400+500)/5
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
        metrics.StepMetrics.Should().ContainKey(stepName);

        var stepMetrics = metrics.StepMetrics[stepName];
        stepMetrics.TotalExecutions.Should().Be(4);
        stepMetrics.Successes.Should().Be(3);
        stepMetrics.Failures.Should().Be(1);
        stepMetrics.SuccessRate.Should().BeApproximately(75.0, 0.1); // 3 out of 4 = 75%
        stepMetrics.AverageDurationMs.Should().BeApproximately(250.0, 0.1); // (100+200+300+400)/4
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
        allMetrics.Should().HaveCount(3);
        allMetrics.Should().ContainKey("OrderSaga");
        allMetrics.Should().ContainKey("PaymentSaga");
        allMetrics.Should().ContainKey("ShippingSaga");
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
        metrics.TotalStarted.Should().Be(0);
        metrics.TotalCompleted.Should().Be(0);
        metrics.TotalFailed.Should().Be(0);
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
        metrics.SagaType.Should().Be("NonExistentSaga");
        metrics.TotalStarted.Should().Be(0);
        metrics.TotalCompleted.Should().Be(0);
        metrics.TotalFailed.Should().Be(0);
        metrics.SuccessRate.Should().Be(0);
        metrics.AverageDurationMs.Should().Be(0);
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
        metrics.TotalStarted.Should().Be(iterations * 10); // 100 * 10 = 1000
        metrics.TotalCompleted.Should().Be(iterations * 10);
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
        result.Should().Contain("OrderSaga");
        result.Should().Contain("Started=100");
        result.Should().Contain("Completed=80");
        result.Should().Contain("Failed=15");
        result.Should().Contain("Compensated=5");
        result.Should().Contain("TimedOut=3");
        result.Should().Contain("SuccessRate=80.0%");
        result.Should().Contain("AvgDuration=250ms");
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
        result.Should().Contain("Executions=100");
        result.Should().Contain("Successes=95");
        result.Should().Contain("Failures=5");
        result.Should().Contain("SuccessRate=95.0%");
        result.Should().Contain("AvgDuration="); // Just check it has this field, rounding may vary
        result.Should().ContainAny("125ms", "126ms"); // Accept either due to rounding
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
        metrics.TotalStarted.Should().Be(9);
        metrics.TotalCompleted.Should().Be(5);
        metrics.TotalFailed.Should().Be(2);
        metrics.TotalCompensated.Should().Be(1);
        metrics.TotalTimedOut.Should().Be(1);

        // Success rate (5 completed out of 8 finished = 62.5%)
        metrics.SuccessRate.Should().BeApproximately(62.5, 0.1);

        // Step metrics
        metrics.StepMetrics.Should().ContainKey("CreateOrder");
        metrics.StepMetrics["CreateOrder"].TotalExecutions.Should().Be(7); // 5 + 2
        metrics.StepMetrics["CreateOrder"].Successes.Should().Be(7);

        metrics.StepMetrics.Should().ContainKey("ProcessPayment");
        metrics.StepMetrics["ProcessPayment"].TotalExecutions.Should().Be(7);
        metrics.StepMetrics["ProcessPayment"].Successes.Should().Be(5);
        metrics.StepMetrics["ProcessPayment"].Failures.Should().Be(2);

        // Failure tracking
        metrics.FailuresByStep.Should().ContainKey("ProcessPayment");
        metrics.FailuresByStep["ProcessPayment"].Should().Be(2);
    }
}
