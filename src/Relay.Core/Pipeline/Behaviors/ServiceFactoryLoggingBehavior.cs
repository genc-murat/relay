using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Infrastructure;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Extensions;

namespace Relay.Core.Pipeline.Behaviors
{
    /// <summary>
    /// Example pipeline behavior demonstrating ServiceFactory usage for dynamic service resolution.
    /// This behavior uses ServiceFactory to resolve ILogger at runtime instead of constructor injection.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <remarks>
    /// This example shows how ServiceFactory provides a flexible alternative to constructor injection,
    /// particularly useful when:
    /// - Services need to be resolved conditionally
    /// - You want to avoid circular dependencies
    /// - You need to resolve services that may not always be registered
    /// - You're migrating from MediatR and want to maintain compatibility
    /// </remarks>
    public class ServiceFactoryLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly ServiceFactory _serviceFactory;

        public ServiceFactoryLoggingBehavior(ServiceFactory serviceFactory)
        {
            _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
        }

        public async ValueTask<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Resolve logger using ServiceFactory - demonstrates flexible service resolution
            var logger = _serviceFactory.GetService<ILogger<ServiceFactoryLoggingBehavior<TRequest, TResponse>>>();

            if (logger != null)
            {
                logger.LogInformation(
                    "Handling request of type {RequestType} using ServiceFactory pattern",
                    typeof(TRequest).Name);
            }

            try
            {
                var response = await next();

                if (logger != null)
                {
                    logger.LogInformation(
                        "Successfully handled request of type {RequestType}",
                        typeof(TRequest).Name);
                }

                return response;
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.LogError(
                        ex,
                        "Error handling request of type {RequestType}",
                        typeof(TRequest).Name);
                }

                throw;
            }
        }
    }
}
