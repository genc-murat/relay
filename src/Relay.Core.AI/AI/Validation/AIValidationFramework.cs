using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{

    /// <summary>
    /// AI validation framework implementation.
    /// </summary>
    public sealed class AIValidationFramework : IAIValidationFramework
    {
        private readonly ILogger<AIValidationFramework> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly Dictionary<OptimizationStrategy, ValidationRules> _validationRules;

        public AIValidationFramework(ILogger<AIValidationFramework> logger, AIOptimizationOptions options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _validationRules = InitializeValidationRules();
        }

        public async ValueTask<ValidationResult> ValidateRecommendationAsync(OptimizationRecommendation recommendation, Type requestType, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var validationErrors = new List<string>();
            var validationWarnings = new List<string>();

            // 1. Validate confidence score
            if (recommendation.ConfidenceScore < _options.MinConfidenceScore)
            {
                validationErrors.Add($"Confidence score {recommendation.ConfidenceScore:P} is below minimum threshold {_options.MinConfidenceScore:P}");
            }

            // 2. Validate risk level
            if (recommendation.Risk > _options.MaxAutomaticOptimizationRisk && _options.EnableAutomaticOptimization)
            {
                validationWarnings.Add($"Risk level {recommendation.Risk} exceeds maximum automatic optimization risk {_options.MaxAutomaticOptimizationRisk}");
            }

            // 3. Validate strategy-specific rules
            if (_validationRules.TryGetValue(recommendation.Strategy, out var rules))
            {
                var strategyValidation = await ValidateStrategyRules(recommendation, requestType, rules, cancellationToken);
                validationErrors.AddRange(strategyValidation.Errors);
                validationWarnings.AddRange(strategyValidation.Warnings);
            }

            // 4. Validate estimated improvement
            if (recommendation.EstimatedImprovement <= TimeSpan.Zero)
            {
                validationWarnings.Add("No performance improvement estimated for this optimization");
            }

            // 5. Validate parameters
            var parameterValidation = ValidateParameters(recommendation.Parameters);
            validationErrors.AddRange(parameterValidation.Errors);
            validationWarnings.AddRange(parameterValidation.Warnings);

            var isValid = validationErrors.Count == 0;
            var severity = validationErrors.Count > 0 ? ValidationSeverity.Error :
                          validationWarnings.Count > 0 ? ValidationSeverity.Warning :
                          ValidationSeverity.Success;

            _logger.LogDebug("Recommendation validation for {Strategy} completed. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                recommendation.Strategy, isValid, validationErrors.Count, validationWarnings.Count);

            await Task.CompletedTask;
            return new ValidationResult
            {
                IsValid = isValid,
                Severity = severity,
                Errors = validationErrors.ToArray(),
                Warnings = validationWarnings.ToArray(),
                ValidationTime = DateTime.UtcNow,
                ValidatedStrategy = recommendation.Strategy
            };
        }

        public async ValueTask<ModelValidationResult> ValidateModelPerformanceAsync(AIModelStatistics statistics, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var issues = new List<ModelValidationIssue>();

            // 1. Validate accuracy
            if (statistics.AccuracyScore < 0.7)
            {
                issues.Add(new ModelValidationIssue
                {
                    Type = ModelIssueType.LowAccuracy,
                    Severity = statistics.AccuracyScore < 0.5 ? ValidationSeverity.Error : ValidationSeverity.Warning,
                    Description = $"Model accuracy {statistics.AccuracyScore:P} is below acceptable threshold",
                    RecommendedAction = "Consider retraining the model with more data or adjusting parameters"
                });
            }

            // 2. Validate prediction consistency
            if (statistics.F1Score < 0.6)
            {
                issues.Add(new ModelValidationIssue
                {
                    Type = ModelIssueType.InconsistentPredictions,
                    Severity = ValidationSeverity.Warning,
                    Description = $"F1 score {statistics.F1Score:P} indicates inconsistent predictions",
                    RecommendedAction = "Review prediction logic and validation criteria"
                });
            }

            // 3. Validate training data sufficiency
            if (statistics.TrainingDataPoints < 100)
            {
                issues.Add(new ModelValidationIssue
                {
                    Type = ModelIssueType.InsufficientData,
                    Severity = ValidationSeverity.Warning,
                    Description = $"Only {statistics.TrainingDataPoints} training data points available",
                    RecommendedAction = "Collect more training data to improve model reliability"
                });
            }

            // 4. Validate model freshness
            var modelAge = DateTime.UtcNow - statistics.LastRetraining;
            if (modelAge > TimeSpan.FromDays(7))
            {
                issues.Add(new ModelValidationIssue
                {
                    Type = ModelIssueType.StaleModel,
                    Severity = ValidationSeverity.Warning,
                    Description = $"Model was last retrained {modelAge.Days} days ago",
                    RecommendedAction = "Consider retraining the model with recent data"
                });
            }

            // 5. Validate prediction performance
            if (statistics.AveragePredictionTime > TimeSpan.FromMilliseconds(100))
            {
                issues.Add(new ModelValidationIssue
                {
                    Type = ModelIssueType.SlowPredictions,
                    Severity = ValidationSeverity.Warning,
                    Description = $"Average prediction time {statistics.AveragePredictionTime.TotalMilliseconds:F1}ms is high",
                    RecommendedAction = "Optimize model complexity or infrastructure"
                });
            }

            var overallScore = CalculateOverallModelScore(statistics, issues);
            var isHealthy = issues.All(i => i.Severity != ValidationSeverity.Error);

            _logger.LogInformation("Model validation completed. Healthy: {IsHealthy}, Score: {Score:F2}, Issues: {IssueCount}",
                isHealthy, overallScore, issues.Count);

            await Task.CompletedTask;
            return new ModelValidationResult
            {
                IsHealthy = isHealthy,
                OverallScore = overallScore,
                Issues = issues.ToArray(),
                ValidationTime = DateTime.UtcNow,
                ModelStatistics = statistics
            };
        }

        public async ValueTask<SystemValidationResult> ValidateSystemHealthAsync(SystemPerformanceInsights insights, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (insights == null)
                throw new ArgumentNullException(nameof(insights));
            
            var systemIssues = new List<SystemValidationIssue>();

            // 1. Validate overall health score
            if (insights.HealthScore.Overall < 0.7)
            {
                systemIssues.Add(new SystemValidationIssue
                {
                    Component = "Overall System",
                    Severity = insights.HealthScore.Overall <= 0.5 ? ValidationSeverity.Error : ValidationSeverity.Warning,
                    Description = $"Overall health score {insights.HealthScore.Overall:F2} is below acceptable threshold",
                    Impact = CalculateHealthImpact(insights.HealthScore.Overall),
                    RecommendedActions = new[] { "Review performance bottlenecks", "Apply recommended optimizations", "Monitor system resources" }
                });
            }

            // 2. Validate performance grade - only flag F grade (failing performance)
            if (insights.PerformanceGrade == 'F')
            {
                systemIssues.Add(new SystemValidationIssue
                {
                    Component = "Performance",
                    Severity = ValidationSeverity.Error,
                    Description = $"Performance grade {insights.PerformanceGrade} indicates significant issues",
                    Impact = "High",
                    RecommendedActions = new[] { "Apply performance optimizations", "Review bottlenecks", "Scale resources" }
                });
            }

            // 3. Validate critical bottlenecks
            foreach (var bottleneck in insights.Bottlenecks.Where(b => b.Severity == BottleneckSeverity.Critical))
            {
                systemIssues.Add(new SystemValidationIssue
                {
                    Component = bottleneck.Component,
                    Severity = ValidationSeverity.Error,
                    Description = bottleneck.Description,
                    Impact = "Critical",
                    RecommendedActions = bottleneck.RecommendedActions.ToArray()
                });
            }

            // 4. Validate reliability metrics
            if (insights.HealthScore.Reliability < 0.9)
            {
                systemIssues.Add(new SystemValidationIssue
                {
                    Component = "Reliability",
                    Severity = ValidationSeverity.Warning,
                    Description = $"Reliability score {insights.HealthScore.Reliability:F2} indicates potential stability issues",
                    Impact = "Medium",
                    RecommendedActions = new[] { "Implement circuit breakers", "Add retry logic", "Monitor error rates" }
                });
            }

            // 5. Validate prediction confidence
            if (insights.Predictions.PredictionConfidence < 0.7)
            {
                systemIssues.Add(new SystemValidationIssue
                {
                    Component = "Predictive Analytics",
                    Severity = ValidationSeverity.Warning,
                    Description = $"Prediction confidence {insights.Predictions.PredictionConfidence:P} is low",
                    Impact = "Low",
                    RecommendedActions = new[] { "Collect more historical data", "Improve model training", "Validate prediction algorithms" }
                });
            }

            var isStable = !systemIssues.Any(i => i.Severity == ValidationSeverity.Error);
            var stabilityScore = CalculateStabilityScore(insights, systemIssues);

            _logger.LogInformation("System validation completed. Stable: {IsStable}, Score: {Score:F2}, Issues: {IssueCount}",
                isStable, stabilityScore, systemIssues.Count);

            await Task.CompletedTask;
            return new SystemValidationResult
            {
                IsStable = isStable,
                StabilityScore = stabilityScore,
                Issues = systemIssues.ToArray(),
                ValidationTime = DateTime.UtcNow,
                SystemInsights = insights
            };
        }

        public async ValueTask<OptimizationValidationResult> ValidateOptimizationResultsAsync(
            OptimizationStrategy[] appliedStrategies,
            RequestExecutionMetrics beforeMetrics,
            RequestExecutionMetrics afterMetrics,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (appliedStrategies == null)
                throw new ArgumentNullException(nameof(appliedStrategies));
            if (beforeMetrics == null)
                throw new ArgumentNullException(nameof(beforeMetrics));
            if (afterMetrics == null)
                throw new ArgumentNullException(nameof(afterMetrics));
            
            var results = new List<ValidationOptimizationResult>();

            foreach (var strategy in appliedStrategies)
            {
                var result = ValidateStrategyResult(strategy, beforeMetrics, afterMetrics);
                results.Add(result);
            }

            var overallImprovement = appliedStrategies.Length > 0 ? CalculateOverallImprovement(beforeMetrics, afterMetrics) : 0;
            var wasSuccessful = appliedStrategies.Length > 0 && overallImprovement > 0;

            _logger.LogInformation("Optimization validation completed. Successful: {WasSuccessful}, Improvement: {Improvement:P}, Strategies: {StrategyCount}",
                wasSuccessful, overallImprovement, appliedStrategies.Length);

            await Task.CompletedTask;
            return new OptimizationValidationResult
            {
                WasSuccessful = wasSuccessful,
                OverallImprovement = overallImprovement,
                StrategyResults = results.ToArray(),
                ValidationTime = DateTime.UtcNow,
                BeforeMetrics = beforeMetrics,
                AfterMetrics = afterMetrics
            };
        }

        private Dictionary<OptimizationStrategy, ValidationRules> InitializeValidationRules()
        {
            return new Dictionary<OptimizationStrategy, ValidationRules>
            {
                [OptimizationStrategy.EnableCaching] = new ValidationRules
                {
                    MinConfidence = 0.7,
                    MaxRisk = RiskLevel.Low,
                    RequiredParameters = new[] { "RequestType", "ExpectedHitRate" },
                    CustomValidation = ValidateCachingStrategy
                },
                [OptimizationStrategy.BatchProcessing] = new ValidationRules
                {
                    MinConfidence = 0.8,
                    MaxRisk = RiskLevel.Medium,
                    RequiredParameters = new[] { "RequestType", "OptimalBatchSize" },
                    CustomValidation = ValidateBatchingStrategy
                },
                [OptimizationStrategy.MemoryPooling] = new ValidationRules
                {
                    MinConfidence = 0.75,
                    MaxRisk = RiskLevel.Low,
                    RequiredParameters = new[] { "RequestType", "MemoryThreshold" },
                    CustomValidation = ValidateMemoryPoolingStrategy
                }
            };
        }

        private async ValueTask<StrategyValidationResult> ValidateStrategyRules(OptimizationRecommendation recommendation, Type requestType, ValidationRules rules, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var errors = new List<string>();
            var warnings = new List<string>();

            // Validate confidence
            if (recommendation.ConfidenceScore < rules.MinConfidence)
            {
                errors.Add($"Strategy confidence {recommendation.ConfidenceScore:P} below required {rules.MinConfidence:P}");
            }

            // Validate risk
            if (recommendation.Risk > rules.MaxRisk)
            {
                warnings.Add($"Strategy risk {recommendation.Risk} exceeds recommended {rules.MaxRisk}");
            }

            // Validate required parameters
            if (recommendation.Parameters != null)
            {
                foreach (var param in rules.RequiredParameters)
                {
                    if (!recommendation.Parameters.ContainsKey(param))
                    {
                        errors.Add($"Required parameter '{param}' is missing");
                    }
                }
            }

            // Custom validation
            if (rules.CustomValidation != null)
            {
                var customResult = await rules.CustomValidation(recommendation, requestType, cancellationToken);
                errors.AddRange(customResult.Errors);
                warnings.AddRange(customResult.Warnings);
            }

            return new StrategyValidationResult { Errors = errors, Warnings = warnings };
        }

        private async ValueTask<StrategyValidationResult> ValidateCachingStrategy(OptimizationRecommendation recommendation, Type requestType, CancellationToken cancellationToken)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (recommendation?.Parameters != null &&
                recommendation.Parameters.TryGetValue("ExpectedHitRate", out var hitRateObj) &&
                hitRateObj is double hitRate && hitRate < 0.3)
            {
                warnings.Add($"Expected cache hit rate {hitRate:P} is low - caching may not be effective");
            }

            // Check if request type is suitable for caching
            if (requestType?.Name?.Contains("Command") == true)
            {
                warnings.Add("Commands are typically not suitable for caching - consider if this is a query instead");
            }

            await Task.CompletedTask;
            return new StrategyValidationResult { Errors = errors, Warnings = warnings };
        }

        private async ValueTask<StrategyValidationResult> ValidateBatchingStrategy(OptimizationRecommendation recommendation, Type requestType, CancellationToken cancellationToken)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (recommendation.Parameters.TryGetValue("OptimalBatchSize", out var batchSizeObj) &&
                batchSizeObj is int batchSize)
            {
                if (batchSize < 2)
                {
                    errors.Add("Batch size must be at least 2 for batching to be effective");
                }
                else if (batchSize > 100)
                {
                    warnings.Add($"Large batch size {batchSize} may cause memory pressure");
                }
            }

            await Task.CompletedTask;
            return new StrategyValidationResult { Errors = errors, Warnings = warnings };
        }

        private async ValueTask<StrategyValidationResult> ValidateMemoryPoolingStrategy(OptimizationRecommendation recommendation, Type requestType, CancellationToken cancellationToken)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (recommendation.Parameters.TryGetValue("MemoryThreshold", out var thresholdObj) &&
                thresholdObj is long threshold && threshold < 1024)
            {
                warnings.Add($"Memory threshold {threshold} bytes is very low - pooling may not provide benefits");
            }

            await Task.CompletedTask;
            return new StrategyValidationResult { Errors = errors, Warnings = warnings };
        }

        private ValidationResult ValidateParameters(Dictionary<string, object> parameters)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (parameters == null)
            {
                warnings.Add("No parameters provided for optimization");
                return new ValidationResult
                {
                    IsValid = errors.Count == 0,
                    Errors = errors.ToArray(),
                    Warnings = warnings.ToArray()
                };
            }

            if (parameters.Count == 0)
            {
                warnings.Add("No parameters provided for optimization");
            }

            foreach (var param in parameters)
            {
                if (param.Value == null)
                {
                    errors.Add($"Parameter '{param.Key}' has null value");
                }
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToArray(),
                Warnings = warnings.ToArray()
            };
        }

        private double CalculateOverallModelScore(AIModelStatistics statistics, List<ModelValidationIssue> issues)
        {
            var baseScore = (statistics.AccuracyScore + statistics.F1Score + statistics.ModelConfidence) / 3;
            var penaltyPerError = 0.1;
            var penaltyPerWarning = 0.05;

            var penalty = issues.Sum(i => i.Severity == ValidationSeverity.Error ? penaltyPerError : penaltyPerWarning);
            return Math.Max(0, baseScore - penalty);
        }

        private string CalculateHealthImpact(double healthScore)
        {
            return healthScore switch
            {
                < 0.3 => "Critical",
                < 0.5 => "High",
                < 0.7 => "Medium",
                _ => "Low"
            };
        }

        private double CalculateStabilityScore(SystemPerformanceInsights insights, List<SystemValidationIssue> issues)
        {
            var baseScore = insights.HealthScore.Overall;
            var criticalIssues = issues.Count(i => i.Severity == ValidationSeverity.Error);
            var warningIssues = issues.Count(i => i.Severity == ValidationSeverity.Warning);

            var penalty = (criticalIssues * 0.25) + (warningIssues * 0.1);
            return Math.Max(0, baseScore - penalty);
        }

        private ValidationOptimizationResult ValidateStrategyResult(OptimizationStrategy strategy, RequestExecutionMetrics before, RequestExecutionMetrics after)
        {
            var timeImprovement = before.AverageExecutionTime - after.AverageExecutionTime;
            var successRateImprovement = after.SuccessRate - before.SuccessRate;
            var memoryImprovement = before.MemoryUsage > 0 ? (double)(before.MemoryUsage - after.MemoryUsage) / before.MemoryUsage : 0;
            
            // Consider multiple factors for success
            var wasSuccessful = timeImprovement.TotalMilliseconds > 0 || successRateImprovement > 0 || memoryImprovement > 0;
            
            // Calculate performance gain - weight time improvement more heavily
            var timeGain = before.AverageExecutionTime.TotalMilliseconds > 0 ? 
                Math.Max(0, timeImprovement.TotalMilliseconds / before.AverageExecutionTime.TotalMilliseconds) : 0;
                
            var hasOtherImprovements = successRateImprovement != 0 || memoryImprovement != 0;
            var performanceGain = !hasOtherImprovements && timeGain != 0 ? timeGain : 
                timeGain > 0.3 ? timeGain : (timeGain * 3 + Math.Max(0, successRateImprovement) + Math.Max(0, memoryImprovement)) / 5;

            return new ValidationOptimizationResult
            {
                Strategy = strategy,
                WasSuccessful = wasSuccessful,
                ActualImprovement = timeImprovement,
                PerformanceGain = performanceGain,
                ValidationTime = DateTime.UtcNow
            };
        }

        private double CalculateOverallImprovement(RequestExecutionMetrics before, RequestExecutionMetrics after)
        {
            var timeImprovement = before.AverageExecutionTime.TotalMilliseconds > 0 ? 
                (before.AverageExecutionTime.TotalMilliseconds - after.AverageExecutionTime.TotalMilliseconds) / before.AverageExecutionTime.TotalMilliseconds : 0;
            var successRateImprovement = after.SuccessRate - before.SuccessRate;
            var memoryImprovement = before.MemoryAllocated > 0 ? 
                (double)(before.MemoryAllocated - after.MemoryAllocated) / before.MemoryAllocated : 0;
            var databaseImprovement = before.DatabaseCalls > 0 ? 
                (double)(before.DatabaseCalls - after.DatabaseCalls) / before.DatabaseCalls : 0;
            var apiImprovement = before.ExternalApiCalls > 0 ? 
                (double)(before.ExternalApiCalls - after.ExternalApiCalls) / before.ExternalApiCalls : 0;

            // Calculate weighted average, but if only time improvement exists, return it directly
            var hasOtherImprovements = successRateImprovement != 0 || memoryImprovement != 0 || databaseImprovement != 0 || apiImprovement != 0;
            
            if (!hasOtherImprovements && timeImprovement != 0)
                return timeImprovement;
                
            // Weight time improvement more heavily as it's often the most critical factor
            return (timeImprovement * 2 + successRateImprovement + memoryImprovement + databaseImprovement + apiImprovement) / 6;
        }
    }
}