using System;

namespace Relay.Core;

/// <summary>
/// Factory delegate for resolving services from a service provider.
/// This delegate provides a standard way to resolve dependencies from the DI container.
/// Compatible with MediatR's ServiceFactory pattern for easier migration.
/// </summary>
/// <param name="serviceType">The type of service to resolve.</param>
/// <returns>An instance of the requested service, or null if the service is not registered.</returns>
/// <remarks>
/// This delegate is typically implemented by wrapping IServiceProvider.GetService():
/// <code>
/// ServiceFactory factory = serviceType => serviceProvider.GetService(serviceType);
/// </code>
/// 
/// Example usage in pipeline behaviors:
/// <code>
/// public class MyBehavior : IPipelineBehavior&lt;TRequest, TResponse&gt;
/// {
///     private readonly ServiceFactory _serviceFactory;
///     
///     public MyBehavior(ServiceFactory serviceFactory)
///     {
///         _serviceFactory = serviceFactory;
///     }
///     
///     public async ValueTask&lt;TResponse&gt; HandleAsync(
///         TRequest request,
///         RequestHandlerDelegate&lt;TResponse&gt; next,
///         CancellationToken cancellationToken)
///     {
///         // Resolve a service dynamically
///         var logger = _serviceFactory(typeof(ILogger)) as ILogger;
///         
///         // Continue with pipeline
///         return await next();
///     }
/// }
/// </code>
/// </remarks>
public delegate object? ServiceFactory(Type serviceType);
