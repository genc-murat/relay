using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
    }
}
