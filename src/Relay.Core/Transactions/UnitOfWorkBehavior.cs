using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.Pipeline;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Pipeline behavior that automatically calls SaveChangesAsync on the Unit of Work
    /// after successful request handler execution.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <remarks>
    /// This behavior implements the Unit of Work pattern commonly used with Entity Framework Core.
    /// It automatically persists changes after the handler completes successfully.
    ///
    /// Features:
    /// - Automatic SaveChanges after successful handler execution
    /// - No changes saved if handler throws exception
    /// - Works with any IUnitOfWork implementation (EF Core DbContext, Dapper UnitOfWork, etc.)
    /// - Optional: Only saves for commands (requests implementing ITransactionalRequest)
    ///
    /// Example usage with EF Core:
    /// <code>
    /// public class ApplicationDbContext : DbContext, IUnitOfWork
    /// {
    ///     // DbContext already implements SaveChangesAsync
    /// }
    ///
    /// services.AddScoped&lt;IUnitOfWork&gt;(sp => sp.GetRequiredService&lt;ApplicationDbContext&gt;());
    /// services.AddRelayUnitOfWork();
    /// </code>
    /// </remarks>
    public class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IUnitOfWork? _unitOfWork;
        private readonly ILogger<UnitOfWorkBehavior<TRequest, TResponse>>? _logger;
        private readonly bool _saveOnlyForTransactionalRequests;

        /// <summary>
        /// Initializes a new instance of UnitOfWorkBehavior.
        /// </summary>
        /// <param name="unitOfWork">The unit of work instance (optional - if null, behavior is skipped).</param>
        /// <param name="options">Configuration options for unit of work behavior.</param>
        /// <param name="logger">Optional logger for unit of work events.</param>
        public UnitOfWorkBehavior(
            IUnitOfWork? unitOfWork = null,
            IOptions<UnitOfWorkOptions>? options = null,
            ILogger<UnitOfWorkBehavior<TRequest, TResponse>>? logger = null)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _saveOnlyForTransactionalRequests = options?.Value?.SaveOnlyForTransactionalRequests ?? false;
        }

        /// <inheritdoc />
        public async ValueTask<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Skip if no unit of work is registered
            if (_unitOfWork == null)
            {
                return await next();
            }

            // Check if we should only save for transactional requests
            if (_saveOnlyForTransactionalRequests && request is not ITransactionalRequest)
            {
                return await next();
            }

            try
            {
                _logger?.LogDebug(
                    "Executing handler for {RequestType}",
                    typeof(TRequest).Name);

                var response = await next();

                _logger?.LogDebug(
                    "Saving changes for {RequestType}",
                    typeof(TRequest).Name);

                var entriesWritten = await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogDebug(
                    "Successfully saved {EntriesWritten} entries for {RequestType}",
                    entriesWritten,
                    typeof(TRequest).Name);

                return response;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(
                    ex,
                    "Handler failed for {RequestType}, changes not saved: {Message}",
                    typeof(TRequest).Name,
                    ex.Message);

                throw;
            }
        }
    }
}
