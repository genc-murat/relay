using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Relay.Core.Testing;

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
