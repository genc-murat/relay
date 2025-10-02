namespace Relay.Core.Transactions
{
    /// <summary>
    /// Configuration options for Unit of Work behavior.
    /// </summary>
    public class UnitOfWorkOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether to save changes only for requests implementing ITransactionalRequest.
        /// If false, saves changes for all requests. Default is false.
        /// </summary>
        public bool SaveOnlyForTransactionalRequests { get; set; } = false;
    }
}
