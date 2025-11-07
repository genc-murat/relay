using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Xunit;

namespace Relay.Core.Tests.AI;

public class ConnectionMetricsCollectorConstructorTests
{
    private readonly ILogger<ConnectionMetricsCollector> _logger;
    private readonly AIOptimizationOptions _options;
    private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;

    public ConnectionMetricsCollectorConstructorTests()
    {
        _logger = NullLogger<ConnectionMetricsCollector>.Instance;
        _options = new AIOptimizationOptions
        {
            MaxEstimatedHttpConnections = 200,
            MaxEstimatedDbConnections = 50,
            EstimatedMaxDbConnections = 100,
            MaxEstimatedExternalConnections = 30,
            MaxEstimatedWebSocketConnections = 1000
        };
        _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
    }

    private ConnectionMetricsCollector CreateCollector()
    {
        return new ConnectionMetricsCollector(_logger, _options, _requestAnalytics);
    }

    [Fact]
    public void Constructor_Should_Initialize_With_Valid_Parameters()
    {
        // Arrange & Act
        var collector = CreateCollector();

        // Assert
        Assert.NotNull(collector);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ConnectionMetricsCollector(null!, _options, _requestAnalytics));

        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Options_Is_Null()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ConnectionMetricsCollector(_logger, null!, _requestAnalytics));

        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void Constructor_Should_Throw_When_RequestAnalytics_Is_Null()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ConnectionMetricsCollector(_logger, _options, null!));

        Assert.Equal("requestAnalytics", exception.ParamName);
    }

    [Fact]
    public void Constructor_Should_Accept_Custom_Options_Values()
    {
        // Arrange
        var customOptions = new AIOptimizationOptions
        {
            MaxEstimatedHttpConnections = 500,
            MaxEstimatedDbConnections = 100,
            EstimatedMaxDbConnections = 200,
            MaxEstimatedExternalConnections = 50,
            MaxEstimatedWebSocketConnections = 2000
        };

        // Act
        var collector = new ConnectionMetricsCollector(_logger, customOptions, _requestAnalytics);

        // Assert
        Assert.NotNull(collector);
    }

    [Fact]
    public void Constructor_Should_Accept_Zero_Options_Values()
    {
        // Arrange
        var zeroOptions = new AIOptimizationOptions
        {
            MaxEstimatedHttpConnections = 0,
            MaxEstimatedDbConnections = 0,
            EstimatedMaxDbConnections = 0,
            MaxEstimatedExternalConnections = 0,
            MaxEstimatedWebSocketConnections = 0
        };

        // Act
        var collector = new ConnectionMetricsCollector(_logger, zeroOptions, _requestAnalytics);

        // Assert
        Assert.NotNull(collector);
    }

    [Fact]
    public void Constructor_Should_Accept_Negative_Options_Values()
    {
        // Arrange
        var negativeOptions = new AIOptimizationOptions
        {
            MaxEstimatedHttpConnections = -100,
            MaxEstimatedDbConnections = -50,
            EstimatedMaxDbConnections = -200,
            MaxEstimatedExternalConnections = -25,
            MaxEstimatedWebSocketConnections = -500
        };

        // Act
        var collector = new ConnectionMetricsCollector(_logger, negativeOptions, _requestAnalytics);

        // Assert
        Assert.NotNull(collector); // Constructor doesn't validate option values, just null checks
    }

    [Fact]
    public void Constructor_Should_Accept_Empty_RequestAnalytics_Dictionary()
    {
        // Arrange
        var emptyAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();

        // Act
        var collector = new ConnectionMetricsCollector(_logger, _options, emptyAnalytics);

        // Assert
        Assert.NotNull(collector);
    }

    [Fact]
    public void Constructor_Should_Accept_Populated_RequestAnalytics_Dictionary()
    {
        // Arrange
        var populatedAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
        populatedAnalytics.TryAdd(typeof(string), new RequestAnalysisData());
        populatedAnalytics.TryAdd(typeof(int), new RequestAnalysisData());

        // Act
        var collector = new ConnectionMetricsCollector(_logger, _options, populatedAnalytics);

        // Assert
        Assert.NotNull(collector);
    }

    [Fact]
    public void Constructor_Should_Work_With_Different_Logger_Implementations()
    {
        // Arrange
        var consoleLogger = new Microsoft.Extensions.Logging.LoggerFactory().CreateLogger<ConnectionMetricsCollector>();

        // Act
        var collector = new ConnectionMetricsCollector(consoleLogger, _options, _requestAnalytics);

        // Assert
        Assert.NotNull(collector);
    }
}