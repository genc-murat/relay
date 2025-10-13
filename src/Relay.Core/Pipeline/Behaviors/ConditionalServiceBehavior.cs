using System;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Infrastructure;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Extensions;

namespace Relay.Core.Pipeline.Behaviors
{
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
                await auditor.AuditRequestAsync(typeof(TRequest).Name, request!, cancellationToken);
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
}
