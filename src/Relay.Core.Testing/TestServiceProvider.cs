using System;
using System.Collections.Generic;

namespace Relay.Core.Testing;

/// <summary>
/// Simple service provider implementation for testing.
/// </summary>
internal class TestServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();

    /// <summary>
    /// Registers a service instance.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="service">The service instance.</param>
    public void Register<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }

    /// <summary>
    /// Gets the service object of the specified type.
    /// </summary>
    /// <param name="serviceType">An object that specifies the type of service object to get.</param>
    /// <returns>A service object of type serviceType, or null if there is no service object of type serviceType.</returns>
    public object? GetService(Type serviceType)
    {
        _services.TryGetValue(serviceType, out var service);
        return service;
    }
}