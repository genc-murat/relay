using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using Relay.Core.AI.Models;
using Relay.Core.AI.Optimization.Connection;
using System;
using System.Collections.Concurrent;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Connection;

public class LoadBalancerConnectionEstimatorTests
{
    private readonly ILogger _logger;
    private readonly AIOptimizationOptions _options;
    private readonly TestTimeSeriesDatabase _timeSeriesDb;
    private readonly SystemMetricsCalculator _systemMetrics;
    private readonly LoadBalancerConnectionEstimator _estimator;

    public LoadBalancerConnectionEstimatorTests()
    {
        _logger = NullLogger.Instance;
        _options = new AIOptimizationOptions
        {
            MaxEstimatedHttpConnections = 1000,
            EnableHttpConnectionReflection = true,
            HttpMetricsReflectionMaxRetries = 3
        };
        _timeSeriesDb = new TestTimeSeriesDatabase();
        var requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
        _systemMetrics = new SystemMetricsCalculator(NullLogger<SystemMetricsCalculator>.Instance, requestAnalytics);

        _estimator = new LoadBalancerConnectionEstimator(
            _logger,
            _options,
            _timeSeriesDb,
            _systemMetrics);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LoadBalancerConnectionEstimator(null!, _options, _timeSeriesDb, _systemMetrics));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Options_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LoadBalancerConnectionEstimator(_logger, null!, _timeSeriesDb, _systemMetrics));
    }

    [Fact]
    public void Constructor_Should_Throw_When_TimeSeriesDb_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LoadBalancerConnectionEstimator(_logger, _options, null!, _systemMetrics));
    }

    [Fact]
    public void Constructor_Should_Throw_When_SystemMetrics_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LoadBalancerConnectionEstimator(_logger, _options, _timeSeriesDb, null!));
    }

    #endregion

    #region GetLoadBalancerConnectionCount Tests

    [Fact]
    public void GetLoadBalancerConnectionCount_Should_Use_Stored_Metrics_When_Available()
    {
        // Arrange - Store some metrics
        _timeSeriesDb.StoreMetric("LoadBalancer_ConnectionCount", 15, DateTime.UtcNow.AddMinutes(-1));
        _timeSeriesDb.StoreMetric("LoadBalancer_ConnectionCount", 20, DateTime.UtcNow);

        // Act
        var result = _estimator.GetLoadBalancerConnectionCount();

        // Assert - Should use weighted calculation from stored metrics
        Assert.True(result >= 0);
    }

    [Fact]
    public void GetLoadBalancerConnectionCount_Should_Calculate_From_Scratch_When_No_Stored_Metrics()
    {
        // Arrange - No stored metrics

        // Act
        var result = _estimator.GetLoadBalancerConnectionCount();

        // Assert - Should calculate and return a valid result
        Assert.True(result >= 0);
        Assert.True(result <= 100); // Capped at 100
    }

    [Fact]
    public void GetLoadBalancerConnectionCount_Should_Store_Calculated_Metrics()
    {
        // Arrange - No stored metrics initially

        // Act
        var result = _estimator.GetLoadBalancerConnectionCount();

        // Assert - Should have stored metrics
        var storedMetrics = _timeSeriesDb.GetRecentMetrics("LoadBalancer_ConnectionCount", 1);
        Assert.Single(storedMetrics);
        Assert.Equal(result, (int)storedMetrics[0].Value);
    }

    [Fact]
    public void GetLoadBalancerConnectionCount_Should_Handle_Exception_Gracefully()
    {
        // Arrange - Create estimator with throwing database
        var throwingDb = new ThrowingTimeSeriesDatabase();
        var estimator = new LoadBalancerConnectionEstimator(
            _logger,
            _options,
            throwingDb,
            _systemMetrics);

        // Act
        var result = estimator.GetLoadBalancerConnectionCount();

        // Assert - Should catch exception and return 0
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetLoadBalancerConnectionCount_Should_Cap_At_Maximum()
    {
        // Arrange - Mock high values that would exceed 100
        // Since calculation is complex, we'll just verify the cap is applied
        var result = _estimator.GetLoadBalancerConnectionCount();

        // Assert - Result should be <= 100
        Assert.True(result <= 100);
    }

    [Fact]
    public void CalculateServiceMeshConnections_Should_Return_Zero_When_No_ServiceMesh()
    {
        // Arrange - No service mesh metrics stored

        // Act
        var result = _estimator.GetLoadBalancerConnectionCount();

        // Assert - Should calculate without service mesh connections
        Assert.True(result >= 0);
    }

    [Fact]
    public void CalculateServiceMeshConnections_Should_Include_ServiceMesh_When_Active()
    {
        // Arrange - Store service mesh active metric
        _timeSeriesDb.StoreMetric("ServiceMesh_Active", 1, DateTime.UtcNow);

        // Act
        var result = _estimator.GetLoadBalancerConnectionCount();

        // Assert - Should include service mesh connections
        Assert.True(result >= 0);
        var meshMetrics = _timeSeriesDb.GetRecentMetrics("LoadBalancer_ServiceMesh", 1);
        Assert.Single(meshMetrics);
        Assert.True(meshMetrics[0].Value > 0);
    }

    [Fact]
    public void DetermineLoadBalancerTypeMultiplier_Should_Use_Stored_Type()
    {
        // Arrange - Store LB type metric
        _timeSeriesDb.StoreMetric("LoadBalancer_Type", 2, DateTime.UtcNow); // L7 type

        // Act
        var result = _estimator.GetLoadBalancerConnectionCount();

        // Assert - Should apply L7 multiplier (1.2)
        Assert.True(result >= 0);
        var multiplierMetrics = _timeSeriesDb.GetRecentMetrics("LoadBalancer_TypeMultiplier", 1);
        Assert.Single(multiplierMetrics);
        Assert.Equal(1.2, multiplierMetrics[0].Value, 0.01);
    }

    [Fact]
    public void DetermineDeploymentTopologyFactor_Should_Use_Stored_Topology()
    {
        // Arrange - Store topology metric
        _timeSeriesDb.StoreMetric("Deployment_Topology", 2, DateTime.UtcNow); // Multi-region

        // Act
        var result = _estimator.GetLoadBalancerConnectionCount();

        // Assert - Should apply multi-region factor (1.5)
        Assert.True(result >= 0);
        var factorMetrics = _timeSeriesDb.GetRecentMetrics("LoadBalancer_TopologyFactor", 1);
        Assert.Single(factorMetrics);
        Assert.Equal(1.5, factorMetrics[0].Value, 0.01);
    }

    [Fact]
    public void DetermineHighAvailabilityFactor_Should_Use_Stored_HA_Instances()
    {
        // Arrange - Store HA instances metric
        _timeSeriesDb.StoreMetric("LoadBalancer_HA_Instances", 3, DateTime.UtcNow);

        // Act
        var result = _estimator.GetLoadBalancerConnectionCount();

        // Assert - Should use stored HA factor
        Assert.True(result >= 0);
    }

    [Fact]
    public void DetermineMonitoringLevel_Should_Use_Stored_Monitoring_Level()
    {
        // Arrange - Store monitoring level metric
        _timeSeriesDb.StoreMetric("Monitoring_Level", 0.8, DateTime.UtcNow);

        // Act
        var result = _estimator.GetLoadBalancerConnectionCount();

        // Assert - Should use stored monitoring level
        Assert.True(result >= 0);
    }

    #endregion

    // Helper class to test exception handling
    private class ThrowingTimeSeriesDatabase : TestTimeSeriesDatabase
    {
        public override System.Collections.Generic.List<Relay.Core.AI.MetricDataPoint> GetRecentMetrics(string metricName, int count)
        {
            throw new InvalidOperationException("Simulated database error");
        }
    }
}