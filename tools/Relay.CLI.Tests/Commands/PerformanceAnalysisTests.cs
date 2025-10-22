using Relay.CLI.Commands.Models.Performance;

namespace Relay.CLI.Tests.Commands;

public class PerformanceAnalysisTests
{
    [Fact]
    public void PerformanceAnalysis_ShouldHaveProjectCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { ProjectCount = 5 };

        // Assert
        Assert.Equal(5, analysis.ProjectCount);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveHandlerCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { HandlerCount = 25 };

        // Assert
        Assert.Equal(25, analysis.HandlerCount);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveOptimizedHandlerCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { OptimizedHandlerCount = 15 };

        // Assert
        Assert.Equal(15, analysis.OptimizedHandlerCount);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveCachedHandlerCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { CachedHandlerCount = 10 };

        // Assert
        Assert.Equal(10, analysis.CachedHandlerCount);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveAsyncMethodCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { AsyncMethodCount = 30 };

        // Assert
        Assert.Equal(30, analysis.AsyncMethodCount);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveValueTaskCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { ValueTaskCount = 20 };

        // Assert
        Assert.Equal(20, analysis.ValueTaskCount);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveTaskCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { TaskCount = 10 };

        // Assert
        Assert.Equal(10, analysis.TaskCount);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveCancellationTokenCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { CancellationTokenCount = 18 };

        // Assert
        Assert.Equal(18, analysis.CancellationTokenCount);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveConfigureAwaitCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { ConfigureAwaitCount = 12 };

        // Assert
        Assert.Equal(12, analysis.ConfigureAwaitCount);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveRecordCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { RecordCount = 8 };

        // Assert
        Assert.Equal(8, analysis.RecordCount);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveStructCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { StructCount = 15 };

        // Assert
        Assert.Equal(15, analysis.StructCount);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveLinqUsageCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { LinqUsageCount = 22 };

        // Assert
        Assert.Equal(22, analysis.LinqUsageCount);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveStringBuilderCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { StringBuilderCount = 7 };

        // Assert
        Assert.Equal(7, analysis.StringBuilderCount);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveStringConcatInLoopCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { StringConcatInLoopCount = 3 };

        // Assert
        Assert.Equal(3, analysis.StringConcatInLoopCount);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveHasRelayProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { HasRelay = true };

        // Assert
        Assert.True(analysis.HasRelay);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveModernFrameworkProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { ModernFramework = true };

        // Assert
        Assert.True(analysis.ModernFramework);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveHasPGOProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { HasPGO = true };

        // Assert
        Assert.True(analysis.HasPGO);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveHasOptimizationsProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { HasOptimizations = true };

        // Assert
        Assert.True(analysis.HasOptimizations);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHavePerformanceScoreProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { PerformanceScore = 85 };

        // Assert
        Assert.Equal(85, analysis.PerformanceScore);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveRecommendationsProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis();

        // Assert
        Assert.NotNull(analysis.Recommendations);
        Assert.Empty(analysis.Recommendations);
    }

    [Fact]
    public void PerformanceAnalysis_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis();

        // Assert
        Assert.Equal(0, analysis.ProjectCount);
        Assert.Equal(0, analysis.HandlerCount);
        Assert.Equal(0, analysis.OptimizedHandlerCount);
        Assert.Equal(0, analysis.CachedHandlerCount);
        Assert.Equal(0, analysis.AsyncMethodCount);
        Assert.Equal(0, analysis.ValueTaskCount);
        Assert.Equal(0, analysis.TaskCount);
        Assert.Equal(0, analysis.CancellationTokenCount);
        Assert.Equal(0, analysis.ConfigureAwaitCount);
        Assert.Equal(0, analysis.RecordCount);
        Assert.Equal(0, analysis.StructCount);
        Assert.Equal(0, analysis.LinqUsageCount);
        Assert.Equal(0, analysis.StringBuilderCount);
        Assert.Equal(0, analysis.StringConcatInLoopCount);
        Assert.False(analysis.HasRelay);
        Assert.False(analysis.ModernFramework);
        Assert.False(analysis.HasPGO);
        Assert.False(analysis.HasOptimizations);
        Assert.Equal(0, analysis.PerformanceScore);
        Assert.Empty(analysis.Recommendations);
    }

    [Fact]
    public void PerformanceAnalysis_CanSetAllPropertiesViaInitializer()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis
        {
            ProjectCount = 3,
            HandlerCount = 45,
            OptimizedHandlerCount = 30,
            CachedHandlerCount = 15,
            AsyncMethodCount = 40,
            ValueTaskCount = 25,
            TaskCount = 15,
            CancellationTokenCount = 35,
            ConfigureAwaitCount = 20,
            RecordCount = 12,
            StructCount = 8,
            LinqUsageCount = 18,
            StringBuilderCount = 5,
            StringConcatInLoopCount = 2,
            HasRelay = true,
            ModernFramework = true,
            HasPGO = false,
            HasOptimizations = true,
            PerformanceScore = 92
        };

        // Assert
        Assert.Equal(3, analysis.ProjectCount);
        Assert.Equal(45, analysis.HandlerCount);
        Assert.Equal(30, analysis.OptimizedHandlerCount);
        Assert.Equal(15, analysis.CachedHandlerCount);
        Assert.Equal(40, analysis.AsyncMethodCount);
        Assert.Equal(25, analysis.ValueTaskCount);
        Assert.Equal(15, analysis.TaskCount);
        Assert.Equal(35, analysis.CancellationTokenCount);
        Assert.Equal(20, analysis.ConfigureAwaitCount);
        Assert.Equal(12, analysis.RecordCount);
        Assert.Equal(8, analysis.StructCount);
        Assert.Equal(18, analysis.LinqUsageCount);
        Assert.Equal(5, analysis.StringBuilderCount);
        Assert.Equal(2, analysis.StringConcatInLoopCount);
        Assert.True(analysis.HasRelay);
        Assert.True(analysis.ModernFramework);
        Assert.False(analysis.HasPGO);
        Assert.True(analysis.HasOptimizations);
        Assert.Equal(92, analysis.PerformanceScore);
    }

    [Fact]
    public void PerformanceAnalysis_WithTypicalProjectData_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis
        {
            ProjectCount = 2,
            HandlerCount = 20,
            OptimizedHandlerCount = 12,
            CachedHandlerCount = 8,
            AsyncMethodCount = 25,
            ValueTaskCount = 15,
            TaskCount = 10,
            CancellationTokenCount = 18,
            ConfigureAwaitCount = 12,
            RecordCount = 6,
            StructCount = 4,
            LinqUsageCount = 14,
            StringBuilderCount = 3,
            StringConcatInLoopCount = 1,
            HasRelay = true,
            ModernFramework = true,
            HasPGO = false,
            HasOptimizations = true,
            PerformanceScore = 78
        };

        // Assert
        Assert.Equal(2, analysis.ProjectCount);
        Assert.Equal(20, analysis.HandlerCount);
        Assert.Equal(12, analysis.OptimizedHandlerCount);
        Assert.Equal(8, analysis.CachedHandlerCount);
        Assert.Equal(25, analysis.AsyncMethodCount);
        Assert.Equal(15, analysis.ValueTaskCount);
        Assert.Equal(10, analysis.TaskCount);
        Assert.Equal(18, analysis.CancellationTokenCount);
        Assert.Equal(12, analysis.ConfigureAwaitCount);
        Assert.Equal(6, analysis.RecordCount);
        Assert.Equal(4, analysis.StructCount);
        Assert.Equal(14, analysis.LinqUsageCount);
        Assert.Equal(3, analysis.StringBuilderCount);
        Assert.Equal(1, analysis.StringConcatInLoopCount);
        Assert.True(analysis.HasRelay);
        Assert.True(analysis.ModernFramework);
        Assert.False(analysis.HasPGO);
        Assert.True(analysis.HasOptimizations);
        Assert.Equal(78, analysis.PerformanceScore);
    }

    [Fact]
    public void PerformanceAnalysis_CanAddRecommendations()
    {
        // Arrange
        var analysis = new PerformanceAnalysis();
        var recommendation = new PerformanceRecommendation
        {
            Category = "Async",
            Priority = "High",
            Title = "Use ValueTask",
            Description = "Replace Task with ValueTask for better performance",
            Impact = "Medium"
        };

        // Act
        analysis.Recommendations.Add(recommendation);

        // Assert
        Assert.Single(analysis.Recommendations);
        Assert.Equal(recommendation, analysis.Recommendations[0]);
    }

    [Fact]
    public void PerformanceAnalysis_RecommendationsList_IsInitiallyEmpty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis();

        // Assert
        Assert.Empty(analysis.Recommendations);
    }

    [Fact]
    public void PerformanceAnalysis_CanHaveMultipleRecommendations()
    {
        // Arrange
        var analysis = new PerformanceAnalysis();
        var rec1 = new PerformanceRecommendation { Category = "Memory", Title = "Use StringBuilder" };
        var rec2 = new PerformanceRecommendation { Category = "Async", Title = "Avoid ConfigureAwait(false)" };
        var rec3 = new PerformanceRecommendation { Category = "Caching", Title = "Implement caching" };

        // Act
        analysis.Recommendations.AddRange(new[] { rec1, rec2, rec3 });

        // Assert
        Assert.Equal(3, analysis.Recommendations.Count);
        Assert.Contains("Memory", analysis.Recommendations.Select(r => r.Category));
        Assert.Contains("Async", analysis.Recommendations.Select(r => r.Category));
        Assert.Contains("Caching", analysis.Recommendations.Select(r => r.Category));
    }

    [Fact]
    public void PerformanceAnalysis_ShouldBeClass()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis();

        // Assert
        Assert.NotNull(analysis);
        Assert.True(analysis.GetType().IsClass);
    }

    [Fact]
    public void PerformanceAnalysis_CanBeUsedInList()
    {
        // Arrange & Act
        var analyses = new List<PerformanceAnalysis>
        {
            new() { ProjectCount = 1, PerformanceScore = 70 },
            new() { ProjectCount = 2, PerformanceScore = 85 },
            new() { ProjectCount = 3, PerformanceScore = 92 }
        };

        // Assert
        Assert.Equal(3, analyses.Count);
        Assert.Equal(6, analyses.Sum(a => a.ProjectCount));
        Assert.Equal(82.33333333333333, analyses.Average(a => a.PerformanceScore));
    }

    [Fact]
    public void PerformanceAnalysis_CanBeFiltered_ByHasRelay()
    {
        // Arrange
        var analyses = new List<PerformanceAnalysis>
        {
            new PerformanceAnalysis { HasRelay = true, PerformanceScore = 90 },
            new PerformanceAnalysis { HasRelay = false, PerformanceScore = 60 },
            new PerformanceAnalysis { HasRelay = true, PerformanceScore = 85 }
        };

        // Act
        var withRelay = analyses.Where(a => a.HasRelay).ToList();

        // Assert
        Assert.Equal(2, withRelay.Count);
        Assert.True(withRelay.All(a => a.HasRelay));
    }

    [Fact]
    public void PerformanceAnalysis_CanBeOrdered_ByPerformanceScore()
    {
        // Arrange
        var analyses = new List<PerformanceAnalysis>
        {
            new() { PerformanceScore = 70 },
            new() { PerformanceScore = 95 },
            new() { PerformanceScore = 80 }
        };

        // Act
        var ordered = analyses.OrderByDescending(a => a.PerformanceScore).ToList();

        // Assert
        Assert.Equal(95, ordered[0].PerformanceScore);
        Assert.Equal(80, ordered[1].PerformanceScore);
        Assert.Equal(70, ordered[2].PerformanceScore);
    }

    [Fact]
    public void PerformanceAnalysis_CanBeGrouped_ByModernFramework()
    {
        // Arrange
        var analyses = new List<PerformanceAnalysis>
        {
            new() { ModernFramework = true, ProjectCount = 1 },
            new() { ModernFramework = false, ProjectCount = 2 },
            new() { ModernFramework = true, ProjectCount = 3 },
            new() { ModernFramework = false, ProjectCount = 4 }
        };

        // Act
        var grouped = analyses.GroupBy(a => a.ModernFramework);

        // Assert
        Assert.Equal(2, grouped.Count());
        Assert.Equal(4, grouped.First(g => g.Key == true).Sum(a => a.ProjectCount));
        Assert.Equal(6, grouped.First(g => g.Key == false).Sum(a => a.ProjectCount));
    }

    [Fact]
    public void PerformanceAnalysis_PropertiesCanBeModified()
    {
        // Arrange
        var analysis = new PerformanceAnalysis
        {
            ProjectCount = 1,
            HandlerCount = 10,
            PerformanceScore = 75
        };

        // Act
        analysis.ProjectCount = 2;
        analysis.HandlerCount = 20;
        analysis.PerformanceScore = 85;

        // Assert
        Assert.Equal(2, analysis.ProjectCount);
        Assert.Equal(20, analysis.HandlerCount);
        Assert.Equal(85, analysis.PerformanceScore);
    }

    [Fact]
    public void PerformanceAnalysis_WithHighPerformanceProject_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis
        {
            ProjectCount = 1,
            HandlerCount = 50,
            OptimizedHandlerCount = 45,
            CachedHandlerCount = 30,
            AsyncMethodCount = 48,
            ValueTaskCount = 40,
            TaskCount = 8,
            CancellationTokenCount = 46,
            ConfigureAwaitCount = 35,
            RecordCount = 25,
            StructCount = 20,
            LinqUsageCount = 5,
            StringBuilderCount = 15,
            StringConcatInLoopCount = 0,
            HasRelay = true,
            ModernFramework = true,
            HasPGO = true,
            HasOptimizations = true,
            PerformanceScore = 98
        };

        // Assert
        Assert.Equal(45, analysis.OptimizedHandlerCount);
        Assert.Equal(40, analysis.ValueTaskCount);
        Assert.Equal(0, analysis.StringConcatInLoopCount);
        Assert.True(analysis.HasPGO);
        Assert.Equal(98, analysis.PerformanceScore);
    }

    [Fact]
    public void PerformanceAnalysis_WithLowPerformanceProject_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis
        {
            ProjectCount = 1,
            HandlerCount = 10,
            OptimizedHandlerCount = 2,
            CachedHandlerCount = 0,
            AsyncMethodCount = 3,
            ValueTaskCount = 1,
            TaskCount = 2,
            CancellationTokenCount = 1,
            ConfigureAwaitCount = 0,
            RecordCount = 0,
            StructCount = 1,
            LinqUsageCount = 8,
            StringBuilderCount = 0,
            StringConcatInLoopCount = 5,
            HasRelay = false,
            ModernFramework = false,
            HasPGO = false,
            HasOptimizations = false,
            PerformanceScore = 25
        };

        // Assert
        Assert.Equal(2, analysis.OptimizedHandlerCount);
        Assert.Equal(1, analysis.ValueTaskCount);
        Assert.Equal(5, analysis.StringConcatInLoopCount);
        Assert.False(analysis.HasRelay);
        Assert.Equal(25, analysis.PerformanceScore);
    }

    [Fact]
    public void PerformanceAnalysis_PerformanceScore_CanBeZero()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { PerformanceScore = 0 };

        // Assert
        Assert.Equal(0, analysis.PerformanceScore);
    }

    [Fact]
    public void PerformanceAnalysis_PerformanceScore_CanBeMaximum()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { PerformanceScore = 100 };

        // Assert
        Assert.Equal(100, analysis.PerformanceScore);
    }

    [Fact]
    public void PerformanceAnalysis_AllCounts_CanBeZero()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis
        {
            ProjectCount = 0,
            HandlerCount = 0,
            OptimizedHandlerCount = 0,
            CachedHandlerCount = 0,
            AsyncMethodCount = 0,
            ValueTaskCount = 0,
            TaskCount = 0,
            CancellationTokenCount = 0,
            ConfigureAwaitCount = 0,
            RecordCount = 0,
            StructCount = 0,
            LinqUsageCount = 0,
            StringBuilderCount = 0,
            StringConcatInLoopCount = 0
        };

        // Assert
        Assert.Equal(0, analysis.ProjectCount);
        Assert.Equal(0, analysis.HandlerCount);
        Assert.Equal(0, analysis.OptimizedHandlerCount);
        Assert.Equal(0, analysis.CachedHandlerCount);
        Assert.Equal(0, analysis.AsyncMethodCount);
        Assert.Equal(0, analysis.ValueTaskCount);
        Assert.Equal(0, analysis.TaskCount);
        Assert.Equal(0, analysis.CancellationTokenCount);
        Assert.Equal(0, analysis.ConfigureAwaitCount);
        Assert.Equal(0, analysis.RecordCount);
        Assert.Equal(0, analysis.StructCount);
        Assert.Equal(0, analysis.LinqUsageCount);
        Assert.Equal(0, analysis.StringBuilderCount);
        Assert.Equal(0, analysis.StringConcatInLoopCount);
    }

    [Fact]
    public void PerformanceAnalysis_BooleanFlags_CanAllBeFalse()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis
        {
            HasRelay = false,
            ModernFramework = false,
            HasPGO = false,
            HasOptimizations = false
        };

        // Assert
        Assert.False(analysis.HasRelay);
        Assert.False(analysis.ModernFramework);
        Assert.False(analysis.HasPGO);
        Assert.False(analysis.HasOptimizations);
    }

    [Fact]
    public void PerformanceAnalysis_BooleanFlags_CanAllBeTrue()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis
        {
            HasRelay = true,
            ModernFramework = true,
            HasPGO = true,
            HasOptimizations = true
        };

        // Assert
        Assert.True(analysis.HasRelay);
        Assert.True(analysis.ModernFramework);
        Assert.True(analysis.HasPGO);
        Assert.True(analysis.HasOptimizations);
    }
}

