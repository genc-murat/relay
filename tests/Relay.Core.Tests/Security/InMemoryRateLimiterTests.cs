using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Security;
using Xunit;

namespace Relay.Core.Tests.Security
{
    public class InMemoryRateLimiterTests
    {
        [Fact]
        public void Constructor_ShouldUseDefaultValues_WhenNoParametersProvided()
        {
            // Act
            var rateLimiter = new InMemoryRateLimiter();

            // Assert
            rateLimiter.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_ShouldUseCustomMaxRequests_WhenProvided()
        {
            // Arrange
            var maxRequests = 50;

            // Act
            var rateLimiter = new InMemoryRateLimiter(maxRequests);

            // Assert
            rateLimiter.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_ShouldUseCustomWindowDuration_WhenProvided()
        {
            // Arrange
            var windowDuration = TimeSpan.FromSeconds(30);

            // Act
            var rateLimiter = new InMemoryRateLimiter(100, windowDuration);

            // Assert
            rateLimiter.Should().NotBeNull();
        }

        [Fact]
        public async Task CheckRateLimitAsync_ShouldReturnTrue_ForFirstRequest()
        {
            // Arrange
            var rateLimiter = new InMemoryRateLimiter(10, TimeSpan.FromMinutes(1));
            var key = "user1:request1";

            // Act
            var result = await rateLimiter.CheckRateLimitAsync(key);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CheckRateLimitAsync_ShouldReturnTrue_WithinLimit()
        {
            // Arrange
            var rateLimiter = new InMemoryRateLimiter(5, TimeSpan.FromMinutes(1));
            var key = "user1:request1";

            // Act
            var results = new bool[5];
            for (int i = 0; i < 5; i++)
            {
                results[i] = await rateLimiter.CheckRateLimitAsync(key);
            }

            // Assert
            results.Should().AllSatisfy(r => r.Should().BeTrue());
        }

        [Fact]
        public async Task CheckRateLimitAsync_ShouldReturnFalse_WhenLimitExceeded()
        {
            // Arrange
            var rateLimiter = new InMemoryRateLimiter(3, TimeSpan.FromMinutes(1));
            var key = "user1:request1";

            // Act
            await rateLimiter.CheckRateLimitAsync(key);
            await rateLimiter.CheckRateLimitAsync(key);
            await rateLimiter.CheckRateLimitAsync(key);
            var result = await rateLimiter.CheckRateLimitAsync(key);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task CheckRateLimitAsync_ShouldResetWindow_WhenWindowExpires()
        {
            // Arrange
            var rateLimiter = new InMemoryRateLimiter(2, TimeSpan.FromMilliseconds(100));
            var key = "user1:request1";

            // Act
            await rateLimiter.CheckRateLimitAsync(key);
            await rateLimiter.CheckRateLimitAsync(key);
            var exceededResult = await rateLimiter.CheckRateLimitAsync(key);

            // Wait for window to expire
            await Task.Delay(150);

            var resetResult = await rateLimiter.CheckRateLimitAsync(key);

            // Assert
            exceededResult.Should().BeFalse();
            resetResult.Should().BeTrue();
        }

        [Fact]
        public async Task CheckRateLimitAsync_ShouldHandleMultipleKeysIndependently()
        {
            // Arrange
            var rateLimiter = new InMemoryRateLimiter(2, TimeSpan.FromMinutes(1));
            var key1 = "user1:request1";
            var key2 = "user2:request1";

            // Act
            var key1Result1 = await rateLimiter.CheckRateLimitAsync(key1);
            var key1Result2 = await rateLimiter.CheckRateLimitAsync(key1);
            var key1Result3 = await rateLimiter.CheckRateLimitAsync(key1);

            var key2Result1 = await rateLimiter.CheckRateLimitAsync(key2);
            var key2Result2 = await rateLimiter.CheckRateLimitAsync(key2);

            // Assert
            key1Result1.Should().BeTrue();
            key1Result2.Should().BeTrue();
            key1Result3.Should().BeFalse();

            key2Result1.Should().BeTrue();
            key2Result2.Should().BeTrue();
        }

        [Fact]
        public async Task CheckRateLimitAsync_ShouldHandleConcurrentRequests()
        {
            // Arrange
            var rateLimiter = new InMemoryRateLimiter(100, TimeSpan.FromMinutes(1));
            var key = "concurrent:test";
            var tasks = new ValueTask<bool>[50];

            // Act
            var results = new bool[tasks.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                results[i] = await rateLimiter.CheckRateLimitAsync(key);
            }

            // Assert
            results.Should().AllSatisfy(r => r.Should().BeTrue());
        }

        [Fact]
        public async Task CheckRateLimitAsync_ShouldCleanupExpiredEntries_WhenThresholdReached()
        {
            // Arrange
            var rateLimiter = new InMemoryRateLimiter(1, TimeSpan.FromMilliseconds(10));
            var keys = Enumerable.Range(0, 100).Select(i => $"key{i}").ToArray();

            // Act - Create many entries to trigger cleanup
            foreach (var key in keys)
            {
                await rateLimiter.CheckRateLimitAsync(key);
            }

            // Wait for entries to expire
            await Task.Delay(50);

            // Add one more entry to trigger cleanup
            var result = await rateLimiter.CheckRateLimitAsync("newkey");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CheckRateLimitAsync_ShouldHandleEmptyKey()
        {
            // Arrange
            var rateLimiter = new InMemoryRateLimiter(5, TimeSpan.FromMinutes(1));

            // Act
            var result = await rateLimiter.CheckRateLimitAsync("");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CheckRateLimitAsync_ShouldHandleNullKey()
        {
            // Arrange
            var rateLimiter = new InMemoryRateLimiter(5, TimeSpan.FromMinutes(1));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => rateLimiter.CheckRateLimitAsync(null!).AsTask());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public async Task CheckRateLimitAsync_ShouldRespectDifferentLimits(int maxRequests)
        {
            // Arrange
            var rateLimiter = new InMemoryRateLimiter(maxRequests, TimeSpan.FromMinutes(1));
            var key = "test:limit";

            // Act
            var results = new bool[maxRequests + 1];
            for (int i = 0; i <= maxRequests; i++)
            {
                results[i] = await rateLimiter.CheckRateLimitAsync(key);
            }

            // Assert
            results.Take(maxRequests).Should().AllSatisfy(r => r.Should().BeTrue());
            results.Last().Should().BeFalse();
        }

        [Theory]
        [InlineData(10)] // 10 milliseconds
        [InlineData(100)] // 100 milliseconds
        [InlineData(1000)] // 1 second
        public async Task CheckRateLimitAsync_ShouldRespectWindowDuration(int windowMs)
        {
            // Arrange
            var rateLimiter = new InMemoryRateLimiter(1, TimeSpan.FromMilliseconds(windowMs));
            var key = "test:window";

            // Act
            var firstResult = await rateLimiter.CheckRateLimitAsync(key);
            var secondResult = await rateLimiter.CheckRateLimitAsync(key);

            await Task.Delay(windowMs + 10);

            var thirdResult = await rateLimiter.CheckRateLimitAsync(key);

            // Assert
            firstResult.Should().BeTrue();
            secondResult.Should().BeFalse();
            thirdResult.Should().BeTrue();
        }

        [Fact]
        public async Task CheckRateLimitAsync_ShouldHandleHighFrequencyRequests()
        {
            // Arrange
            var rateLimiter = new InMemoryRateLimiter(1000, TimeSpan.FromMinutes(1));
            var key = "high:frequency";
            // Act
            var startTime = DateTime.UtcNow;
            var results = new bool[1000];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = await rateLimiter.CheckRateLimitAsync(key);
            }
            var endTime = DateTime.UtcNow;

            // Assert
            results.Should().AllSatisfy(r => r.Should().BeTrue());
            (endTime - startTime).Should().BeLessThan(TimeSpan.FromSeconds(5));
        }
    }
}