using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Cached implementation that uses both cache and database.
/// </summary>
public class CachedUsernameUniquenessChecker : IUsernameUniquenessChecker
{
    private readonly IUsernameUniquenessChecker _innerChecker;
    private readonly ICache _cache;

    public CachedUsernameUniquenessChecker(
        IUsernameUniquenessChecker innerChecker,
        ICache cache)
    {
        _innerChecker = innerChecker ?? throw new ArgumentNullException(nameof(innerChecker));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async ValueTask<bool> IsUsernameUniqueAsync(string username, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"username_unique_{username}";

        // Try cache first
        var cached = await _cache.GetAsync<bool?>(cacheKey, cancellationToken);
        if (cached.HasValue)
        {
            return cached.Value;
        }

        // Check database
        var isUnique = await _innerChecker.IsUsernameUniqueAsync(username, cancellationToken);

        // Cache result for 5 minutes
        await _cache.SetAsync(cacheKey, isUnique, TimeSpan.FromMinutes(5), cancellationToken);

        return isUnique;
    }
}

