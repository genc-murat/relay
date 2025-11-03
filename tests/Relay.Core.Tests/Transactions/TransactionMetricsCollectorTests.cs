using System;
using System.Data;
using Relay.Core.Transactions;
using Xunit;

namespace Relay.Core.Tests.Transactions
{
    public class TransactionMetricsCollectorTests
    {
        [Fact]
        public void GetMetrics_Should_Return_Zero_Metrics_Initially()
        {
            var collector = new TransactionMetricsCollector();

            var metrics = collector.GetMetrics();

            Assert.Equal(0, metrics.TotalTransactions);
            Assert.Equal(0, metrics.SuccessfulTransactions);
            Assert.Equal(0, metrics.FailedTransactions);
        }

        [Fact]
        public void RecordTransactionSuccess_Should_Increment_Counters()
        {
            var collector = new TransactionMetricsCollector();

            collector.RecordTransactionSuccess(
                IsolationLevel.ReadCommitted,
                "TestRequest",
                TimeSpan.FromMilliseconds(100));

            var metrics = collector.GetMetrics();
            Assert.Equal(1, metrics.TotalTransactions);
            Assert.Equal(1, metrics.SuccessfulTransactions);
            Assert.Equal(0, metrics.FailedTransactions);
        }

        [Fact]
        public void RecordTransactionFailure_Should_Increment_Counters()
        {
            var collector = new TransactionMetricsCollector();

            collector.RecordTransactionFailure("TestRequest", TimeSpan.FromMilliseconds(100));

            var metrics = collector.GetMetrics();
            Assert.Equal(1, metrics.TotalTransactions);
            Assert.Equal(0, metrics.SuccessfulTransactions);
            Assert.Equal(1, metrics.FailedTransactions);
        }

        [Fact]
        public void RecordTransactionRollback_Should_Increment_Counters()
        {
            var collector = new TransactionMetricsCollector();

            collector.RecordTransactionRollback("TestRequest", TimeSpan.FromMilliseconds(100));

            var metrics = collector.GetMetrics();
            Assert.Equal(1, metrics.TotalTransactions);
            Assert.Equal(1, metrics.RolledBackTransactions);
        }

        [Fact]
        public void RecordTransactionTimeout_Should_Increment_Counters()
        {
            var collector = new TransactionMetricsCollector();

            collector.RecordTransactionTimeout("TestRequest", TimeSpan.FromMilliseconds(100));

            var metrics = collector.GetMetrics();
            Assert.Equal(1, metrics.TotalTransactions);
            Assert.Equal(1, metrics.TimeoutTransactions);
        }

        [Fact]
        public void GetMetrics_Should_Calculate_Average_Duration()
        {
            var collector = new TransactionMetricsCollector();

            collector.RecordTransactionSuccess(IsolationLevel.ReadCommitted, "Test1", TimeSpan.FromMilliseconds(100));
            collector.RecordTransactionSuccess(IsolationLevel.ReadCommitted, "Test2", TimeSpan.FromMilliseconds(200));

            var metrics = collector.GetMetrics();
            Assert.True(metrics.AverageDurationMs > 0);
        }

        [Fact]
        public void Reset_Should_Clear_All_Metrics()
        {
            var collector = new TransactionMetricsCollector();
            collector.RecordTransactionSuccess(IsolationLevel.ReadCommitted, "Test", TimeSpan.FromMilliseconds(100));

            collector.Reset();

            var metrics = collector.GetMetrics();
            Assert.Equal(0, metrics.TotalTransactions);
        }

        [Fact]
        public void RecordSavepointCreated_Should_Track_Savepoint_Operations()
        {
            var collector = new TransactionMetricsCollector();

            collector.RecordSavepointCreated("sp1");

            var metrics = collector.GetMetrics();
            Assert.True(metrics.SavepointOperations.ContainsKey("Created"));
        }

        [Fact]
        public void RecordSavepointRolledBack_Should_Track_Rollback_Operations()
        {
            var collector = new TransactionMetricsCollector();

            collector.RecordSavepointRolledBack("sp1");

            var metrics = collector.GetMetrics();
            Assert.True(metrics.SavepointOperations.ContainsKey("RolledBack"));
        }

        [Fact]
        public void RecordSavepointReleased_Should_Track_Release_Operations()
        {
            var collector = new TransactionMetricsCollector();

            collector.RecordSavepointReleased("sp1");

            var metrics = collector.GetMetrics();
            Assert.True(metrics.SavepointOperations.ContainsKey("Released"));
        }

        [Fact]
        public void GetTransactionsByRequestType_Should_Return_Request_Type_Breakdown()
        {
            var collector = new TransactionMetricsCollector();

            collector.RecordTransactionSuccess(IsolationLevel.ReadCommitted, "Request1", TimeSpan.FromMilliseconds(100));
            collector.RecordTransactionSuccess(IsolationLevel.ReadCommitted, "Request2", TimeSpan.FromMilliseconds(100));
            collector.RecordTransactionSuccess(IsolationLevel.ReadCommitted, "Request1", TimeSpan.FromMilliseconds(100));

            var byRequestType = collector.GetTransactionsByRequestType();

            Assert.Equal(2, byRequestType["Request1"]);
            Assert.Equal(1, byRequestType["Request2"]);
        }

        [Fact]
        public void Metrics_Should_Be_Thread_Safe()
        {
            var collector = new TransactionMetricsCollector();
            var tasks = new System.Collections.Generic.List<System.Threading.Tasks.Task>();

            // Record transactions from multiple threads
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(System.Threading.Tasks.Task.Run(() =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        collector.RecordTransactionSuccess(
                            IsolationLevel.ReadCommitted,
                            "TestRequest",
                            TimeSpan.FromMilliseconds(100));
                    }
                }));
            }

            System.Threading.Tasks.Task.WaitAll(tasks.ToArray());

            var metrics = collector.GetMetrics();
            Assert.Equal(1000, metrics.TotalTransactions);
            Assert.Equal(1000, metrics.SuccessfulTransactions);
        }

        [Fact]
        public void GetMetrics_Should_Calculate_Success_Rate()
        {
            var collector = new TransactionMetricsCollector();

            collector.RecordTransactionSuccess(IsolationLevel.ReadCommitted, "Test", TimeSpan.FromMilliseconds(100));
            collector.RecordTransactionSuccess(IsolationLevel.ReadCommitted, "Test", TimeSpan.FromMilliseconds(100));
            collector.RecordTransactionFailure("Test", TimeSpan.FromMilliseconds(100));

            var metrics = collector.GetMetrics();

            Assert.Equal(3, metrics.TotalTransactions);
            Assert.True(metrics.SuccessRate > 0.66 && metrics.SuccessRate < 0.67);
        }

        [Fact]
        public void GetMetrics_Should_Track_Isolation_Levels()
        {
            var collector = new TransactionMetricsCollector();

            collector.RecordTransactionSuccess(IsolationLevel.ReadCommitted, "Test", TimeSpan.FromMilliseconds(100));
            collector.RecordTransactionSuccess(IsolationLevel.Serializable, "Test", TimeSpan.FromMilliseconds(100));
            collector.RecordTransactionSuccess(IsolationLevel.ReadCommitted, "Test", TimeSpan.FromMilliseconds(100));

            var metrics = collector.GetMetrics();

            Assert.Equal(2, metrics.TransactionsByIsolationLevel["ReadCommitted"]);
            Assert.Equal(1, metrics.TransactionsByIsolationLevel["Serializable"]);
        }
    }
}
