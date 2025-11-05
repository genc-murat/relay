using System;
using System.Linq;
using Xunit;
using Relay.Core.AI;

namespace Relay.Core.Tests.AI
{
    public class CachingAnalysisDataTests
    {
        [Fact]
        public void CleanupOldAccessPatterns_EmptyList_ReturnsZero()
        {
            // Arrange
            var data = new CachingAnalysisData();

            // Act
            var removedCount = data.CleanupOldAccessPatterns(DateTime.UtcNow);

            // Assert
            Assert.Equal(0, removedCount);
            Assert.Equal(0, data.AccessPatternsCount);
        }

        [Fact]
        public void CleanupOldAccessPatterns_AllPatternsNew_ReturnsZero()
        {
            // Arrange
            var data = new CachingAnalysisData();
            var patterns = new[]
            {
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-1), WasCacheHit = true },
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-2), WasCacheHit = false },
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-5), WasCacheHit = true }
            };
            data.AddAccessPatterns(patterns);

            // Act
            var removedCount = data.CleanupOldAccessPatterns(DateTime.UtcNow.AddMinutes(-10));

            // Assert
            Assert.Equal(0, removedCount);
            Assert.Equal(3, data.AccessPatternsCount);
        }

        [Fact]
        public void CleanupOldAccessPatterns_AllPatternsOld_ReturnsAllRemoved()
        {
            // Arrange
            var data = new CachingAnalysisData();
            var patterns = new[]
            {
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-15), WasCacheHit = true },
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-20), WasCacheHit = false },
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-25), WasCacheHit = true }
            };
            data.AddAccessPatterns(patterns);

            // Act
            var removedCount = data.CleanupOldAccessPatterns(DateTime.UtcNow.AddMinutes(-10));

            // Assert
            Assert.Equal(3, removedCount);
            Assert.Equal(0, data.AccessPatternsCount);
        }

        [Fact]
        public void CleanupOldAccessPatterns_MixedPatterns_RemovesOnlyOldOnes()
        {
            // Arrange
            var data = new CachingAnalysisData();
            var patterns = new[]
            {
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-5), WasCacheHit = true },  // Keep
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-15), WasCacheHit = false }, // Remove
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-8), WasCacheHit = true },  // Keep
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-20), WasCacheHit = false }, // Remove
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-3), WasCacheHit = true }   // Keep
            };
            data.AddAccessPatterns(patterns);

            // Act
            var removedCount = data.CleanupOldAccessPatterns(DateTime.UtcNow.AddMinutes(-10));

            // Assert
            Assert.Equal(2, removedCount);
            Assert.Equal(3, data.AccessPatternsCount);
        }

        [Fact]
        public void CleanupOldAccessPatterns_UpdatesStatisticsCorrectly()
        {
            // Arrange
            var data = new CachingAnalysisData();
            var patterns = new[]
            {
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-5), WasCacheHit = true },  // Keep
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-15), WasCacheHit = false }, // Remove
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-8), WasCacheHit = true },  // Keep
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-20), WasCacheHit = false }, // Remove
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-3), WasCacheHit = true }   // Keep
            };
            data.AddAccessPatterns(patterns);

            // Initial state: 5 total, 3 hits, hit rate = 0.6
            Assert.Equal(5, data.TotalAccesses);
            Assert.Equal(3, data.CacheHits);
            Assert.Equal(0.6, data.CacheHitRate);

            // Act
            data.CleanupOldAccessPatterns(DateTime.UtcNow.AddMinutes(-10));

            // Assert - After cleanup: 3 total, 3 hits, hit rate = 1.0
            Assert.Equal(3, data.TotalAccesses);
            Assert.Equal(3, data.CacheHits);
            Assert.Equal(1.0, data.CacheHitRate);
        }

        [Fact]
        public void CleanupOldAccessPatterns_UpdatesStatistics_EmptyAfterCleanup()
        {
            // Arrange
            var data = new CachingAnalysisData();
            var patterns = new[]
            {
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-15), WasCacheHit = true },
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-20), WasCacheHit = false }
            };
            data.AddAccessPatterns(patterns);

            // Act
            data.CleanupOldAccessPatterns(DateTime.UtcNow.AddMinutes(-10));

            // Assert
            Assert.Equal(0, data.TotalAccesses);
            Assert.Equal(0, data.CacheHits);
            Assert.Equal(0.0, data.CacheHitRate);
        }

        [Fact]
        public void CleanupOldAccessPatterns_UpdatesStatistics_AllCacheHitsRemoved()
        {
            // Arrange
            var data = new CachingAnalysisData();
            var patterns = new[]
            {
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-5), WasCacheHit = false }, // Keep
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-15), WasCacheHit = true },  // Remove
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-20), WasCacheHit = true }   // Remove
            };
            data.AddAccessPatterns(patterns);

            // Act
            data.CleanupOldAccessPatterns(DateTime.UtcNow.AddMinutes(-10));

            // Assert - After cleanup: 1 total, 0 hits, hit rate = 0.0
            Assert.Equal(1, data.TotalAccesses);
            Assert.Equal(0, data.CacheHits);
            Assert.Equal(0.0, data.CacheHitRate);
        }

        [Fact]
        public void CleanupOldAccessPatterns_ExactCutoffTime_KeepsEqualTimestamp()
        {
            // Arrange
            var cutoffTime = DateTime.UtcNow.AddMinutes(-10);
            var data = new CachingAnalysisData();
            var patterns = new[]
            {
                new AccessPattern { Timestamp = cutoffTime, WasCacheHit = true }, // Should be kept (equal to cutoff)
                new AccessPattern { Timestamp = cutoffTime.AddMilliseconds(1), WasCacheHit = false } // Should be kept
            };
            data.AddAccessPatterns(patterns);

            // Act
            var removedCount = data.CleanupOldAccessPatterns(cutoffTime);

            // Assert
            Assert.Equal(0, removedCount);
            Assert.Equal(2, data.AccessPatternsCount);
        }

        [Fact]
        public void CleanupOldAccessPatterns_FutureCutoffTime_RemovesAll()
        {
            // Arrange
            var data = new CachingAnalysisData();
            var patterns = new[]
            {
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-5), WasCacheHit = true },
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-10), WasCacheHit = false }
            };
            data.AddAccessPatterns(patterns);

            // Act - Future cutoff removes all
            var removedCount = data.CleanupOldAccessPatterns(DateTime.UtcNow.AddMinutes(1));

            // Assert
            Assert.Equal(2, removedCount);
            Assert.Equal(0, data.AccessPatternsCount);
        }

        [Fact]
        public void CleanupOldAccessPatterns_PastCutoffTime_RemovesNone()
        {
            // Arrange
            var data = new CachingAnalysisData();
            var patterns = new[]
            {
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-5), WasCacheHit = true },
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-10), WasCacheHit = false }
            };
            data.AddAccessPatterns(patterns);

            // Act - Past cutoff removes none
            var removedCount = data.CleanupOldAccessPatterns(DateTime.UtcNow.AddMinutes(-20));

            // Assert
            Assert.Equal(0, removedCount);
            Assert.Equal(2, data.AccessPatternsCount);
        }

        [Fact]
        public void CleanupOldAccessPatterns_LargeDataset_Performance()
        {
            // Arrange
            var data = new CachingAnalysisData();
            var patterns = new AccessPattern[1000];
            for (int i = 0; i < 1000; i++)
            {
                patterns[i] = new AccessPattern
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-i), // 0 to 999 minutes ago
                    WasCacheHit = i % 2 == 0 // Alternate hit/miss
                };
            }
            data.AddAccessPatterns(patterns);

            // Act - Remove patterns older than 500 minutes
            var removedCount = data.CleanupOldAccessPatterns(DateTime.UtcNow.AddMinutes(-500));

            // Assert
            Assert.Equal(500, removedCount); // Should remove 500 patterns (500-999 minutes ago)
            Assert.Equal(500, data.AccessPatternsCount);
        }

        [Fact]
        public void CleanupOldAccessPatterns_ConcurrentAccessPatterns_RemovesCorrectly()
        {
            // Arrange
            var data = new CachingAnalysisData();
            var baseTime = DateTime.UtcNow;
            var patterns = new[]
            {
                new AccessPattern { Timestamp = baseTime.AddMinutes(-1), WasCacheHit = true },
                new AccessPattern { Timestamp = baseTime.AddMinutes(-1), WasCacheHit = false }, // Same timestamp
                new AccessPattern { Timestamp = baseTime.AddMinutes(-1), WasCacheHit = true },
                new AccessPattern { Timestamp = baseTime.AddMinutes(-15), WasCacheHit = false },
                new AccessPattern { Timestamp = baseTime.AddMinutes(-15), WasCacheHit = true }
            };
            data.AddAccessPatterns(patterns);

            // Act
            var removedCount = data.CleanupOldAccessPatterns(baseTime.AddMinutes(-10));

            // Assert - Should remove the 2 patterns with -15 minutes
            Assert.Equal(2, removedCount);
            Assert.Equal(3, data.AccessPatternsCount);
        }

        [Fact]
        public void CleanupOldAccessPatterns_AfterMultipleCleanups_WorksCorrectly()
        {
            // Arrange
            var data = new CachingAnalysisData();
            var patterns = new[]
            {
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-5), WasCacheHit = true },
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-15), WasCacheHit = false },
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-25), WasCacheHit = true },
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-35), WasCacheHit = false }
            };
            data.AddAccessPatterns(patterns);

            // First cleanup - remove very old patterns
            var firstRemoved = data.CleanupOldAccessPatterns(DateTime.UtcNow.AddMinutes(-20));
            Assert.Equal(2, firstRemoved); // -25 and -35 minutes
            Assert.Equal(2, data.AccessPatternsCount);

            // Second cleanup - remove remaining old patterns
            var secondRemoved = data.CleanupOldAccessPatterns(DateTime.UtcNow.AddMinutes(-10));
            Assert.Equal(1, secondRemoved); // -15 minutes
            Assert.Equal(1, data.AccessPatternsCount);

            // Third cleanup - remove the remaining old pattern
            var thirdRemoved = data.CleanupOldAccessPatterns(DateTime.UtcNow.AddMinutes(-1));
            Assert.Equal(1, thirdRemoved); // -5 minutes is older than -1 minute
            Assert.Equal(0, data.AccessPatternsCount);
        }

        [Fact]
        public void CleanupOldAccessPatterns_ReturnsCorrectCount()
        {
            // Arrange
            var data = new CachingAnalysisData();
            var patterns = new[]
            {
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-1), WasCacheHit = true },
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-5), WasCacheHit = false },
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-10), WasCacheHit = true },
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-15), WasCacheHit = false },
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-20), WasCacheHit = true }
            };
            data.AddAccessPatterns(patterns);

            // Act
            var removedCount = data.CleanupOldAccessPatterns(DateTime.UtcNow.AddMinutes(-12));

            // Assert - Should remove 2 patterns (-15 and -20 minutes)
            Assert.Equal(2, removedCount);
        }

        [Fact]
        public void CleanupOldAccessPatterns_EdgeCase_SinglePatternRemoved()
        {
            // Arrange
            var data = new CachingAnalysisData();
            var patterns = new[]
            {
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-5), WasCacheHit = true },
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-15), WasCacheHit = false }
            };
            data.AddAccessPatterns(patterns);

            // Act
            var removedCount = data.CleanupOldAccessPatterns(DateTime.UtcNow.AddMinutes(-10));

            // Assert
            Assert.Equal(1, removedCount);
            Assert.Equal(1, data.AccessPatternsCount);
            Assert.Equal(1, data.TotalAccesses);
            Assert.Equal(1, data.CacheHits); // Only the cache hit remains
            Assert.Equal(1.0, data.CacheHitRate);
        }

        [Fact]
        public void CleanupOldAccessPatterns_EdgeCase_SinglePatternKept()
        {
            // Arrange
            var data = new CachingAnalysisData();
            var patterns = new[]
            {
                new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-5), WasCacheHit = true }
            };
            data.AddAccessPatterns(patterns);

            // Act
            var removedCount = data.CleanupOldAccessPatterns(DateTime.UtcNow.AddMinutes(-10));

            // Assert
            Assert.Equal(0, removedCount);
            Assert.Equal(1, data.AccessPatternsCount);
            Assert.Equal(1, data.TotalAccesses);
            Assert.Equal(1, data.CacheHits);
            Assert.Equal(1.0, data.CacheHitRate);
        }
    }
}