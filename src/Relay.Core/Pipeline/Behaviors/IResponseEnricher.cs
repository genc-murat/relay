using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Pipeline.Behaviors
{

    /// <summary>
    /// Example response enricher interface for demonstration purposes.
    /// </summary>
    /// <typeparam name="TResponse">The type of response to enrich.</typeparam>
    public interface IResponseEnricher<TResponse>
    {
        ValueTask<TResponse> EnrichAsync(TResponse response, CancellationToken cancellationToken);
    }
}
