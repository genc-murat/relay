using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.AI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Extensions;

public class AICircuitBreakerHealthCheckTests
{
    private readonly ILogger<AICircuitBreakerHealthCheck> _logger;
    private readonly AIHealthCheckOptions _options;

    public AICircuitBreakerHealthCheckTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<AICircuitBreakerHealthCheck>();
        _options = new AIHealthCheckOptions
        {
            MaxCircuitBreakerFailureRate = 0.25,
            HealthCheckTimeout = TimeSpan.FromSeconds(5)
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AICircuitBreakerHealthCheck(null!, optionsMock.Object));
    }

    [Fact]
    public void Constructor_Should_Use_Default_Options_When_Options_Is_Null()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
        optionsMock.Setup(o => o.Value).Returns((AIHealthCheckOptions)null!);

        // Act
        var healthCheck = new AICircuitBreakerHealthCheck(_logger, optionsMock.Object);

        // Assert - should not throw, uses default options
        Assert.NotNull(healthCheck);
    }

    [Fact]
    public void Constructor_Should_Accept_Valid_Parameters()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);

        // Act
        var healthCheck = new AICircuitBreakerHealthCheck(_logger, optionsMock.Object);

        // Assert
        Assert.NotNull(healthCheck);
    }

    #endregion

    #region CheckHealthAsync Tests

    [Fact]
    public async Task CheckHealthAsync_Should_Return_Healthy_Result()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);

        var healthCheck = new AICircuitBreakerHealthCheck(_logger, optionsMock.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        Assert.True(result.IsHealthy);
        Assert.Equal("Operational", result.Status);
        Assert.Equal("AI Circuit Breakers", result.ComponentName);
        Assert.Equal(1.0, result.HealthScore);
        Assert.Equal("Circuit breaker mechanisms are ready", result.Description);
        Assert.True(result.Duration > TimeSpan.Zero);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Warnings);
        Assert.Empty(result.Errors);
        Assert.Null(result.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_Return_Healthy_Result_With_Default_CancellationToken()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);

        var healthCheck = new AICircuitBreakerHealthCheck(_logger, optionsMock.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        Assert.True(result.IsHealthy);
        Assert.Equal("Operational", result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_Return_Healthy_Result_With_Custom_CancellationToken()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);

        var healthCheck = new AICircuitBreakerHealthCheck(_logger, optionsMock.Object);
        var cancellationToken = new CancellationTokenSource().Token;

        // Act
        var result = await healthCheck.CheckHealthAsync(cancellationToken);

        // Assert
        Assert.True(result.IsHealthy);
        Assert.Equal("Operational", result.Status);
    }



    [Fact]
    public async Task CheckHealthAsync_Should_Initialize_Data_Dictionary()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);

        var healthCheck = new AICircuitBreakerHealthCheck(_logger, optionsMock.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        Assert.NotNull(result.Data);
        Assert.IsType<Dictionary<string, object>>(result.Data);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_Initialize_Warnings_List()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);

        var healthCheck = new AICircuitBreakerHealthCheck(_logger, optionsMock.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        Assert.NotNull(result.Warnings);
        Assert.IsType<List<string>>(result.Warnings);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_Initialize_Errors_List()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);

        var healthCheck = new AICircuitBreakerHealthCheck(_logger, optionsMock.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        Assert.NotNull(result.Errors);
        Assert.IsType<List<string>>(result.Errors);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_Measure_Duration()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);

        var healthCheck = new AICircuitBreakerHealthCheck(_logger, optionsMock.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        Assert.True(result.Duration > TimeSpan.Zero);
        Assert.True(result.Duration < TimeSpan.FromSeconds(1)); // Should be very fast
    }

    [Fact]
    public async Task CheckHealthAsync_Should_Handle_Exceptions_In_Try_Block()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);

        var loggerMock = new Mock<ILogger<AICircuitBreakerHealthCheck>>();
        loggerMock.Setup(l => l.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Throws(new InvalidOperationException("Logger error"));

        var healthCheck = new AICircuitBreakerHealthCheck(loggerMock.Object, optionsMock.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        Assert.False(result.IsHealthy);
        Assert.Equal("Failed", result.Status);
        Assert.Contains("Logger error", result.Description);
        Assert.NotNull(result.Exception);
        Assert.IsType<InvalidOperationException>(result.Exception);
        Assert.Contains("Logger error", result.Errors);
        Assert.True(result.Duration > TimeSpan.Zero);
    }

    #endregion


}