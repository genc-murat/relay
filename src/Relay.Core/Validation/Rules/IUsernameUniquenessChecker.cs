using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Interface for checking username uniqueness.
/// This abstraction allows for different implementations (database, cache, external service).
/// </summary>
public interface IUsernameUniquenessChecker
{
    /// <summary>
    /// Checks if the given username is unique.
    /// </summary>
    /// <param name="username">The username to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if username is unique, false otherwise</returns>
    ValueTask<bool> IsUsernameUniqueAsync(string username, CancellationToken cancellationToken = default);
}

