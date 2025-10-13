using Relay.Core.Security;
using System;
using System.Threading.Tasks;
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
            Assert.NotNull(rateLimiter);
        }

        [Fact]
        public void Constructor_ShouldUseCustomMaxRequests_WhenProvided()
        {
            // Arrange
            var maxRequests = 50;

            // Act
            var rateLimiter = new InMemoryRateLimiter(maxRequests);

            // Assert
            Assert.NotNull(rateLimiter);
        }

        [Fact]
        public void Constructor_ShouldUseCustomWindowDuration_WhenProvided()
        {
            // Arrange
            var windowDuration = TimeSpan.FromSeconds(30);

            // Act
            var rateLimiter = new InMemoryRateLimiter(100, windowDuration);

            // Assert
            Assert.NotNull(rateLimiter);
        }

        [Fact]
        public void Constructor_ShouldHandleZeroMaxRequests()
        {
            // Act
            var rateLimiter = new InMemoryRateLimiter(0);

            // Assert
            Assert.NotNull(rateLimiter);
        }

        [Fact]
        public void Constructor_ShouldHandleNegativeMaxRequests()
        {
            // Act
            var rateLimiter = new InMemoryRateLimiter(-1);

            // Assert
            Assert.NotNull(rateLimiter);
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
            Assert.True(result);
        }

        [Fact]
        public async Task CheckRateLimitAsync_ShouldHandleHighFrequencyRequests()
        {
            // Arrange
            var rateLimiter = new InMemoryRateLimiter(1000, TimeSpan.FromMinutes(1));
            var key = "high:frequency";
            var startTime = DateTime.UtcNow;
            var results = new bool[1000];

            // Act
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = await rateLimiter.CheckRateLimitAsync(key);
            }
            var endTime = DateTime.UtcNow;

            // Assert
            Assert.All(results, r => Assert.True(r));
            Assert.True((endTime - startTime) < TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task CheckRateLimitAsync_ShouldReturnFalse_WhenRateLimitExceeded()
        {
            // Arrange
            var rateLimiter = new InMemoryRateLimiter(1, TimeSpan.FromMinutes(1));
            var key = "user1:request1";

            // Act
            var firstResult = await rateLimiter.CheckRateLimitAsync(key);
            var secondResult = await rateLimiter.CheckRateLimitAsync(key);

            // Assert
            Assert.True(firstResult);
            Assert.False(secondResult);
        }

        [Fact]
        public async Task CheckRateLimitAsync_ShouldResetCount_AfterWindowExpires()
        {
            // Arrange
            var windowDuration = TimeSpan.FromMilliseconds(50);
            var rateLimiter = new InMemoryRateLimiter(1, windowDuration);
            var key = "user1:request1";

            // Act
            var firstResult = await rateLimiter.CheckRateLimitAsync(key);
            await Task.Delay(windowDuration.Add(TimeSpan.FromMilliseconds(10))); // Wait for window to expire
            var secondResult = await rateLimiter.CheckRateLimitAsync(key);

            // Assert
            Assert.True(firstResult);
            Assert.True(secondResult); // Should be allowed again after window reset
        }

        [Fact]
        public async Task CheckRateLimitAsync_ShouldHandleMultipleKeysIndependently()
        {
            // Arrange
            var rateLimiter = new InMemoryRateLimiter(1, TimeSpan.FromMinutes(1));
            var key1 = "user1:request1";
            var key2 = "user2:request1";

            // Act
            var result1First = await rateLimiter.CheckRateLimitAsync(key1);
            var result1Second = await rateLimiter.CheckRateLimitAsync(key1); // Should be false
            var result2First = await rateLimiter.CheckRateLimitAsync(key2); // Should be true for different key

            // Assert
            Assert.True(result1First);
            Assert.False(result1Second);
            Assert.True(result2First);
        }

        [Fact]
        public async Task CheckRateLimitAsync_ShouldReturnFalse_WhenMaxRequestsIsZero()
        {
            // Arrange
            var rateLimiter = new InMemoryRateLimiter(0, TimeSpan.FromMinutes(1));
            var key = "user1:request1";

            // Act
            var result = await rateLimiter.CheckRateLimitAsync(key);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CheckRateLimitAsync_ShouldThrowArgumentNullException_ForNullKey()
        {
            // Arrange
            var rateLimiter = new InMemoryRateLimiter();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => rateLimiter.CheckRateLimitAsync(null!));
        }

        [Fact]
        public async Task CheckRateLimitAsync_ShouldHandleEmptyStringKey()
        {
            // Arrange
            var rateLimiter = new InMemoryRateLimiter(1, TimeSpan.FromMinutes(1));
            var key = string.Empty;

            // Act
            var firstResult = await rateLimiter.CheckRateLimitAsync(key);
            var secondResult = await rateLimiter.CheckRateLimitAsync(key);

            // Assert
            Assert.True(firstResult);
            Assert.False(secondResult);
        }
    }
}