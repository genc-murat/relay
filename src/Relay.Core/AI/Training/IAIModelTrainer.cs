using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI
{
    public interface IAIModelTrainer
    {
        ValueTask TrainModelAsync(AITrainingData trainingData, CancellationToken cancellationToken = default);
    }
}