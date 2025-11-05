using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

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

/// <summary>
/// Represents a mock instance with setup and verification capabilities.
/// </summary>
public class MockInstance
{
    private readonly Type _mockType;
    private readonly Dictionary<string, MockSetup> _setups = new();
    private readonly Dictionary<string, List<MockInvocation>> _invocations = new();

    /// <summary>
    /// Gets the proxy object that implements the mocked interface.
    /// </summary>
    public object Proxy { get; }

    internal MockInstance(Type mockType)
    {
        _mockType = mockType;
        Proxy = MockProxyGenerator.CreateProxy(mockType, this);
    }

    internal void Setup(LambdaExpression expression, object returnValue)
    {
        var methodCall = expression.Body as MethodCallExpression;
        if (methodCall == null)
            throw new ArgumentException("Expression must be a method call", nameof(expression));

        var methodName = GetMethodSignature(methodCall);
        _setups[methodName] = new MockSetup
        {
            ReturnValue = returnValue,
            SetupType = SetupType.ReturnValue
        };
    }

    internal void Setup(LambdaExpression expression, Delegate func)
    {
        var methodCall = expression.Body as MethodCallExpression;
        if (methodCall == null)
            throw new ArgumentException("Expression must be a method call", nameof(expression));

        var methodName = GetMethodSignature(methodCall);
        _setups[methodName] = new MockSetup
        {
            Func = func,
            SetupType = SetupType.Function
        };
    }

    internal void SetupThrows(LambdaExpression expression, Exception exception)
    {
        var methodCall = expression.Body as MethodCallExpression;
        if (methodCall == null)
            throw new ArgumentException("Expression must be a method call", nameof(expression));

        var methodName = GetMethodSignature(methodCall);
        _setups[methodName] = new MockSetup
        {
            Exception = exception,
            SetupType = SetupType.Throw
        };
    }

    internal void SetupSequence(LambdaExpression expression, Array returnValues)
    {
        var methodCall = expression.Body as MethodCallExpression;
        if (methodCall == null)
            throw new ArgumentException("Expression must be a method call", nameof(expression));

        var methodName = GetMethodSignature(methodCall);
        var values = new List<object>();
        foreach (var item in returnValues)
        {
            values.Add(item);
        }

        _setups[methodName] = new MockSetup
        {
            SequenceValues = values,
            SetupType = SetupType.Sequence,
            SequenceIndex = 0
        };
    }

    internal object Invoke(string methodName, object[] arguments)
    {
        // Record the invocation
        if (!_invocations.ContainsKey(methodName))
        {
            _invocations[methodName] = new List<MockInvocation>();
        }

        _invocations[methodName].Add(new MockInvocation
        {
            Arguments = arguments,
            Timestamp = DateTime.UtcNow
        });

        // Execute the setup
        if (_setups.TryGetValue(methodName, out var setup))
        {
            return setup.Execute(arguments);
        }

        // Default behavior - return default value for value types, null for reference types
        var methodInfo = _mockType.GetMethod(methodName.Split('(')[0]);
        if (methodInfo == null)
            throw new InvalidOperationException($"Method {methodName} not found on type {_mockType.Name}");

        return methodInfo.ReturnType.IsValueType ? Activator.CreateInstance(methodInfo.ReturnType) : null;
    }

    internal void Verify(LambdaExpression expression, CallTimes times)
    {
        var methodCall = expression.Body as MethodCallExpression;
        if (methodCall == null)
            throw new ArgumentException("Expression must be a method call", nameof(expression));

        var methodName = GetMethodSignature(methodCall);

        if (!_invocations.TryGetValue(methodName, out var invocations))
        {
            invocations = new List<MockInvocation>();
        }

        if (!times.Validate(invocations.Count))
        {
            throw new MockVerificationException(
                $"Expected {times.Description} calls to {methodName}, but received {invocations.Count} calls.");
        }
    }

    internal void Reset()
    {
        _invocations.Clear();
        foreach (var setup in _setups.Values)
        {
            setup.SequenceIndex = 0;
        }
    }

    private string GetMethodSignature(MethodCallExpression methodCall)
    {
        var method = methodCall.Method;
        var parameters = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
        return $"{method.Name}({parameters})";
    }
}

/// <summary>
/// Generator for creating dynamic proxy objects.
/// </summary>
internal static class MockProxyGenerator
{
    public static object CreateProxy(Type interfaceType, MockInstance mockInstance)
    {
        if (!interfaceType.IsInterface)
        {
            throw new ArgumentException("Only interfaces can be mocked", nameof(interfaceType));
        }

        // For simplicity, we'll use a basic proxy implementation
        // In a real implementation, you might use a library like Castle DynamicProxy
        return InterfaceProxy.Create(interfaceType, mockInstance);
    }
}

/// <summary>
/// Simple interface proxy implementation.
/// </summary>
internal class InterfaceProxy : DispatchProxy
{
    private Type _interfaceType;
    private MockInstance _mockInstance;

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null || args == null)
            throw new ArgumentNullException();

        var methodName = GetMethodSignature(targetMethod);
        return _mockInstance.Invoke(methodName, args);
    }

    public static object Create(Type interfaceType, MockInstance mockInstance)
    {
        var proxy = (InterfaceProxy)Create(interfaceType, typeof(InterfaceProxy));
        proxy._interfaceType = interfaceType;
        proxy._mockInstance = mockInstance;
        return proxy;
    }

    private string GetMethodSignature(MethodInfo method)
    {
        var parameters = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
        return $"{method.Name}({parameters})";
    }
}

/// <summary>
/// Represents a mock setup configuration.
/// </summary>
internal class MockSetup
{
    public SetupType SetupType { get; set; }
    public object ReturnValue { get; set; }
    public Delegate Func { get; set; }
    public Exception Exception { get; set; }
    public List<object> SequenceValues { get; set; }
    public int SequenceIndex { get; set; }

    public object Execute(object[] arguments)
    {
        switch (SetupType)
        {
            case SetupType.ReturnValue:
                return ReturnValue;

            case SetupType.Function:
                return Func.DynamicInvoke(arguments);

            case SetupType.Throw:
                throw Exception;

            case SetupType.Sequence:
                var value = SequenceValues[SequenceIndex % SequenceValues.Count];
                SequenceIndex++;
                return value;

            default:
                throw new InvalidOperationException($"Unknown setup type: {SetupType}");
        }
    }
}

/// <summary>
/// Represents a method invocation on a mock.
/// </summary>
internal class MockInvocation
{
    public object[] Arguments { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Defines the types of mock setups.
/// </summary>
internal enum SetupType
{
    ReturnValue,
    Function,
    Throw,
    Sequence
}

/// <summary>
/// <summary>
/// Represents expected call counts for mock verification.
/// </summary>
public class CallTimes
{
    private readonly int _from;
    private readonly int _to;

    private CallTimes(int from, int to)
    {
        _from = from;
        _to = to;
    }

    /// <summary>
    /// Gets a human-readable description of the expected call count range.
    /// </summary>
    /// <value>A string describing the expected call count.</value>
    public string Description => _from == _to ? $"exactly {_from}" : $"between {_from} and {_to}";

    /// <summary>
    /// Validates whether the actual call count falls within the expected range.
    /// </summary>
    /// <param name="actualCount">The actual number of calls made.</param>
    /// <returns><c>true</c> if the actual count is within the expected range; otherwise, <c>false</c>.</returns>
    public bool Validate(int actualCount)
    {
        return actualCount >= _from && actualCount <= _to;
    }

    /// <summary>
    /// Specifies that the method should be called exactly once.
    /// </summary>
    /// <returns>A <see cref="CallTimes"/> instance representing exactly one call.</returns>
    public static CallTimes Once() => new(1, 1);

    /// <summary>
    /// Specifies that the method should never be called.
    /// </summary>
    /// <returns>A <see cref="CallTimes"/> instance representing zero calls.</returns>
    public static CallTimes Never() => new(0, 0);

    /// <summary>
    /// Specifies that the method should be called at least once.
    /// </summary>
    /// <returns>A <see cref="CallTimes"/> instance representing one or more calls.</returns>
    public static CallTimes AtLeastOnce() => new(1, int.MaxValue);

    /// <summary>
    /// Specifies that the method should be called at most once.
    /// </summary>
    /// <returns>A <see cref="CallTimes"/> instance representing zero or one call.</returns>
    public static CallTimes AtMostOnce() => new(0, 1);

    /// <summary>
    /// Specifies that the method should be called exactly the specified number of times.
    /// </summary>
    /// <param name="count">The exact number of calls expected.</param>
    /// <returns>A <see cref="CallTimes"/> instance representing exactly <paramref name="count"/> calls.</returns>
    public static CallTimes Exactly(int count) => new(count, count);

    /// <summary>
    /// Specifies that the method should be called between the specified minimum and maximum number of times.
    /// </summary>
    /// <param name="from">The minimum number of calls expected (inclusive).</param>
    /// <param name="to">The maximum number of calls expected (inclusive).</param>
    /// <returns>A <see cref="CallTimes"/> instance representing calls between <paramref name="from"/> and <paramref name="to"/>.</returns>
    public static CallTimes Between(int from, int to) => new(from, to);
}

/// <summary>
/// Exception thrown when mock verification fails.
/// </summary>
public class MockVerificationException : Exception
{
    public MockVerificationException(string message) : base(message) { }
}