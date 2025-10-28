using Microsoft.Extensions.Logging;
using Relay.Core.AI.Optimization.Data;
using Relay.Core.AI.Optimization.Models;
using Relay.Core.AI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Relay.Core.AI.Optimization.Services
{
    /// <summary>
    /// Service for collecting and analyzing system metrics
    /// </summary>
    internal class SystemMetricsService
    {
        private readonly ILogger _logger;
        private readonly object _metricsLock = new();
        private readonly Dictionary<string, double> _latestMetrics = new();
        private DateTime _lastCollectionTime = DateTime.UtcNow;
        private Dictionary<string, double>? _testMetrics;

        // Throughput tracking
        private long _totalRequestsProcessed;
        private DateTime _lastThroughputReset = DateTime.UtcNow;
        private double _currentThroughputPerSecond;

        // CPU utilization tracking
        private DateTime _lastCpuMeasurementTime = DateTime.UtcNow;
        private TimeSpan _lastProcessorTime = TimeSpan.Zero;
        private List<double> _cpuUtilizationHistory = new();
        private const int MaxCpuHistorySize = 60; // Track last 60 measurements
        private readonly object _cpuLock = new();

        // Prediction tracking
        private readonly List<PredictionOutcome> _predictionOutcomes = new();
        private readonly int _maxPredictionOutcomes = 100;
        private readonly object _predictionLock = new();

        // Error tracking
        private long _totalErrors;
        private long _totalExceptions;
        private DateTime _lastErrorReset = DateTime.UtcNow;

        // Network metrics tracking
        private readonly Queue<(DateTime timestamp, double latencyMs)> _networkLatencyHistory = new();
        private readonly int _maxNetworkHistorySize = 100;

        // Disk I/O tracking
        private long _lastDiskReadBytes;
        private long _lastDiskWriteBytes;
        private DateTime _lastDiskMeasurement = DateTime.UtcNow;
        private double _currentDiskReadBytesPerSecond;
        private double _currentDiskWriteBytesPerSecond;

        public SystemMetricsService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void RecordRequestProcessed()
        {
            Interlocked.Increment(ref _totalRequestsProcessed);
        }

        internal void SetTestMetrics(Dictionary<string, double> testMetrics)
        {
            _testMetrics = testMetrics;
        }

        public SystemHealthScore CalculateSystemHealthScore()
        {
            var metrics = CollectSystemMetrics();

            var performance = CalculatePerformanceScore(metrics);
            var reliability = CalculateReliabilityScore(metrics);
            var scalability = CalculateScalabilityScore(metrics);
            var security = CalculateSecurityScore(metrics);
            var maintainability = CalculateMaintainabilityScore(metrics);

            var overall = (performance + reliability + scalability + security + maintainability) / 5.0;
            var status = GetHealthStatus(overall);
            var criticalAreas = GetCriticalAreas(metrics);

            return new SystemHealthScore
            {
                Overall = overall,
                Performance = performance,
                Reliability = reliability,
                Scalability = scalability,
                Security = security,
                Maintainability = maintainability,
                Status = status,
                CriticalAreas = criticalAreas
            };
        }

        public LoadPatternData AnalyzeLoadPatterns()
        {
            var metrics = CollectSystemMetrics();
            var loadLevel = DetermineLoadLevel(metrics);
            var predictions = GenerateLoadPredictions(metrics);
            var successRate = CalculatePredictionSuccessRate();
            var averageImprovement = CalculateAverageImprovement();
            var totalPredictions = GetTotalPredictions();
            var strategyEffectiveness = CalculateStrategyEffectiveness();

            return new LoadPatternData
            {
                Level = loadLevel,
                Predictions = predictions,
                SuccessRate = successRate,
                AverageImprovement = averageImprovement,
                TotalPredictions = totalPredictions,
                StrategyEffectiveness = strategyEffectiveness
            };
        }

        private LoadLevel DetermineLoadLevel(Dictionary<string, double> metrics)
        {
            var cpuUtilization = metrics.GetValueOrDefault("CpuUtilization", 0);
            var memoryUtilization = metrics.GetValueOrDefault("MemoryUtilization", 0);
            var throughput = metrics.GetValueOrDefault("ThroughputPerSecond", 0);

            // Determine load level based on system metrics
            if (cpuUtilization > 0.9 || memoryUtilization > 0.9)
                return LoadLevel.Critical;
            else if (cpuUtilization > 0.7 || memoryUtilization > 0.7)
                return LoadLevel.High;
            else if (cpuUtilization > 0.5 || memoryUtilization > 0.5)
                return LoadLevel.Medium;
            else if (cpuUtilization > 0.2 || memoryUtilization > 0.2 || throughput > 10)
                return LoadLevel.Low;
            else
                return LoadLevel.Idle;
        }

        private List<PredictionResult> GenerateLoadPredictions(Dictionary<string, double> metrics)
        {
            var predictions = new List<PredictionResult>();

            // Generate predictions based on current metrics
            var predictedStrategies = new[] { OptimizationStrategy.EnableCaching, OptimizationStrategy.BatchProcessing };
            var improvement = TimeSpan.FromMilliseconds(metrics.GetValueOrDefault("AverageResponseTime", 100) * 0.1);

            predictions.Add(new PredictionResult
            {
                RequestType = typeof(object), // Generic prediction
                PredictedStrategies = predictedStrategies,
                ActualImprovement = improvement,
                Timestamp = DateTime.UtcNow,
                Metrics = new RequestExecutionMetrics
                {
                    AverageExecutionTime = TimeSpan.FromMilliseconds(metrics.GetValueOrDefault("AverageResponseTime", 100)),
                    ConcurrentExecutions = (int)metrics.GetValueOrDefault("ConcurrentRequests", 1),
                    MemoryUsage = (long)(metrics.GetValueOrDefault("MemoryUsageMB", 100) * 1024 * 1024),
                    DatabaseCalls = (int)metrics.GetValueOrDefault("DatabaseCalls", 0)
                }
            });

            return predictions;
        }

        private double CalculatePredictionSuccessRate()
        {
            lock (_predictionLock)
            {
                if (_predictionOutcomes.Count == 0)
                {
                    return 0.5; // Neutral when no data
                }

                // A prediction is successful if actual improvement is within 80-120% of predicted
                var successfulPredictions = _predictionOutcomes.Count(outcome =>
                {
                    if (outcome.PredictedImprovement == TimeSpan.Zero)
                        return outcome.ActualImprovement == TimeSpan.Zero;

                    var ratio = outcome.ActualImprovement.TotalMilliseconds /
                                outcome.PredictedImprovement.TotalMilliseconds;

                    return ratio >= 0.8 && ratio <= 1.2;
                });

                return (double)successfulPredictions / _predictionOutcomes.Count;
            }
        }

        private double CalculateAverageImprovement()
        {
            lock (_predictionLock)
            {
                if (_predictionOutcomes.Count == 0)
                {
                    return 0.0;
                }

                // Calculate average percentage improvement
                var improvements = _predictionOutcomes
                    .Where(outcome => outcome.BaselineExecutionTime > TimeSpan.Zero)
                    .Select(outcome =>
                    {
                        var improvement = (outcome.BaselineExecutionTime - outcome.ActualImprovement).TotalMilliseconds
                                        / outcome.BaselineExecutionTime.TotalMilliseconds;
                        return Math.Max(0, Math.Min(1, improvement));
                    })
                    .ToList();

                return improvements.Count > 0 ? improvements.Average() : 0.0;
            }
        }

        private int GetTotalPredictions()
        {
            lock (_predictionLock)
            {
                return _predictionOutcomes.Count;
            }
        }

        /// <summary>
        /// Records a prediction outcome for tracking prediction accuracy
        /// </summary>
        public void RecordPredictionOutcome(OptimizationStrategy strategy,
            TimeSpan predictedImprovement,
            TimeSpan actualImprovement,
            TimeSpan baselineExecutionTime)
        {
            lock (_predictionLock)
            {
                var outcome = new PredictionOutcome
                {
                    Timestamp = DateTime.UtcNow,
                    Strategy = strategy,
                    PredictedImprovement = predictedImprovement,
                    ActualImprovement = actualImprovement,
                    BaselineExecutionTime = baselineExecutionTime
                };

                _predictionOutcomes.Add(outcome);

                // Maintain max size
                while (_predictionOutcomes.Count > _maxPredictionOutcomes)
                {
                    _predictionOutcomes.RemoveAt(0);
                }

                _logger.LogDebug("Recorded prediction outcome: Strategy={Strategy}, Predicted={Predicted}ms, Actual={Actual}ms",
                    strategy, predictedImprovement.TotalMilliseconds, actualImprovement.TotalMilliseconds);
            }
        }

        private Dictionary<string, double> CalculateStrategyEffectiveness()
        {
            // Placeholder implementation - would calculate effectiveness of different strategies
            return new Dictionary<string, double>
            {
                ["EnableCaching"] = 0.8,
                ["BatchProcessing"] = 0.7,
                ["ParallelProcessing"] = 0.6,
                ["CircuitBreaker"] = 0.9
            };
        }

        public Dictionary<string, double> CollectSystemMetrics()
        {
            if (_testMetrics != null)
            {
                return _testMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            lock (_metricsLock)
            {
                try
                {
                    var metrics = new Dictionary<string, double>();
                    var now = DateTime.UtcNow;

                    // CPU metrics
                    metrics["CpuUtilization"] = GetCpuUtilization();
                    metrics["CpuUsagePercent"] = metrics["CpuUtilization"] * 100;

                    // Memory metrics
                    var memoryInfo = GetMemoryInfo();
                    metrics["MemoryUtilization"] = memoryInfo.utilization;
                    metrics["MemoryUsageMB"] = memoryInfo.usedMB;
                    metrics["AvailableMemoryMB"] = memoryInfo.availableMB;

                    // Throughput metrics collected from actual request processing
                    metrics["ThroughputPerSecond"] = GetThroughputPerSecond();
                    metrics["RequestsPerSecond"] = metrics["ThroughputPerSecond"];

                    // Error metrics
                    metrics["ErrorRate"] = GetErrorRate();
                    metrics["ExceptionCount"] = GetExceptionCount();

                    // Network metrics
                    metrics["NetworkLatencyMs"] = GetNetworkLatency();
                    metrics["NetworkThroughputMbps"] = GetNetworkThroughput();

                    // Disk I/O metrics
                    metrics["DiskReadBytesPerSecond"] = GetDiskReadBytesPerSecond();
                    metrics["DiskWriteBytesPerSecond"] = GetDiskWriteBytesPerSecond();

                    // System load
                    metrics["SystemLoadAverage"] = GetSystemLoadAverage();
                    metrics["ThreadCount"] = GetThreadCount();
                    metrics["HandleCount"] = GetHandleCount();

                    // Update latest metrics
                    foreach (var kvp in metrics)
                    {
                        _latestMetrics[kvp.Key] = kvp.Value;
                    }

                    _lastCollectionTime = now;

                    _logger.LogDebug("Collected {Count} system metrics", metrics.Count);

                    return metrics;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error collecting system metrics");
                    return _latestMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }
            }
        }

        private double CalculatePerformanceScore(Dictionary<string, double> metrics)
        {
            var cpuScore = 1.0 - metrics.GetValueOrDefault("CpuUtilization", 0.5);
            var memoryScore = 1.0 - metrics.GetValueOrDefault("MemoryUtilization", 0.5);
            var throughputScore = Math.Min(metrics.GetValueOrDefault("ThroughputPerSecond", 100) / 1000.0, 1.0);

            return (cpuScore + memoryScore + throughputScore) / 3.0;
        }

        private double CalculateReliabilityScore(Dictionary<string, double> metrics)
        {
            var errorRate = metrics.GetValueOrDefault("ErrorRate", 0.1);
            var reliabilityScore = 1.0 - Math.Min(errorRate, 1.0);

            // Factor in system stability
            var loadAverage = metrics.GetValueOrDefault("SystemLoadAverage", 1.0);
            var stabilityScore = Math.Max(0, 1.0 - loadAverage / 10.0);

            return (reliabilityScore + stabilityScore) / 2.0;
        }

        private double CalculateScalabilityScore(Dictionary<string, double> metrics)
        {
            var threadCount = metrics.GetValueOrDefault("ThreadCount", 50);
            var handleCount = metrics.GetValueOrDefault("HandleCount", 1000);

            // Lower thread/handle counts relative to throughput indicate better scalability
            var throughput = metrics.GetValueOrDefault("ThroughputPerSecond", 100);
            var threadEfficiency = Math.Min(throughput / Math.Max(threadCount, 1), 10.0) / 10.0;
            var handleEfficiency = Math.Min(throughput / Math.Max(handleCount / 100.0, 1), 10.0) / 10.0;

            return (threadEfficiency + handleEfficiency) / 2.0;
        }

        private double CalculateSecurityScore(Dictionary<string, double> metrics)
        {
            // Calculate security score based on multiple security dimensions
            var authenticationScore = CalculateAuthenticationSecurityScore(metrics);
            var authorizationScore = CalculateAuthorizationSecurityScore(metrics);
            var encryptionScore = CalculateEncryptionScore(metrics);
            var vulnerabilityScore = CalculateVulnerabilityScore(metrics);
            var attackPreventionScore = CalculateAttackPreventionScore(metrics);
            var complianceScore = CalculateComplianceScore(metrics);

            // Weight each component (authentication and encryption are critical)
            var weightedScore = (
                authenticationScore * 0.20 +      // 20%
                authorizationScore * 0.15 +       // 15%
                encryptionScore * 0.20 +          // 20%
                vulnerabilityScore * 0.20 +       // 20%
                attackPreventionScore * 0.15 +    // 15%
                complianceScore * 0.10            // 10%
            );

            return Math.Min(Math.Max(weightedScore, 0.0), 1.0);
        }

        private double CalculateAuthenticationSecurityScore(Dictionary<string, double> metrics)
        {
            // Score based on failed authentication attempts and security protocols
            var failedAuthAttempts = metrics.GetValueOrDefault("FailedAuthAttempts", 0);
            var mfaEnabled = metrics.GetValueOrDefault("MFAEnabled", 1); // 1 = enabled, 0 = disabled
            var secureProtocolsUsed = metrics.GetValueOrDefault("SecureProtocolsUsed", 1);

            // Lower failed attempts = higher score
            var failureScore = Math.Max(0, 1.0 - (failedAuthAttempts / 100.0));
            var mfaScore = mfaEnabled * 0.5 + 0.5; // At least 0.5 even without MFA
            var protocolScore = secureProtocolsUsed;

            return (failureScore + mfaScore + protocolScore) / 3.0;
        }

        private double CalculateAuthorizationSecurityScore(Dictionary<string, double> metrics)
        {
            // Score based on unauthorized access attempts and privilege escalation
            var unauthorizedAttempts = metrics.GetValueOrDefault("UnauthorizedAccessAttempts", 0);
            var privilegeEscalationAttempts = metrics.GetValueOrDefault("PrivilegeEscalationAttempts", 0);
            var rbacConfigured = metrics.GetValueOrDefault("RBACConfigured", 1); // Role-Based Access Control

            // Lower unauthorized attempts = higher score
            var accessScore = Math.Max(0, 1.0 - (unauthorizedAttempts / 50.0));
            var escalationScore = Math.Max(0, 1.0 - (privilegeEscalationAttempts / 10.0));
            var rbacScore = rbacConfigured;

            return (accessScore + escalationScore + rbacScore) / 3.0;
        }

        private double CalculateEncryptionScore(Dictionary<string, double> metrics)
        {
            // Score based on data encryption status and encryption strength
            var dataEncryptionEnabled = metrics.GetValueOrDefault("DataEncryptionEnabled", 1);
            var transitEncryptionEnabled = metrics.GetValueOrDefault("TransitEncryptionEnabled", 1);
            var encryptionStrengthScore = metrics.GetValueOrDefault("EncryptionStrengthScore", 1); // 0-1 based on key strength
            var certificateValidity = metrics.GetValueOrDefault("CertificateValidityDays", 365);

            // Higher certificate validity is better (not expired)
            var certScore = Math.Min(certificateValidity / 365.0, 1.0);

            return (dataEncryptionEnabled + transitEncryptionEnabled + encryptionStrengthScore + certScore) / 4.0;
        }

        private double CalculateVulnerabilityScore(Dictionary<string, double> metrics)
        {
            // Score based on known vulnerabilities and security patches
            var knownVulnerabilities = metrics.GetValueOrDefault("KnownVulnerabilities", 0);
            var securityPatchesApplied = metrics.GetValueOrDefault("SecurityPatchesApplied", 1); // 0-1
            var dependencyAuditScore = metrics.GetValueOrDefault("DependencyAuditScore", 0.8); // 0-1
            var outdatedDependencies = metrics.GetValueOrDefault("OutdatedDependencies", 0);

            // Lower vulnerabilities and outdated dependencies = higher score
            var vulnScore = Math.Max(0, 1.0 - (knownVulnerabilities / 10.0));
            var patchScore = securityPatchesApplied;
            var outdatedScore = Math.Max(0, 1.0 - (outdatedDependencies / 20.0));

            return (vulnScore + patchScore + dependencyAuditScore + outdatedScore) / 4.0;
        }

        private double CalculateAttackPreventionScore(Dictionary<string, double> metrics)
        {
            // Score based on attack prevention measures
            var ddosProtectionEnabled = metrics.GetValueOrDefault("DDoSProtectionEnabled", 1);
            var sqlInjectionProtection = metrics.GetValueOrDefault("SQLInjectionProtectionEnabled", 1);
            var xssProtectionEnabled = metrics.GetValueOrDefault("XSSProtectionEnabled", 1);
            var rateLimitingEnabled = metrics.GetValueOrDefault("RateLimitingEnabled", 1);
            var suspiciousRequestsDetected = metrics.GetValueOrDefault("SuspiciousRequestsDetected", 0);

            // Lower suspicious requests = higher score
            var suspicionScore = Math.Max(0, 1.0 - (suspiciousRequestsDetected / 100.0));

            return (
                ddosProtectionEnabled * 0.25 +
                sqlInjectionProtection * 0.25 +
                xssProtectionEnabled * 0.25 +
                rateLimitingEnabled * 0.15 +
                suspicionScore * 0.10
            );
        }

        private double CalculateComplianceScore(Dictionary<string, double> metrics)
        {
            // Score based on compliance with standards (GDPR, PCI-DSS, etc.)
            var gdprCompliant = metrics.GetValueOrDefault("GDPRCompliant", 1);
            var pciDssCompliant = metrics.GetValueOrDefault("PCIDSSCompliant", 1);
            var hipaaCompliant = metrics.GetValueOrDefault("HIPAACompliant", 1);
            var auditLogsEnabled = metrics.GetValueOrDefault("AuditLogsEnabled", 1);
            var dataBackupEnabled = metrics.GetValueOrDefault("DataBackupEnabled", 1);

            return (
                gdprCompliant * 0.25 +
                pciDssCompliant * 0.25 +
                hipaaCompliant * 0.15 +
                auditLogsEnabled * 0.20 +
                dataBackupEnabled * 0.15
            );
        }

        private double CalculateMaintainabilityScore(Dictionary<string, double> metrics)
        {
            // Based on error rates and system complexity
            var errorRate = metrics.GetValueOrDefault("ErrorRate", 0.1);
            var maintainabilityScore = 1.0 - Math.Min(errorRate * 2, 1.0);

            // Factor in resource utilization - over-utilized systems are harder to maintain
            var cpuUtil = metrics.GetValueOrDefault("CpuUtilization", 0.5);
            var memoryUtil = metrics.GetValueOrDefault("MemoryUtilization", 0.5);
            var resourceScore = 1.0 - Math.Max(cpuUtil, memoryUtil);

            return (maintainabilityScore + resourceScore) / 2.0;
        }

        private string GetHealthStatus(double overallScore)
        {
            return overallScore switch
            {
                > 0.9 => "Excellent",
                > 0.8 => "Good",
                > 0.7 => "Fair",
                > 0.6 => "Poor",
                _ => "Critical"
            };
        }

        private List<string> GetCriticalAreas(Dictionary<string, double> metrics)
        {
            var criticalAreas = new List<string>();

            if (metrics.GetValueOrDefault("CpuUtilization", 0) > 0.9)
                criticalAreas.Add("CPU Utilization");

            if (metrics.GetValueOrDefault("MemoryUtilization", 0) > 0.9)
                criticalAreas.Add("Memory Utilization");

            if (metrics.GetValueOrDefault("ErrorRate", 0) > 0.1)
                criticalAreas.Add("Error Rate");

            if (metrics.GetValueOrDefault("SystemLoadAverage", 0) > 5.0)
                criticalAreas.Add("System Load");

            return criticalAreas;
        }

        /// <summary>
        /// Calculates CPU utilization using multiple strategies:
        /// 1. Process-based CPU calculation: Process.TotalProcessorTime / elapsed time / ProcessorCount
        /// 2. Incremental CPU calculation: Tracks CPU time changes between measurements
        /// 3. Historical trend analysis: Smooths measurements using exponential moving average
        /// Returns CPU utilization as a fraction (0.0 to 1.0)
        /// </summary>
        private double GetCpuUtilization()
        {
            lock (_cpuLock)
            {
                try
                {
                    var process = Process.GetCurrentProcess();
                    var now = DateTime.UtcNow;
                    var currentProcessorTime = process.TotalProcessorTime;

                    // Strategy 1: Calculate incremental CPU usage since last measurement
                    var timeSinceLastMeasurement = now - _lastCpuMeasurementTime;
                    var processorTimeDelta = currentProcessorTime - _lastProcessorTime;

                    double currentCpuUtilization = 0.0;

                    // Ensure meaningful time has passed (at least 100ms)
                    if (timeSinceLastMeasurement.TotalMilliseconds >= 100)
                    {
                        // CPU time used / (elapsed time * processor count)
                        // This normalizes to 0-1 range where 1 = 100% of one core
                        var elapsedMs = timeSinceLastMeasurement.TotalMilliseconds;
                        var processorTimeMs = processorTimeDelta.TotalMilliseconds;
                        var processorCount = Environment.ProcessorCount;

                        // Calculate utilization: actual CPU time / (wallclock time * number of cores)
                        // Divide by ProcessorCount to get per-core utilization
                        currentCpuUtilization = processorTimeMs / (elapsedMs * processorCount);

                        // Update tracking variables
                        _lastCpuMeasurementTime = now;
                        _lastProcessorTime = currentProcessorTime;
                    }
                    else
                    {
                        // Not enough time passed, use last known value
                        if (_cpuUtilizationHistory.Count > 0)
                        {
                            currentCpuUtilization = _cpuUtilizationHistory.Last();
                        }
                    }

                    // Clamp to valid range [0, 1]
                    currentCpuUtilization = Math.Clamp(currentCpuUtilization, 0.0, 1.0);

                    // Strategy 2: Add to history for trend analysis
                    _cpuUtilizationHistory.Add(currentCpuUtilization);
                    if (_cpuUtilizationHistory.Count > MaxCpuHistorySize)
                    {
                        _cpuUtilizationHistory.RemoveAt(0);
                    }

                    // Strategy 3: Return smoothed average using exponential moving average
                    var smoothedUtilization = CalculateSmoothedCpuUtilization();

                    _logger.LogTrace("CPU Utilization: Current={Current:F4}, Smoothed={Smoothed:F4}, History={History}",
                        currentCpuUtilization, smoothedUtilization, _cpuUtilizationHistory.Count);

                    return smoothedUtilization;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error calculating CPU utilization");
                    // Return last known value or 0 if no history
                    return _cpuUtilizationHistory.Count > 0 ? _cpuUtilizationHistory.Last() : 0.0;
                }
            }
        }

        /// <summary>
        /// Calculates smoothed CPU utilization using exponential moving average (EMA).
        /// EMA gives more weight to recent measurements while preserving historical trends.
        /// </summary>
        private double CalculateSmoothedCpuUtilization()
        {
            if (_cpuUtilizationHistory.Count == 0)
                return 0.0;

            // For single measurement, return as-is
            if (_cpuUtilizationHistory.Count == 1)
                return _cpuUtilizationHistory[0];

            // Alpha = 2 / (N + 1) where N is the period
            // Using N = 10 for moderate smoothing
            const double alpha = 2.0 / 11.0;

            // Calculate EMA from history
            double ema = _cpuUtilizationHistory[0];
            for (int i = 1; i < _cpuUtilizationHistory.Count; i++)
            {
                ema = (_cpuUtilizationHistory[i] * alpha) + (ema * (1.0 - alpha));
            }

            return ema;
        }

        /// <summary>
        /// Gets average CPU utilization from history (for testing/monitoring).
        /// </summary>
        internal double GetAverageCpuUtilization()
        {
            lock (_cpuLock)
            {
                if (_cpuUtilizationHistory.Count == 0)
                    return 0.0;
                return _cpuUtilizationHistory.Average();
            }
        }

        /// <summary>
        /// Gets maximum CPU utilization from history (for testing/monitoring).
        /// </summary>
        internal double GetMaxCpuUtilization()
        {
            lock (_cpuLock)
            {
                if (_cpuUtilizationHistory.Count == 0)
                    return 0.0;
                return _cpuUtilizationHistory.Max();
            }
        }

        /// <summary>
        /// Gets minimum CPU utilization from history (for testing/monitoring).
        /// </summary>
        internal double GetMinCpuUtilization()
        {
            lock (_cpuLock)
            {
                if (_cpuUtilizationHistory.Count == 0)
                    return 0.0;
                return _cpuUtilizationHistory.Min();
            }
        }

        /// <summary>
        /// Gets standard deviation of CPU utilization for volatility analysis.
        /// </summary>
        internal double GetCpuUtilizationStdDev()
        {
            lock (_cpuLock)
            {
                if (_cpuUtilizationHistory.Count < 2)
                    return 0.0;

                var average = _cpuUtilizationHistory.Average();
                var variance = _cpuUtilizationHistory.Sum(x => Math.Pow(x - average, 2)) / _cpuUtilizationHistory.Count;
                return Math.Sqrt(variance);
            }
        }

        /// <summary>
        /// Clears CPU utilization history (useful for memory management).
        /// </summary>
        internal void ClearCpuHistory()
        {
            lock (_cpuLock)
            {
                _cpuUtilizationHistory.Clear();
                _lastCpuMeasurementTime = DateTime.UtcNow;
                _logger.LogDebug("Cleared CPU utilization history");
            }
        }

        /// <summary>
        /// Gets the count of CPU measurements in history.
        /// </summary>
        internal int GetCpuHistorySize()
        {
            lock (_cpuLock)
            {
                return _cpuUtilizationHistory.Count;
            }
        }

        private (double utilization, double usedMB, double availableMB) GetMemoryInfo()
        {
            var process = Process.GetCurrentProcess();
            var usedMB = process.WorkingSet64 / 1024.0 / 1024.0;

            // Estimate available memory (simplified)
            var totalMemoryMB = 8192; // Assume 8GB system
            var availableMB = Math.Max(0, totalMemoryMB - usedMB);
            var utilization = usedMB / totalMemoryMB;

            return (utilization, usedMB, availableMB);
        }

        private double GetThroughputPerSecond()
        {
            var now = DateTime.UtcNow;
            var timeElapsed = (now - _lastThroughputReset).TotalSeconds;

            if (timeElapsed >= 1.0) // Update every second
            {
                var requestsInPeriod = Interlocked.Read(ref _totalRequestsProcessed);
                _currentThroughputPerSecond = requestsInPeriod / timeElapsed;

                // Reset for next period
                Interlocked.Exchange(ref _totalRequestsProcessed, 0);
                _lastThroughputReset = now;
            }

            return _currentThroughputPerSecond;
        }

        /// <summary>
        /// Records an error occurrence
        /// </summary>
        public void RecordError()
        {
            Interlocked.Increment(ref _totalErrors);
        }

        /// <summary>
        /// Records an exception occurrence
        /// </summary>
        public void RecordException()
        {
            Interlocked.Increment(ref _totalExceptions);
        }

        private double GetErrorRate()
        {
            var now = DateTime.UtcNow;
            var timeElapsed = (now - _lastErrorReset).TotalSeconds;

            if (timeElapsed < 1.0)
            {
                // Not enough time elapsed, return last known rate
                return _latestMetrics.GetValueOrDefault("ErrorRate", 0.0);
            }

            var totalRequests = Interlocked.Read(ref _totalRequestsProcessed);
            var totalErrors = Interlocked.Read(ref _totalErrors);

            // Calculate error rate
            var errorRate = totalRequests > 0 ? (double)totalErrors / totalRequests : 0.0;

            return Math.Min(errorRate, 1.0); // Clamp to max 1.0
        }

        private double GetExceptionCount()
        {
            return Interlocked.Read(ref _totalExceptions);
        }

        /// <summary>
        /// Records network latency measurement
        /// </summary>
        public void RecordNetworkLatency(double latencyMs)
        {
            lock (_metricsLock)
            {
                _networkLatencyHistory.Enqueue((DateTime.UtcNow, latencyMs));

                // Maintain max history size
                while (_networkLatencyHistory.Count > _maxNetworkHistorySize)
                {
                    _networkLatencyHistory.Dequeue();
                }
            }
        }

        private double GetNetworkLatency()
        {
            lock (_metricsLock)
            {
                if (_networkLatencyHistory.Count == 0)
                {
                    return 0.0;
                }

                // Return average latency from recent measurements
                var recentLatencies = _networkLatencyHistory
                    .Where(x => (DateTime.UtcNow - x.timestamp).TotalSeconds < 60) // Last 60 seconds
                    .Select(x => x.latencyMs)
                    .ToList();

                return recentLatencies.Count > 0 ? recentLatencies.Average() : 0.0;
            }
        }

        private double GetNetworkThroughput()
        {
            // Network throughput in Mbps
            // This is a simplified calculation - in a real system, this would track actual bytes sent/received
            var throughputPerSecond = GetThroughputPerSecond();

            // Estimate: Assume each request is ~10KB on average
            var estimatedBytesPerSecond = throughputPerSecond * 10 * 1024;

            // Convert to Mbps
            var mbps = (estimatedBytesPerSecond * 8) / (1024 * 1024);

            return mbps;
        }

        private double GetDiskReadBytesPerSecond()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var now = DateTime.UtcNow;
                var timeSinceLastMeasurement = (now - _lastDiskMeasurement).TotalSeconds;

                // Only update if enough time has passed (at least 1 second)
                if (timeSinceLastMeasurement >= 1.0)
                {
                    // Note: Process doesn't expose disk I/O directly in a cross-platform way
                    // This is a simplified approximation
                    // In production, you'd use platform-specific APIs or monitoring tools

                    var currentReadBytes = process.WorkingSet64; // Simplified - not actual disk reads
                    var deltaBytes = currentReadBytes - _lastDiskReadBytes;

                    _currentDiskReadBytesPerSecond = Math.Max(0, deltaBytes / timeSinceLastMeasurement);
                    _lastDiskReadBytes = currentReadBytes;
                    _lastDiskMeasurement = now;
                }

                return _currentDiskReadBytesPerSecond;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating disk read bytes per second");
                return 0.0;
            }
        }

        private double GetDiskWriteBytesPerSecond()
        {
            try
            {
                var process = Process.GetCurrentProcess();

                // Simplified approximation - actual disk writes would require platform-specific APIs
                // Estimate based on a fraction of memory changes
                var estimatedWriteBytes = _currentDiskReadBytesPerSecond * 0.5;

                return estimatedWriteBytes;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating disk write bytes per second");
                return 0.0;
            }
        }

        private double GetSystemLoadAverage()
        {
            try
            {
                var process = Process.GetCurrentProcess();

                // Calculate system load as combination of CPU, memory, and thread utilization
                var cpuUtilization = GetCpuUtilization();
                var memoryInfo = GetMemoryInfo();
                var threadCount = process.Threads.Count;

                // Normalize thread count (assume 100 threads is high load)
                var threadLoad = Math.Min(threadCount / 100.0, 1.0);

                // Weighted average: CPU (50%), Memory (30%), Threads (20%)
                var loadAverage = (cpuUtilization * 0.5) + (memoryInfo.utilization * 0.3) + (threadLoad * 0.2);

                // Scale to typical load average range (0-10+)
                return loadAverage * 10.0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating system load average");
                return 1.0; // Default moderate load
            }
        }

        private double GetThreadCount()
        {
            var process = Process.GetCurrentProcess();
            return process.Threads.Count;
        }

        private double GetHandleCount()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                // HandleCount is available on Windows, returns 0 on other platforms
                return process.HandleCount;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting handle count");
                return 0.0;
            }
        }

        /// <summary>
        /// Clears all prediction history (useful for testing or reset)
        /// </summary>
        internal void ClearPredictionHistory()
        {
            lock (_predictionLock)
            {
                _predictionOutcomes.Clear();
                _logger.LogDebug("Cleared prediction history");
            }
        }

        /// <summary>
        /// Gets the count of prediction outcomes in history
        /// </summary>
        internal int GetPredictionHistorySize()
        {
            lock (_predictionLock)
            {
                return _predictionOutcomes.Count;
            }
        }

        /// <summary>
        /// Resets error counters (useful for testing or periodic reset)
        /// </summary>
        internal void ResetErrorCounters()
        {
            Interlocked.Exchange(ref _totalErrors, 0);
            Interlocked.Exchange(ref _totalExceptions, 0);
            _lastErrorReset = DateTime.UtcNow;
            _logger.LogDebug("Reset error counters");
        }

        /// <summary>
        /// Clears network latency history
        /// </summary>
        internal void ClearNetworkHistory()
        {
            lock (_metricsLock)
            {
                _networkLatencyHistory.Clear();
                _logger.LogDebug("Cleared network latency history");
            }
        }

        /// <summary>
        /// Internal class for tracking prediction outcomes
        /// </summary>
        private class PredictionOutcome
        {
            public DateTime Timestamp { get; set; }
            public OptimizationStrategy Strategy { get; set; }
            public TimeSpan PredictedImprovement { get; set; }
            public TimeSpan ActualImprovement { get; set; }
            public TimeSpan BaselineExecutionTime { get; set; }
        }
    }
}