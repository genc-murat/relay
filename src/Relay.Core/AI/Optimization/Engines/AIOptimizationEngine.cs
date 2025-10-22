using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.AI.Analysis.TimeSeries;
using Relay.Core.AI.Optimization.Data;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI;

/// <summary>
/// Simplified AI Optimization Engine - refactored from monolithic structure
/// </summary>
public sealed class AIOptimizationEngine : IAIOptimizationEngine, IDisposable
{
    private readonly ILogger<AIOptimizationEngine> _logger;
    private readonly AIOptimizationOptions _options;
    private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
    private readonly ConcurrentDictionary<Type, CachingAnalysisData> _cachingAnalytics;
    private readonly ConcurrentDictionary<Type, OptimizationStrategy[]> _lastPredictions;

    // Modular services
    private readonly PatternAnalysisService _patternAnalysisService;
    private readonly CachingAnalysisService _cachingAnalysisService;
    private readonly ResourceOptimizationService _resourceOptimizationService;
    private readonly MachineLearningEnhancementService _machineLearningEnhancementService;
    private readonly RiskAssessmentService _riskAssessmentService;
    private readonly SystemMetricsService _systemMetricsService;
    private readonly SystemMetricsCalculator _systemMetricsCalculator;
    private readonly PredictiveAnalysisService _predictiveAnalysisService;
    private readonly ModelStatisticsService _modelStatisticsService;

    // Timers and storage (placeholder implementations)
    private readonly Timer _modelUpdateTimer;
    private readonly Timer _metricsCollectionTimer;
    private readonly TimeSeriesDatabase _timeSeriesDb;

    private volatile bool _learningEnabled = true;
    private volatile bool _disposed = false;

    public AIOptimizationEngine(
        ILogger<AIOptimizationEngine> logger,
        IOptions<AIOptimizationOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
        _cachingAnalytics = new ConcurrentDictionary<Type, CachingAnalysisData>();
        _lastPredictions = new ConcurrentDictionary<Type, OptimizationStrategy[]>();

        // Initialize services with appropriate loggers
        _patternAnalysisService = new PatternAnalysisService(_logger);
        _cachingAnalysisService = new CachingAnalysisService(_logger);
        _resourceOptimizationService = new ResourceOptimizationService(_logger);
        _machineLearningEnhancementService = new MachineLearningEnhancementService(_logger);
        _riskAssessmentService = new RiskAssessmentService(_logger);
        _systemMetricsService = new SystemMetricsService(_logger);
        var calculatorLogger = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger<SystemMetricsCalculator>();
        _systemMetricsCalculator = new SystemMetricsCalculator(calculatorLogger, _requestAnalytics);
        _predictiveAnalysisService = new PredictiveAnalysisService(_logger);
        _modelStatisticsService = new ModelStatisticsService(_logger);

        // Initialize timers and storage
        var tsLogger = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger<Relay.Core.AI.Analysis.TimeSeries.TimeSeriesDatabase>();
        _timeSeriesDb = TimeSeriesDatabase.Create(tsLogger, 1000);
        _modelUpdateTimer = new Timer(UpdateModelCallback, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        _metricsCollectionTimer = new Timer(CollectMetricsCallback, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

        _logger.LogInformation("AI Optimization Engine initialized with modular services");
    }

    public async ValueTask<OptimizationRecommendation> AnalyzeRequestAsync<TRequest>(
        TRequest request,
        RequestExecutionMetrics executionMetrics,
        CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(AIOptimizationEngine));
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (executionMetrics == null) throw new ArgumentNullException(nameof(executionMetrics));

        var requestType = typeof(TRequest);
        var analysisData = _requestAnalytics.GetOrAdd(requestType, _ => new RequestAnalysisData());

        // Update analysis data
        analysisData.AddMetrics(executionMetrics);

        // Use pattern analysis service
        var recommendation = await _patternAnalysisService.AnalyzePatternsAsync(
            requestType, analysisData, executionMetrics, cancellationToken);

        // Record the prediction in model statistics
        _modelStatisticsService.RecordPrediction(requestType);

        // Store the predicted strategies for accuracy calculation
        _lastPredictions[requestType] = new[] { recommendation.Strategy };

        _logger.LogDebug("Generated optimization recommendation for {RequestType}: {Strategy} (Confidence: {Confidence:P})",
            requestType.Name, recommendation.Strategy, recommendation.ConfidenceScore);

        return recommendation;
    }

    public async ValueTask<int> PredictOptimalBatchSizeAsync(
        Type requestType,
        SystemLoadMetrics currentLoad,
        CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(AIOptimizationEngine));
        if (requestType == null) throw new ArgumentNullException(nameof(requestType));
        if (currentLoad == null) throw new ArgumentNullException(nameof(currentLoad));

        var analysisData = _requestAnalytics.GetOrAdd(requestType, _ => new RequestAnalysisData());

        // AI algorithm for optimal batch size prediction
        var baseSize = _options.DefaultBatchSize;
        var systemLoadFactor = 1.0 - currentLoad.CpuUtilization;
        var memoryFactor = 1.0 - currentLoad.MemoryUtilization;

        // Historical performance analysis
        var avgExecutionTime = analysisData.AverageExecutionTime.TotalMilliseconds;
        var executionVariance = analysisData.CalculateExecutionVariance();

        // ML-based prediction (simplified heuristic model)
        var predictedOptimalSize = (int)(baseSize * systemLoadFactor * memoryFactor);

        // Adjust based on request type characteristics
        if (avgExecutionTime > 1000) // Long-running requests
            predictedOptimalSize = Math.Max(1, predictedOptimalSize / 2);
        else if (avgExecutionTime < 50) // Fast requests
            predictedOptimalSize = Math.Min(100, predictedOptimalSize * 2);

        // Consider system stability
        if (executionVariance > 0.5) // High variance = lower batch size
            predictedOptimalSize = Math.Max(1, (int)(predictedOptimalSize * 0.7));

        var optimalSize = Math.Max(1, Math.Min(_options.MaxBatchSize, predictedOptimalSize));

        _logger.LogDebug("Predicted optimal batch size for {RequestType}: {BatchSize} (Load: CPU={CpuLoad:P}, Memory={MemoryLoad:P})",
            requestType.Name, optimalSize, currentLoad.CpuUtilization, currentLoad.MemoryUtilization);

        await Task.CompletedTask; // For async pattern consistency
        return optimalSize;
    }

    public async ValueTask<CachingRecommendation> ShouldCacheAsync(
        Type requestType,
        AccessPattern[] accessPatterns,
        CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(AIOptimizationEngine));
        if (requestType == null) throw new ArgumentNullException(nameof(requestType));
        if (accessPatterns == null) throw new ArgumentNullException(nameof(accessPatterns));

        var analysisData = _cachingAnalytics.GetOrAdd(requestType, _ => new CachingAnalysisData());
        analysisData.AddAccessPatterns(accessPatterns);

        // Use caching analysis service
        var recommendation = _cachingAnalysisService.AnalyzeCachingPatterns(requestType, analysisData, accessPatterns);

        _logger.LogDebug("Caching recommendation for {RequestType}: {ShouldCache} (Hit Rate: {ExpectedHitRate:P}, TTL: {Ttl})",
            requestType.Name, recommendation.ShouldCache, recommendation.ExpectedHitRate, recommendation.RecommendedTtl);

        await Task.CompletedTask;
        return recommendation;
    }

    public async ValueTask LearnFromExecutionAsync(
        Type requestType,
        OptimizationStrategy[] appliedOptimizations,
        RequestExecutionMetrics actualMetrics,
        CancellationToken cancellationToken = default)
    {
        if (_disposed || !_learningEnabled) return;
        if (requestType == null) throw new ArgumentNullException(nameof(requestType));
        if (appliedOptimizations == null) throw new ArgumentNullException(nameof(appliedOptimizations));
        if (actualMetrics == null) throw new ArgumentNullException(nameof(actualMetrics));

        // Check if the applied optimizations match the predicted ones
        var predictedStrategies = _lastPredictions.GetValueOrDefault(requestType, Array.Empty<OptimizationStrategy>());
        var strategiesMatch = predictedStrategies.Length == appliedOptimizations.Length &&
                              predictedStrategies.All(s => appliedOptimizations.Contains(s));

        // Use model statistics service to learn from results
        _modelStatisticsService.UpdateModelAccuracy(requestType, appliedOptimizations, actualMetrics, strategiesMatch);

        _logger.LogDebug("Learned from execution of {RequestType} with {StrategyCount} optimizations",
            requestType.Name, appliedOptimizations.Length);

        await Task.CompletedTask;
    }

    public async ValueTask<SystemPerformanceInsights> GetSystemInsightsAsync(
        TimeSpan timeWindow,
        CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(AIOptimizationEngine));

        cancellationToken.ThrowIfCancellationRequested();

        var keyMetrics = CollectKeyMetrics();
        var insights = new SystemPerformanceInsights
        {
            AnalysisTime = DateTime.UtcNow,
            AnalysisPeriod = timeWindow,
            Bottlenecks = IdentifyBottlenecks(timeWindow),
            Opportunities = IdentifyOptimizationOpportunities(timeWindow),
            HealthScore = _systemMetricsService.CalculateSystemHealthScore(),
            Predictions = _predictiveAnalysisService.GeneratePredictiveAnalysis(),
            PerformanceGrade = CalculatePerformanceGrade(),
            KeyMetrics = keyMetrics,
            SeasonalPatterns = DetectSeasonalPatterns(keyMetrics),
            ResourceOptimization = _resourceOptimizationService.AnalyzeResourceUsage(keyMetrics, new Dictionary<string, double>()) // Using current metrics as historical for simplicity
        };

        _logger.LogInformation("Generated system performance insights: Grade {Grade}, Health Score {HealthScore:F2}",
            insights.PerformanceGrade, insights.HealthScore.Overall);

        await Task.CompletedTask;
        return insights;
    }

    public void SetLearningMode(bool enabled)
    {
        _learningEnabled = enabled;
        _logger.LogInformation("AI learning mode {Status}", enabled ? "enabled" : "disabled");
    }

    public AIModelStatistics GetModelStatistics()
    {
        return _modelStatisticsService.GetModelStatistics();
    }

    internal List<LoadTransition> GetLoadTransitions()
    {
        return _predictiveAnalysisService.GetLoadTransitions();
    }

    public LoadPatternData GetLoadPatternAnalysis()
    {
        // Aggregate load pattern data from system metrics and predictive analysis services
        var systemLoadData = _systemMetricsService.AnalyzeLoadPatterns();
        var predictiveLoadData = _predictiveAnalysisService.AnalyzeLoadPatterns();

        // Combine the data - use the higher load level and merge predictions
        var combinedLevel = (LoadLevel)Math.Max((int)systemLoadData.Level, (int)predictiveLoadData.Level);
        var combinedPredictions = new List<PredictionResult>();
        combinedPredictions.AddRange(systemLoadData.Predictions);
        combinedPredictions.AddRange(predictiveLoadData.Predictions);

        // Calculate weighted averages for success rate and improvement
        var totalPredictions = systemLoadData.TotalPredictions + predictiveLoadData.TotalPredictions;
        var weightedSuccessRate = totalPredictions > 0
            ? (systemLoadData.SuccessRate * systemLoadData.TotalPredictions +
               predictiveLoadData.SuccessRate * predictiveLoadData.TotalPredictions) / totalPredictions
            : 0.0;

        var weightedImprovement = totalPredictions > 0
            ? (systemLoadData.AverageImprovement * systemLoadData.TotalPredictions +
               predictiveLoadData.AverageImprovement * predictiveLoadData.TotalPredictions) / totalPredictions
            : 0.0;

        // Merge strategy effectiveness dictionaries
        var combinedStrategyEffectiveness = new Dictionary<string, double>();
        foreach (var kvp in systemLoadData.StrategyEffectiveness)
        {
            combinedStrategyEffectiveness[kvp.Key] = kvp.Value;
        }
        foreach (var kvp in predictiveLoadData.StrategyEffectiveness)
        {
            if (combinedStrategyEffectiveness.ContainsKey(kvp.Key))
            {
                // Average the effectiveness scores
                combinedStrategyEffectiveness[kvp.Key] =
                    (combinedStrategyEffectiveness[kvp.Key] + kvp.Value) / 2.0;
            }
            else
            {
                combinedStrategyEffectiveness[kvp.Key] = kvp.Value;
            }
        }

        return new LoadPatternData
        {
            Level = combinedLevel,
            Predictions = combinedPredictions,
            SuccessRate = weightedSuccessRate,
            AverageImprovement = weightedImprovement,
            TotalPredictions = totalPredictions,
            StrategyEffectiveness = combinedStrategyEffectiveness
        };
    }

    // Simplified private methods that delegate to services
    private List<PerformanceBottleneck> IdentifyBottlenecks(TimeSpan timeWindow)
    {
        // This would use a dedicated bottleneck analysis service
        // For now, return empty list as placeholder
        return new List<PerformanceBottleneck>();
    }

    private List<OptimizationOpportunity> IdentifyOptimizationOpportunities(TimeSpan timeWindow)
    {
        // This would use a dedicated opportunity analysis service
        // For now, return empty list as placeholder
        return new List<OptimizationOpportunity>();
    }

    private char CalculatePerformanceGrade()
    {
        var healthScore = _systemMetricsService.CalculateSystemHealthScore();
        return healthScore.Overall switch
        {
            > 0.9 => 'A',
            > 0.8 => 'B',
            > 0.7 => 'C',
            > 0.6 => 'D',
            _ => 'F'
        };
    }

    private Dictionary<string, double> CollectKeyMetrics()
    {
        return _systemMetricsService.CollectSystemMetrics();
    }

    private void UpdateModelCallback(object? state)
    {
        if (_disposed || !_learningEnabled) return;

        try
        {
            // Periodic model updates and retraining
            _logger.LogDebug("Updating AI model with latest data...");

            // This would coordinate updates across all services
            // For now, simplified implementation

            _logger.LogInformation("AI model update completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI model update");
        }
    }

    private void CollectMetricsCallback(object? state)
    {
        if (_disposed) return;

        try
        {
            // Collect comprehensive system metrics for AI analysis
            var metrics = _systemMetricsService.CollectSystemMetrics();

            // Store metrics in time-series database for ML.NET forecasting
            var timestamp = DateTime.UtcNow;
            var throughput = metrics.GetValueOrDefault("ThroughputPerSecond", 0.0);

            _timeSeriesDb.StoreMetric("ThroughputPerSecond", throughput, timestamp);
            _timeSeriesDb.StoreMetric("MemoryUtilization", metrics.GetValueOrDefault("MemoryUtilization", 0.0), timestamp);
            _timeSeriesDb.StoreMetric("ErrorRate", metrics.GetValueOrDefault("ErrorRate", 0.0), timestamp);

            // This would trigger analysis in various services
            // For now, simplified

            _logger.LogDebug("Collected and analyzed AI metrics");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error collecting AI metrics");
        }
    }

    /// <summary>
    /// Classifies the seasonal type based on the period in hours
    /// </summary>
    private string ClassifySeasonalType(int periodHours)
    {
        if (periodHours <= 8) return "Intraday";
        if (periodHours <= 24) return "Daily";
        if (periodHours <= 48) return "Semi-weekly";
        if (periodHours <= 168) return "Weekly";
        if (periodHours <= 336) return "Bi-weekly";
        return "Monthly"; // >= 337
    }

    /// <summary>
    /// Detects seasonal patterns in time series data
    /// </summary>
    private List<SeasonalPattern> DetectSeasonalPatterns(Dictionary<string, double> metrics)
    {
        var patterns = new List<SeasonalPattern>();

        // Get data from time series database
        var dataPoints = _timeSeriesDb.GetHistory("ThroughputPerSecond", TimeSpan.FromHours(24));
        var data = dataPoints.Select(dp => (double)dp.Value).ToList();

        if (data.Count < 24) // Need at least 24 hours of data
            return patterns;

        // Test common periods
        var testPeriods = new[] { 8, 12, 24, 48, 168, 336 };

        foreach (var period in testPeriods)
        {
            if (data.Count >= period * 2)
            {
                // Simple autocorrelation-based pattern detection
                var correlation = CalculateAutocorrelation(data, period);
                if (correlation > 0.7) // Strong correlation indicates pattern
                {
                    patterns.Add(new SeasonalPattern
                    {
                        Period = period,
                        Strength = correlation,
                        Type = ClassifySeasonalType(period)
                    });
                }
            }
        }

        return patterns;
    }

    /// <summary>
    /// Calculates autocorrelation for a given lag
    /// </summary>
    private double CalculateAutocorrelation(List<double> data, int lag)
    {
        if (data.Count < lag * 2) return 0.0;

        var mean = data.Average();
        var variance = data.Sum(x => Math.Pow(x - mean, 2));

        if (variance == 0) return 1.0; // All values are the same

        double covariance = 0;
        for (int i = 0; i < data.Count - lag; i++)
        {
            covariance += (data[i] - mean) * (data[i + lag] - mean);
        }

        return covariance / variance;
    }

    /// <summary>
    /// Calculates regularization strength based on overfitting risk
    /// </summary>
    private double CalculateRegularizationStrength(double overfittingRisk, Dictionary<string, double> metrics)
    {
        var baseStrength = 0.01;

        if (overfittingRisk > 0.7)
            baseStrength += 0.1;
        else if (overfittingRisk > 0.5)
            baseStrength += 0.05;

        // Adjust based on model complexity
        var modelComplexity = metrics?.GetValueOrDefault("ModelComplexity", 0.5) ?? 0.5;
        if (modelComplexity > 0.8)
            baseStrength += 0.05;

        return Math.Max(0.001, Math.Min(0.5, baseStrength));
    }

    /// <summary>
    /// Calculates optimal tree count for ensemble models
    /// </summary>
    private int CalculateOptimalTreeCount(double accuracy, Dictionary<string, double> metrics)
    {
        if (metrics == null)
            return 100; // Default

        var baseCount = 100;

        // Reduce trees for high accuracy to prevent overfitting
        if (accuracy > 0.9)
            baseCount = (int)(baseCount * 0.7);
        else if (accuracy > 0.8)
            baseCount = (int)(baseCount * 0.8);

        // Adjust based on system stability
        var stability = metrics.GetValueOrDefault("SystemStability", 0.8);
        if (stability < 0.5)
            baseCount = (int)(baseCount * 0.6); // Fewer trees for unstable systems

        return Math.Max(10, Math.Min(1000, baseCount));
    }

    /// <summary>
    /// Calculates optimal epochs for training
    /// </summary>
    private int CalculateOptimalEpochs(long dataSize, Dictionary<string, double> metrics)
    {
        if (metrics == null)
            return 100; // Default

        var baseEpochs = 100;

        // More data allows more epochs
        if (dataSize > 100000)
            baseEpochs += 50;
        else if (dataSize > 10000)
            baseEpochs += 25;

        // Reduce epochs for high model complexity
        var complexity = metrics.GetValueOrDefault("ModelComplexity", 0.5);
        if (complexity > 0.8)
            baseEpochs = (int)(baseEpochs * 0.7);

        return Math.Max(10, Math.Min(1000, baseEpochs));
    }

    /// <summary>
    /// Calculates optimal leaf count for decision trees
    /// </summary>
    private int CalculateOptimalLeafCount(long dataSize, Dictionary<string, double> metrics)
    {
        if (metrics == null)
            return 31; // Default

        var baseLeaves = 31;

        // More data allows more leaves
        if (dataSize > 10000)
            baseLeaves += 10;

        // Reduce leaves for high accuracy to prevent overfitting
        var accuracy = metrics.GetValueOrDefault("Accuracy", 0.8);
        if (accuracy > 0.9)
            baseLeaves = (int)(baseLeaves * 0.8);

        return Math.Max(2, Math.Min(100, baseLeaves));
    }

    /// <summary>
    /// Calculates minimum examples per leaf
    /// </summary>
    private int CalculateMinExamplesPerLeaf(double accuracy, Dictionary<string, double> metrics)
    {
        var baseMin = 1;

        // Higher accuracy allows fewer examples per leaf
        if (accuracy > 0.9)
            baseMin = 1;
        else if (accuracy > 0.8)
            baseMin = 2;
        else if (accuracy > 0.7)
            baseMin = 5;
        else
            baseMin = 10;

        return baseMin;
    }

    /// <summary>
    /// Analyzes caching patterns and returns detailed caching analysis result
    /// </summary>
    private CachingAnalysisResult AnalyzeCachingPatterns(PatternAnalysisContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var analysisData = context.AnalysisData;
        var metrics = context.CurrentMetrics;

        // Calculate repeat rate from analysis data
        var repeatRate = analysisData.TotalExecutions > 0 ?
            (double)analysisData.RepeatRequestCount / analysisData.TotalExecutions : 0.0;

        // Determine if caching should be enabled
        var shouldCache = repeatRate > 0.2 || metrics.AverageExecutionTime.TotalMilliseconds > 500;

        // Calculate expected hit rate based on repeat rate
        var expectedHitRate = repeatRate <= 0.1 ? 0.0 : Math.Min(repeatRate * 1.1, 0.95);

        // Calculate expected improvement
        var expectedImprovement = expectedHitRate * metrics.AverageExecutionTime.TotalMilliseconds * 0.7;

        // Calculate confidence based on data quality
        var baseConfidence = 0.5 + (repeatRate * 1.5);
        var confidence = repeatRate <= 0.1 ? 0.0 : Math.Min(baseConfidence, 0.9);

        // Generate reasoning
        var reasoning = string.Empty;
        if (analysisData.TotalExecutions == 0)
        {
            reasoning = string.Empty;
        }
        else if (shouldCache)
        {
            reasoning = $"High repeat rate ({repeatRate:P}) and execution time ({metrics.AverageExecutionTime.TotalMilliseconds:F0}ms) suggest caching benefits with expected {expectedImprovement:F0}ms improvement.";
        }
        else
        {
            reasoning = string.Empty;
        }

        // Determine recommended strategy based on repeat rate thresholds
        var recommendedStrategy = CacheStrategy.None;
        if (shouldCache)
        {
            if (repeatRate > 0.6)
            {
                recommendedStrategy = CacheStrategy.LFU;
            }
            else
            {
                recommendedStrategy = CacheStrategy.LRU;
            }
        }

        // Calculate TTL based on repeat rate (higher repeat rate = shorter TTL)
        var recommendedTTL = TimeSpan.Zero;
        if (shouldCache)
        {
            // For repeatRate=1.0, TTL=30 minutes
            // Scale inversely with repeat rate
            var ttlMinutes = Math.Max(5, 60 - (repeatRate * 30));
            recommendedTTL = TimeSpan.FromMinutes(ttlMinutes);
        }

        return new CachingAnalysisResult
        {
            ShouldCache = shouldCache,
            ExpectedHitRate = expectedHitRate,
            ExpectedImprovement = expectedImprovement,
            Confidence = confidence,
            Reasoning = reasoning,
            RecommendedStrategy = recommendedStrategy,
            RecommendedTTL = recommendedTTL
        };
    }

    /// <summary>
    /// Calculates the adaptive exploration rate based on effectiveness and system metrics
    /// </summary>
    private double CalculateAdaptiveExplorationRate(double effectiveness, Dictionary<string, double> metrics)
    {
        try
        {
            const double baseRate = 0.1;
            var explorationRate = baseRate;

            // Adjust based on effectiveness - lower effectiveness increases exploration
            if (effectiveness < 0.5)
            {
                explorationRate += 0.2; // Increase exploration for low effectiveness
            }
            else if (effectiveness < 0.7)
            {
                explorationRate += 0.1; // Moderate increase
            }
            // High effectiveness (>= 0.7) keeps base rate

            // Adjust based on system stability - lower stability increases exploration
            if (metrics != null)
            {
                var systemStability = metrics.GetValueOrDefault("SystemStability", 0.8);
                if (systemStability < 0.5)
                {
                    explorationRate += 0.15; // Significant increase for unstable systems
                }
                else if (systemStability < 0.7)
                {
                    explorationRate += 0.05; // Moderate increase
                }
            }

            // Ensure rate stays within reasonable bounds
            return Math.Max(0.01, Math.Min(0.5, explorationRate));
        }
        catch (Exception)
        {
            // Return safe default on any exception
            return 0.1;
        }
    }

    // System metrics methods
    private double CalculateMemoryUsage() => _systemMetricsCalculator.CalculateMemoryUsage();
    private int GetActiveRequestCount() => _systemMetricsCalculator.GetActiveRequestCount();
    private int GetQueuedRequestCount() => _systemMetricsCalculator.GetQueuedRequestCount();
    private double CalculateCurrentThroughput() => _systemMetricsCalculator.CalculateCurrentThroughput();
    private double CalculateCurrentErrorRate() => _systemMetricsCalculator.CalculateCurrentErrorRate();
    private double GetDatabasePoolUtilization() => _systemMetricsCalculator.GetDatabasePoolUtilization();
    private double GetThreadPoolUtilization() => _systemMetricsCalculator.GetThreadPoolUtilization();

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _modelUpdateTimer?.Dispose();
        _metricsCollectionTimer?.Dispose();
        (_timeSeriesDb as IDisposable)?.Dispose();
    }
}