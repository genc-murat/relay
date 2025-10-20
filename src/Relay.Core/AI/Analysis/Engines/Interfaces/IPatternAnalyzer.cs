using System;

namespace Relay.Core.AI.Analysis.Engines
{
    /// <summary>
    /// Interface for analyzing prediction patterns
    /// </summary>
    internal interface IPatternAnalyzer
    {
        /// <summary>
        /// Analyzes prediction patterns from recent predictions
        /// </summary>
        /// <param name="predictions">Array of recent predictions</param>
        /// <returns>Analysis result containing pattern insights</returns>
        PatternAnalysisResult AnalyzePatterns(PredictionResult[] predictions);
    }
}