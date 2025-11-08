using System;
using System.Linq;
using System.Reflection;

namespace Relay.Core.Testing;

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
