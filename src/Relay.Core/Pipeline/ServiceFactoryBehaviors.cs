using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Extensions;

namespace Relay.Core.Pipeline
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

    /// <summary>
    /// Example pipeline behavior that resolves multiple services using ServiceFactory.
    /// Demonstrates how to work with collections of services.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    public class MultiServiceResolutionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly ServiceFactory _serviceFactory;

        public MultiServiceResolutionBehavior(ServiceFactory serviceFactory)
        {
            _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
        }

        public async ValueTask<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Example: Resolve multiple validators for a request
            var validators = _serviceFactory.GetServices<IValidator<TRequest>>();

            foreach (var validator in validators)
            {
                // Validate using dynamically resolved validators
                await validator.ValidateAsync(request, cancellationToken);
            }

            // Continue pipeline
            return await next();
        }
    }

    /// <summary>
    /// Example validator interface for demonstration purposes.
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    public interface IValidator<in T>
    {
        ValueTask ValidateAsync(T instance, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Example pipeline behavior that demonstrates conditional service resolution.
    /// Uses TryGetService to safely attempt service resolution.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    public class ConditionalServiceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly ServiceFactory _serviceFactory;

        public ConditionalServiceBehavior(ServiceFactory serviceFactory)
        {
            _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
        }

        public async ValueTask<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Try to resolve an optional service - doesn't throw if not registered
            if (_serviceFactory.TryGetService<IRequestAuditor>(out var auditor) && auditor != null)
            {
                await auditor.AuditRequestAsync(typeof(TRequest).Name, request, cancellationToken);
            }

            var response = await next();

            // Optional response enrichment
            if (_serviceFactory.TryGetService<IResponseEnricher<TResponse>>(out var enricher) && enricher != null)
            {
                return await enricher.EnrichAsync(response, cancellationToken);
            }

            return response;
        }
    }

    /// <summary>
    /// Example auditor interface for demonstration purposes.
    /// </summary>
    public interface IRequestAuditor
    {
        ValueTask AuditRequestAsync(string requestType, object request, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Example response enricher interface for demonstration purposes.
    /// </summary>
    /// <typeparam name="TResponse">The type of response to enrich.</typeparam>
    public interface IResponseEnricher<TResponse>
    {
        ValueTask<TResponse> EnrichAsync(TResponse response, CancellationToken cancellationToken);
    }
}
