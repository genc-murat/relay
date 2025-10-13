using System;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Infrastructure;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Extensions;

namespace Relay.Core.Pipeline.Behaviors
{
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
}
