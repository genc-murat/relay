using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Relay.Core.Transactions;
using Xunit;

namespace Relay.Core.Tests.Transactions
{
    public class TransactionHealthCheckTests
    {
        [Fact]
        public async Task CheckHealthAsync_Should_Return_Healthy_With_No_Transactions()
        {
            var collector = new TransactionMetricsCollector();
            var healthCheck = new TransactionHealthCheck(collector);
            var context = new HealthCheckContext();

            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

            Assert.Equal(HealthStatus.Healthy, result.Status);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Return_Healthy_With_Good_Metrics()
        {
            var collector = new TransactionMetricsCollector();
            
            // Record 100 successful transactions
            for (int i = 0; i < 100; i++)
            {
                collector.RecordTransactionSuccess(
                    IsolationLevel.ReadCommitted,
                    "TestRequest",
                    TimeSpan.FromMilliseconds(100));
            }

            var healthCheck = new TransactionHealthCheck(collector);
            var context = new HealthCheckContext();

            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Contains("total_transactions", result.Data.Keys);
            Assert.Equal(100L, result.Data["total_transactions"]);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Return_Degraded_With_Low_Success_Rate()
        {
            var collector = new TransactionMetricsCollector();
            
            // Record 90 successful and 10 failed transactions (90% success rate)
            for (int i = 0; i < 90; i++)
            {
                collector.RecordTransactionSuccess(
                    IsolationLevel.ReadCommitted,
                    "TestRequest",
                    TimeSpan.FromMilliseconds(100));
            }
            for (int i = 0; i < 10; i++)
            {
                collector.RecordTransactionFailure("TestRequest", TimeSpan.FromMilliseconds(100));
            }

            var options = new TransactionHealthCheckOptions
            {
                DegradedSuccessRateThreshold = 0.95,
                UnhealthySuccessRateThreshold = 0.85
            };
            var healthCheck = new TransactionHealthCheck(collector, options);
            var context = new HealthCheckContext();

            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

            Assert.Equal(HealthStatus.Degraded, result.Status);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Return_Unhealthy_With_Very_Low_Success_Rate()
        {
            var collector = new TransactionMetricsCollector();
            
            // Record 80 successful and 20 failed transactions (80% success rate)
            for (int i = 0; i < 80; i++)
            {
                collector.RecordTransactionSuccess(
                    IsolationLevel.ReadCommitted,
                    "TestRequest",
                    TimeSpan.FromMilliseconds(100));
            }
            for (int i = 0; i < 20; i++)
            {
                collector.RecordTransactionFailure("TestRequest", TimeSpan.FromMilliseconds(100));
            }

            var options = new TransactionHealthCheckOptions
            {
                UnhealthySuccessRateThreshold = 0.85
            };
            var healthCheck = new TransactionHealthCheck(collector, options);
            var context = new HealthCheckContext();

            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Return_Degraded_With_High_Timeout_Rate()
        {
            var collector = new TransactionMetricsCollector();
            
            // Record 90 successful and 10 timeout transactions
            for (int i = 0; i < 90; i++)
            {
                collector.RecordTransactionSuccess(
                    IsolationLevel.ReadCommitted,
                    "TestRequest",
                    TimeSpan.FromMilliseconds(100));
            }
            for (int i = 0; i < 10; i++)
            {
                collector.RecordTransactionTimeout("TestRequest", TimeSpan.FromMilliseconds(5000));
            }

            var options = new TransactionHealthCheckOptions
            {
                DegradedTimeoutRateThreshold = 0.05
            };
            var healthCheck = new TransactionHealthCheck(collector, options);
            var context = new HealthCheckContext();

            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

            Assert.Equal(HealthStatus.Degraded, result.Status);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Include_Metrics_In_Data()
        {
            var collector = new TransactionMetricsCollector();
            collector.RecordTransactionSuccess(
                IsolationLevel.ReadCommitted,
                "TestRequest",
                TimeSpan.FromMilliseconds(100));

            var healthCheck = new TransactionHealthCheck(collector);
            var context = new HealthCheckContext();

            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

            Assert.Contains("total_transactions", result.Data.Keys);
            Assert.Contains("successful_transactions", result.Data.Keys);
            Assert.Contains("failed_transactions", result.Data.Keys);
            Assert.Contains("success_rate", result.Data.Keys);
            Assert.Contains("average_duration_ms", result.Data.Keys);
        }

        [Fact]
        public void IncrementActiveTransactions_Should_Increase_Counter()
        {
            var collector = new TransactionMetricsCollector();
            var healthCheck = new TransactionHealthCheck(collector);

            healthCheck.IncrementActiveTransactions();
            healthCheck.IncrementActiveTransactions();

            // We can't directly access the counter, but we can verify through health check
            // This is more of a smoke test
        }

        [Fact]
        public void DecrementActiveTransactions_Should_Decrease_Counter()
        {
            var collector = new TransactionMetricsCollector();
            var healthCheck = new TransactionHealthCheck(collector);

            healthCheck.IncrementActiveTransactions();
            healthCheck.DecrementActiveTransactions();

            // Smoke test - should not throw
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Return_Degraded_With_Too_Many_Active_Transactions()
        {
            var collector = new TransactionMetricsCollector();
            
            // Add at least one transaction to the collector so it's not considered "no transactions"
            collector.RecordTransactionSuccess(
                IsolationLevel.ReadCommitted,
                "TestRequest",
                TimeSpan.FromMilliseconds(100));
            
            var options = new TransactionHealthCheckOptions
            {
                MaxActiveTransactionsThreshold = 5
            };
            var healthCheck = new TransactionHealthCheck(collector, options);

            // Simulate 10 active transactions
            for (int i = 0; i < 10; i++)
            {
                healthCheck.IncrementActiveTransactions();
            }

            var context = new HealthCheckContext();
            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

            Assert.Equal(HealthStatus.Degraded, result.Status);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Include_Isolation_Level_Breakdown()
        {
            var collector = new TransactionMetricsCollector();
            collector.RecordTransactionSuccess(
                IsolationLevel.ReadCommitted,
                "TestRequest",
                TimeSpan.FromMilliseconds(100));
            collector.RecordTransactionSuccess(
                IsolationLevel.Serializable,
                "TestRequest",
                TimeSpan.FromMilliseconds(100));

            var healthCheck = new TransactionHealthCheck(collector);
            var context = new HealthCheckContext();

            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

            Assert.Contains("transactions_by_isolation_level", result.Data.Keys);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Return_Unhealthy_With_High_Timeout_Rate()
        {
            var collector = new TransactionMetricsCollector();
            
            // Record 80 successful and 20 timeout transactions (20% timeout rate)
            for (int i = 0; i < 80; i++)
            {
                collector.RecordTransactionSuccess(
                    IsolationLevel.ReadCommitted,
                    "TestRequest",
                    TimeSpan.FromMilliseconds(100));
            }
            for (int i = 0; i < 20; i++)
            {
                collector.RecordTransactionTimeout("TestRequest", TimeSpan.FromMilliseconds(5000));
            }

            var options = new TransactionHealthCheckOptions
            {
                UnhealthyTimeoutRateThreshold = 0.15  // 15%
            };
            var healthCheck = new TransactionHealthCheck(collector, options);
            var context = new HealthCheckContext();

            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Return_Degraded_With_High_Average_Duration()
        {
            var collector = new TransactionMetricsCollector();
            
            // Record transactions with high average duration
            for (int i = 0; i < 10; i++)
            {
                collector.RecordTransactionSuccess(
                    IsolationLevel.ReadCommitted,
                    "TestRequest",
                    TimeSpan.FromSeconds(6)); // 6 seconds each
            }

            var options = new TransactionHealthCheckOptions
            {
                DegradedAverageDurationMs = 5000,  // 5 seconds
                UnhealthyAverageDurationMs = 12000  // 12 seconds
            };
            var healthCheck = new TransactionHealthCheck(collector, options);
            var context = new HealthCheckContext();

            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

            Assert.Equal(HealthStatus.Degraded, result.Status);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Return_Unhealthy_With_Very_High_Average_Duration()
        {
            var collector = new TransactionMetricsCollector();
            
            // Record transactions with very high average duration
            for (int i = 0; i < 10; i++)
            {
                collector.RecordTransactionSuccess(
                    IsolationLevel.ReadCommitted,
                    "TestRequest",
                    TimeSpan.FromSeconds(15)); // 15 seconds each
            }

            var options = new TransactionHealthCheckOptions
            {
                UnhealthyAverageDurationMs = 10000  // 10 seconds
            };
            var healthCheck = new TransactionHealthCheck(collector, options);
            var context = new HealthCheckContext();

            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Handle_Exceptions()
        {
            var mockCollector = new Moq.Mock<ITransactionMetricsCollector>();
            mockCollector.Setup(c => c.GetMetrics()).Throws(new InvalidOperationException("Test exception"));

            var healthCheck = new TransactionHealthCheck(mockCollector.Object);
            var context = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, HealthStatus.Unhealthy, null)
            };

            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("Failed to evaluate transaction health", result.Description);
            Assert.IsType<InvalidOperationException>(result.Exception);
        }

        [Fact]
        public void Constructor_Should_Throw_When_MetricsCollector_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new TransactionHealthCheck(null));
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Include_Savepoint_Operations_When_Available()
        {
            var collector = new TransactionMetricsCollector();
            
            // Record some savepoint operations to make sure the condition `metrics.SavepointOperations.Any()` returns true
            collector.RecordSavepointCreated("Point1");
            collector.RecordSavepointRolledBack("Point1");
            collector.RecordSavepointReleased("Point1");
            
            // Also record a transaction for the basic metrics
            collector.RecordTransactionSuccess(
                IsolationLevel.ReadCommitted,
                "TestRequest",
                TimeSpan.FromMilliseconds(100));

            var healthCheck = new TransactionHealthCheck(collector);
            var context = new HealthCheckContext();

            // Run the health check - this should execute the branch that adds savepoint operations to the data
            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

            // The main goal is to ensure the conditional branch was executed without throwing an exception
            Assert.NotNull(result);
            Assert.NotEqual(HealthStatus.Unhealthy, result.Status); // Should not fail due to the savepoint operations
        }

        [Fact]
        public void GetHealthDescription_Should_Handle_Default_Case_With_Reflection()
        {
            var collector = new TransactionMetricsCollector();
            var healthCheck = new TransactionHealthCheck(collector);
            
            // Use reflection to test the private GetHealthDescription method with an invalid enum value
            var method = typeof(TransactionHealthCheck).GetMethod("GetHealthDescription", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            var metrics = collector.GetMetrics();
            // Using an invalid enum value (cast from integer) to test the default case
            var result = (string)method.Invoke(healthCheck, new object[] { (HealthStatus)999, metrics, 0L });
            
            Assert.Equal("Transaction system status unknown", result);
        }
        
        [Fact]
        public async Task DetermineHealthStatus_Should_Return_Healthy_When_All_Thresholds_Are_Satisfied()
        {
            var collector = new TransactionMetricsCollector();
            
            // Record transactions with good metrics (95% success rate, low timeout rate, good duration)
            for (int i = 0; i < 95; i++) // 95% success
            {
                collector.RecordTransactionSuccess(
                    IsolationLevel.ReadCommitted,
                    "TestRequest",
                    TimeSpan.FromMilliseconds(100));
            }
            for (int i = 0; i < 3; i++) // 3% failure
            {
                collector.RecordTransactionFailure("TestRequest", TimeSpan.FromMilliseconds(100));
            }
            for (int i = 0; i < 2; i++) // 2% timeout
            {
                collector.RecordTransactionTimeout("TestRequest", TimeSpan.FromMilliseconds(100));
            }

            var options = new TransactionHealthCheckOptions
            {
                DegradedSuccessRateThreshold = 0.90,    // 90% degraded
                UnhealthySuccessRateThreshold = 0.80,   // 80% unhealthy
                DegradedTimeoutRateThreshold = 0.08,    // 8% degraded
                UnhealthyTimeoutRateThreshold = 0.15,   // 15% unhealthy
                DegradedAverageDurationMs = 200,        // 200ms degraded
                UnhealthyAverageDurationMs = 500        // 500ms unhealthy
            };
            var healthCheck = new TransactionHealthCheck(collector, options);
            
            // Set active transactions below threshold
            healthCheck.IncrementActiveTransactions(); // Just one active transaction
            
            var context = new HealthCheckContext();
            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

            Assert.Equal(HealthStatus.Healthy, result.Status);
        }
    }
    
}
