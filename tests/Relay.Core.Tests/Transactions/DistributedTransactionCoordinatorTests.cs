using System;
using System.Data;
using System.Threading;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Transactions;
using Xunit;

namespace Relay.Core.Tests.Transactions;

/// <summary>
/// Tests for DistributedTransactionCoordinator.
/// </summary>
public class DistributedTransactionCoordinatorTests
{
    private readonly Mock<ILogger<DistributedTransactionCoordinator>> _loggerMock;
    private readonly DistributedTransactionCoordinator _coordinator;

    public DistributedTransactionCoordinatorTests()
    {
        _loggerMock = new Mock<ILogger<DistributedTransactionCoordinator>>();
        _coordinator = new DistributedTransactionCoordinator(_loggerMock.Object);
    }

    [Fact]
    public void CreateDistributedTransactionScope_WithValidConfiguration_ShouldReturnScopeWithTransactionId()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

        // Assert
        Assert.NotNull(result.Scope);
        Assert.NotNull(result.TransactionId);
        Assert.NotEmpty(result.TransactionId);
        Assert.True(result.StartTime <= DateTime.UtcNow);
        Assert.True(result.StartTime > DateTime.UtcNow.AddMinutes(-1));

        // Verify logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Creating distributed transaction scope")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("created successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void CreateDistributedTransactionScope_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _coordinator.CreateDistributedTransactionScope(null, requestType, cancellationToken));
    }

    [Theory]
    [InlineData(IsolationLevel.Unspecified)]
    [InlineData(IsolationLevel.Chaos)]
    [InlineData(IsolationLevel.ReadUncommitted)]
    [InlineData(IsolationLevel.ReadCommitted)]
    [InlineData(IsolationLevel.RepeatableRead)]
    [InlineData(IsolationLevel.Serializable)]
    [InlineData(IsolationLevel.Snapshot)]
    public void CreateDistributedTransactionScope_WithDifferentIsolationLevels_ShouldCreateScopeCorrectly(IsolationLevel isolationLevel)
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = isolationLevel,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

        // Assert
        Assert.NotNull(result.Scope);
        Assert.NotNull(result.TransactionId);
    }

        [Theory]
        [InlineData(0)]
        [InlineData(30)]
        [InlineData(300)]
        [InlineData(-1)]
        public void CreateDistributedTransactionScope_WithDifferentTimeouts_ShouldCreateScopeCorrectly(int timeoutSeconds)
        {
            // Arrange
            var timeout = timeoutSeconds switch
            {
                0 => TimeSpan.Zero,
                -1 => Timeout.InfiniteTimeSpan,
                _ => TimeSpan.FromSeconds(timeoutSeconds)
            };
            
            var configuration = new TestTransactionConfiguration
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = timeout,
                IsReadOnly = false,
                UseDistributedTransaction = true,
                RetryPolicy = null
            };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

        // Assert
        Assert.NotNull(result.Scope);
        Assert.NotNull(result.TransactionId);
    }

    [Fact]
    public void CreateDistributedTransactionScope_WithException_ShouldThrowDistributedTransactionException()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Mock logger to throw exception during logging to simulate failure
        _loggerMock.Setup(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()))
            .Throws(new InvalidOperationException("Simulated failure"));

        // Act & Assert
        var exception = Assert.Throws<DistributedTransactionException>(() =>
            _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken));

        Assert.Contains("Failed to create distributed transaction scope", exception.Message);
        Assert.NotNull(exception.TransactionId);
        Assert.NotNull(exception.InnerException);

        // Verify error logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to create distributed transaction scope")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void CreateDistributedTransactionScope_ShouldGenerateUniqueTransactionIds()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        var result1 = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);
        var result2 = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

        // Assert
        Assert.NotEqual(result1.TransactionId, result2.TransactionId);
    }

    [Fact]
    public void CreateDistributedTransactionScope_ShouldLogCorrectInformation()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.Serializable,
            Timeout = TimeSpan.FromMinutes(2),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "CreateOrderCommand";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("Creating distributed transaction scope") &&
                    v.ToString().Contains(requestType) &&
                    v.ToString().Contains(configuration.IsolationLevel.ToString()) &&
                    v.ToString().Contains(configuration.Timeout.TotalSeconds.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    private class TestTransactionConfiguration : ITransactionConfiguration
    {
        public IsolationLevel IsolationLevel { get; set; }
        public TimeSpan Timeout { get; set; }
        public bool IsReadOnly { get; set; }
        public bool UseDistributedTransaction { get; set; }
        public TransactionRetryPolicy? RetryPolicy { get; set; }
    }
}