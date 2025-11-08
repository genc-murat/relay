using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Relay.Core.Testing;

/// <summary>
/// Builder for configuring mock behaviors.
/// </summary>
/// <typeparam name="T">The type being mocked.</typeparam>
public class MockBuilder<T> where T : class
{
    private readonly MockInstance _mockInstance;

    internal MockBuilder(MockInstance mockInstance)
    {
        _mockInstance = mockInstance;
    }

    /// <summary>
    /// Configures a method to return a specific value.
    /// </summary>
    /// <typeparam name="TResult">The return type.</typeparam>
    /// <param name="expression">Expression representing the method to configure.</param>
    /// <param name="returnValue">The value to return.</param>
    /// <returns>The mock builder for chaining.</returns>
    public MockBuilder<T> Setup<TResult>(Expression<Func<T, TResult>> expression, TResult returnValue)
    {
        _mockInstance.Setup(expression, returnValue);
        return this;
    }

    /// <summary>
    /// Configures a method to execute a function.
    /// </summary>
    /// <typeparam name="TResult">The return type.</typeparam>
    /// <param name="expression">Expression representing the method to configure.</param>
    /// <param name="func">The function to execute.</param>
    /// <returns>The mock builder for chaining.</returns>
    public MockBuilder<T> Setup<TResult>(Expression<Func<T, TResult>> expression, Func<TResult> func)
    {
        _mockInstance.Setup(expression, func);
        return this;
    }

    /// <summary>
    /// Configures a method to execute an asynchronous function.
    /// </summary>
    /// <typeparam name="TResult">The return type.</typeparam>
    /// <param name="expression">Expression representing the method to configure.</param>
    /// <param name="func">The asynchronous function to execute.</param>
    /// <returns>The mock builder for chaining.</returns>
    public MockBuilder<T> Setup<TResult>(Expression<Func<T, TResult>> expression, Func<Task<TResult>> func)
    {
        _mockInstance.Setup(expression, func);
        return this;
    }

    /// <summary>
    /// Configures a method to throw an exception.
    /// </summary>
    /// <typeparam name="TResult">The return type.</typeparam>
    /// <param name="expression">Expression representing the method to configure.</param>
    /// <param name="exception">The exception to throw.</param>
    /// <returns>The mock builder for chaining.</returns>
    public MockBuilder<T> SetupThrows<TResult>(Expression<Func<T, TResult>> expression, Exception exception)
    {
        _mockInstance.SetupThrows(expression, exception);
        return this;
    }

    /// <summary>
    /// Configures a void method to execute an action.
    /// </summary>
    /// <param name="expression">Expression representing the method to configure.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>The mock builder for chaining.</returns>
    public MockBuilder<T> Setup(Expression<Action<T>> expression, Action action)
    {
        _mockInstance.Setup(expression, action);
        return this;
    }

    /// <summary>
    /// Configures a void method to throw an exception.
    /// </summary>
    /// <param name="expression">Expression representing the method to configure.</param>
    /// <param name="exception">The exception to throw.</param>
    /// <returns>The mock builder for chaining.</returns>
    public MockBuilder<T> SetupThrows(Expression<Action<T>> expression, Exception exception)
    {
        _mockInstance.SetupThrows(expression, exception);
        return this;
    }

    /// <summary>
    /// Configures a method to return values in sequence.
    /// </summary>
    /// <typeparam name="TResult">The return type.</typeparam>
    /// <param name="expression">Expression representing the method to configure.</param>
    /// <param name="returnValues">The sequence of values to return.</param>
    /// <returns>The mock builder for chaining.</returns>
    public MockBuilder<T> SetupSequence<TResult>(Expression<Func<T, TResult>> expression, params TResult[] returnValues)
    {
        _mockInstance.SetupSequence(expression, returnValues);
        return this;
    }

    /// <summary>
    /// Gets the mock instance for verification.
    /// </summary>
    public MockInstance Instance => _mockInstance;
}
