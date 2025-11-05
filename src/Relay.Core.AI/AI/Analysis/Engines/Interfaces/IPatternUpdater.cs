using System;

namespace Relay.Core.AI.Analysis.Engines
{
    /// <summary>
    /// Interface for updating specific types of patterns
    /// </summary>
    internal interface IPatternUpdater
    {
        /// <summary>
        /// Updates patterns based on prediction analysis
        /// </summary>
        /// <param name="predictions">Array of recent predictions</param>
        /// <param name="analysis">Analysis result from pattern analyzer</param>
        /// <returns>Number of patterns updated</returns>
        int UpdatePatterns(PredictionResult[] predictions, PatternAnalysisResult analysis);
    }
}