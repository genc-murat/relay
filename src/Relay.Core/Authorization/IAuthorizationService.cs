using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Authorization
{
    /// <summary>
    /// Interface for authorization services.
    /// </summary>
    public interface IAuthorizationService
    {
        /// <summary>
        /// Authorizes the specified context.
        /// </summary>
        /// <param name="context">The authorization context.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if authorized, false otherwise.</returns>
        ValueTask<bool> AuthorizeAsync(IAuthorizationContext context, CancellationToken cancellationToken = default);
    }
}