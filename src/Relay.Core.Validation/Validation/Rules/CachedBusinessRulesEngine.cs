using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Cached business rules engine for performance.
/// </summary>
public class CachedBusinessRulesEngine : IBusinessRulesEngine
{
    private readonly IBusinessRulesEngine _innerEngine;
    private readonly ICache _cache;

    public CachedBusinessRulesEngine(IBusinessRulesEngine innerEngine, ICache cache)
    {
        _innerEngine = innerEngine ?? throw new ArgumentNullException(nameof(innerEngine));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async ValueTask<IEnumerable<string>> ValidateBusinessRulesAsync(
        BusinessValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Create cache key based on request properties that affect validation
        var cacheKey = $"business_rules_{request.UserType}_{request.CountryCode}_{request.BusinessCategory}";

        // Try cache first
        var cached = await _cache.GetAsync<IEnumerable<string>>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        // Execute validation
        var errors = await _innerEngine.ValidateBusinessRulesAsync(request, cancellationToken);

        // Cache results for 10 minutes (business rules don't change frequently)
        await _cache.SetAsync(cacheKey, errors, TimeSpan.FromMinutes(10), cancellationToken);

        return errors;
    }
}
