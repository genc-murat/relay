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
    }
}