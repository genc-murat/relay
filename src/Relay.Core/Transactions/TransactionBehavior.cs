using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Pipeline;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// A pipeline behavior that wraps the request handler in a database transaction.
    /// It begins a transaction, executes the handler, saves changes via the unit of work,
    /// and then commits the transaction. If an exception occurs, it rolls back.
    /// This behavior only applies to requests that implement <see cref="ITransactionalRequest"/>.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

        public TransactionBehavior(IUnitOfWork unitOfWork, ILogger<TransactionBehavior<TRequest, TResponse>> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (request is not ITransactionalRequest)
            {
                return await next();
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                _logger.LogInformation("----- Beginning transaction for {RequestName}", typeof(TRequest).Name);

                var response = await next();

                _logger.LogInformation("----- Saving changes for {RequestName}", typeof(TRequest).Name);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("----- Transaction committed for {RequestName}", typeof(TRequest).Name);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "----- Transaction for {RequestName} failed. Rolling back.", typeof(TRequest).Name);
                
                await transaction.RollbackAsync(cancellationToken);

                throw;
            }
        }
    }
}