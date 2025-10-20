using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI
{
    public interface IAIPredictionModel
    {
        ValueTask<OptimizationRecommendation> PredictAsync(RequestContext context, CancellationToken cancellationToken = default);
    }
}