using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI
{
    public interface IAIPredictionCache
    {
        ValueTask<OptimizationRecommendation?> GetCachedPredictionAsync(string key, CancellationToken cancellationToken = default);
        ValueTask SetCachedPredictionAsync(string key, OptimizationRecommendation recommendation, TimeSpan expiry, CancellationToken cancellationToken = default);
    }
}