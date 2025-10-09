using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI
{
    public interface IAIModelTrainer
    {
        /// <summary>
        /// Train AI models with the provided training data
        /// </summary>
        /// <param name="trainingData">Training data containing execution, optimization, and system load history</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        ValueTask TrainModelAsync(AITrainingData trainingData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Train AI models with progress reporting
        /// </summary>
        /// <param name="trainingData">Training data containing execution, optimization, and system load history</param>
        /// <param name="progressCallback">Callback to report training progress</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        ValueTask TrainModelAsync(AITrainingData trainingData, TrainingProgressCallback? progressCallback, CancellationToken cancellationToken = default);
    }
}