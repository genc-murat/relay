using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Transactions;
using Relay.Core.Transactions.Template;
using Xunit;

namespace Relay.Core.Tests.Transactions.Template
{
    public abstract class TransactionExecutionTemplateTests
    {
        protected abstract TransactionExecutionTemplate CreateTemplate();

        public class TestTransactionExecutionTemplate : TransactionExecutionTemplate
        {
            public bool ShouldProcessCalled { get; private set; }
            public bool ResolveConfigurationCalled { get; private set; }
            public bool PreProcessCalled { get; private set; }
            public bool PostProcessCalled { get; private set; }
            public bool ExecuteTransactionCalled { get; private set; }

            public TestTransactionExecutionTemplate(
                ILogger logger,
                ITransactionConfigurationResolver configurationResolver,
                ITransactionRetryHandler retryHandler)
                : base(logger, configurationResolver, retryHandler)
            {
            }

            protected override bool ShouldProcessTransaction<TRequest>(TRequest request)
            {
                ShouldProcessCalled = true;
                return request is ITransactionalRequest;
            }

            protected override async ValueTask<ITransactionConfiguration> ResolveAndValidateConfigurationAsync<TRequest>(
                TRequest request,
                string requestType)
            {
                ResolveConfigurationCalled = true;
                var config = new TransactionConfiguration(
                    IsolationLevel.ReadCommitted,
                    TimeSpan.FromMinutes(1));
                return await ValueTask.FromResult(config);
            }

            protected override void PreProcess<TRequest>(
                TRequest request,
                ITransactionConfiguration configuration,
                string requestType)
            {
                PreProcessCalled = true;
            }

            protected override void PostProcess<TRequest>(
                TRequest request,
                ITransactionConfiguration configuration,
                string requestType)
            {
                PostProcessCalled = true;
            }

            protected override async ValueTask<TResponse> ExecuteTransactionAsync<TRequest, TResponse>(
                TRequest request,
                RequestHandlerDelegate<TResponse> next,
                ITransactionConfiguration configuration,
                string requestType,
                CancellationToken cancellationToken)
            {
                ExecuteTransactionCalled = true;
                return await next();
            }
        }

        public class ConstructorTests : TransactionExecutionTemplateTests
        {
            protected override TransactionExecutionTemplate CreateTemplate()
            {
                return new TestTransactionExecutionTemplate(
                    NullLogger.Instance,
                    Mock.Of<ITransactionConfigurationResolver>(),
                    Mock.Of<ITransactionRetryHandler>());
            }

            [Fact]
            public void Constructor_WithNullLogger_ThrowsArgumentNullException()
            {
                // Act & Assert
                Assert.Throws<ArgumentNullException>(() => new TestTransactionExecutionTemplate(
                    null,
                    Mock.Of<ITransactionConfigurationResolver>(),
                    Mock.Of<ITransactionRetryHandler>()));
            }

            [Fact]
            public void Constructor_WithNullConfigurationResolver_ThrowsArgumentNullException()
            {
                // Act & Assert
                Assert.Throws<ArgumentNullException>(() => new TestTransactionExecutionTemplate(
                    NullLogger.Instance,
                    null,
                    Mock.Of<ITransactionRetryHandler>()));
            }

            [Fact]
            public void Constructor_WithNullRetryHandler_ThrowsArgumentNullException()
            {
                // Act & Assert
                Assert.Throws<ArgumentNullException>(() => new TestTransactionExecutionTemplate(
                    NullLogger.Instance,
                    Mock.Of<ITransactionConfigurationResolver>(),
                    null));
            }
        }

        public class ExecuteAsyncTests : TransactionExecutionTemplateTests
        {
            protected override TransactionExecutionTemplate CreateTemplate()
            {
                return new TestTransactionExecutionTemplate(
                    NullLogger.Instance,
                    Mock.Of<ITransactionConfigurationResolver>(),
                    Mock.Of<ITransactionRetryHandler>());
            }

            [Fact]
            public async Task ExecuteAsync_CallsTemplateMethodsInCorrectOrder()
            {
                // Arrange
                var template = (TestTransactionExecutionTemplate)CreateTemplate();
                var request = new TestTransactionalRequest();
                var expectedResponse = "success";
                var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>(expectedResponse));

                // Act
                var response = await template.ExecuteAsync(request, nextDelegate, CancellationToken.None);

                // Assert
                Assert.Equal(expectedResponse, response);
                Assert.True(template.ShouldProcessCalled);
                Assert.True(template.ResolveConfigurationCalled);
                Assert.True(template.PreProcessCalled);
                Assert.True(template.ExecuteTransactionCalled);
                Assert.True(template.PostProcessCalled);
            }

            [Fact]
            public async Task ExecuteAsync_WithNonTransactionalRequest_SkipsProcessing()
            {
                // Arrange
                var template = (TestTransactionExecutionTemplate)CreateTemplate();
                var request = new NonTransactionalRequest();
                var expectedResponse = "success";
                var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>(expectedResponse));

                // Act
                var response = await template.ExecuteAsync(request, nextDelegate, CancellationToken.None);

                // Assert
                Assert.Equal(expectedResponse, response);
                Assert.True(template.ShouldProcessCalled);
                Assert.False(template.ResolveConfigurationCalled);
                Assert.False(template.PreProcessCalled);
                Assert.False(template.ExecuteTransactionCalled);
                Assert.False(template.PostProcessCalled);
            }
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
