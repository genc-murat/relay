using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Extensions
{
    /// <summary>
    /// Extension methods for IAsyncEnumerable for better performance and functionality.
    /// </summary>
    public static class AsyncEnumerableExtensions
    {
        /// <summary>
        /// Converts an async enumerable to a list asynchronously.
        /// </summary>
        public static async ValueTask<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var list = new List<T>();
            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                list.Add(item);
            }
            return list;
        }

        /// <summary>
        /// Returns the first element of an async enumerable.
        /// </summary>
        public static async ValueTask<T> FirstAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            await using var enumerator = source.GetAsyncEnumerator(cancellationToken);
            if (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                return enumerator.Current;
            }
            throw new InvalidOperationException("Sequence contains no elements");
        }

        /// <summary>
        /// Returns the first element of an async enumerable, or default if empty.
        /// </summary>
        public static async ValueTask<T?> FirstOrDefaultAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            await using var enumerator = source.GetAsyncEnumerator(cancellationToken);
            if (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                return enumerator.Current;
            }
            return default;
        }

        /// <summary>
        /// Buffers items from an async enumerable into batches.
        /// </summary>
        public static async IAsyncEnumerable<T[]> BufferAsync<T>(
            this IAsyncEnumerable<T> source,
            int size,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size));

            var buffer = new List<T>(size);
            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                buffer.Add(item);
                if (buffer.Count == size)
                {
                    yield return buffer.ToArray();
                    buffer.Clear();
                }
            }

            if (buffer.Count > 0)
            {
                yield return buffer.ToArray();
            }
        }
    }
}