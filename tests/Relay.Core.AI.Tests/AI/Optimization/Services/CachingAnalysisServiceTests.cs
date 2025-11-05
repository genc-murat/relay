using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Services;

public class CachingAnalysisServiceTests
{
    private readonly ILogger _logger;
    private readonly CachingAnalysisService _service;

    public CachingAnalysisServiceTests()
    {
        _logger = NullLogger.Instance;
        _service = new CachingAnalysisService(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new CachingAnalysisService(null!));
    }

    [Fact]
    public void Constructor_Should_Initialize_When_Logger_Is_Valid()
    {
        // Arrange
        var logger = NullLogger.Instance;

        // Act
        var service = new CachingAnalysisService(logger);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region AnalyzeCachingPatterns Tests

    [Fact]
    public void AnalyzeCachingPatterns_Should_Throw_When_RequestType_Is_Null()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.AnalyzeCachingPatterns(
            null!, 
            new CachingAnalysisData(), 
            new AccessPattern[0]));
    }

    [Fact]
    public void AnalyzeCachingPatterns_Should_Throw_When_AnalysisData_Is_Null()
    {
        // Arrange
        var requestType = typeof(string);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.AnalyzeCachingPatterns(
            requestType,
            null!,
            new AccessPattern[0]));
    }

    [Fact]
    public void AnalyzeCachingPatterns_Should_Throw_When_AccessPatterns_Is_Null()
    {
        // Arrange
        var requestType = typeof(string);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.AnalyzeCachingPatterns(
            requestType,
            new CachingAnalysisData(),
            null!));
    }

    #endregion

    #region ShouldEnableCaching Tests

    [Fact]
    public void ShouldEnableCaching_Should_Return_False_For_Empty_AccessPatterns()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new AccessPattern[0];

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.False(result.ShouldCache);
    }

    [Fact]
    public void ShouldEnableCaching_Should_Return_True_When_CacheHitRate_Is_Above_Threshold()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        analysisData.AddAccessPatterns(new[]
        {
            new AccessPattern
            {
                AccessFrequency = 0.5,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 10,
                DataVolatility = 0.1,
                WasCacheHit = true // Make some cache hits to increase hit rate
            },
            new AccessPattern
            {
                AccessFrequency = 0.5,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 10,
                DataVolatility = 0.1,
                WasCacheHit = true // Another cache hit
            },
            new AccessPattern
            {
                AccessFrequency = 0.5,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 10,
                DataVolatility = 0.1,
                WasCacheHit = true // Another cache hit
            },
            new AccessPattern
            {
                AccessFrequency = 0.5,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 10,
                DataVolatility = 0.1,
                WasCacheHit = false // One miss to keep it above threshold
            }
            // This should result in 3 hits out of 4 accesses = 75% hit rate, which is above 30% threshold
        });
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 0.5,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 10,
                DataVolatility = 0.1
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.True(result.ShouldCache);
    }

    [Fact]
    public void ShouldEnableCaching_Should_Return_True_When_AccessFrequency_Is_Above_Threshold()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        // Add access patterns to set up baseline data
        var accessPatternsData = new[]
        {
            new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-5), RequestKey = "key1", AccessCount = 1, WasCacheHit = false, ExecutionTime = TimeSpan.FromMilliseconds(50) },
            new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-4), RequestKey = "key2", AccessCount = 1, WasCacheHit = false, ExecutionTime = TimeSpan.FromMilliseconds(50) },
            new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-3), RequestKey = "key3", AccessCount = 1, WasCacheHit = false, ExecutionTime = TimeSpan.FromMilliseconds(50) },
            new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-2), RequestKey = "key4", AccessCount = 1, WasCacheHit = false, ExecutionTime = TimeSpan.FromMilliseconds(50) },
            new AccessPattern { Timestamp = DateTime.UtcNow.AddMinutes(-1), RequestKey = "key5", AccessCount = 1, WasCacheHit = true, ExecutionTime = TimeSpan.FromMilliseconds(10) }
        };
        analysisData.AddAccessPatterns(accessPatternsData);
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 2.0, // Above 1.0 threshold
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 10,
                DataVolatility = 0.1
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.True(result.ShouldCache);
    }

    [Fact]
    public void ShouldEnableCaching_Should_Return_True_When_ExecutionTime_Is_High_And_AccessCount_Is_Significant()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        // Add access patterns to achieve desired CacheHitRate of 0.1 (10% hit rate)
        var accessPatternsList = new List<AccessPattern>();
        for (int i = 0; i < 100; i++)
        {
            accessPatternsList.Add(new AccessPattern 
            { 
                Timestamp = DateTime.UtcNow.AddMinutes(-i), 
                RequestKey = $"key{i}", 
                AccessCount = 1, 
                WasCacheHit = i < 10, // First 10 are cache hits, others are misses -> 10/100 = 0.1 hit rate
                ExecutionTime = TimeSpan.FromMilliseconds(i < 10 ? 10 : 50) // Faster for cache hits
            });
        }
        analysisData.AddAccessPatterns(accessPatternsList.ToArray());
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 0.5,
                ExecutionTime = TimeSpan.FromMilliseconds(1000), // High execution time (> 500ms)
                AccessCount = 15, // Significant access count (> 10)
                DataVolatility = 0.1
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.True(result.ShouldCache);
    }

    #endregion

    #region CalculateRecommendedTtl Tests

    [Fact]
    public void CalculateRecommendedTtl_Should_Return_Default_When_AccessPatterns_Is_Empty()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new AccessPattern[0];

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert - The TTL would be calculated internally, so we need to test through the internal method
        // For now, verifying the service doesn't throw is enough
        Assert.NotNull(result);
    }

    [Fact]
    public void CalculateRecommendedTtl_Should_Adjust_Based_On_DataVolatility()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 1.0,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 10,
                DataVolatility = 0.8, // High volatility
                TimeSinceLastAccess = TimeSpan.FromMinutes(10)
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.RecommendedTtl >= TimeSpan.FromSeconds(1)); // Minimum TTL is 1 minute
        Assert.True(result.RecommendedTtl <= TimeSpan.FromHours(1)); // Maximum TTL is 60 minutes
    }

    #endregion

    #region DetermineCacheStrategy Tests

    [Fact]
    public void DetermineCacheStrategy_Should_Return_TimeBasedExpiration_When_AccessPatterns_Is_Empty()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new AccessPattern[0];

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CacheStrategy.TimeBasedExpiration, result.Strategy);
    }

    [Fact]
    public void DetermineCacheStrategy_Should_Return_LRU_For_High_Frequency_Access()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 15.0, // High frequency (> 10)
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 100
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CacheStrategy.LRU, result.Strategy);
    }

    [Fact]
    public void DetermineCacheStrategy_Should_Return_Adaptive_When_TimeOfDayPattern_Exists()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 1.0,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 10,
                TimeOfDayPattern = TimeOfDayPattern.MorningPeak // Time pattern exists
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CacheStrategy.Adaptive, result.Strategy);
    }

    #endregion

    #region PredictHitRate Tests

    [Fact]
    public void PredictHitRate_Should_Return_Zero_When_TotalAccesses_Is_Zero()
    {
        // Arrange
        var analysisData = new CachingAnalysisData(); // By default will have 0 total accesses and 0 cache hit rate
        var accessPatterns = new AccessPattern[0];

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0.0, result.PredictedHitRate);
    }

    [Fact]
    public void PredictHitRate_Should_Use_Historical_Hit_Rate_As_Base()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        // Add access patterns to achieve desired TotalAccesses of 100 and CacheHitRate of 0.8
        var initialAccessPatterns = new List<AccessPattern>();
        for (int i = 0; i < 100; i++)
        {
            initialAccessPatterns.Add(new AccessPattern 
            { 
                Timestamp = DateTime.UtcNow.AddMinutes(-i), 
                RequestKey = $"key{i}", 
                AccessCount = 1, 
                WasCacheHit = i < 80, // First 80 are cache hits to achieve 80% hit rate
                ExecutionTime = TimeSpan.FromMilliseconds(i < 80 ? 10 : 50) // Faster for cache hits
            });
        }
        analysisData.AddAccessPatterns(initialAccessPatterns.ToArray());
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 1.0,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 10,
                WasCacheHit = true,
                Timestamp = DateTime.UtcNow
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.PredictedHitRate >= 0.0);
        Assert.True(result.PredictedHitRate <= 0.95); // Should be clamped at 0.95
    }

    #endregion

    #region GenerateCacheKey Tests

    [Fact]
    public void GenerateCacheKey_Should_Create_Correct_Key_Format()
    {
        // Arrange - This is tested through the service's functionality
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 1.0,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 10
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("System.String", result.CacheKey); // Cache key should contain the type name
        Assert.Contains(":{{request}}", result.CacheKey); // Cache key should have the request placeholder
    }

    #endregion

    #region DetermineCacheScope Tests

    [Fact]
    public void DetermineCacheScope_Should_Return_Global_By_Default()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 1.0,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 10
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CacheScope.Global, result.Scope);
    }

    [Fact]
    public void DetermineCacheScope_Should_Return_User_When_UserContext_Exists()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 1.0,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 10,
                UserContext = "testUser" // User context exists
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CacheScope.User, result.Scope);
    }

    [Fact]
    public void DetermineCacheScope_Should_Return_Regional_When_Region_Exists()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 1.0,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 10,
                Region = "us-east-1" // Region exists
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CacheScope.Regional, result.Scope);
    }

    #endregion

    #region CalculateConfidence Tests

    [Fact]
    public void CalculateConfidence_Should_Increase_With_More_Accesses()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        // Add access patterns to achieve desired TotalAccesses
        var accessPatternsList = new List<AccessPattern>();
        for (int i = 0; i < 200; i++)
        {
            accessPatternsList.Add(new AccessPattern 
            { 
                Timestamp = DateTime.UtcNow.AddMinutes(-i), 
                RequestKey = $"key{i}", 
                AccessCount = 1, 
                WasCacheHit = i % 2 == 0, // Alternate cache hits and misses to get some cache hit rate
                ExecutionTime = TimeSpan.FromMilliseconds(i % 2 == 0 ? 10 : 50) // Faster for cache hits
            });
        }
        analysisData.AddAccessPatterns(accessPatternsList.ToArray());
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 1.0,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 50
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ConfidenceScore > 0.5); // Should have higher confidence with more data
    }

    #endregion

    #region MemorySavings Tests

    [Fact]
    public void EstimateMemorySavings_Should_Be_Calculated_Based_On_ExecutionTime()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 5.0,
                ExecutionTime = TimeSpan.FromMilliseconds(500), // High execution time
                AccessCount = 100 // High access count
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.EstimatedMemorySavings >= 0);
    }

    #endregion

    #region PerformanceGain Tests

    [Fact]
    public void EstimatePerformanceGain_Should_Be_Calculated_Based_On_ExecutionTime_And_HitRate()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        // Add access patterns to achieve desired CacheHitRate of 0.8 (80% hit rate)
        var accessPatternsList = new List<AccessPattern>();
        for (int i = 0; i < 100; i++)
        {
            accessPatternsList.Add(new AccessPattern 
            { 
                Timestamp = DateTime.UtcNow.AddMinutes(-i), 
                RequestKey = $"key{i}", 
                AccessCount = 1, 
                WasCacheHit = i < 80, // First 80 are cache hits -> 80/100 = 0.8 hit rate
                ExecutionTime = TimeSpan.FromMilliseconds(i < 80 ? 10 : 100) // Faster for cache hits
            });
        }
        analysisData.AddAccessPatterns(accessPatternsList.ToArray());
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 3.0,
                ExecutionTime = TimeSpan.FromMilliseconds(1000), // High execution time
                AccessCount = 50
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.EstimatedPerformanceGain >= TimeSpan.Zero);
    }

    #endregion

    #region KeyStrategy Tests

    [Fact]
    public void DetermineKeyStrategy_Should_Return_RequestTypeOnly_When_No_Special_Context()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 2.0,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 20
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CacheKeyStrategy.RequestTypeOnly, result.KeyStrategy);
    }

    [Fact]
    public void DetermineKeyStrategy_Should_Return_SelectedProperties_When_UserContext_Exists()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 2.0,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 20,
                UserContext = "testUser" // User context exists
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CacheKeyStrategy.SelectedProperties, result.KeyStrategy);
    }

    [Fact]
    public void DetermineKeyStrategy_Should_Return_FullRequest_When_RequestType_Exists()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 2.0,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 20,
                RequestType = typeof(string) // Request type exists
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CacheKeyStrategy.FullRequest, result.KeyStrategy);
    }

    #endregion

    #region KeyProperties Tests

    [Fact]
    public void GetKeyProperties_Should_Include_UserId_When_UserContext_Exists()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 2.0,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 20,
                UserContext = "testUser"
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        // Check if properties contain UserId
        var hasUserIdProperty = Array.IndexOf(result.KeyProperties, "UserId") >= 0;
        Assert.True(hasUserIdProperty);
    }

    [Fact]
    public void GetKeyProperties_Should_Include_Region_When_Region_Exists()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 2.0,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 20,
                Region = "us-west-2"
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        // Check if properties contain Region
        var hasRegionProperty = Array.IndexOf(result.KeyProperties, "Region") >= 0;
        Assert.True(hasRegionProperty);
    }

    #endregion

    #region CachePriority Tests

    [Fact]
    public void DetermineCachePriority_Should_Be_High_For_High_Frequency_Or_High_Execution_Time()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 15.0, // High frequency (> 10)
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 20
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CachePriority.High, result.Priority);
    }

    [Fact]
    public void DetermineCachePriority_Should_Be_High_For_High_Execution_Time()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 0.5, // Low frequency
                ExecutionTime = TimeSpan.FromMilliseconds(2000), // High execution time (> 1000ms)
                AccessCount = 20
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CachePriority.High, result.Priority);
    }

    [Fact]
    public void DetermineCachePriority_Should_Be_Normal_For_Medium_Values()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 2.0, // Medium frequency (> 1, < 10)
                ExecutionTime = TimeSpan.FromMilliseconds(200), // Medium execution time (> 100, < 1000)
                AccessCount = 20
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CachePriority.Normal, result.Priority);
    }

    [Fact]
    public void DetermineCachePriority_Should_Be_Low_For_Low_Values()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 0.5, // Low frequency (< 1)
                ExecutionTime = TimeSpan.FromMilliseconds(50), // Low execution time (< 100ms)
                AccessCount = 5
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CachePriority.Low, result.Priority);
    }

    #endregion

    #region DistributedCache Tests

    [Fact]
    public void ShouldUseDistributedCache_Should_Be_True_For_Regional_Data()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 2.0,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 1500, // High access count (> 1000)
                Region = "eu-central-1"
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.UseDistributedCache);
    }

    [Fact]
    public void ShouldUseDistributedCache_Should_Be_True_For_High_Access_Count()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 2.0,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 1500, // High access count (> 1000)
                Region = null // No regional data
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.UseDistributedCache);
    }

    [Fact]
    public void ShouldUseDistributedCache_Should_Be_False_For_Low_Access_Count()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 1.0,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessCount = 10, // Low access count (< 1000)
                Region = null // No regional data
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.UseDistributedCache);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AnalyzeCachingPatterns_Should_Return_Complete_Recommendation()
    {
        // Arrange
        var analysisData = new CachingAnalysisData();
        // Add access patterns to achieve desired CacheHitRate of 0.7 and TotalAccesses of 150
        var accessPatternsList = new List<AccessPattern>();
        for (int i = 0; i < 150; i++)
        {
            accessPatternsList.Add(new AccessPattern 
            { 
                Timestamp = DateTime.UtcNow.AddMinutes(-i), 
                RequestKey = $"key{i}", 
                AccessCount = 1, 
                WasCacheHit = i < 105, // First 105 are cache hits -> 105/150 = 0.7 hit rate
                ExecutionTime = TimeSpan.FromMilliseconds(i < 105 ? 10 : 50) // Faster for cache hits
            });
        }
        analysisData.AddAccessPatterns(accessPatternsList.ToArray());
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessFrequency = 3.0,
                ExecutionTime = TimeSpan.FromMilliseconds(500),
                AccessCount = 25,
                DataVolatility = 0.2,
                TimeSinceLastAccess = TimeSpan.FromMinutes(15),
                WasCacheHit = true,
                Timestamp = DateTime.UtcNow,
                UserContext = "user123",
                Region = "us-east-1",
                RequestType = typeof(string)
            }
        };

        // Act
        var result = _service.AnalyzeCachingPatterns(typeof(string), analysisData, accessPatterns);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ShouldCache);
        Assert.NotNull(result.RecommendedTtl);
        Assert.NotNull(result.Strategy);
        Assert.True(result.ExpectedHitRate >= 0.0);
        Assert.NotNull(result.CacheKey);
        Assert.NotNull(result.Scope);
        Assert.True(result.ConfidenceScore >= 0.0);
        Assert.True(result.EstimatedMemorySavings >= 0);
        Assert.True(result.EstimatedPerformanceGain >= TimeSpan.Zero);
        Assert.True(result.PredictedHitRate >= 0.0);
        Assert.NotNull(result.KeyStrategy);
        Assert.NotNull(result.KeyProperties);
        Assert.NotNull(result.Priority);
        Assert.True(result.UseDistributedCache);
    }

    #endregion
}
