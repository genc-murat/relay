using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Analysis.Engines
{
    /// <summary>
    /// Retrains and optimizes pattern recognition models based on prediction feedback
    /// </summary>
    internal sealed class PatternRecognitionEngine
    {
        private readonly ILogger<PatternRecognitionEngine> _logger;
        private readonly IPatternAnalyzer _patternAnalyzer;
        private readonly IEnumerable<IPatternUpdater> _patternUpdaters;
        private readonly PatternRecognitionConfig _config;

        public PatternRecognitionEngine(
            ILogger<PatternRecognitionEngine> logger,
            IPatternAnalyzer patternAnalyzer,
            IEnumerable<IPatternUpdater> patternUpdaters,
            PatternRecognitionConfig config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _patternAnalyzer = patternAnalyzer ?? throw new ArgumentNullException(nameof(patternAnalyzer));
            _patternUpdaters = patternUpdaters ?? throw new ArgumentNullException(nameof(patternUpdaters));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void RetrainPatternRecognition(PredictionResult[] recentPredictions)
        {
            if (recentPredictions.Length < _config.MinimumPredictionsForRetraining)
            {
                _logger.LogDebug("Insufficient data for pattern retraining: {Count} predictions (minimum: {Minimum})",
                    recentPredictions.Length, _config.MinimumPredictionsForRetraining);
                return;
            }

            try
            {
                _logger.LogInformation("Starting pattern recognition retraining with {Count} predictions",
                    recentPredictions.Length);

                var patternAnalysis = _patternAnalyzer.AnalyzePatterns(recentPredictions);

                foreach (var updater in _patternUpdaters)
                {
                    patternAnalysis.PatternsUpdated += updater.UpdatePatterns(recentPredictions, patternAnalysis);
                }

                _logger.LogInformation("Pattern recognition retraining completed. " +
                    "Overall accuracy: {Accuracy:P}, Patterns updated: {PatternsUpdated}",
                    patternAnalysis.OverallAccuracy, patternAnalysis.PatternsUpdated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during pattern recognition retraining");
            }
        }

    }
}
