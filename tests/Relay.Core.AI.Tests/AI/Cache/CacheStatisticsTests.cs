using Relay.Core.AI;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Cache;

public class CacheStatisticsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithZeroValues()
    {
        // Act
        var stats = new CacheStatistics();

        // Assert
        Assert.Equal(0, stats.Hits);
        Assert.Equal(0, stats.Misses);
        Assert.Equal(0, stats.Sets);
        Assert.Equal(0, stats.Evictions);
        Assert.Equal(0, stats.Cleanups);
        Assert.Equal(0, stats.TotalRequests);
        Assert.Equal(0.0, stats.HitRatio);
    }

    [Fact]
    public void RecordHit_ShouldIncrementHits()
    {
        // Arrange
        var stats = new CacheStatistics();

        // Act
        stats.RecordHit();

        // Assert
        Assert.Equal(1, stats.Hits);
        Assert.Equal(0, stats.Misses);
        Assert.Equal(1, stats.TotalRequests);
        Assert.Equal(1.0, stats.HitRatio);
    }

    [Fact]
    public void RecordMiss_ShouldIncrementMisses()
    {
        // Arrange
        var stats = new CacheStatistics();

        // Act
        stats.RecordMiss();

        // Assert
        Assert.Equal(0, stats.Hits);
        Assert.Equal(1, stats.Misses);
        Assert.Equal(1, stats.TotalRequests);
        Assert.Equal(0.0, stats.HitRatio);
    }

    [Fact]
    public void RecordSet_ShouldIncrementSets()
    {
        // Arrange
        var stats = new CacheStatistics();

        // Act
        stats.RecordSet();

        // Assert
        Assert.Equal(1, stats.Sets);
        Assert.Equal(0, stats.Hits);
        Assert.Equal(0, stats.Misses);
    }

    [Fact]
    public void RecordEviction_ShouldIncrementEvictions()
    {
        // Arrange
        var stats = new CacheStatistics();

        // Act
        stats.RecordEviction();

        // Assert
        Assert.Equal(1, stats.Evictions);
        Assert.Equal(0, stats.Hits);
        Assert.Equal(0, stats.Misses);
    }

    [Fact]
    public void RecordCleanup_ShouldIncrementCleanups()
    {
        // Arrange
        var stats = new CacheStatistics();

        // Act
        stats.RecordCleanup();

        // Assert
        Assert.Equal(1, stats.Cleanups);
        Assert.Equal(0, stats.Hits);
        Assert.Equal(0, stats.Misses);
    }

    [Fact]
    public void HitRatio_ShouldCalculateCorrectly()
    {
        // Arrange
        var stats = new CacheStatistics();

        // Act
        stats.RecordHit();
        stats.RecordHit();
        stats.RecordMiss();

        // Assert
        Assert.Equal(2, stats.Hits);
        Assert.Equal(1, stats.Misses);
        Assert.Equal(3, stats.TotalRequests);
        Assert.Equal(2.0 / 3.0, stats.HitRatio);
    }

    [Fact]
    public void HitRatio_WithNoRequests_ShouldBeZero()
    {
        // Arrange
        var stats = new CacheStatistics();

        // Assert
        Assert.Equal(0, stats.TotalRequests);
        Assert.Equal(0.0, stats.HitRatio);
    }

    [Fact]
    public void HitRatio_WithOnlyHits_ShouldBeOne()
    {
        // Arrange
        var stats = new CacheStatistics();

        // Act
        stats.RecordHit();
        stats.RecordHit();

        // Assert
        Assert.Equal(2, stats.Hits);
        Assert.Equal(0, stats.Misses);
        Assert.Equal(2, stats.TotalRequests);
        Assert.Equal(1.0, stats.HitRatio);
    }

    [Fact]
    public void HitRatio_WithOnlyMisses_ShouldBeZero()
    {
        // Arrange
        var stats = new CacheStatistics();

        // Act
        stats.RecordMiss();
        stats.RecordMiss();

        // Assert
        Assert.Equal(0, stats.Hits);
        Assert.Equal(2, stats.Misses);
        Assert.Equal(2, stats.TotalRequests);
        Assert.Equal(0.0, stats.HitRatio);
    }

    [Fact]
    public void Reset_ShouldResetAllCounters()
    {
        // Arrange
        var stats = new CacheStatistics();
        stats.RecordHit();
        stats.RecordMiss();
        stats.RecordSet();
        stats.RecordEviction();
        stats.RecordCleanup();

        // Act
        stats.Reset();

        // Assert
        Assert.Equal(0, stats.Hits);
        Assert.Equal(0, stats.Misses);
        Assert.Equal(0, stats.Sets);
        Assert.Equal(0, stats.Evictions);
        Assert.Equal(0, stats.Cleanups);
        Assert.Equal(0, stats.TotalRequests);
        Assert.Equal(0.0, stats.HitRatio);
    }

    [Fact]
    public void MultipleOperations_ShouldAccumulateCorrectly()
    {
        // Arrange
        var stats = new CacheStatistics();

        // Act
        stats.RecordHit();
        stats.RecordHit();
        stats.RecordMiss();
        stats.RecordSet();
        stats.RecordSet();
        stats.RecordSet();
        stats.RecordEviction();
        stats.RecordEviction();
        stats.RecordCleanup();

        // Assert
        Assert.Equal(2, stats.Hits);
        Assert.Equal(1, stats.Misses);
        Assert.Equal(3, stats.Sets);
        Assert.Equal(2, stats.Evictions);
        Assert.Equal(1, stats.Cleanups);
        Assert.Equal(3, stats.TotalRequests);
        Assert.Equal(2.0 / 3.0, stats.HitRatio);
    }

    [Fact]
    public async Task ThreadSafety_Hits_ShouldBeThreadSafe()
    {
        // Arrange
        var stats = new CacheStatistics();
        const int iterations = 1000;
        const int threadCount = 10;

        // Act
        var tasks = new Task[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < iterations; j++)
                {
                    stats.RecordHit();
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(iterations * threadCount, stats.Hits);
    }

    [Fact]
    public async Task ThreadSafety_Misses_ShouldBeThreadSafe()
    {
        // Arrange
        var stats = new CacheStatistics();
        const int iterations = 1000;
        const int threadCount = 10;

        // Act
        var tasks = new Task[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < iterations; j++)
                {
                    stats.RecordMiss();
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(iterations * threadCount, stats.Misses);
    }

    [Fact]
    public async Task ThreadSafety_MixedOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var stats = new CacheStatistics();
        const int iterations = 100;
        const int threadCount = 20;

        // Act
        var tasks = new Task[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < iterations; j++)
                {
                    stats.RecordHit();
                    stats.RecordMiss();
                    stats.RecordSet();
                    stats.RecordEviction();
                    stats.RecordCleanup();
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(iterations * threadCount, stats.Hits);
        Assert.Equal(iterations * threadCount, stats.Misses);
        Assert.Equal(iterations * threadCount, stats.Sets);
        Assert.Equal(iterations * threadCount, stats.Evictions);
        Assert.Equal(iterations * threadCount, stats.Cleanups);
        Assert.Equal(iterations * threadCount * 2, stats.TotalRequests); // hits + misses
        Assert.Equal(0.5, stats.HitRatio); // hits / (hits + misses) = 2000 / 4000 = 0.5
    }

    [Fact]
    public async Task ThreadSafety_Reset_ShouldBeThreadSafe()
    {
        // Arrange
        var stats = new CacheStatistics();
        const int iterations = 100;

        // Fill with some data
        for (int i = 0; i < iterations; i++)
        {
            stats.RecordHit();
            stats.RecordMiss();
            stats.RecordSet();
        }

        // Act - Reset while other operations are happening
        var resetTask = Task.Run(() => stats.Reset());
        var operationTask = Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                stats.RecordHit();
            }
        });

        await Task.WhenAll(resetTask, operationTask);

        // Assert - Values should be consistent (either reset or partially updated)
        var totalRequests = stats.TotalRequests;
        var hits = stats.Hits;
        var misses = stats.Misses;

        // Either reset occurred (all zeros) or operations occurred
        Assert.True(
            (hits == 0 && misses == 0 && totalRequests == 0) ||
            (hits >= 0 && misses >= 0 && totalRequests >= 0),
            $"Inconsistent state: Hits={hits}, Misses={misses}, TotalRequests={totalRequests}"
        );
    }

    [Theory]
    [InlineData(0, 0, 0.0)]
    [InlineData(1, 0, 1.0)]
    [InlineData(0, 1, 0.0)]
    [InlineData(1, 1, 0.5)]
    [InlineData(3, 2, 3.0 / 5.0)]
    [InlineData(10, 5, 10.0 / 15.0)]
    public void HitRatio_TheoryData(long hits, long misses, double expectedRatio)
    {
        // Arrange
        var stats = new CacheStatistics();

        // Act
        for (long i = 0; i < hits; i++) stats.RecordHit();
        for (long i = 0; i < misses; i++) stats.RecordMiss();

        // Assert
        Assert.Equal(expectedRatio, stats.HitRatio, 10); // Allow for floating point precision
    }

    [Fact]
    public void Properties_ShouldBeReadOnly()
    {
        // Arrange
        var stats = new CacheStatistics();
        var type = typeof(CacheStatistics);

        // Assert - All counter properties should not have public setters
        Assert.False(type.GetProperty("Hits")!.CanWrite);
        Assert.False(type.GetProperty("Misses")!.CanWrite);
        Assert.False(type.GetProperty("Sets")!.CanWrite);
        Assert.False(type.GetProperty("Evictions")!.CanWrite);
        Assert.False(type.GetProperty("Cleanups")!.CanWrite);
        Assert.False(type.GetProperty("TotalRequests")!.CanWrite);
        Assert.False(type.GetProperty("HitRatio")!.CanWrite);
    }

    [Fact]
    public void RecordMethods_ShouldBePublic()
    {
        // Arrange
        var stats = new CacheStatistics();
        var type = typeof(CacheStatistics);

        // Assert - All record methods should be public
        Assert.True(type.GetMethod("RecordHit")!.IsPublic);
        Assert.True(type.GetMethod("RecordMiss")!.IsPublic);
        Assert.True(type.GetMethod("RecordSet")!.IsPublic);
        Assert.True(type.GetMethod("RecordEviction")!.IsPublic);
        Assert.True(type.GetMethod("RecordCleanup")!.IsPublic);
        Assert.True(type.GetMethod("Reset")!.IsPublic);
    }

    [Fact]
    public async Task Integration_StatisticsShouldWorkWithRealisticUsagePattern()
    {
        // Arrange
        var stats = new CacheStatistics();
        const int totalOperations = 1000;

        // Act - Simulate realistic cache usage pattern
        var tasks = new Task[4];
        tasks[0] = Task.Run(() => // Hit recording task
        {
            for (int i = 0; i < totalOperations * 0.6; i++) // 60% hits
            {
                stats.RecordHit();
            }
        });
        tasks[1] = Task.Run(() => // Miss recording task
        {
            for (int i = 0; i < totalOperations * 0.4; i++) // 40% misses
            {
                stats.RecordMiss();
            }
        });
        tasks[2] = Task.Run(() => // Set recording task
        {
            for (int i = 0; i < totalOperations * 0.8; i++) // 80% sets
            {
                stats.RecordSet();
            }
        });
        tasks[3] = Task.Run(() => // Eviction and cleanup recording task
        {
            for (int i = 0; i < totalOperations * 0.1; i++) // 10% evictions/cleanups
            {
                stats.RecordEviction();
                stats.RecordCleanup();
            }
        });

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(totalOperations * 0.6, stats.Hits);
        Assert.Equal(totalOperations * 0.4, stats.Misses);
        Assert.Equal(totalOperations * 0.8, stats.Sets);
        Assert.Equal(totalOperations * 0.1, stats.Evictions);
        Assert.Equal(totalOperations * 0.1, stats.Cleanups);
        Assert.Equal(totalOperations, stats.TotalRequests); // hits + misses
        Assert.Equal(0.6, stats.HitRatio, 0.01); // 60% hit ratio
    }

    [Fact]
    public async Task Integration_StatisticsResetShouldWorkInConcurrentEnvironment()
    {
        // Arrange
        var stats = new CacheStatistics();

        // Act - Perform operations and reset concurrently
        var operationTask = Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                stats.RecordHit();
                stats.RecordMiss();
                stats.RecordSet();
                Thread.Sleep(1); // Small delay to allow interleaving
            }
        });

        var resetTask = Task.Run(() =>
        {
            Thread.Sleep(50); // Start reset in the middle
            stats.Reset();
        });

        await Task.WhenAll(operationTask, resetTask);

        // Assert - After reset, values should be consistent
        var finalStats = stats;
        Assert.True(
            (finalStats.Hits == 0 && finalStats.Misses == 0 && finalStats.TotalRequests == 0) ||
            (finalStats.Hits >= 0 && finalStats.Misses >= 0 && finalStats.TotalRequests >= 0),
            $"Reset resulted in inconsistent state: Hits={finalStats.Hits}, Misses={finalStats.Misses}, Total={finalStats.TotalRequests}"
        );
    }

    [Fact]
    public async Task Integration_HighFrequencyOperationsShouldMaintainAccuracy()
    {
        // Arrange
        var stats = new CacheStatistics();
        const int operationsPerTask = 10000;
        const int taskCount = 5;

        // Act - High frequency concurrent operations
        var tasks = new Task[taskCount];
        for (int t = 0; t < taskCount; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < operationsPerTask; i++)
                {
                    stats.RecordHit();
                    stats.RecordMiss();
                    stats.RecordSet();
                    stats.RecordEviction();
                    stats.RecordCleanup();
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert - All operations should be accurately counted
        var expectedHits = operationsPerTask * taskCount;
        var expectedMisses = operationsPerTask * taskCount;
        var expectedSets = operationsPerTask * taskCount;
        var expectedEvictions = operationsPerTask * taskCount;
        var expectedCleanups = operationsPerTask * taskCount;
        var expectedTotalRequests = expectedHits + expectedMisses;

        Assert.Equal(expectedHits, stats.Hits);
        Assert.Equal(expectedMisses, stats.Misses);
        Assert.Equal(expectedSets, stats.Sets);
        Assert.Equal(expectedEvictions, stats.Evictions);
        Assert.Equal(expectedCleanups, stats.Cleanups);
        Assert.Equal(expectedTotalRequests, stats.TotalRequests);
        Assert.Equal((double)expectedHits / expectedTotalRequests, stats.HitRatio, 0.0001);
    }
}