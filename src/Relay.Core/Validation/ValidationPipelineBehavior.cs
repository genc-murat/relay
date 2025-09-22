using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation
{
    /// <summary>
    /// Pipeline behavior that automatically validates requests before they reach handlers.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    public class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IValidator<TRequest> _validator;

        /// <summary>
        /// Initializes a new instance of the ValidationPipelineBehavior class.
        /// </summary>
        /// <param name="validator">The validator to use for request validation.</param>
        public ValidationPipelineBehavior(IValidator<TRequest> validator)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        /// <inheritdoc />
        public async ValueTask<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Validate the request
            var errors = await _validator.ValidateAsync(request, cancellationToken);

            // If there are validation errors, throw a ValidationException
            if (errors.Any())
            {
                throw new ValidationException(typeof(TRequest), errors);
            }

            // If validation passes, continue with the pipeline
            return await next();
        }
    }

    /// <summary>
    /// Pipeline behavior that automatically validates streaming requests before they reach handlers.
    /// </summary>
    /// <typeparam name="TRequest">The type of the streaming request.</typeparam>
    /// <typeparam name="TResponse">The type of the response items.</typeparam>
    public class StreamValidationPipelineBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    {
        private readonly IValidator<TRequest> _validator;

        /// <summary>
        /// Initializes a new instance of the StreamValidationPipelineBehavior class.
        /// </summary>
        /// <param name="validator">The validator to use for request validation.</param>
        public StreamValidationPipelineBehavior(IValidator<TRequest> validator)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<TResponse> HandleAsync(
            TRequest request,
            StreamHandlerDelegate<TResponse> next,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // Validate the request
            var errors = await _validator.ValidateAsync(request, cancellationToken);

            // If there are validation errors, throw a ValidationException
            if (errors.Any())
            {
                throw new ValidationException(typeof(TRequest), errors);
            }

            // If validation passes, continue with the pipeline
            await foreach (var item in next().WithCancellation(cancellationToken))
            {
                yield return item;
            }
        }
    }
}