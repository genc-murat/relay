using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Testing;

/// <summary>
/// Extension methods for common testing patterns and fluent assertions.
/// </summary>
public static class TestExtensions
{
    #region Async Extensions

    /// <summary>
    /// Executes an async action and ensures it completes within the specified timeout.
    /// </summary>
    /// <param name="action">The async action to execute.</param>
    /// <param name="timeout">The maximum time to wait for completion.</param>
    /// <param name="timeoutMessage">The message to include in the timeout exception.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task ShouldCompleteWithin(this Func<Task> action, TimeSpan timeout, string timeoutMessage = null)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await action().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            var message = timeoutMessage ?? $"Operation did not complete within {timeout.TotalSeconds} seconds";
            throw new TimeoutException(message);
        }
    }

    /// <summary>
    /// Executes an async function and ensures it completes within the specified timeout.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="function">The async function to execute.</param>
    /// <param name="timeout">The maximum time to wait for completion.</param>
    /// <param name="timeoutMessage">The message to include in the timeout exception.</param>
    /// <returns>The result of the function.</returns>
    public static async Task<TResult> ShouldCompleteWithin<TResult>(this Func<Task<TResult>> function, TimeSpan timeout, string timeoutMessage = null)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            return await function().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            var message = timeoutMessage ?? $"Operation did not complete within {timeout.TotalSeconds} seconds";
            throw new TimeoutException(message);
        }
    }

    /// <summary>
    /// Asserts that an async action throws an exception of the specified type.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <param name="action">The async action to execute.</param>
    /// <param name="exceptionMessage">Optional expected exception message.</param>
    /// <returns>The caught exception.</returns>
    public static async Task<TException> ShouldThrow<TException>(this Func<Task> action, string exceptionMessage = null)
        where TException : Exception
    {
        try
        {
            await action();
            throw new AssertionException($"Expected exception of type {typeof(TException).Name} but no exception was thrown");
        }
        catch (TException ex)
        {
            if (!string.IsNullOrEmpty(exceptionMessage) && ex.Message != exceptionMessage)
            {
                throw new AssertionException($"Expected exception message '{exceptionMessage}' but got '{ex.Message}'");
            }
            return ex;
        }
    }

    /// <summary>
    /// Asserts that an async function throws an exception of the specified type.
    /// </summary>
    /// <typeparam name="TResult">The function result type.</typeparam>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <param name="function">The async function to execute.</param>
    /// <param name="exceptionMessage">Optional expected exception message.</param>
    /// <returns>The caught exception.</returns>
    public static async Task<TException> ShouldThrow<TResult, TException>(this Func<Task<TResult>> function, string exceptionMessage = null)
        where TException : Exception
    {
        try
        {
            await function();
            throw new AssertionException($"Expected exception of type {typeof(TException).Name} but no exception was thrown");
        }
        catch (TException ex)
        {
            if (!string.IsNullOrEmpty(exceptionMessage) && ex.Message != exceptionMessage)
            {
                throw new AssertionException($"Expected exception message '{exceptionMessage}' but got '{ex.Message}'");
            }
            return ex;
        }
    }

    /// <summary>
    /// Measures the execution time of an async action.
    /// </summary>
    /// <param name="action">The async action to measure.</param>
    /// <returns>The execution time.</returns>
    public static async Task<TimeSpan> MeasureExecutionTime(this Func<Task> action)
    {
        var stopwatch = Stopwatch.StartNew();
        await action();
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    /// <summary>
    /// Measures the execution time of an async function.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="function">The async function to measure.</param>
    /// <returns>A tuple containing the result and execution time.</returns>
    public static async Task<(TResult Result, TimeSpan ExecutionTime)> MeasureExecutionTime<TResult>(this Func<Task<TResult>> function)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await function();
        stopwatch.Stop();
        return (result, stopwatch.Elapsed);
    }

    #endregion

    #region Collection Extensions

    /// <summary>
    /// Asserts that a collection contains the specified item.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <param name="item">The item to find.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldContain<T>(this IEnumerable<T> collection, T item, string message = null)
    {
        if (!collection.Contains(item))
        {
            var msg = message ?? $"Collection does not contain expected item: {item}";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// Asserts that a collection does not contain the specified item.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <param name="item">The item that should not be present.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldNotContain<T>(this IEnumerable<T> collection, T item, string message = null)
    {
        if (collection.Contains(item))
        {
            var msg = message ?? $"Collection should not contain item: {item}";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// Asserts that a collection is empty.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldBeEmpty<T>(this IEnumerable<T> collection, string message = null)
    {
        if (collection.Any())
        {
            var msg = message ?? $"Collection should be empty but contains {collection.Count()} items";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// Asserts that a collection is not empty.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldNotBeEmpty<T>(this IEnumerable<T> collection, string message = null)
    {
        if (!collection.Any())
        {
            var msg = message ?? "Collection should not be empty";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// Asserts that a collection has the expected count.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <param name="expectedCount">The expected number of items.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldHaveCount<T>(this IEnumerable<T> collection, int expectedCount, string message = null)
    {
        var actualCount = collection.Count();
        if (actualCount != expectedCount)
        {
            var msg = message ?? $"Collection should have {expectedCount} items but has {actualCount}";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// Asserts that all items in a collection satisfy a predicate.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <param name="predicate">The predicate that all items should satisfy.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldAll<T>(this IEnumerable<T> collection, Func<T, bool> predicate, string message = null)
    {
        var failures = collection.Where(item => !predicate(item)).ToList();
        if (failures.Any())
        {
            var msg = message ?? $"{failures.Count} items failed the predicate check";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// Asserts that any item in a collection satisfies a predicate.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <param name="predicate">The predicate that at least one item should satisfy.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldAny<T>(this IEnumerable<T> collection, Func<T, bool> predicate, string message = null)
    {
        if (!collection.Any(predicate))
        {
            var msg = message ?? "No items in the collection satisfy the predicate";
            throw new AssertionException(msg);
        }
    }

    #endregion

    #region Object Extensions

    /// <summary>
    /// Asserts that an object is not null.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldNotBeNull(this object obj, string message = null)
    {
        if (obj == null)
        {
            var msg = message ?? "Object should not be null";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// Asserts that an object is null.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldBeNull(this object obj, string message = null)
    {
        if (obj != null)
        {
            var msg = message ?? "Object should be null";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// Asserts that two objects are equal.
    /// </summary>
    /// <param name="actual">The actual value.</param>
    /// <param name="expected">The expected value.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldEqual(this object actual, object expected, string message = null)
    {
        if (!Equals(actual, expected))
        {
            var msg = message ?? $"Expected {expected} but got {actual}";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// Asserts that two objects are not equal.
    /// </summary>
    /// <param name="actual">The actual value.</param>
    /// <param name="unexpected">The unexpected value.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldNotEqual(this object actual, object unexpected, string message = null)
    {
        if (Equals(actual, unexpected))
        {
            var msg = message ?? $"Value should not equal {unexpected}";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// Asserts that an object is of the specified type.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <param name="expectedType">The expected type.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldBeOfType(this object obj, Type expectedType, string message = null)
    {
        if (obj.GetType() != expectedType)
        {
            var msg = message ?? $"Object should be of type {expectedType.Name} but is {obj.GetType().Name}";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// Asserts that an object is assignable to the specified type.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <param name="expectedType">The expected type.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldBeAssignableTo(this object obj, Type expectedType, string message = null)
    {
        if (!expectedType.IsAssignableFrom(obj.GetType()))
        {
            var msg = message ?? $"Object should be assignable to {expectedType.Name} but is {obj.GetType().Name}";
            throw new AssertionException(msg);
        }
    }

    #endregion

    #region String Extensions

    /// <summary>
    /// Asserts that a string is not null or empty.
    /// </summary>
    /// <param name="str">The string to check.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldNotBeNullOrEmpty(this string str, string message = null)
    {
        if (string.IsNullOrEmpty(str))
        {
            var msg = message ?? "String should not be null or empty";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// Asserts that a string is not null or whitespace.
    /// </summary>
    /// <param name="str">The string to check.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldNotBeNullOrWhiteSpace(this string str, string message = null)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            var msg = message ?? "String should not be null or whitespace";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// Asserts that a string contains the specified substring.
    /// </summary>
    /// <param name="str">The string to check.</param>
    /// <param name="substring">The substring to find.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldContain(this string str, string substring, string message = null)
    {
        if (!str.Contains(substring))
        {
            var msg = message ?? $"String should contain '{substring}'";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// Asserts that a string starts with the specified prefix.
    /// </summary>
    /// <param name="str">The string to check.</param>
    /// <param name="prefix">The expected prefix.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldStartWith(this string str, string prefix, string message = null)
    {
        if (!str.StartsWith(prefix))
        {
            var msg = message ?? $"String should start with '{prefix}'";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// Asserts that a string ends with the specified suffix.
    /// </summary>
    /// <param name="str">The string to check.</param>
    /// <param name="suffix">The expected suffix.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldEndWith(this string str, string suffix, string message = null)
    {
        if (!str.EndsWith(suffix))
        {
            var msg = message ?? $"String should end with '{suffix}'";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// Asserts that a string matches a regular expression pattern.
    /// </summary>
    /// <param name="str">The string to check.</param>
    /// <param name="pattern">The regex pattern.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldMatch(this string str, string pattern, string message = null)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(str, pattern))
        {
            var msg = message ?? $"String should match pattern '{pattern}'";
            throw new AssertionException(msg);
        }
    }

    #endregion

    #region Numeric Extensions

    /// <summary>
    /// Asserts that a value is greater than the specified threshold.
    /// </summary>
    /// <typeparam name="T">The numeric type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="threshold">The threshold value.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldBeGreaterThan<T>(this T value, T threshold, string message = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(threshold) <= 0)
        {
            var msg = message ?? $"{value} should be greater than {threshold}";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// Asserts that a value is less than the specified threshold.
    /// </summary>
    /// <typeparam name="T">The numeric type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="threshold">The threshold value.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldBeLessThan<T>(this T value, T threshold, string message = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(threshold) >= 0)
        {
            var msg = message ?? $"{value} should be less than {threshold}";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// Asserts that a value is within the specified range (inclusive).
    /// </summary>
    /// <typeparam name="T">The numeric type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldBeInRange<T>(this T value, T min, T max, string message = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
        {
            var msg = message ?? $"{value} should be between {min} and {max}";
            throw new AssertionException(msg);
        }
    }

    #endregion

    #region Boolean Extensions

    /// <summary>
    /// Asserts that a boolean value is true.
    /// </summary>
    /// <param name="value">The boolean value to check.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldBeTrue(this bool value, string message = null)
    {
        if (!value)
        {
            var msg = message ?? "Value should be true";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// Asserts that a boolean value is false.
    /// </summary>
    /// <param name="value">The boolean value to check.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldBeFalse(this bool value, string message = null)
    {
        if (value)
        {
            var msg = message ?? "Value should be false";
            throw new AssertionException(msg);
        }
    }

    #endregion

    #region Time Extensions

    /// <summary>
    /// Asserts that a TimeSpan is within the expected range.
    /// </summary>
    /// <param name="actual">The actual TimeSpan.</param>
    /// <param name="expected">The expected TimeSpan.</param>
    /// <param name="tolerance">The allowed tolerance.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldBeCloseTo(this TimeSpan actual, TimeSpan expected, TimeSpan tolerance, string message = null)
    {
        var difference = (actual - expected).Duration();
        if (difference > tolerance)
        {
            var msg = message ?? $"{actual} should be within {tolerance} of {expected}";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// Asserts that a DateTime is within the expected range.
    /// </summary>
    /// <param name="actual">The actual DateTime.</param>
    /// <param name="expected">The expected DateTime.</param>
    /// <param name="tolerance">The allowed tolerance.</param>
    /// <param name="message">Optional assertion message.</param>
    public static void ShouldBeCloseTo(this DateTime actual, DateTime expected, TimeSpan tolerance, string message = null)
    {
        var difference = (actual - expected).Duration();
        if (difference > tolerance)
        {
            var msg = message ?? $"{actual} should be within {tolerance} of {expected}";
            throw new AssertionException(msg);
        }
    }

    #endregion

    #region Test Context Extensions

    /// <summary>
    /// Creates a test scenario with the specified name and configuration.
    /// </summary>
    /// <param name="testClass">The test class instance.</param>
    /// <param name="scenarioName">The name of the scenario.</param>
    /// <param name="configure">Action to configure the scenario.</param>
    /// <returns>A configured test scenario.</returns>
    public static TestScenario WithScenario(this object testClass, string scenarioName, Action<TestScenario> configure = null)
    {
        var scenario = new TestScenario { Name = scenarioName };
        configure?.Invoke(scenario);
        return scenario;
    }

    /// <summary>
    /// Creates an isolated test context for the specified action.
    /// </summary>
    /// <param name="testClass">The test class instance.</param>
    /// <param name="action">The action to execute in isolation.</param>
    /// <returns>A task representing the isolated execution.</returns>
    public static async Task InIsolation(this object testClass, Func<Task> action)
    {
        using var isolation = new TestDataIsolationHelper();
        await isolation.ExecuteIsolatedAsync(action);
    }

    /// <summary>
    /// Creates an isolated test context for the specified function.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="testClass">The test class instance.</param>
    /// <param name="function">The function to execute in isolation.</param>
    /// <returns>The result of the isolated execution.</returns>
    public static async Task<TResult> InIsolation<TResult>(this object testClass, Func<Task<TResult>> function)
    {
        using var isolation = new TestDataIsolationHelper();
        return await isolation.ExecuteIsolatedAsync(function);
    }

    #endregion
}