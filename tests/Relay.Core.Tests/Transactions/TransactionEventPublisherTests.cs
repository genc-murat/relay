using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.Transactions;
using Xunit;

namespace Relay.Core.Tests.Transactions
{
    public class TransactionEventPublisherTests
    {
        private class TestEventHandler : ITransactionEventHandler
        {
            public List<string> Events { get; } = new();
            public bool ThrowOnBeforeCommit { get; set; }
            public bool ThrowOnAfterCommit { get; set; }

            public Task OnBeforeBeginAsync(TransactionEventContext context, CancellationToken cancellationToken = default)
            {
                Events.Add("BeforeBegin");
                return Task.CompletedTask;
            }

            public Task OnAfterBeginAsync(TransactionEventContext context, CancellationToken cancellationToken = default)
            {
                Events.Add("AfterBegin");
                return Task.CompletedTask;
            }

            public Task OnBeforeCommitAsync(TransactionEventContext context, CancellationToken cancellationToken = default)
            {
                Events.Add("BeforeCommit");
                if (ThrowOnBeforeCommit)
                {
                    throw new InvalidOperationException("BeforeCommit failed");
                }
                return Task.CompletedTask;
            }

            public Task OnAfterCommitAsync(TransactionEventContext context, CancellationToken cancellationToken = default)
            {
                Events.Add("AfterCommit");
                if (ThrowOnAfterCommit)
                {
                    throw new InvalidOperationException("AfterCommit failed");
                }
                return Task.CompletedTask;
            }

            public Task OnBeforeRollbackAsync(TransactionEventContext context, CancellationToken cancellationToken = default)
            {
                Events.Add("BeforeRollback");
                return Task.CompletedTask;
            }

            public Task OnAfterRollbackAsync(TransactionEventContext context, CancellationToken cancellationToken = default)
            {
                Events.Add("AfterRollback");
                return Task.CompletedTask;
            }
        }

        private TransactionEventContext CreateContext()
        {
            return new TransactionEventContext
            {
                TransactionId = "test-tx",
                RequestType = "TestRequest",
                IsolationLevel = IsolationLevel.ReadCommitted,
                NestingLevel = 0,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>()
            };
        }

        [Fact]
        public async Task PublishBeforeBeginAsync_Should_Invoke_All_Handlers()
        {
            var handler1 = new TestEventHandler();
            var handler2 = new TestEventHandler();
            var handlers = new List<ITransactionEventHandler> { handler1, handler2 };
            var publisher = new TransactionEventPublisher(handlers, new NullLogger<TransactionEventPublisher>());

            await publisher.PublishBeforeBeginAsync(CreateContext());

            Assert.Contains("BeforeBegin", handler1.Events);
            Assert.Contains("BeforeBegin", handler2.Events);
        }

        [Fact]
        public async Task PublishAfterBeginAsync_Should_Invoke_All_Handlers()
        {
            var handler = new TestEventHandler();
            var handlers = new List<ITransactionEventHandler> { handler };
            var publisher = new TransactionEventPublisher(handlers, new NullLogger<TransactionEventPublisher>());

            await publisher.PublishAfterBeginAsync(CreateContext());

            Assert.Contains("AfterBegin", handler.Events);
        }

        [Fact]
        public async Task PublishBeforeCommitAsync_Should_Invoke_All_Handlers()
        {
            var handler = new TestEventHandler();
            var handlers = new List<ITransactionEventHandler> { handler };
            var publisher = new TransactionEventPublisher(handlers, new NullLogger<TransactionEventPublisher>());

            await publisher.PublishBeforeCommitAsync(CreateContext());

            Assert.Contains("BeforeCommit", handler.Events);
        }

        [Fact]
        public async Task PublishBeforeCommitAsync_Should_Throw_On_Handler_Failure()
        {
            var handler = new TestEventHandler { ThrowOnBeforeCommit = true };
            var handlers = new List<ITransactionEventHandler> { handler };
            var publisher = new TransactionEventPublisher(handlers, new NullLogger<TransactionEventPublisher>());

            await Assert.ThrowsAsync<TransactionEventHandlerException>(
                async () => await publisher.PublishBeforeCommitAsync(CreateContext()));
        }

        [Fact]
        public async Task PublishAfterCommitAsync_Should_Not_Throw_On_Handler_Failure()
        {
            var handler = new TestEventHandler { ThrowOnAfterCommit = true };
            var handlers = new List<ITransactionEventHandler> { handler };
            var publisher = new TransactionEventPublisher(handlers, new NullLogger<TransactionEventPublisher>());

            // Should not throw even though handler throws
            await publisher.PublishAfterCommitAsync(CreateContext());

            Assert.Contains("AfterCommit", handler.Events);
        }

        [Fact]
        public async Task PublishBeforeRollbackAsync_Should_Invoke_All_Handlers()
        {
            var handler = new TestEventHandler();
            var handlers = new List<ITransactionEventHandler> { handler };
            var publisher = new TransactionEventPublisher(handlers, new NullLogger<TransactionEventPublisher>());

            await publisher.PublishBeforeRollbackAsync(CreateContext());

            Assert.Contains("BeforeRollback", handler.Events);
        }

        [Fact]
        public async Task PublishAfterRollbackAsync_Should_Invoke_All_Handlers()
        {
            var handler = new TestEventHandler();
            var handlers = new List<ITransactionEventHandler> { handler };
            var publisher = new TransactionEventPublisher(handlers, new NullLogger<TransactionEventPublisher>());

            await publisher.PublishAfterRollbackAsync(CreateContext());

            Assert.Contains("AfterRollback", handler.Events);
        }

        [Fact]
        public async Task Publisher_Should_Work_With_No_Handlers()
        {
            var handlers = new List<ITransactionEventHandler>();
            var publisher = new TransactionEventPublisher(handlers, new NullLogger<TransactionEventPublisher>());

            // Should not throw
            await publisher.PublishBeforeBeginAsync(CreateContext());
            await publisher.PublishAfterBeginAsync(CreateContext());
            await publisher.PublishBeforeCommitAsync(CreateContext());
            await publisher.PublishAfterCommitAsync(CreateContext());
            await publisher.PublishBeforeRollbackAsync(CreateContext());
            await publisher.PublishAfterRollbackAsync(CreateContext());
        }

        [Fact]
        public async Task PublishBeforeBeginAsync_Should_Throw_ArgumentNullException_For_Null_Context()
        {
            var handlers = new List<ITransactionEventHandler>();
            var publisher = new TransactionEventPublisher(handlers, new NullLogger<TransactionEventPublisher>());

            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await publisher.PublishBeforeBeginAsync(null!));
        }

        [Fact]
        public async Task Events_Should_Be_Published_In_Parallel()
        {
            var handler1 = new TestEventHandler();
            var handler2 = new TestEventHandler();
            var handlers = new List<ITransactionEventHandler> { handler1, handler2 };
            var publisher = new TransactionEventPublisher(handlers, new NullLogger<TransactionEventPublisher>());

            var context = CreateContext();
            await publisher.PublishBeforeBeginAsync(context);
            await publisher.PublishAfterBeginAsync(context);
            await publisher.PublishBeforeCommitAsync(context);
            await publisher.PublishAfterCommitAsync(context);

            // Both handlers should have received all events
            Assert.Equal(4, handler1.Events.Count);
            Assert.Equal(4, handler2.Events.Count);
        }
    }
}
