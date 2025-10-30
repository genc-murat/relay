using Microsoft.Extensions.Logging.Abstractions;
using Relay.MessageBroker.ConnectionPool;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class ConnectionPoolManagerTests
{
    private class TestConnection
    {
        public Guid Id { get; } = Guid.NewGuid();
        public bool IsDisposed { get; set; }
    }

    [Fact]
    public async Task AcquireAsync_ShouldCreateNewConnectionWhenPoolIsEmpty()
    {
        // Arrange
        var options = new ConnectionPoolOptions
        {
            MinPoolSize = 0,
            MaxPoolSize = 10,
            ConnectionTimeout = TimeSpan.FromSeconds(5)
        };
        var pool = new ConnectionPoolManager<TestConnection>(
            _ => ValueTask.FromResult(new TestConnection()),
            options,
            NullLogger<ConnectionPoolManager<TestConnection>>.Instance);

        // Act
        var connection = await pool.AcquireAsync();

        // Assert
        Assert.NotNull(connection);
        Assert.NotNull(connection.Connection);
        Assert.NotEqual(Guid.Empty, connection.Id);

        await pool.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_ShouldReuseReleasedConnection()
    {
        // Arrange
        var options = new ConnectionPoolOptions
        {
            MinPoolSize = 0,
            MaxPoolSize = 10,
            ConnectionTimeout = TimeSpan.FromSeconds(5)
        };
        var pool = new ConnectionPoolManager<TestConnection>(
            _ => ValueTask.FromResult(new TestConnection()),
            options);

        // Act
        var connection1 = await pool.AcquireAsync();
        var firstId = connection1.Id;
        await pool.ReleaseAsync(connection1);

        var connection2 = await pool.AcquireAsync();

        // Assert
        Assert.Equal(firstId, connection2.Id);

        await pool.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_ShouldThrowTimeoutExceptionWhenPoolIsExhausted()
    {
        // Arrange
        var options = new ConnectionPoolOptions
        {
            MinPoolSize = 0,
            MaxPoolSize = 1,
            ConnectionTimeout = TimeSpan.FromMilliseconds(100)
        };
        var pool = new ConnectionPoolManager<TestConnection>(
            _ => ValueTask.FromResult(new TestConnection()),
            options);

        // Act
        var connection1 = await pool.AcquireAsync();

        // Assert
        await Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await pool.AcquireAsync();
        });

        await pool.DisposeAsync();
    }

    [Fact]
    public async Task ReleaseAsync_ShouldThrowWhenConnectionIsNull()
    {
        // Arrange
        var options = new ConnectionPoolOptions
        {
            MinPoolSize = 0,
            MaxPoolSize = 10
        };
        var pool = new ConnectionPoolManager<TestConnection>(
            _ => ValueTask.FromResult(new TestConnection()),
            options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await pool.ReleaseAsync(null!);
        });

        await pool.DisposeAsync();
    }

    [Fact]
    public async Task GetMetrics_ShouldReturnCorrectActiveConnections()
    {
        // Arrange
        var options = new ConnectionPoolOptions
        {
            MinPoolSize = 0,
            MaxPoolSize = 10
        };
        var pool = new ConnectionPoolManager<TestConnection>(
            _ => ValueTask.FromResult(new TestConnection()),
            options);

        // Act
        var connection1 = await pool.AcquireAsync();
        var connection2 = await pool.AcquireAsync();
        var metrics = pool.GetMetrics();

        // Assert
        Assert.Equal(2, metrics.ActiveConnections);
        Assert.Equal(0, metrics.IdleConnections);
        Assert.Equal(2, metrics.TotalConnections);

        await pool.DisposeAsync();
    }

    [Fact]
    public async Task GetMetrics_ShouldReturnCorrectIdleConnections()
    {
        // Arrange
        var options = new ConnectionPoolOptions
        {
            MinPoolSize = 0,
            MaxPoolSize = 10
        };
        var pool = new ConnectionPoolManager<TestConnection>(
            _ => ValueTask.FromResult(new TestConnection()),
            options);

        // Act
        var connection1 = await pool.AcquireAsync();
        var connection2 = await pool.AcquireAsync();
        await pool.ReleaseAsync(connection1);
        var metrics = pool.GetMetrics();

        // Assert
        Assert.Equal(1, metrics.ActiveConnections);
        Assert.Equal(1, metrics.IdleConnections);
        Assert.Equal(2, metrics.TotalConnections);

        await pool.DisposeAsync();
    }

    [Fact]
    public async Task GetMetrics_ShouldTrackConnectionsCreated()
    {
        // Arrange
        var options = new ConnectionPoolOptions
        {
            MinPoolSize = 0,
            MaxPoolSize = 10
        };
        var pool = new ConnectionPoolManager<TestConnection>(
            _ => ValueTask.FromResult(new TestConnection()),
            options);

        // Act
        var connection1 = await pool.AcquireAsync();
        var connection2 = await pool.AcquireAsync();
        var metrics = pool.GetMetrics();

        // Assert
        Assert.Equal(2, metrics.TotalConnectionsCreated);

        await pool.DisposeAsync();
    }

    [Fact]
    public async Task Constructor_ShouldThrowWhenConnectionFactoryIsNull()
    {
        // Arrange
        var options = new ConnectionPoolOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new ConnectionPoolManager<TestConnection>(null!, options);
        });
    }

    [Fact]
    public async Task Constructor_ShouldThrowWhenOptionsIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new ConnectionPoolManager<TestConnection>(
                _ => ValueTask.FromResult(new TestConnection()),
                null!);
        });
    }

    [Fact]
    public async Task Constructor_ShouldThrowWhenMinPoolSizeIsNegative()
    {
        // Arrange
        var options = new ConnectionPoolOptions
        {
            MinPoolSize = -1,
            MaxPoolSize = 10
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
        {
            new ConnectionPoolManager<TestConnection>(
                _ => ValueTask.FromResult(new TestConnection()),
                options);
        });
    }

    [Fact]
    public async Task Constructor_ShouldThrowWhenMaxPoolSizeIsLessThanMinPoolSize()
    {
        // Arrange
        var options = new ConnectionPoolOptions
        {
            MinPoolSize = 10,
            MaxPoolSize = 5
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
        {
            new ConnectionPoolManager<TestConnection>(
                _ => ValueTask.FromResult(new TestConnection()),
                options);
        });
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeAllConnections()
    {
        // Arrange
        var disposedConnections = new List<TestConnection>();
        var options = new ConnectionPoolOptions
        {
            MinPoolSize = 0,
            MaxPoolSize = 10
        };
        var pool = new ConnectionPoolManager<TestConnection>(
            _ => ValueTask.FromResult(new TestConnection()),
            options,
            connectionDisposer: conn =>
            {
                conn.IsDisposed = true;
                disposedConnections.Add(conn);
                return ValueTask.CompletedTask;
            });

        var connection1 = await pool.AcquireAsync();
        var connection2 = await pool.AcquireAsync();

        // Act
        await pool.DisposeAsync();

        // Assert
        Assert.Equal(2, disposedConnections.Count);
        Assert.All(disposedConnections, c => Assert.True(c.IsDisposed));
    }

    [Fact]
    public async Task AcquireAsync_ShouldThrowWhenPoolIsDisposed()
    {
        // Arrange
        var options = new ConnectionPoolOptions
        {
            MinPoolSize = 0,
            MaxPoolSize = 10
        };
        var pool = new ConnectionPoolManager<TestConnection>(
            _ => ValueTask.FromResult(new TestConnection()),
            options);

        await pool.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            await pool.AcquireAsync();
        });
    }

    [Fact]
    public async Task AcquireAsync_ShouldValidateConnectionWhenValidatorProvided()
    {
        // Arrange
        var validationCallCount = 0;
        var options = new ConnectionPoolOptions
        {
            MinPoolSize = 0,
            MaxPoolSize = 10
        };
        var pool = new ConnectionPoolManager<TestConnection>(
            _ => ValueTask.FromResult(new TestConnection()),
            options,
            connectionValidator: conn =>
            {
                validationCallCount++;
                return ValueTask.FromResult(true);
            });

        // Act
        var connection1 = await pool.AcquireAsync();
        await pool.ReleaseAsync(connection1);
        var connection2 = await pool.AcquireAsync();

        // Assert
        Assert.True(validationCallCount > 0);

        await pool.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_ShouldCreateNewConnectionWhenValidationFails()
    {
        // Arrange
        var callCount = 0;
        var options = new ConnectionPoolOptions
        {
            MinPoolSize = 0,
            MaxPoolSize = 10
        };
        var pool = new ConnectionPoolManager<TestConnection>(
            _ => ValueTask.FromResult(new TestConnection()),
            options,
            connectionValidator: conn =>
            {
                callCount++;
                return ValueTask.FromResult(callCount > 1); // First validation fails
            });

        // Act
        var connection1 = await pool.AcquireAsync();
        var firstId = connection1.Id;
        await pool.ReleaseAsync(connection1);
        var connection2 = await pool.AcquireAsync();

        // Assert
        Assert.NotEqual(firstId, connection2.Id);

        await pool.DisposeAsync();
    }
}
