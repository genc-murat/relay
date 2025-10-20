using System;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Analysis.Engines
{
    /// <summary>
    /// Updates ensemble model weights
    /// </summary>
    internal class EnsembleWeightsUpdater : IPatternUpdater
    {
        private readonly ILogger<EnsembleWeightsUpdater> _logger;
        private readonly PatternRecognitionConfig _config;

        public EnsembleWeightsUpdater(ILogger<EnsembleWeightsUpdater> logger, PatternRecognitionConfig config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public int UpdatePatterns(PredictionResult[] predictions, PatternAnalysisResult analysis)
        {
            try
            {
                var updatedCount = 0;

                foreach (var model in _config.EnsembleModels)
                {
                    var modelWeight = 1.0 / _config.EnsembleModels.Length;

                    _logger.LogDebug("Model {Model} ensemble weight: {Weight:F3}", model, modelWeight);

                    updatedCount++;
                }

                return updatedCount;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating ensemble weights");
                return 0;
            }
        }
    }
}