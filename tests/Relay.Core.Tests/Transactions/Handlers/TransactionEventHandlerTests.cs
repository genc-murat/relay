using System;
using System.Collections.Generic;
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
        private readonly Mock<ITransactionEventHandler> _failingEventHandlerMock;
        private readonly TransactionEventPublisher _eventPublisher;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<TransactionEventHandler>> _loggerMock;
        private readonly TransactionEventHandler _handler;

        public TransactionEventHandlerTests()
        {
            _failingEventHandlerMock = new Mock<ITransactionEventHandler>();
            _eventPublisher = new TransactionEventPublisher(
                new List<ITransactionEventHandler> { _failingEventHandlerMock.Object }, 
                NullLogger<TransactionEventPublisher>.Instance);
            _loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<TransactionEventHandler>>();
            
            _handler = new TransactionEventHandler(
                _eventPublisher,
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

            // Setup event handler to succeed
            _failingEventHandlerMock.Setup(x => x.OnBeforeBeginAsync(context, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.PublishBeforeBeginAsync(context, CancellationToken.None);

            // Assert
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

            // Setup event handler to fail
            _failingEventHandlerMock.Setup(x => x.OnBeforeBeginAsync(context, It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act
            await _handler.PublishBeforeBeginAsync(context, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.Is<TransactionEventHandlerException>(ex => ex.InnerException == expectedException),
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

            // Setup event handler to succeed
            _failingEventHandlerMock.Setup(x => x.OnAfterBeginAsync(context, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.PublishAfterBeginAsync(context, CancellationToken.None);

            // Assert
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

            // Setup event handler to fail
            _failingEventHandlerMock.Setup(x => x.OnAfterBeginAsync(context, It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act
            await _handler.PublishAfterBeginAsync(context, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.Is<TransactionEventHandlerException>(ex => ex.InnerException == expectedException),
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

            // Setup event handler to succeed
            _failingEventHandlerMock.Setup(x => x.OnBeforeCommitAsync(context, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.PublishBeforeCommitAsync(context, CancellationToken.None);

            // Assert
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

            // Setup event handler to fail
            _failingEventHandlerMock.Setup(x => x.OnBeforeCommitAsync(context, It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<TransactionEventHandlerException>(
                async () => await _handler.PublishBeforeCommitAsync(context, CancellationToken.None));

            Assert.Equal("BeforeCommit", actualException.EventName);
            Assert.Equal("test-id", actualException.TransactionId);
            Assert.Equal("BeforeCommit", actualException.EventName);
            Assert.NotNull(actualException.InnerException);
            // The inner exception is also a TransactionEventHandlerException with its own message
            Assert.Equal("Event handler failed for BeforeCommit event", actualException.InnerException?.Message);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<TransactionEventHandlerException>(),
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

            // Setup event handler to succeed
            _failingEventHandlerMock.Setup(x => x.OnAfterCommitAsync(context, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.PublishAfterCommitAsync(context, CancellationToken.None);

            // Assert
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
        public async Task PublishAfterCommitAsync_WhenEventPublisherFails_DoesNotThrow()
        {
            // Arrange
            var context = new TransactionEventContext
            {
                TransactionId = "test-id",
                RequestType = "TestRequest"
            };
            var expectedException = new InvalidOperationException("Event failed");

            // Setup event handler to fail
            _failingEventHandlerMock.Setup(x => x.OnAfterCommitAsync(context, It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act & Assert - Should not throw
            await _handler.PublishAfterCommitAsync(context, CancellationToken.None);
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

            // Setup event handler to succeed
            _failingEventHandlerMock.Setup(x => x.OnBeforeRollbackAsync(context, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.PublishBeforeRollbackAsync(context, CancellationToken.None);

            // Assert
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

            // Setup event handler to fail
            _failingEventHandlerMock.Setup(x => x.OnBeforeRollbackAsync(context, It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act
            await _handler.PublishBeforeRollbackAsync(context, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.Is<TransactionEventHandlerException>(ex => ex.InnerException == expectedException),
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

            // Setup event handler to succeed
            _failingEventHandlerMock.Setup(x => x.OnAfterRollbackAsync(context, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.PublishAfterRollbackAsync(context, CancellationToken.None);

            // Assert
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
        public async Task PublishAfterRollbackAsync_WhenEventPublisherFails_DoesNotThrow()
        {
            // Arrange
            var context = new TransactionEventContext
            {
                TransactionId = "test-id",
                RequestType = "TestRequest"
            };
            var expectedException = new InvalidOperationException("Event failed");

            // Setup event handler to fail
            _failingEventHandlerMock.Setup(x => x.OnAfterRollbackAsync(context, It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act & Assert - Should not throw
            await _handler.PublishAfterRollbackAsync(context, CancellationToken.None);
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
                _eventPublisher,
                null));
        }
    }
}