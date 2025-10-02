namespace Relay.Core.Transactions
{
    /// <summary>
    /// Marker interface indicating that a request should be executed within a transaction.
    /// Handlers for requests implementing this interface will be wrapped in a database transaction.
    /// </summary>
    /// <remarks>
    /// This is a marker interface compatible with MediatR's transaction management patterns.
    /// When a request implements this interface, the TransactionBehavior will automatically
    /// wrap the handler execution in a transaction scope.
    /// </remarks>
    public interface ITransactionalRequest
    {
    }

    /// <summary>
    /// Marker interface for transactional requests with a response.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    public interface ITransactionalRequest<out TResponse> : ITransactionalRequest
    {
    }
}
