using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Extensions;
using Xunit;

namespace Relay.Core.Tests.Extensions
{
    public class AsyncEnumerableExtensionsTests
    {
        [Fact]
        public async Task ToListAsync_ShouldConvertAsyncEnumerableToList()
        {
            // Arrange
            var items = new[] { 1, 2, 3, 4, 5 };
            var asyncEnumerable = CreateAsyncEnumerable(items);

            // Act
            var result = await asyncEnumerable.ToListAsync();

            // Assert
            result.Should().Equal(items);
        }

        [Fact]
        public async Task ToListAsync_ShouldThrowArgumentNullException_WhenSourceIsNull()
        {
            // Arrange
            IAsyncEnumerable<int>? source = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await source!.ToListAsync());
        }

        [Fact]
        public async Task ToListAsync_ShouldHandleEmptyEnumerable()
        {
            // Arrange
            var asyncEnumerable = CreateAsyncEnumerable(Array.Empty<int>());

            // Act
            var result = await asyncEnumerable.ToListAsync();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact()]
        public async Task ToListAsync_ShouldRespectCancellationToken()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var asyncEnumerable = CreateSlowAsyncEnumerableWithCancellation(100, cts.Token);
            cts.CancelAfter(50); // Cancel before first item delay completes

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await asyncEnumerable.ToListAsync(cts.Token));
        }

        [Fact]
        public async Task FirstAsync_ShouldReturnFirstElement()
        {
            // Arrange
            var items = new[] { 1, 2, 3, 4, 5 };
            var asyncEnumerable = CreateAsyncEnumerable(items);

            // Act
            var result = await asyncEnumerable.FirstAsync();

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public async Task FirstAsync_ShouldThrowInvalidOperationException_WhenSequenceIsEmpty()
        {
            // Arrange
            var asyncEnumerable = CreateAsyncEnumerable(Array.Empty<int>());

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await asyncEnumerable.FirstAsync());
        }

        [Fact]
        public async Task FirstAsync_ShouldThrowArgumentNullException_WhenSourceIsNull()
        {
            // Arrange
            IAsyncEnumerable<int>? source = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await source!.FirstAsync());
        }

        [Fact]
        public async Task FirstOrDefaultAsync_ShouldReturnFirstElement()
        {
            // Arrange
            var items = new[] { 1, 2, 3, 4, 5 };
            var asyncEnumerable = CreateAsyncEnumerable(items);

            // Act
            var result = await asyncEnumerable.FirstOrDefaultAsync();

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public async Task FirstOrDefaultAsync_ShouldReturnDefault_WhenSequenceIsEmpty()
        {
            // Arrange
            var asyncEnumerable = CreateAsyncEnumerable(Array.Empty<int>());

            // Act
            var result = await asyncEnumerable.FirstOrDefaultAsync();

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task FirstOrDefaultAsync_ShouldThrowArgumentNullException_WhenSourceIsNull()
        {
            // Arrange
            IAsyncEnumerable<int>? source = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await source!.FirstOrDefaultAsync());
        }

        [Fact]
        public async Task BufferAsync_ShouldBufferItemsInBatches()
        {
            // Arrange
            var items = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var asyncEnumerable = CreateAsyncEnumerable(items);

            // Act
            var batches = await asyncEnumerable.BufferAsync(3).ToListAsync();

            // Assert
            batches.Should().HaveCount(4);
            batches[0].Should().Equal(1, 2, 3);
            batches[1].Should().Equal(4, 5, 6);
            batches[2].Should().Equal(7, 8, 9);
            batches[3].Should().Equal(10);
        }

        [Fact]
        public async Task BufferAsync_ShouldHandleExactMultiple()
        {
            // Arrange
            var items = new[] { 1, 2, 3, 4, 5, 6 };
            var asyncEnumerable = CreateAsyncEnumerable(items);

            // Act
            var batches = await asyncEnumerable.BufferAsync(3).ToListAsync();

            // Assert
            batches.Should().HaveCount(2);
            batches[0].Should().Equal(1, 2, 3);
            batches[1].Should().Equal(4, 5, 6);
        }

        [Fact]
        public async Task BufferAsync_ShouldThrowArgumentNullException_WhenSourceIsNull()
        {
            // Arrange
            IAsyncEnumerable<int>? source = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await foreach (var _ in source!.BufferAsync(3))
                {
                }
            });
        }

        [Fact]
        public async Task BufferAsync_ShouldThrowArgumentOutOfRangeException_WhenSizeIsInvalid()
        {
            // Arrange
            var asyncEnumerable = CreateAsyncEnumerable(new[] { 1, 2, 3 });

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await foreach (var _ in asyncEnumerable.BufferAsync(0))
                {
                }
            });
        }

        [Fact()]
        public async Task BufferAsync_ShouldRespectCancellationToken()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var asyncEnumerable = CreateSlowAsyncEnumerableWithCancellation(100, cts.Token);
            cts.CancelAfter(50); // Cancel before first item delay completes

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await foreach (var _ in asyncEnumerable.BufferAsync(3, cts.Token))
                {
                }
            });
        }

        private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items)
        {
            await Task.Yield();
            foreach (var item in items)
            {
                yield return item;
            }
        }

        private static async IAsyncEnumerable<int> CreateSlowAsyncEnumerable(int count)
        {
            for (int i = 0; i < count; i++)
            {
                await Task.Delay(100);
                yield return i;
            }
        }

        private static async IAsyncEnumerable<int> CreateSlowAsyncEnumerableWithCancellation(
            int count,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < count; i++)
            {
                await Task.Delay(100, cancellationToken);
                yield return i;
            }
        }
    }
}
