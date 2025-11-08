using System;

namespace Relay.Core.Testing;

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
