using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Transactions;
using Xunit;

namespace Relay.Core.Tests.Transactions
{
    public class TransactionContextSuppressorTests
    {
        // Mock implementation of ITransactionContext for testing
        private class MockTransactionContext : ITransactionContext
        {
            public string TransactionId { get; set; } = "test-transaction-id";
            public int NestingLevel { get; set; } = 1;
            public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
            public bool IsReadOnly { get; set; } = false;
            public DateTime StartedAt { get; set; } = DateTime.UtcNow;
            public IRelayDbTransaction? CurrentTransaction { get; set; } = null;

            public Task<ISavepoint> CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void Constructor_WithTransactionContext_SuppressesContext()
        {
            // Arrange
            var originalContext = new MockTransactionContext();
            TransactionContextAccessor.Current = originalContext;

            // Act
            using var suppressor = new TransactionContextSuppressor();

            // Assert
            Assert.True(suppressor.IsSuppressed);
            Assert.Null(TransactionContextAccessor.Current);
        }

        [Fact]
        public void Constructor_WithoutTransactionContext_DoesNotSuppress()
        {
            // Arrange
            TransactionContextAccessor.Current = null;

            // Act
            using var suppressor = new TransactionContextSuppressor();

            // Assert
            Assert.False(suppressor.IsSuppressed);
            Assert.Null(TransactionContextAccessor.Current);
        }

        [Fact]
        public void Constructor_WithTransactionContext_SavesOriginalContext()
        {
            // Arrange
            var originalContext = new MockTransactionContext();
            TransactionContextAccessor.Current = originalContext;

            // Act
            using var suppressor = new TransactionContextSuppressor();

            // Assert (we can verify by testing restore functionality)
            Assert.True(suppressor.IsSuppressed);
            Assert.Null(TransactionContextAccessor.Current);
            
            // After disposing, original context should be restored
            suppressor.Dispose();
            Assert.Equal(originalContext, TransactionContextAccessor.Current);
        }

        [Fact]
        public void Restore_WhenSuppressed_RestoresOriginalContext()
        {
            // Arrange
            var originalContext = new MockTransactionContext();
            TransactionContextAccessor.Current = originalContext;
            var suppressor = new TransactionContextSuppressor();
            
            // Verify suppression
            Assert.True(suppressor.IsSuppressed);
            Assert.Null(TransactionContextAccessor.Current);

            // Act
            suppressor.Restore();

            // Assert
            Assert.False(suppressor.IsSuppressed);
            Assert.Equal(originalContext, TransactionContextAccessor.Current);
        }

        [Fact]
        public void Restore_WhenNotSuppressed_DoesNotThrow()
        {
            // Arrange
            TransactionContextAccessor.Current = null;
            var suppressor = new TransactionContextSuppressor();

            // Act & Assert - should not throw
            suppressor.Restore();
            Assert.False(suppressor.IsSuppressed);
            Assert.Null(TransactionContextAccessor.Current);
        }

        [Fact]
        public void Restore_WhenAlreadyRestored_DoesNotThrow()
        {
            // Arrange
            var originalContext = new MockTransactionContext();
            TransactionContextAccessor.Current = originalContext;
            var suppressor = new TransactionContextSuppressor();
            suppressor.Restore(); // First restore
            
            // Act & Assert - second restore should not throw
            suppressor.Restore();
            Assert.False(suppressor.IsSuppressed);
            Assert.Equal(originalContext, TransactionContextAccessor.Current);
        }

        [Fact]
        public void Dispose_WhenSuppressed_RestoresContext()
        {
            // Arrange
            var originalContext = new MockTransactionContext();
            TransactionContextAccessor.Current = originalContext;
            var suppressor = new TransactionContextSuppressor();
            
            // Verify suppression
            Assert.True(suppressor.IsSuppressed);
            Assert.Null(TransactionContextAccessor.Current);

            // Act
            suppressor.Dispose();

            // Assert
            Assert.False(suppressor.IsSuppressed);
            Assert.Equal(originalContext, TransactionContextAccessor.Current);
        }

        [Fact]
        public void Dispose_WhenNotSuppressed_DoesNotThrow()
        {
            // Arrange
            TransactionContextAccessor.Current = null;
            var suppressor = new TransactionContextSuppressor();

            // Act & Assert - should not throw
            suppressor.Dispose();
            Assert.False(suppressor.IsSuppressed);
            Assert.Null(TransactionContextAccessor.Current);
        }

        [Fact]
        public void Dispose_WhenAlreadyDisposed_DoesNotThrow()
        {
            // Arrange
            var originalContext = new MockTransactionContext();
            TransactionContextAccessor.Current = originalContext;
            var suppressor = new TransactionContextSuppressor();
            suppressor.Dispose(); // First dispose
            
            // Verify context was restored and suppressor is disposed
            Assert.False(suppressor.IsSuppressed);
            Assert.Equal(originalContext, TransactionContextAccessor.Current);

            // Act & Assert - second dispose should not throw
            suppressor.Dispose();
        }

        [Fact]
        public void IsSuppressed_PropertyReflectsSuppressionState()
        {
            // Test with context present
            var originalContext = new MockTransactionContext();
            TransactionContextAccessor.Current = originalContext;
            
            using (var suppressor = new TransactionContextSuppressor())
            {
                Assert.True(suppressor.IsSuppressed);
            }
            
            // After disposal, context should be restored
            Assert.Equal(originalContext, TransactionContextAccessor.Current);

            // Test without context present
            TransactionContextAccessor.Current = null;
            using (var suppressor2 = new TransactionContextSuppressor())
            {
                Assert.False(suppressor2.IsSuppressed);
            }
            
            Assert.Null(TransactionContextAccessor.Current);
        }

        [Fact]
        public void Restore_AfterDispose_DoesNotRestoreAgain()
        {
            // Arrange
            var originalContext = new MockTransactionContext();
            TransactionContextAccessor.Current = originalContext;
            var suppressor = new TransactionContextSuppressor();
            
            // Dispose first
            suppressor.Dispose();
            Assert.False(suppressor.IsSuppressed);
            Assert.Equal(originalContext, TransactionContextAccessor.Current);

            // Act - Try to restore after disposal
            suppressor.Restore();

            // Assert - Should remain unchanged
            Assert.False(suppressor.IsSuppressed);
            Assert.Equal(originalContext, TransactionContextAccessor.Current);
        }

        [Fact]
        public void SuppressRestoreFlow_WorksCorrectly()
        {
            // Arrange
            var originalContext = new MockTransactionContext();
            TransactionContextAccessor.Current = originalContext;

            // Act
            var suppressor = new TransactionContextSuppressor();
            
            // Assert suppression
            Assert.True(suppressor.IsSuppressed);
            Assert.Null(TransactionContextAccessor.Current);

            // Restore
            suppressor.Restore();
            Assert.False(suppressor.IsSuppressed);
            Assert.Equal(originalContext, TransactionContextAccessor.Current);

            // Suppress again by reassigning
            TransactionContextAccessor.Current = originalContext;
            var suppressor2 = new TransactionContextSuppressor();
            Assert.True(suppressor2.IsSuppressed);
            Assert.Null(TransactionContextAccessor.Current);

            // Dispose to restore
            suppressor2.Dispose();
            Assert.False(suppressor2.IsSuppressed);
            Assert.Equal(originalContext, TransactionContextAccessor.Current);
        }

        [Fact]
        public void MultipleSuppressionsInSameScope()
        {
            // Arrange - Initialize context with original value
            var originalContext = new MockTransactionContext() { TransactionId = "original", StartedAt = DateTime.UtcNow };
            TransactionContextAccessor.Current = originalContext;
            
            var context1 = new MockTransactionContext() { TransactionId = "context1", StartedAt = DateTime.UtcNow };
            var context2 = new MockTransactionContext() { TransactionId = "context2", StartedAt = DateTime.UtcNow };
            
            // First suppression
            var suppressor1 = new TransactionContextSuppressor();
            Assert.True(suppressor1.IsSuppressed);
            Assert.Null(TransactionContextAccessor.Current);
            
            // Set context2 while suppressor1 is active
            TransactionContextAccessor.Current = context2;
            var suppressor2 = new TransactionContextSuppressor();
            Assert.True(suppressor2.IsSuppressed);
            Assert.Null(TransactionContextAccessor.Current);

            // Restore second suppressor - should restore context2
            suppressor2.Dispose();
            Assert.False(suppressor2.IsSuppressed);
            Assert.Equal(context2.TransactionId, TransactionContextAccessor.Current?.TransactionId);
            
            // Restore first suppressor - should restore original context
            suppressor1.Dispose();
            Assert.False(suppressor1.IsSuppressed);
            Assert.Equal(originalContext.TransactionId, TransactionContextAccessor.Current?.TransactionId);
        }
    }
}
