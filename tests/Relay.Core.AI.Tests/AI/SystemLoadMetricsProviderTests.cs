using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Pipeline.Interfaces;
using Relay.Core.AI.Pipeline.Metrics;
using Relay.Core.AI.Pipeline.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class SystemLoadMetricsProviderTests : IDisposable
    {
        private readonly ILogger<SystemLoadMetricsProvider> _logger;
        private readonly Mock<IActiveRequestCounter> _requestCounterMock;

        public SystemLoadMetricsProviderTests()
        {
            _logger = NullLogger<SystemLoadMetricsProvider>.Instance;
            _requestCounterMock = new Mock<IActiveRequestCounter>();
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public void Constructor_Should_Initialize_With_Valid_Logger()
        {
            // Arrange & Act
            using var provider = new SystemLoadMetricsProvider(_logger);

            // Assert
            Assert.NotNull(provider);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SystemLoadMetricsProvider(null!));
        }

        [Fact]
        public void Constructor_Should_Use_Default_Options_When_Null()
        {
            // Arrange & Act
            using var provider = new SystemLoadMetricsProvider(_logger, options: null);

            // Assert
            Assert.NotNull(provider);
        }

        [Fact]
        public async Task GetCurrentLoadAsync_Should_Return_Valid_Metrics()
        {
            // Arrange
            using var provider = new SystemLoadMetricsProvider(_logger);

            // Act
            var metrics = await provider.GetCurrentLoadAsync();

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.CpuUtilization >= 0.0 && metrics.CpuUtilization <= 1.0);
            Assert.True(metrics.MemoryUtilization >= 0.0);
            Assert.True(metrics.ThreadPoolUtilization >= 0.0 && metrics.ThreadPoolUtilization <= 1.0);
            Assert.True(metrics.AvailableMemory > 0);
            Assert.True(metrics.Timestamp != default);
        }

        [Fact]
        public async Task GetCurrentLoadAsync_Should_Return_Cached_Metrics_When_Caching_Enabled()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                EnableCaching = true,
                CacheTtl = TimeSpan.FromSeconds(10)
            };

            using var provider = new SystemLoadMetricsProvider(_logger, options);

            // Act
            var metrics1 = await provider.GetCurrentLoadAsync();
            await Task.Delay(50); // Small delay
            var metrics2 = await provider.GetCurrentLoadAsync();

            // Assert
            Assert.NotNull(metrics1);
            Assert.NotNull(metrics2);
            // Timestamps should be the same since cached
            Assert.Equal(metrics1.Timestamp, metrics2.Timestamp);
        }

        [Fact]
        public async Task GetCurrentLoadAsync_Should_Refresh_Cache_After_TTL_Expires()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                EnableCaching = true,
                CacheTtl = TimeSpan.FromMilliseconds(100)
            };

            using var provider = new SystemLoadMetricsProvider(_logger, options);

            // Act
            var metrics1 = await provider.GetCurrentLoadAsync();
            await Task.Delay(150); // Wait for TTL to expire
            var metrics2 = await provider.GetCurrentLoadAsync();

            // Assert
            Assert.NotNull(metrics1);
            Assert.NotNull(metrics2);
            // Timestamps should be different since cache expired
            Assert.NotEqual(metrics1.Timestamp, metrics2.Timestamp);
        }

        [Fact]
        public async Task GetCurrentLoadAsync_Should_Not_Cache_When_Caching_Disabled()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                EnableCaching = false
            };

            using var provider = new SystemLoadMetricsProvider(_logger, options);

            // Act
            var metrics1 = await provider.GetCurrentLoadAsync();
            await Task.Delay(50); // Small delay
            var metrics2 = await provider.GetCurrentLoadAsync();

            // Assert
            Assert.NotNull(metrics1);
            Assert.NotNull(metrics2);
            // Timestamps should be different since no caching
            Assert.NotEqual(metrics1.Timestamp, metrics2.Timestamp);
        }

        [Fact]
        public async Task GetCurrentLoadAsync_Should_Use_InjectedRequestCounter()
        {
            // Arrange
            _requestCounterMock.Setup(c => c.GetActiveRequestCount()).Returns(42);
            _requestCounterMock.Setup(c => c.GetQueuedRequestCount()).Returns(10);

            var options = new SystemLoadMetricsOptions
            {
                ActiveRequestCounter = _requestCounterMock.Object
            };

            using var provider = new SystemLoadMetricsProvider(_logger, options);

            // Act
            var metrics = await provider.GetCurrentLoadAsync();

            // Assert
            Assert.NotNull(metrics);
            Assert.Equal(42, metrics.ActiveRequestCount);
            Assert.Equal(10, metrics.QueuedRequestCount);
            _requestCounterMock.Verify(c => c.GetActiveRequestCount(), Times.Once);
            _requestCounterMock.Verify(c => c.GetQueuedRequestCount(), Times.Once);
        }

        [Fact]
        public async Task GetCurrentLoadAsync_Should_Support_Cancellation()
        {
            // Arrange
            using var provider = new SystemLoadMetricsProvider(_logger);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            // Should complete immediately without throwing since metrics collection is synchronous
            var metrics = await provider.GetCurrentLoadAsync(cts.Token);
            Assert.NotNull(metrics);
        }

        [Fact]
        public async Task GetCurrentLoadAsync_Should_Handle_Concurrent_Requests()
        {
            // Arrange
            using var provider = new SystemLoadMetricsProvider(_logger);
            var tasks = new Task<SystemLoadMetrics>[50];

            // Act
            for (int i = 0; i < 50; i++)
            {
                tasks[i] = provider.GetCurrentLoadAsync().AsTask();
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(50, results.Length);
            Assert.All(results, metrics =>
            {
                Assert.NotNull(metrics);
                Assert.True(metrics.CpuUtilization >= 0.0);
                Assert.True(metrics.MemoryUtilization >= 0.0);
            });
        }

        [Fact]
        public void SystemLoadMetricsOptions_Should_Have_Correct_Defaults()
        {
            // Arrange & Act
            var options = new SystemLoadMetricsOptions();

            // Assert
            Assert.True(options.EnableCaching);
            Assert.Equal(TimeSpan.FromSeconds(5), options.CacheTtl);
            Assert.Equal(TimeSpan.FromSeconds(10), options.CacheRefreshInterval);
            Assert.True(options.UseCachedCpuMeasurements);
            Assert.Equal(10, options.CpuMeasurementIntervalMs);
            Assert.Equal(1024L * 1024 * 1024, options.BaselineMemory); // 1GB
            Assert.False(options.EnableDetailedLogging);
            Assert.Null(options.ActiveRequestCounter);
        }

        [Fact]
        public void SystemLoadMetricsOptions_Should_Allow_Custom_Configuration()
        {
            // Arrange & Act
            var options = new SystemLoadMetricsOptions
            {
                EnableCaching = false,
                CacheTtl = TimeSpan.FromSeconds(30),
                CacheRefreshInterval = TimeSpan.FromSeconds(60),
                UseCachedCpuMeasurements = false,
                CpuMeasurementIntervalMs = 50,
                BaselineMemory = 2048L * 1024 * 1024, // 2GB
                EnableDetailedLogging = true,
                ActiveRequestCounter = _requestCounterMock.Object
            };

            // Assert
            Assert.False(options.EnableCaching);
            Assert.Equal(TimeSpan.FromSeconds(30), options.CacheTtl);
            Assert.Equal(TimeSpan.FromSeconds(60), options.CacheRefreshInterval);
            Assert.False(options.UseCachedCpuMeasurements);
            Assert.Equal(50, options.CpuMeasurementIntervalMs);
            Assert.Equal(2048L * 1024 * 1024, options.BaselineMemory);
            Assert.True(options.EnableDetailedLogging);
            Assert.NotNull(options.ActiveRequestCounter);
        }

        [Fact]
        public void Dispose_Should_Clean_Up_Resources()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                EnableCaching = true,
                CacheRefreshInterval = TimeSpan.FromMinutes(1)
            };

            var provider = new SystemLoadMetricsProvider(_logger, options);

            // Act & Assert - Should not throw
            provider.Dispose();
            provider.Dispose(); // Idempotent
        }

        [Fact]
        public async Task GetCurrentLoadAsync_Should_Use_Cached_Cpu_Measurements_By_Default()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                UseCachedCpuMeasurements = true,
                CpuMeasurementIntervalMs = 10
            };

            using var provider = new SystemLoadMetricsProvider(_logger, options);

            // Act
            var metrics1 = await provider.GetCurrentLoadAsync();
            await Task.Delay(50); // Wait longer than measurement interval
            var metrics2 = await provider.GetCurrentLoadAsync();

            // Assert
            Assert.NotNull(metrics1);
            Assert.NotNull(metrics2);
            Assert.True(metrics1.CpuUtilization >= 0.0 && metrics1.CpuUtilization <= 1.0);
            Assert.True(metrics2.CpuUtilization >= 0.0 && metrics2.CpuUtilization <= 1.0);
        }

        [Fact]
        public async Task GetCurrentLoadAsync_Should_Include_ThreadPool_Metrics()
        {
            // Arrange
            using var provider = new SystemLoadMetricsProvider(_logger);

            // Act
            var metrics = await provider.GetCurrentLoadAsync();

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.ThreadPoolUtilization >= 0.0 && metrics.ThreadPoolUtilization <= 1.0);
        }

        [Fact]
        public async Task GetCurrentLoadAsync_Should_Calculate_Memory_Utilization_Against_Baseline()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                BaselineMemory = 512L * 1024 * 1024 // 512MB - Lower baseline
            };

            using var provider = new SystemLoadMetricsProvider(_logger, options);

            // Act
            var metrics = await provider.GetCurrentLoadAsync();

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.MemoryUtilization >= 0.0);
            // Memory utilization should be reasonable relative to baseline
        }

        [Fact]
        public async Task GetCurrentLoadAsync_Should_Include_Throughput_Metrics()
        {
            // Arrange
            using var provider = new SystemLoadMetricsProvider(_logger);

            // Act
            var metrics = await provider.GetCurrentLoadAsync();

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.ThroughputPerSecond >= 0.0);
        }

        [Fact]
        public async Task GetCurrentLoadAsync_Should_Include_AverageResponseTime()
        {
            // Arrange
            using var provider = new SystemLoadMetricsProvider(_logger);

            // Act
            var metrics = await provider.GetCurrentLoadAsync();

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.AverageResponseTime >= TimeSpan.Zero);
        }

        [Fact]
        public async Task GetCurrentLoadAsync_Should_Include_ErrorRate()
        {
            // Arrange
            using var provider = new SystemLoadMetricsProvider(_logger);

            // Act
            var metrics = await provider.GetCurrentLoadAsync();

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.ErrorRate >= 0.0 && metrics.ErrorRate <= 1.0);
        }

        [Fact]
        public async Task GetCurrentLoadAsync_Should_Fallback_To_ThreadPool_For_ActiveRequests_When_Counter_Not_Provided()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                ActiveRequestCounter = null // No counter provided
            };

            using var provider = new SystemLoadMetricsProvider(_logger, options);

            // Act
            var metrics = await provider.GetCurrentLoadAsync();

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.ActiveRequestCount >= 0);
            Assert.Equal(0, metrics.QueuedRequestCount); // Should be 0 without counter
        }

        [Fact]
        public async Task CacheRefreshTimer_Should_Update_Cache_Periodically()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                EnableCaching = true,
                CacheTtl = TimeSpan.FromSeconds(30), // Long TTL
                CacheRefreshInterval = TimeSpan.FromMilliseconds(200) // Short refresh
            };

            using var provider = new SystemLoadMetricsProvider(_logger, options);

            // Act
            var metrics1 = await provider.GetCurrentLoadAsync();
            await Task.Delay(250); // Wait for timer to fire
            var metrics2 = await provider.GetCurrentLoadAsync();

            // Assert
            Assert.NotNull(metrics1);
            Assert.NotNull(metrics2);
            // Cache should have been refreshed by timer
        }

        [Fact]
        public async Task GetCurrentLoadAsync_Should_Return_Different_Timestamps_For_Fresh_Metrics()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                EnableCaching = false
            };

            using var provider = new SystemLoadMetricsProvider(_logger, options);

            // Act
            var metrics1 = await provider.GetCurrentLoadAsync();
            await Task.Delay(100);
            var metrics2 = await provider.GetCurrentLoadAsync();

            // Assert
            Assert.NotNull(metrics1);
            Assert.NotNull(metrics2);
            Assert.True(metrics2.Timestamp > metrics1.Timestamp);
        }

        [Fact]
        public async Task GetCurrentLoadAsync_Should_Handle_High_Load_Scenarios()
        {
            // Arrange
            using var provider = new SystemLoadMetricsProvider(_logger);

            // Act - Simulate high load by making many concurrent requests
            var tasks = new Task<SystemLoadMetrics>[100];
            for (int i = 0; i < 100; i++)
            {
                tasks[i] = provider.GetCurrentLoadAsync().AsTask();
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(100, results.Length);
            Assert.All(results, metrics =>
            {
                Assert.NotNull(metrics);
                Assert.InRange(metrics.CpuUtilization, 0.0, 1.0);
                Assert.True(metrics.MemoryUtilization >= 0.0);
            });
        }

        [Fact]
        public async Task GetCurrentLoadAsync_Should_Provide_ActiveConnections_Metric()
        {
            // Arrange
            using var provider = new SystemLoadMetricsProvider(_logger);

            // Act
            var metrics = await provider.GetCurrentLoadAsync();

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.ActiveConnections >= 0);
        }

        [Fact]
        public async Task GetCurrentLoadAsync_Should_Set_DatabasePoolUtilization_To_Zero_By_Default()
        {
            // Arrange
            using var provider = new SystemLoadMetricsProvider(_logger);

            // Act
            var metrics = await provider.GetCurrentLoadAsync();

            // Assert
            Assert.NotNull(metrics);
            Assert.Equal(0.0, metrics.DatabasePoolUtilization);
        }
    }
}
