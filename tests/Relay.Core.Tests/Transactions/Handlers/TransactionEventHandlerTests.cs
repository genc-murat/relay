using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.Transactions;
using Relay.Core.Transactions.Handlers;
using Xunit;

namespace Relay.Core.Tests.Transactions.Handlers
{
    public class TransactionEventHandlerTests
    {
        private readonly Mock<TransactionEventPublisher> _eventPublisherMock;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<TransactionEventHandler>> _loggerMock;
        private readonly TransactionEventHandler _handler;

        public TransactionEventHandlerTests()
        {
            _eventPublisherMock = new Mock<TransactionEventPublisher>();
            _loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<TransactionEventHandler>>();
            
            _handler = new TransactionEventHandler(
                _eventPublisherMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task PublishBeforeBeginAsync_WhenEventPublisherSucceeds_LogsNothing()
        {
            // Arrange
            var context = new TransactionEventContext
            {
                TransactionId = "test-id",
                RequestType = "TestRequest"
            };

            _eventPublisherMock.Setup(x => x.PublishBeforeBeginAsync(context, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.PublishBeforeBeginAsync(context, CancellationToken.None);

            // Assert
            _eventPublisherMock.Verify(x => x.PublishBeforeBeginAsync(context, CancellationToken.None), Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task PublishBeforeBeginAsync_WhenEventPublisherFails_LogsWarning()
        {
            // Arrange
            var context = new TransactionEventContext
            {
                TransactionId = "test-id",
                RequestType = "TestRequest"
            };
            var expectedException = new InvalidOperationException("Event failed");

            _eventPublisherMock.Setup(x => x.PublishBeforeBeginAsync(context, It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act
            await _handler.PublishBeforeBeginAsync(context, CancellationToken.None);

            // Assert
            _eventPublisherMock.Verify(x => x.PublishBeforeBeginAsync(context, CancellationToken.None), Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task PublishAfterBeginAsync_WhenEventPublisherSucceeds_LogsNothing()
        {
            // Arrange
            var context = new TransactionEventContext
            {
                TransactionId = "test-id",
                RequestType = "TestRequest"
            };

            _eventPublisherMock.Setup(x => x.PublishAfterBeginAsync(context, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.PublishAfterBeginAsync(context, CancellationToken.None);

            // Assert
            _eventPublisherMock.Verify(x => x.PublishAfterBeginAsync(context, CancellationToken.None), Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task PublishAfterBeginAsync_WhenEventPublisherFails_LogsWarning()
        {
            // Arrange
            var context = new TransactionEventContext
            {
                TransactionId = "test-id",
                RequestType = "TestRequest"
            };
            var expectedException = new InvalidOperationException("Event failed");

            _eventPublisherMock.Setup(x => x.PublishAfterBeginAsync(context, It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act
            await _handler.PublishAfterBeginAsync(context, CancellationToken.None);

            // Assert
            _eventPublisherMock.Verify(x => x.PublishAfterBeginAsync(context, CancellationToken.None), Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task PublishBeforeCommitAsync_WhenEventPublisherSucceeds_LogsNothing()
        {
            // Arrange
            var context = new TransactionEventContext
            {
                TransactionId = "test-id",
                RequestType = "TestRequest"
            };

            _eventPublisherMock.Setup(x => x.PublishBeforeCommitAsync(context, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.PublishBeforeCommitAsync(context, CancellationToken.None);

            // Assert
            _eventPublisherMock.Verify(x => x.PublishBeforeCommitAsync(context, CancellationToken.None), Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task PublishBeforeCommitAsync_WhenEventPublisherFails_ThrowsTransactionEventHandlerException()
        {
            // Arrange
            var context = new TransactionEventContext
            {
                TransactionId = "test-id",
                RequestType = "TestRequest"
            };
            var expectedException = new InvalidOperationException("Event failed");

            _eventPublisherMock.Setup(x => x.PublishBeforeCommitAsync(context, It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<TransactionEventHandlerException>(
                async () => await _handler.PublishBeforeCommitAsync(context, CancellationToken.None));

            Assert.Equal("BeforeCommit", actualException.EventName);
            Assert.Equal("test-id", actualException.TransactionId);
            Assert.Equal(expectedException, actualException.InnerException);

            _eventPublisherMock.Verify(x => x.PublishBeforeCommitAsync(context, CancellationToken.None), Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task PublishAfterCommitAsync_WhenEventPublisherSucceeds_LogsNothing()
        {
            // Arrange
            var context = new TransactionEventContext
            {
                TransactionId = "test-id",
                RequestType = "TestRequest"
            };

            _eventPublisherMock.Setup(x => x.PublishAfterCommitAsync(context, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.PublishAfterCommitAsync(context, CancellationToken.None);

            // Assert
            _eventPublisherMock.Verify(x => x.PublishAfterCommitAsync(context, CancellationToken.None), Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task PublishAfterCommitAsync_WhenEventPublisherFails_LogsWarning()
        {
            // Arrange
            var context = new TransactionEventContext
            {
                TransactionId = "test-id",
                RequestType = "TestRequest"
            };
            var expectedException = new InvalidOperationException("Event failed");

            _eventPublisherMock.Setup(x => x.PublishAfterCommitAsync(context, It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act
            await _handler.PublishAfterCommitAsync(context, CancellationToken.None);

            // Assert
            _eventPublisherMock.Verify(x => x.PublishAfterCommitAsync(context, CancellationToken.None), Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task PublishBeforeRollbackAsync_WhenEventPublisherSucceeds_LogsNothing()
        {
            // Arrange
            var context = new TransactionEventContext
            {
                TransactionId = "test-id",
                RequestType = "TestRequest"
            };

            _eventPublisherMock.Setup(x => x.PublishBeforeRollbackAsync(context, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.PublishBeforeRollbackAsync(context, CancellationToken.None);

            // Assert
            _eventPublisherMock.Verify(x => x.PublishBeforeRollbackAsync(context, CancellationToken.None), Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task PublishBeforeRollbackAsync_WhenEventPublisherFails_LogsWarning()
        {
            // Arrange
            var context = new TransactionEventContext
            {
                TransactionId = "test-id",
                RequestType = "TestRequest"
            };
            var expectedException = new InvalidOperationException("Event failed");

            _eventPublisherMock.Setup(x => x.PublishBeforeRollbackAsync(context, It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act
            await _handler.PublishBeforeRollbackAsync(context, CancellationToken.None);

            // Assert
            _eventPublisherMock.Verify(x => x.PublishBeforeRollbackAsync(context, CancellationToken.None), Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task PublishAfterRollbackAsync_WhenEventPublisherSucceeds_LogsNothing()
        {
            // Arrange
            var context = new TransactionEventContext
            {
                TransactionId = "test-id",
                RequestType = "TestRequest"
            };

            _eventPublisherMock.Setup(x => x.PublishAfterRollbackAsync(context, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.PublishAfterRollbackAsync(context, CancellationToken.None);

            // Assert
            _eventPublisherMock.Verify(x => x.PublishAfterRollbackAsync(context, CancellationToken.None), Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task PublishAfterRollbackAsync_WhenEventPublisherFails_LogsWarning()
        {
            // Arrange
            var context = new TransactionEventContext
            {
                TransactionId = "test-id",
                RequestType = "TestRequest"
            };
            var expectedException = new InvalidOperationException("Event failed");

            _eventPublisherMock.Setup(x => x.PublishAfterRollbackAsync(context, It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act
            await _handler.PublishAfterRollbackAsync(context, CancellationToken.None);

            // Assert
            _eventPublisherMock.Verify(x => x.PublishAfterRollbackAsync(context, CancellationToken.None), Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Constructor_WithNullEventPublisher_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TransactionEventHandler(
                null,
                _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TransactionEventHandler(
                _eventPublisherMock.Object,
                null));
        }
    }
}