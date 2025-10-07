using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Relay.Core.Implementation.Base
{
    /// <summary>
    /// Base class for generated dispatchers providing common functionality.
    /// </summary>
    public abstract class BaseDispatcher
    {
        /// <summary>
        /// Gets the service provider for dependency resolution.
        /// </summary>
        protected IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Initializes a new instance of the BaseDispatcher class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency resolution.</param>
        protected BaseDispatcher(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Resolves a service from the service provider.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <returns>The resolved service instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected T GetService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Tries to resolve a service from the service provider.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <returns>The resolved service instance, or null if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected T? GetServiceOrNull<T>() where T : class
        {
            return ServiceProvider.GetService<T>();
        }

        /// <summary>
        /// Creates a scoped service provider for handler execution.
        /// </summary>
        /// <returns>A scoped service provider.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected IServiceScope CreateScope()
        {
            return ServiceProvider.CreateScope();
        }

        /// <summary>
        /// Handles exceptions that occur during handler execution.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="requestType">The type of the request being processed.</param>
        /// <param name="handlerName">The name of the handler, if applicable.</param>
        /// <returns>A RelayException wrapping the original exception.</returns>
        protected static RelayException HandleException(Exception exception, string requestType, string? handlerName = null)
        {
            if (exception is RelayException relayException)
            {
                return relayException;
            }

            return new RelayException(requestType, handlerName,
                $"An error occurred while processing request of type '{requestType}'", exception);
        }

        /// <summary>
        /// Validates that a request is not null.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <param name="parameterName">The name of the parameter for exception reporting.</param>
        /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void ValidateRequest(object? request, string parameterName = "request")
        {
            if (request == null)
                throw new ArgumentNullException(parameterName);
        }

        /// <summary>
        /// Validates that a handler name is not null or empty.
        /// </summary>
        /// <param name="handlerName">The handler name to validate.</param>
        /// <param name="parameterName">The name of the parameter for exception reporting.</param>
        /// <exception cref="ArgumentException">Thrown when the handler name is null or empty.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void ValidateHandlerName(string? handlerName, string parameterName = "handlerName")
        {
            if (string.IsNullOrWhiteSpace(handlerName))
                throw new ArgumentException("Handler name cannot be null or empty.", parameterName);
        }
    }
}