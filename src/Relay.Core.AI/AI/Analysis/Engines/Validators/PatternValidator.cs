using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Analysis.Engines;

/// <summary>
/// Validates retrained patterns
/// </summary>
internal class PatternValidator : IPatternUpdater
{
    private readonly ILogger<PatternValidator> _logger;
    private readonly PatternRecognitionConfig _config;

    public PatternValidator(ILogger<PatternValidator> logger, PatternRecognitionConfig config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public int UpdatePatterns(PredictionResult[] predictions, PatternAnalysisResult analysis)
    {
        try
        {
            var validationIssues = new List<string>();

            if (analysis.OverallAccuracy < _config.MinimumOverallAccuracy)
            {
                validationIssues.Add($"Overall accuracy below acceptable threshold: {analysis.OverallAccuracy:P}");
            }

            if (analysis.PatternsUpdated == 0)
            {
                validationIssues.Add("No patterns were updated during retraining");
            }

            if (validationIssues.Count > 0)
            {
                _logger.LogWarning("Pattern validation found {Count} issues: {Issues}",
                    validationIssues.Count, string.Join(", ", validationIssues));
            }
            else
            {
                _logger.LogDebug("All retrained patterns validated successfully");
            }

            return 0; // Validation doesn't update patterns, just validates
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating retrained patterns");
            return 0;
        }
    }
}