using System;
using System.Threading.Tasks;

namespace Relay.Core
{
    /// <summary>
    /// Extension methods for ValueTask to provide compatibility across target frameworks.
    /// </summary>
    internal static class ValueTaskExtensions
    {
#if NETSTANDARD2_1
        /// <summary>
        /// Creates a ValueTask that's completed with the specified exception.
        /// </summary>
        public static ValueTask<TResult> FromException<TResult>(Exception exception)
        {
            var tcs = new TaskCompletionSource<TResult>();
            tcs.SetException(exception);
            return new ValueTask<TResult>(tcs.Task);
        }

        /// <summary>
        /// Creates a ValueTask that's completed with the specified exception.
        /// </summary>
        public static ValueTask FromException(Exception exception)
        {
            var tcs = new TaskCompletionSource<bool>();
            tcs.SetException(exception);
            return new ValueTask(tcs.Task);
        }

        /// <summary>
        /// Gets a completed ValueTask.
        /// </summary>
        public static ValueTask CompletedTask => new ValueTask(Task.CompletedTask);
#else
        /// <summary>
        /// Creates a ValueTask that's completed with the specified exception.
        /// </summary>
        public static ValueTask<TResult> FromException<TResult>(Exception exception)
        {
            return ValueTask.FromException<TResult>(exception);
        }

        /// <summary>
        /// Creates a ValueTask that's completed with the specified exception.
        /// </summary>
        public static ValueTask FromException(Exception exception)
        {
            return ValueTask.FromException(exception);
        }

        /// <summary>
        /// Gets a completed ValueTask.
        /// </summary>
        public static ValueTask CompletedTask => ValueTask.CompletedTask;
#endif
    }
}