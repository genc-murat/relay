using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization.Services
{
    /// <summary>
    /// Interface for calculating health scores
    /// </summary>
    public interface IHealthScorer
    {
        /// <summary>
        /// Name of this health scorer
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Calculate health score from metrics
        /// </summary>
        double CalculateScore(Dictionary<string, double> metrics);

        /// <summary>
        /// Calculate health score asynchronously
        /// </summary>
        Task<double> CalculateScoreAsync(Dictionary<string, double> metrics, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get critical areas based on metrics
        /// </summary>
        IEnumerable<string> GetCriticalAreas(Dictionary<string, double> metrics);

        /// <summary>
        /// Get recommendations for improvement
        /// </summary>
        IEnumerable<string> GetRecommendations(Dictionary<string, double> metrics);
    }

    /// <summary>
    /// Composite health scorer that combines multiple scorers
    /// </summary>
    public class CompositeHealthScorer : IHealthScorer
    {
        private readonly ILogger _logger;
        private readonly HealthScoringOptions _options;
        private readonly Dictionary<string, IHealthScorer> _scorers;

        public CompositeHealthScorer(
            ILogger logger,
            HealthScoringOptions options,
            IEnumerable<IHealthScorer> scorers)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _scorers = new Dictionary<string, IHealthScorer>();

            foreach (var scorer in scorers)
            {
                _scorers[scorer.Name] = scorer;
            }
        }

        public string Name => "CompositeHealthScorer";

        public double CalculateScore(Dictionary<string, double> metrics)
        {
            var scores = new Dictionary<string, double>();
            var totalWeight = 0.0;

            // Performance
            if (_scorers.TryGetValue("PerformanceScorer", out var performanceScorer))
            {
                scores["Performance"] = performanceScorer.CalculateScore(metrics);
                totalWeight += _options.Weights.Performance;
            }

            // Reliability
            if (_scorers.TryGetValue("ReliabilityScorer", out var reliabilityScorer))
            {
                scores["Reliability"] = reliabilityScorer.CalculateScore(metrics);
                totalWeight += _options.Weights.Reliability;
            }

            // Scalability
            if (_scorers.TryGetValue("ScalabilityScorer", out var scalabilityScorer))
            {
                scores["Scalability"] = scalabilityScorer.CalculateScore(metrics);
                totalWeight += _options.Weights.Scalability;
            }

            // Security
            if (_scorers.TryGetValue("SecurityScorer", out var securityScorer))
            {
                scores["Security"] = securityScorer.CalculateScore(metrics);
                totalWeight += _options.Weights.Security;
            }

            // Maintainability
            if (_scorers.TryGetValue("MaintainabilityScorer", out var maintainabilityScorer))
            {
                scores["Maintainability"] = maintainabilityScorer.CalculateScore(metrics);
                totalWeight += _options.Weights.Maintainability;
            }

            if (totalWeight == 0)
            {
                _logger.LogWarning("No health scorers available for calculation");
                return 0.5; // Neutral score
            }

            // Calculate weighted average
            var weightedSum = 0.0;
            foreach (var score in scores)
            {
                var weight = GetWeightForScore(score.Key);
                weightedSum += score.Value * weight;
            }

            return weightedSum / totalWeight;
        }

        public async Task<double> CalculateScoreAsync(Dictionary<string, double> metrics, CancellationToken cancellationToken = default)
        {
            // For composite scorer, we can parallelize the individual score calculations
            var scoreTasks = new List<Task<KeyValuePair<string, double>>>();

            foreach (var scorer in _scorers.Values)
            {
                scoreTasks.Add(CalculateIndividualScoreAsync(scorer, metrics, cancellationToken));
            }

            var results = await Task.WhenAll(scoreTasks);

            var scores = new Dictionary<string, double>();
            foreach (var result in results)
            {
                scores[result.Key] = result.Value;
            }

            // Calculate weighted average (same logic as sync version)
            var totalWeight = 0.0;
            var weightedSum = 0.0;

            foreach (var score in scores)
            {
                var weight = GetWeightForScore(score.Key);
                weightedSum += score.Value * weight;
                totalWeight += weight;
            }

            return totalWeight > 0 ? weightedSum / totalWeight : 0.5;
        }

        public IEnumerable<string> GetCriticalAreas(Dictionary<string, double> metrics)
        {
            var criticalAreas = new List<string>();

            foreach (var scorer in _scorers.Values)
            {
                criticalAreas.AddRange(scorer.GetCriticalAreas(metrics));
            }

            return criticalAreas.Distinct();
        }

        public IEnumerable<string> GetRecommendations(Dictionary<string, double> metrics)
        {
            var recommendations = new List<string>();

            foreach (var scorer in _scorers.Values)
            {
                recommendations.AddRange(scorer.GetRecommendations(metrics));
            }

            return recommendations.Distinct();
        }

        private double GetWeightForScore(string scoreName)
        {
            return scoreName switch
            {
                "Performance" => _options.Weights.Performance,
                "Reliability" => _options.Weights.Reliability,
                "Scalability" => _options.Weights.Scalability,
                "Security" => _options.Weights.Security,
                "Maintainability" => _options.Weights.Maintainability,
                _ => 0.0
            };
        }

        private async Task<KeyValuePair<string, double>> CalculateIndividualScoreAsync(
            IHealthScorer scorer,
            Dictionary<string, double> metrics,
            CancellationToken cancellationToken)
        {
            try
            {
                var score = await scorer.CalculateScoreAsync(metrics, cancellationToken);
                return new KeyValuePair<string, double>(scorer.Name, score);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating score from {ScorerName}", scorer.Name);
                return new KeyValuePair<string, double>(scorer.Name, 0.5); // Neutral score on error
            }
        }
    }

    /// <summary>
    /// Base class for health scorers
    /// </summary>
    public abstract class HealthScorerBase : IHealthScorer
    {
        protected readonly ILogger _logger;

        protected HealthScorerBase(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public abstract string Name { get; }

        public virtual double CalculateScore(Dictionary<string, double> metrics)
        {
            try
            {
                return CalculateScoreCore(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating {ScorerName} score", Name);
                return 0.5; // Neutral score
            }
        }

        public virtual Task<double> CalculateScoreAsync(Dictionary<string, double> metrics, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CalculateScore(metrics));
        }

        public virtual IEnumerable<string> GetCriticalAreas(Dictionary<string, double> metrics)
        {
            return Array.Empty<string>();
        }

        public virtual IEnumerable<string> GetRecommendations(Dictionary<string, double> metrics)
        {
            return Array.Empty<string>();
        }

        protected abstract double CalculateScoreCore(Dictionary<string, double> metrics);
    }
}