using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Authorization;

/// <summary>
/// Default implementation of IAuthorizationService.
/// </summary>
public class DefaultAuthorizationService : IAuthorizationService
{
    /// <inheritdoc />
    public async ValueTask<bool> AuthorizeAsync(IAuthorizationContext context, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Make method async for interface compliance

        // If no roles or policies are specified, allow access
        // In a real implementation, you would have more sophisticated logic here
        return true;
    }
}