using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.Transactions;
using Relay.Core.Transactions.Tracking;
using Xunit;

namespace Relay.Core.Tests.Transactions.Tracking
{
    public class TransactionTrackerTests
    {
        private readonly Mock<ITransactionMetricsCollector> _metricsCollectorMock;
        private readonly Mock<ITransactionActivitySource> _activitySourceMock;
        private readonly Mock<ITransactionLogger> _transactionLoggerMock;
        private readonly Mock<ILogger<TransactionTracker>> _loggerMock;
        private readonly TransactionTracker _tracker;

        public TransactionTrackerTests()
        {
            _metricsCollectorMock = new Mock<ITransactionMetricsCollector>();
            _activitySourceMock = new Mock<ITransactionActivitySource>();
            _transactionLoggerMock = new Mock<ITransactionLogger>();
            _loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<TransactionTracker>>();

            _tracker = new TransactionTracker(
                _metricsCollectorMock.Object,
                _activitySourceMock.Object,
                _transactionLoggerMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public void RecordSuccess_CallsMetricsCollectorAndActivitySource()
        {
            // Arrange
            var isolationLevel = IsolationLevel.ReadCommitted;
            var requestType = "TestRequest";
            var stopwatch = Stopwatch.StartNew();
            var activity = new Activity("test-activity");
            var context = new Mock<ITransactionContext>();
            context.Setup(x => x.TransactionId).Returns("test-id");

            // Act
            _tracker.RecordSuccess(isolationLevel, requestType, stopwatch, activity, context.Object);

            // Assert
            _metricsCollectorMock.Verify(x => x.RecordTransactionSuccess(
                isolationLevel, requestType, stopwatch.Elapsed), Times.Once);

            _activitySourceMock.Verify(x => x.RecordTransactionSuccess(
                activity, context.Object, stopwatch.Elapsed), Times.Once);
        }

        [Fact]
        public void RecordDistributedSuccess_CallsMetricsCollectorAndLogger()
        {
            // Arrange
            var isolationLevel = IsolationLevel.ReadCommitted;
            var requestType = "TestRequest";
            var transactionId = "distributed-transaction-id";
            var stopwatch = Stopwatch.StartNew();

            // Act
            _tracker.RecordDistributedSuccess(isolationLevel, requestType, stopwatch, transactionId);

            // Assert
            _metricsCollectorMock.Verify(x => x.RecordTransactionSuccess(
                isolationLevel, requestType, stopwatch.Elapsed), Times.Once);

            _transactionLoggerMock.Verify(x => x.LogDistributedTransactionCommitted(
                transactionId, requestType, isolationLevel), Times.Once);
        }

        [Fact]
        public void RecordTimeout_CallsMetricsCollectorAndActivitySource()
        {
            // Arrange
            var requestType = "TestRequest";
            var stopwatch = Stopwatch.StartNew();
            var activity = new Activity("test-activity");
            var context = new Mock<ITransactionContext>();
            context.Setup(x => x.TransactionId).Returns("test-id");
            var exception = new TransactionTimeoutException("Timeout occurred");

            // Act
            _tracker.RecordTimeout(requestType, stopwatch, activity, context.Object, exception);

            // Assert
            _metricsCollectorMock.Verify(x => x.RecordTransactionTimeout(
                requestType, stopwatch.Elapsed), Times.Once);

            _activitySourceMock.Verify(x => x.RecordTransactionTimeout(
                activity, context.Object, exception), Times.Once);
        }

        [Fact]
        public void RecordRollback_CallsMetricsCollectorAndActivitySource()
        {
            // Arrange
            var requestType = "TestRequest";
            var stopwatch = Stopwatch.StartNew();
            var activity = new Activity("test-activity");
            var context = new Mock<ITransactionContext>();
            context.Setup(x => x.TransactionId).Returns("test-id");
            var exception = new InvalidOperationException("Rollback occurred");

            // Act
            _tracker.RecordRollback(requestType, stopwatch, activity, context.Object, exception);

            // Assert
            _metricsCollectorMock.Verify(x => x.RecordTransactionRollback(
                requestType, stopwatch.Elapsed), Times.Once);

            _activitySourceMock.Verify(x => x.RecordTransactionRollback(
                activity, context.Object, exception), Times.Once);
        }

        [Fact]
        public void RecordFailure_CallsMetricsCollectorAndActivitySource()
        {
            // Arrange
            var requestType = "TestRequest";
            var stopwatch = Stopwatch.StartNew();
            var activity = new Activity("test-activity");
            var context = new Mock<ITransactionContext>();
            context.Setup(x => x.TransactionId).Returns("test-id");
            var exception = new InvalidOperationException("Failure occurred");

            // Act
            _tracker.RecordFailure(requestType, stopwatch, activity, context.Object, exception);

            // Assert
            _metricsCollectorMock.Verify(x => x.RecordTransactionFailure(
                requestType, stopwatch.Elapsed), Times.Once);

            _activitySourceMock.Verify(x => x.RecordTransactionFailure(
                activity, context.Object, exception), Times.Once);
        }

        [Fact]
        public void RecordDistributedRollback_CallsMetricsCollector()
        {
            // Arrange
            var requestType = "TestRequest";
            var stopwatch = Stopwatch.StartNew();
            var exception = new InvalidOperationException("Distributed rollback occurred");

            // Act
            _tracker.RecordDistributedRollback(requestType, stopwatch, exception);

            // Assert
            _metricsCollectorMock.Verify(x => x.RecordTransactionRollback(
                requestType, stopwatch.Elapsed), Times.Once);
        }

        [Fact]
        public void RecordDistributedFailure_CallsMetricsCollectorAndLogger()
        {
            // Arrange
            var requestType = "TestRequest";
            var requestTypeName = "TestRequest";
            var transactionId = "distributed-transaction-id";
            var isolationLevel = IsolationLevel.ReadCommitted;
            var stopwatch = Stopwatch.StartNew();
            var exception = new InvalidOperationException("Distributed failure occurred");

            // Act
            _tracker.RecordDistributedFailure(requestType, stopwatch, transactionId, requestTypeName, isolationLevel, exception);

            // Assert
            _metricsCollectorMock.Verify(x => x.RecordTransactionFailure(
                requestType, stopwatch.Elapsed), Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogDistributedTransactionCreated_CallsTransactionLogger()
        {
            // Arrange
            var transactionId = "distributed-transaction-id";
            var requestType = "TestRequest";
            var isolationLevel = IsolationLevel.ReadCommitted;

            // Act
            _tracker.LogDistributedTransactionCreated(transactionId, requestType, isolationLevel);

            // Assert
            _transactionLoggerMock.Verify(x => x.LogDistributedTransactionCreated(
                transactionId, requestType, isolationLevel), Times.Once);
        }

        [Fact]
        public void LogDistributedBeforeCommitWarning_CallsLogger()
        {
            // Arrange
            var exception = new TransactionEventHandlerException("Event failed", "BeforeCommit", "test-id");
            var transactionId = "distributed-transaction-id";
            var requestType = "TestRequest";

            // Act
            _tracker.LogDistributedBeforeCommitWarning(exception, transactionId, requestType);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Constructor_WithNullMetricsCollector_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TransactionTracker(
                null,
                _activitySourceMock.Object,
                _transactionLoggerMock.Object,
                _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullActivitySource_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TransactionTracker(
                _metricsCollectorMock.Object,
                null,
                _transactionLoggerMock.Object,
                _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullTransactionLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TransactionTracker(
                _metricsCollectorMock.Object,
                _activitySourceMock.Object,
                null,
                _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TransactionTracker(
                _metricsCollectorMock.Object,
                _activitySourceMock.Object,
                _transactionLoggerMock.Object,
                null));
        }
    }
}
