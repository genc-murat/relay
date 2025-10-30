using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker;
using Relay.MessageBroker.CircuitBreaker;
using Relay.MessageBroker.ConnectionPool;
using Relay.MessageBroker.HealthChecks;
using Xunit;

namespace Relay.MessageBroker.Tests.HealthChecks;

public class MessageBrokerHealthCheckTests
{
    private readonly Mock<IMessageBroker> _brokerMock;
    private readonly Mock<ILogger<MessageBrokerHealthCheck>> _loggerMock;
    private readonly HealthCheckOptions _options;

    public MessageBrokerHealthCheckTests()
    {
        _brokerMock = new Mock<IMessageBroker>();
        _loggerMock = new Mock<ILogger<MessageBrokerHealthCheck>>();
        _options = new HealthCheckOptions
        {
            ConnectivityTimeout = TimeSpan.FromSeconds(1),
            IncludeCircuitBreakerState = true,
            IncludeConnectionPoolMetrics = true
        };
    }

    [Fact]
    public void Constructor_WithNullBroker_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MessageBrokerHealthCheck(
                null!,
                _loggerMock.Object,
                Options.Create(_options)));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MessageBrokerHealthCheck(
                _brokerMock.Object,
                null!,
                Options.Create(_options)));
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MessageBrokerHealthCheck(
                _brokerMock.Object,
                _loggerMock.Object,
                null!));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenBrokerIsHealthy_ShouldReturnHealthy()
    {
        // Arrange
        var healthCheck = new MessageBrokerHealthCheck(
            _brokerMock.Object,
            _loggerMock.Object,
            Options.Create(_options));

        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("Message broker is healthy", result.Description);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.ContainsKey("broker_connected"));
        Assert.True(result.Data.ContainsKey("broker_type"));
        Assert.True(result.Data.ContainsKey("check_timestamp"));
    }

    [Fact]
    public async Task CheckHealthAsync_WithCircuitBreakerOpen_ShouldReturnUnhealthy()
    {
        // Arrange
        var circuitBreakerMock = new Mock<ICircuitBreaker>();
        circuitBreakerMock.Setup(x => x.State).Returns(CircuitBreakerState.Open);
        circuitBreakerMock.Setup(x => x.Metrics).Returns(new CircuitBreakerMetrics
        {
            FailedCalls = 5,
            SuccessfulCalls = 1,
            TotalCalls = 6
        });

        var healthCheck = new MessageBrokerHealthCheck(
            _brokerMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            circuitBreakerMock.Object);

        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("Circuit breaker is open", result.Description);
        Assert.NotNull(result.Data);
        Assert.Equal("Open", result.Data["circuit_breaker_state"]);
        Assert.Equal(5L, result.Data["circuit_breaker_failed_calls"]);
        Assert.Equal(1L, result.Data["circuit_breaker_successful_calls"]);
        Assert.Equal(6L, result.Data["circuit_breaker_total_calls"]);
        Assert.Equal(5.0 / 6.0, result.Data["circuit_breaker_failure_rate"]);
    }

    [Fact]
    public async Task CheckHealthAsync_WithCircuitBreakerClosed_ShouldReturnHealthy()
    {
        // Arrange
        var circuitBreakerMock = new Mock<ICircuitBreaker>();
        circuitBreakerMock.Setup(x => x.State).Returns(CircuitBreakerState.Closed);
        circuitBreakerMock.Setup(x => x.Metrics).Returns(new CircuitBreakerMetrics
        {
            FailedCalls = 1,
            SuccessfulCalls = 9,
            TotalCalls = 10
        });

        var healthCheck = new MessageBrokerHealthCheck(
            _brokerMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            circuitBreakerMock.Object);

        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.NotNull(result.Data);
        Assert.Equal("Closed", result.Data["circuit_breaker_state"]);
    }

    [Fact]
    public async Task CheckHealthAsync_WithConnectionPoolMetrics_ShouldIncludeMetrics()
    {
        // Arrange
        var poolMetrics = new ConnectionPoolMetrics
        {
            ActiveConnections = 5,
            IdleConnections = 3,
            TotalConnections = 8,
            AverageWaitTimeMs = 150.5,
            WaitingThreads = 2
        };

        Func<ConnectionPoolMetrics> getMetrics = () => poolMetrics;

        var healthCheck = new MessageBrokerHealthCheck(
            _brokerMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            null,
            getMetrics);

        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.NotNull(result.Data);
        Assert.Equal(5, result.Data["pool_active_connections"]);
        Assert.Equal(3, result.Data["pool_idle_connections"]);
        Assert.Equal(8, result.Data["pool_total_connections"]);
        Assert.Equal(150.5, result.Data["pool_wait_time_ms"]);
        Assert.Equal(2, result.Data["pool_waiting_threads"]);
    }

    [Fact]
    public async Task CheckHealthAsync_WithExhaustedConnectionPool_ShouldIncludeExhaustedFlag()
    {
        // Arrange
        var poolMetrics = new ConnectionPoolMetrics
        {
            ActiveConnections = 10,
            IdleConnections = 0,
            TotalConnections = 10,
            AverageWaitTimeMs = 500.0,
            WaitingThreads = 5
        };

        Func<ConnectionPoolMetrics> getMetrics = () => poolMetrics;

        var healthCheck = new MessageBrokerHealthCheck(
            _brokerMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            null,
            getMetrics);

        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status); // Pool exhaustion doesn't make it unhealthy by default
        Assert.NotNull(result.Data);
        Assert.True((bool)result.Data["pool_exhausted"]);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCircuitBreakerDisabled_ShouldNotIncludeCircuitBreakerData()
    {
        // Arrange
        var options = new HealthCheckOptions
        {
            IncludeCircuitBreakerState = false,
            IncludeConnectionPoolMetrics = true
        };

        var circuitBreakerMock = new Mock<ICircuitBreaker>();
        circuitBreakerMock.Setup(x => x.State).Returns(CircuitBreakerState.Open);

        var healthCheck = new MessageBrokerHealthCheck(
            _brokerMock.Object,
            _loggerMock.Object,
            Options.Create(options),
            circuitBreakerMock.Object);

        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.NotNull(result.Data);
        Assert.False(result.Data.ContainsKey("circuit_breaker_state"));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionPoolMetricsDisabled_ShouldNotIncludePoolData()
    {
        // Arrange
        var options = new HealthCheckOptions
        {
            IncludeCircuitBreakerState = true,
            IncludeConnectionPoolMetrics = false
        };

        var poolMetrics = new ConnectionPoolMetrics
        {
            ActiveConnections = 5,
            IdleConnections = 3,
            TotalConnections = 8
        };

        Func<ConnectionPoolMetrics> getMetrics = () => poolMetrics;

        var healthCheck = new MessageBrokerHealthCheck(
            _brokerMock.Object,
            _loggerMock.Object,
            Options.Create(options),
            null,
            getMetrics);

        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.NotNull(result.Data);
        Assert.False(result.Data.ContainsKey("pool_active_connections"));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCircuitBreakerThrows_ShouldReturnUnhealthy()
    {
        // Arrange
        var circuitBreakerMock = new Mock<ICircuitBreaker>();
        circuitBreakerMock.Setup(x => x.State).Throws(new InvalidOperationException("Circuit breaker error"));

        var healthCheck = new MessageBrokerHealthCheck(
            _brokerMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            circuitBreakerMock.Object);

        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("Health check failed with exception", result.Description);
        Assert.NotNull(result.Data);
        Assert.Equal("Circuit breaker error", result.Data["error"]);
        Assert.Equal("InvalidOperationException", result.Data["error_type"]);
    }


}