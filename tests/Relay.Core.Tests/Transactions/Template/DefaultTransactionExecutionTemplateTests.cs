using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Transactions;
using Relay.Core.Transactions.Factories;
using Relay.Core.Transactions.Strategies;
using Relay.Core.Transactions.Template;
using Xunit;

namespace Relay.Core.Tests.Transactions.Template
{
    public class DefaultTransactionExecutionTemplateTests
    {
        private readonly Mock<ILogger<DefaultTransactionExecutionTemplate>> _loggerMock;
        private readonly Mock<ITransactionConfigurationResolver> _configurationResolverMock;
        private readonly Mock<ITransactionRetryHandler> _retryHandlerMock;
        private readonly Mock<ITransactionStrategyFactory> _strategyFactoryMock;
        private readonly Mock<ITransactionExecutionStrategy> _strategyMock;
        private readonly DefaultTransactionExecutionTemplate _template;

        public DefaultTransactionExecutionTemplateTests()
        {
            _loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<DefaultTransactionExecutionTemplate>>();
            _configurationResolverMock = new Mock<ITransactionConfigurationResolver>();
            _retryHandlerMock = new Mock<ITransactionRetryHandler>();
            _strategyFactoryMock = new Mock<ITransactionStrategyFactory>();
            _strategyMock = new Mock<ITransactionExecutionStrategy>();

            _template = new DefaultTransactionExecutionTemplate(
                _loggerMock.Object,
                _configurationResolverMock.Object,
                _retryHandlerMock.Object,
                _strategyFactoryMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_WithNonTransactionalRequest_SkipsTransaction()
        {
            // Arrange
            var request = new NonTransactionalRequest();
            var expectedResponse = "success";
            var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>(expectedResponse));

            // Act
            var response = await _template.ExecuteAsync(request, nextDelegate, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);

            _configurationResolverMock.Verify(x => x.Resolve(It.IsAny<object>()), Times.Never);
            _strategyFactoryMock.Verify(x => x.CreateStrategy(It.IsAny<ITransactionConfiguration>()), Times.Never);
            _strategyMock.Verify(x => x.ExecuteAsync(
                It.IsAny<object>(), It.IsAny<RequestHandlerDelegate<string>>(),
                It.IsAny<ITransactionConfiguration>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WithTransactionalRequest_ExecutesStrategy()
        {
            // Arrange
            var request = new TestTransactionalRequest();
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(1));
            var expectedResponse = "success";
            var requestType = "TestTransactionalRequest";

            _configurationResolverMock.Setup(x => x.Resolve(request)).Returns(configuration);
            _strategyFactoryMock.Setup(x => x.CreateStrategy(configuration)).Returns(_strategyMock.Object);

            _strategyMock.Setup(x => x.ExecuteAsync(
                request, It.IsAny<RequestHandlerDelegate<string>>(), configuration, requestType, It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<string>(expectedResponse));

            var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>(expectedResponse));

            // Act
            var response = await _template.ExecuteAsync(request, nextDelegate, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);

            _configurationResolverMock.Verify(x => x.Resolve(request), Times.Once);
            _strategyFactoryMock.Verify(x => x.CreateStrategy(configuration), Times.Once);
            _strategyMock.Verify(x => x.ExecuteAsync(
                request, It.IsAny<RequestHandlerDelegate<string>>(), configuration, requestType, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithUnspecifiedIsolationLevel_ThrowsConfigurationException()
        {
            // Arrange
            var request = new TestTransactionalRequest();
            var configuration = new TransactionConfiguration(
                IsolationLevel.Unspecified,
                TimeSpan.FromMinutes(1));

            _configurationResolverMock.Setup(x => x.Resolve(request)).Returns(configuration);

            var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>("success"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TransactionConfigurationException>(
                async () => await _template.ExecuteAsync(request, nextDelegate, CancellationToken.None));

            Assert.Contains("cannot be Unspecified", exception.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithConfigurationResolverError_PropagatesException()
        {
            // Arrange
            var request = new TestTransactionalRequest();
            var expectedException = new TransactionConfigurationException("Configuration error");

            _configurationResolverMock.Setup(x => x.Resolve(request)).Throws(expectedException);

            var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>("success"));

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<TransactionConfigurationException>(
                async () => await _template.ExecuteAsync(request, nextDelegate, CancellationToken.None));

            Assert.Equal(expectedException, actualException);
        }

        [Fact]
        public async Task ExecuteAsync_WithRetryPolicy_ExecutesWithRetry()
        {
            // Arrange
            var request = new TestTransactionalRequest();
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(1),
                retryPolicy: new TransactionRetryPolicy { MaxRetries = 3 });
            var expectedResponse = "success";
            var requestType = "TestTransactionalRequest";

            _configurationResolverMock.Setup(x => x.Resolve(request)).Returns(configuration);
            _strategyFactoryMock.Setup(x => x.CreateStrategy(configuration)).Returns(_strategyMock.Object);

            _retryHandlerMock.Setup(x => x.ExecuteWithRetryAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                configuration.RetryPolicy,
                null,
                requestType,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>(expectedResponse));

            // Act
            var response = await _template.ExecuteAsync(request, nextDelegate, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);

            _retryHandlerMock.Verify(x => x.ExecuteWithRetryAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                configuration.RetryPolicy,
                null,
                requestType,
                It.IsAny<CancellationToken>()), Times.Once);

            _strategyMock.Verify(x => x.ExecuteAsync(
                It.IsAny<object>(), It.IsAny<RequestHandlerDelegate<string>>(),
                It.IsAny<ITransactionConfiguration>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WithoutRetryPolicy_ExecutesStrategyDirectly()
        {
            // Arrange
            var request = new TestTransactionalRequest();
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(1),
                retryPolicy: null);
            var expectedResponse = "success";
            var requestType = "TestTransactionalRequest";

            _configurationResolverMock.Setup(x => x.Resolve(request)).Returns(configuration);
            _strategyFactoryMock.Setup(x => x.CreateStrategy(configuration)).Returns(_strategyMock.Object);

            _strategyMock.Setup(x => x.ExecuteAsync(
                request, It.IsAny<RequestHandlerDelegate<string>>(), configuration, requestType, It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<string>(expectedResponse));

            var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>(expectedResponse));

            // Act
            var response = await _template.ExecuteAsync(request, nextDelegate, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);

            _retryHandlerMock.Verify(x => x.ExecuteWithRetryAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                It.IsAny<TransactionRetryPolicy>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);

            _strategyMock.Verify(x => x.ExecuteAsync(
                request, It.IsAny<RequestHandlerDelegate<string>>(), configuration, requestType, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithZeroMaxRetries_ExecutesStrategyDirectly()
        {
            // Arrange
            var request = new TestTransactionalRequest();
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(1),
                retryPolicy: new TransactionRetryPolicy { MaxRetries = 0 });
            var expectedResponse = "success";
            var requestType = "TestTransactionalRequest";

            _configurationResolverMock.Setup(x => x.Resolve(request)).Returns(configuration);
            _strategyFactoryMock.Setup(x => x.CreateStrategy(configuration)).Returns(_strategyMock.Object);

            _strategyMock.Setup(x => x.ExecuteAsync(
                request, It.IsAny<RequestHandlerDelegate<string>>(), configuration, requestType, It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<string>(expectedResponse));

            var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>(expectedResponse));

            // Act
            var response = await _template.ExecuteAsync(request, nextDelegate, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);

            _retryHandlerMock.Verify(x => x.ExecuteWithRetryAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                It.IsAny<TransactionRetryPolicy>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);

            _strategyMock.Verify(x => x.ExecuteAsync(
                request, It.IsAny<RequestHandlerDelegate<string>>(), configuration, requestType, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DefaultTransactionExecutionTemplate(
                null,
                _configurationResolverMock.Object,
                _retryHandlerMock.Object,
                _strategyFactoryMock.Object));
        }

        [Fact]
        public void Constructor_WithNullConfigurationResolver_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DefaultTransactionExecutionTemplate(
                _loggerMock.Object,
                null,
                _retryHandlerMock.Object,
                _strategyFactoryMock.Object));
        }

        [Fact]
        public void Constructor_WithNullRetryHandler_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DefaultTransactionExecutionTemplate(
                _loggerMock.Object,
                _configurationResolverMock.Object,
                null,
                _strategyFactoryMock.Object));
        }

        [Fact]
        public void Constructor_WithNullStrategyFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DefaultTransactionExecutionTemplate(
                _loggerMock.Object,
                _configurationResolverMock.Object,
                _retryHandlerMock.Object,
                null));
        }

        [Transaction(IsolationLevel.ReadCommitted)]
        private class TestTransactionalRequest : ITransactionalRequest
        {
            public string Data { get; set; } = "test";
        }

        private class NonTransactionalRequest
        {
            public string Data { get; set; } = "test";
        }
    }
}