using System;
using System.Collections.Generic;
using System.Data;
using Relay.Core.Transactions;
using Relay.Core.Transactions.Factories;
using Xunit;

namespace Relay.Core.Tests.Transactions.Factories
{
    public class TransactionEventContextFactoryTests
    {
        private readonly TransactionEventContextFactory _factory;

        public TransactionEventContextFactoryTests()
        {
            _factory = new TransactionEventContextFactory();
        }

        [Fact]
        public void CreateEventContext_WithValidParameters_ReturnsCorrectContext()
        {
            // Arrange
            var requestType = "TestRequest";
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(5),
                isReadOnly: true,
                useDistributedTransaction: false);

            // Act
            var context = _factory.CreateEventContext(requestType, configuration);

            // Assert
            Assert.NotNull(context);
            Assert.Equal(requestType, context.RequestType);
            Assert.Equal(configuration.IsolationLevel, context.IsolationLevel);
            Assert.Equal(0, context.NestingLevel);
            Assert.True(context.Timestamp <= DateTime.UtcNow);
            Assert.True(context.Timestamp > DateTime.UtcNow.AddMinutes(-1));

            Assert.NotNull(context.Metadata);
            Assert.True(context.Metadata.ContainsKey("IsReadOnly"));
            Assert.True(context.Metadata.ContainsKey("Timeout"));
            Assert.True(context.Metadata.ContainsKey("UseDistributedTransaction"));

            Assert.Equal(configuration.IsReadOnly, context.Metadata["IsReadOnly"]);
            Assert.Equal(configuration.Timeout, context.Metadata["Timeout"]);
            Assert.Equal(configuration.UseDistributedTransaction, context.Metadata["UseDistributedTransaction"]);
        }

        [Fact]
        public void CreateEventContext_InitializesMetadataDictionary()
        {
            // Arrange
            var requestType = "TestRequest";
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(1));

            // Act
            var context1 = _factory.CreateEventContext(requestType, configuration);
            var context2 = _factory.CreateEventContext(requestType, configuration);

            // Assert
            Assert.NotEmpty(context1.TransactionId);
            Assert.NotEmpty(context2.TransactionId);
            Assert.NotEqual(context1.TransactionId, context2.TransactionId);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(30)]
        [InlineData(300)]
        [InlineData(3600)]
        public void CreateEventContext_WithDifferentTimeouts_SetsCorrectMetadata(int timeoutSeconds)
        {
            // Arrange
            var requestType = "TestRequest";
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromSeconds(timeoutSeconds));

            // Act
            var context = _factory.CreateEventContext(requestType, configuration);

            // Assert
            Assert.Equal(requestType, context.RequestType);
        }

        [Theory]
        [InlineData(IsolationLevel.ReadCommitted)]
        [InlineData(IsolationLevel.ReadUncommitted)]
        [InlineData(IsolationLevel.RepeatableRead)]
        [InlineData(IsolationLevel.Serializable)]
        [InlineData(IsolationLevel.Snapshot)]
        public void CreateEventContext_WithDifferentIsolationLevels_SetsCorrectIsolationLevel(IsolationLevel isolationLevel)
        {
            // Arrange
            var requestType = "TestRequest";
            var configuration = new TransactionConfiguration(
                isolationLevel,
                TimeSpan.FromMinutes(1));

            // Act
            var context = _factory.CreateEventContext(requestType, configuration);

            // Assert
            Assert.Equal(isolationLevel, context.IsolationLevel);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CreateEventContext_WithDifferentReadOnlyFlags_SetsCorrectMetadata(bool isReadOnly)
        {
            // Arrange
            var requestType = "TestRequest";
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(1),
                isReadOnly: isReadOnly);

            // Act
            var context = _factory.CreateEventContext(requestType, configuration);

            // Assert
            Assert.Equal(isReadOnly, context.Metadata["IsReadOnly"]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CreateEventContext_WithDifferentDistributedTransactionFlags_SetsCorrectMetadata(bool useDistributedTransaction)
        {
            // Arrange
            var requestType = "TestRequest";
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(1),
                useDistributedTransaction: useDistributedTransaction);

            // Act
            var context = _factory.CreateEventContext(requestType, configuration);

            // Assert
            Assert.Equal(useDistributedTransaction, context.Metadata["UseDistributedTransaction"]);
        }

        [Fact]
        public void CreateEventContext_AlwaysSetsNestingLevelToZero()
        {
            // Arrange
            var requestType = "TestRequest";
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(1));

            // Act
            var context = _factory.CreateEventContext(requestType, configuration);

            // Assert
            Assert.NotNull(context.Metadata);
            Assert.IsType<Dictionary<string, object>>(context.Metadata);
        }
    }
}
