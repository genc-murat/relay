using Relay.Core.AI.Pipeline.Options;
using System;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AICachingOptimizationBehaviorOptionsTests
    {
        [Fact]
        public void AICachingOptimizationOptions_Should_Have_Correct_Defaults()
        {
            // Arrange & Act
            var options = new AICachingOptimizationOptions();

            // Assert
            Assert.True(options.EnableCaching);
            Assert.Equal(0.7, options.MinConfidenceScore);
            Assert.Equal(10.0, options.MinExecutionTimeForCaching);
            Assert.Equal(1024 * 1024, options.MaxCacheSizeBytes);
            Assert.Equal(TimeSpan.FromMinutes(10), options.DefaultCacheTtl);
            Assert.Equal(TimeSpan.FromMinutes(1), options.MinCacheTtl);
            Assert.Equal(TimeSpan.FromHours(1), options.MaxCacheTtl);
            Assert.NotNull(options.SerializerOptions);
        }

        [Fact]
        public void AICachingOptimizationOptions_Should_Allow_Custom_Configuration()
        {
            // Arrange & Act
            var options = new AICachingOptimizationOptions
            {
                EnableCaching = false,
                MinConfidenceScore = 0.8,
                MinExecutionTimeForCaching = 50.0,
                MaxCacheSizeBytes = 512 * 1024,
                DefaultCacheTtl = TimeSpan.FromMinutes(5),
                MinCacheTtl = TimeSpan.FromSeconds(30),
                MaxCacheTtl = TimeSpan.FromMinutes(30)
            };

            // Assert
            Assert.False(options.EnableCaching);
            Assert.Equal(0.8, options.MinConfidenceScore);
            Assert.Equal(50.0, options.MinExecutionTimeForCaching);
            Assert.Equal(512 * 1024, options.MaxCacheSizeBytes);
            Assert.Equal(TimeSpan.FromMinutes(5), options.DefaultCacheTtl);
            Assert.Equal(TimeSpan.FromSeconds(30), options.MinCacheTtl);
            Assert.Equal(TimeSpan.FromMinutes(30), options.MaxCacheTtl);
        }
    }
}