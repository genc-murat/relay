using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;

namespace Relay.Core.Testing;

/// <summary>
/// Helper class for creating and managing dependency mocks in tests.
/// Provides fluent API for configuring mock behaviors and verification.
/// </summary>
public class DependencyMockHelper
{
    private readonly Dictionary<Type, MockInstance> _mocks = new();
    private readonly TestServiceProvider _serviceProvider = new();

    /// <summary>
    /// Gets the service provider containing all registered mocks.
    /// </summary>
    public IServiceProvider ServiceProvider => _serviceProvider;

    /// <summary>
    /// Creates a mock for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to mock.</typeparam>
    /// <returns>A mock builder for the specified type.</returns>
    public MockBuilder<T> Mock<T>() where T : class
    {
        var mockType = typeof(T);
        if (!_mocks.ContainsKey(mockType))
        {
            var mockInstance = new MockInstance(typeof(T));
            _mocks[mockType] = mockInstance;
            _serviceProvider.Register<T>(mockInstance.Proxy as T);
        }

        return new MockBuilder<T>(_mocks[mockType]);
    }

    /// <summary>
    /// Gets a mock instance for verification.
    /// </summary>
    /// <typeparam name="T">The mock type.</typeparam>
    /// <returns>The mock instance for verification.</returns>
    public MockInstance GetMock<T>() where T : class
    {
        var mockType = typeof(T);
        if (!_mocks.TryGetValue(mockType, out var mock))
        {
            throw new InvalidOperationException($"No mock registered for type {mockType.Name}");
        }

        return mock;
    }

    /// <summary>
    /// Verifies that a method was called on a mock.
    /// </summary>
    /// <typeparam name="T">The mock type.</typeparam>
    /// <param name="expression">Expression representing the method call to verify.</param>
    /// <param name="times">The expected number of calls.</param>
    public void Verify<T>(Expression<Action<T>> expression, CallTimes times = null) where T : class
    {
        times ??= CallTimes.Once();
        var mock = GetMock<T>();
        mock.Verify(expression, times);
    }

    /// <summary>
    /// Verifies that a method was called on a mock with a return value.
    /// </summary>
    /// <typeparam name="T">The mock type.</typeparam>
    /// <typeparam name="TResult">The return type.</typeparam>
    /// <param name="expression">Expression representing the method call to verify.</param>
    /// <param name="times">The expected number of calls.</param>
    public void Verify<T, TResult>(Expression<Func<T, TResult>> expression, CallTimes times = null) where T : class
    {
        times ??= CallTimes.Once();
        var mock = GetMock<T>();
        mock.Verify(expression, times);
    }

    /// <summary>
    /// Resets all mocks to their initial state.
    /// </summary>
    public void ResetAll()
    {
        foreach (var mock in _mocks.Values)
        {
            mock.Reset();
        }
    }
}
