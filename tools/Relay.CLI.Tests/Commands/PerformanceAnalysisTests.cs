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
        analysis.ProjectCount.Should().Be(5);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveHandlerCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { HandlerCount = 25 };

        // Assert
        analysis.HandlerCount.Should().Be(25);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveOptimizedHandlerCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { OptimizedHandlerCount = 15 };

        // Assert
        analysis.OptimizedHandlerCount.Should().Be(15);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveCachedHandlerCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { CachedHandlerCount = 10 };

        // Assert
        analysis.CachedHandlerCount.Should().Be(10);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveAsyncMethodCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { AsyncMethodCount = 30 };

        // Assert
        analysis.AsyncMethodCount.Should().Be(30);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveValueTaskCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { ValueTaskCount = 20 };

        // Assert
        analysis.ValueTaskCount.Should().Be(20);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveTaskCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { TaskCount = 10 };

        // Assert
        analysis.TaskCount.Should().Be(10);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveCancellationTokenCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { CancellationTokenCount = 18 };

        // Assert
        analysis.CancellationTokenCount.Should().Be(18);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveConfigureAwaitCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { ConfigureAwaitCount = 12 };

        // Assert
        analysis.ConfigureAwaitCount.Should().Be(12);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveRecordCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { RecordCount = 8 };

        // Assert
        analysis.RecordCount.Should().Be(8);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveStructCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { StructCount = 15 };

        // Assert
        analysis.StructCount.Should().Be(15);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveLinqUsageCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { LinqUsageCount = 22 };

        // Assert
        analysis.LinqUsageCount.Should().Be(22);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveStringBuilderCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { StringBuilderCount = 7 };

        // Assert
        analysis.StringBuilderCount.Should().Be(7);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveStringConcatInLoopCountProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { StringConcatInLoopCount = 3 };

        // Assert
        analysis.StringConcatInLoopCount.Should().Be(3);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveHasRelayProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { HasRelay = true };

        // Assert
        analysis.HasRelay.Should().BeTrue();
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveModernFrameworkProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { ModernFramework = true };

        // Assert
        analysis.ModernFramework.Should().BeTrue();
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveHasPGOProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { HasPGO = true };

        // Assert
        analysis.HasPGO.Should().BeTrue();
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveHasOptimizationsProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { HasOptimizations = true };

        // Assert
        analysis.HasOptimizations.Should().BeTrue();
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHavePerformanceScoreProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { PerformanceScore = 85 };

        // Assert
        analysis.PerformanceScore.Should().Be(85);
    }

    [Fact]
    public void PerformanceAnalysis_ShouldHaveRecommendationsProperty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis();

        // Assert
        analysis.Recommendations.Should().NotBeNull();
        analysis.Recommendations.Should().BeEmpty();
    }

    [Fact]
    public void PerformanceAnalysis_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis();

        // Assert
        analysis.ProjectCount.Should().Be(0);
        analysis.HandlerCount.Should().Be(0);
        analysis.OptimizedHandlerCount.Should().Be(0);
        analysis.CachedHandlerCount.Should().Be(0);
        analysis.AsyncMethodCount.Should().Be(0);
        analysis.ValueTaskCount.Should().Be(0);
        analysis.TaskCount.Should().Be(0);
        analysis.CancellationTokenCount.Should().Be(0);
        analysis.ConfigureAwaitCount.Should().Be(0);
        analysis.RecordCount.Should().Be(0);
        analysis.StructCount.Should().Be(0);
        analysis.LinqUsageCount.Should().Be(0);
        analysis.StringBuilderCount.Should().Be(0);
        analysis.StringConcatInLoopCount.Should().Be(0);
        analysis.HasRelay.Should().BeFalse();
        analysis.ModernFramework.Should().BeFalse();
        analysis.HasPGO.Should().BeFalse();
        analysis.HasOptimizations.Should().BeFalse();
        analysis.PerformanceScore.Should().Be(0);
        analysis.Recommendations.Should().BeEmpty();
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
        analysis.ProjectCount.Should().Be(3);
        analysis.HandlerCount.Should().Be(45);
        analysis.OptimizedHandlerCount.Should().Be(30);
        analysis.CachedHandlerCount.Should().Be(15);
        analysis.AsyncMethodCount.Should().Be(40);
        analysis.ValueTaskCount.Should().Be(25);
        analysis.TaskCount.Should().Be(15);
        analysis.CancellationTokenCount.Should().Be(35);
        analysis.ConfigureAwaitCount.Should().Be(20);
        analysis.RecordCount.Should().Be(12);
        analysis.StructCount.Should().Be(8);
        analysis.LinqUsageCount.Should().Be(18);
        analysis.StringBuilderCount.Should().Be(5);
        analysis.StringConcatInLoopCount.Should().Be(2);
        analysis.HasRelay.Should().BeTrue();
        analysis.ModernFramework.Should().BeTrue();
        analysis.HasPGO.Should().BeFalse();
        analysis.HasOptimizations.Should().BeTrue();
        analysis.PerformanceScore.Should().Be(92);
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
        analysis.ProjectCount.Should().Be(2);
        analysis.HandlerCount.Should().Be(20);
        analysis.OptimizedHandlerCount.Should().Be(12);
        analysis.CachedHandlerCount.Should().Be(8);
        analysis.AsyncMethodCount.Should().Be(25);
        analysis.ValueTaskCount.Should().Be(15);
        analysis.TaskCount.Should().Be(10);
        analysis.CancellationTokenCount.Should().Be(18);
        analysis.ConfigureAwaitCount.Should().Be(12);
        analysis.RecordCount.Should().Be(6);
        analysis.StructCount.Should().Be(4);
        analysis.LinqUsageCount.Should().Be(14);
        analysis.StringBuilderCount.Should().Be(3);
        analysis.StringConcatInLoopCount.Should().Be(1);
        analysis.HasRelay.Should().BeTrue();
        analysis.ModernFramework.Should().BeTrue();
        analysis.HasPGO.Should().BeFalse();
        analysis.HasOptimizations.Should().BeTrue();
        analysis.PerformanceScore.Should().Be(78);
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
        analysis.Recommendations.Should().HaveCount(1);
        analysis.Recommendations[0].Should().Be(recommendation);
    }

    [Fact]
    public void PerformanceAnalysis_RecommendationsList_IsInitiallyEmpty()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis();

        // Assert
        analysis.Recommendations.Should().BeEmpty();
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
        analysis.Recommendations.Should().HaveCount(3);
        analysis.Recommendations.Select(r => r.Category).Should().Contain(new[] { "Memory", "Async", "Caching" });
    }

    [Fact]
    public void PerformanceAnalysis_ShouldBeClass()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis();

        // Assert
        analysis.Should().NotBeNull();
        analysis.GetType().IsClass.Should().BeTrue();
    }

    [Fact]
    public void PerformanceAnalysis_CanBeUsedInList()
    {
        // Arrange & Act
        var analyses = new List<PerformanceAnalysis>
        {
            new PerformanceAnalysis { ProjectCount = 1, PerformanceScore = 70 },
            new PerformanceAnalysis { ProjectCount = 2, PerformanceScore = 85 },
            new PerformanceAnalysis { ProjectCount = 3, PerformanceScore = 92 }
        };

        // Assert
        analyses.Should().HaveCount(3);
        analyses.Sum(a => a.ProjectCount).Should().Be(6);
        analyses.Average(a => a.PerformanceScore).Should().Be(82.33333333333333);
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
        withRelay.Should().HaveCount(2);
        withRelay.All(a => a.HasRelay).Should().BeTrue();
    }

    [Fact]
    public void PerformanceAnalysis_CanBeOrdered_ByPerformanceScore()
    {
        // Arrange
        var analyses = new List<PerformanceAnalysis>
        {
            new PerformanceAnalysis { PerformanceScore = 70 },
            new PerformanceAnalysis { PerformanceScore = 95 },
            new PerformanceAnalysis { PerformanceScore = 80 }
        };

        // Act
        var ordered = analyses.OrderByDescending(a => a.PerformanceScore).ToList();

        // Assert
        ordered[0].PerformanceScore.Should().Be(95);
        ordered[1].PerformanceScore.Should().Be(80);
        ordered[2].PerformanceScore.Should().Be(70);
    }

    [Fact]
    public void PerformanceAnalysis_CanBeGrouped_ByModernFramework()
    {
        // Arrange
        var analyses = new List<PerformanceAnalysis>
        {
            new PerformanceAnalysis { ModernFramework = true, ProjectCount = 1 },
            new PerformanceAnalysis { ModernFramework = false, ProjectCount = 2 },
            new PerformanceAnalysis { ModernFramework = true, ProjectCount = 3 },
            new PerformanceAnalysis { ModernFramework = false, ProjectCount = 4 }
        };

        // Act
        var grouped = analyses.GroupBy(a => a.ModernFramework);

        // Assert
        grouped.Should().HaveCount(2);
        grouped.First(g => g.Key == true).Sum(a => a.ProjectCount).Should().Be(4);
        grouped.First(g => g.Key == false).Sum(a => a.ProjectCount).Should().Be(6);
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
        analysis.ProjectCount.Should().Be(2);
        analysis.HandlerCount.Should().Be(20);
        analysis.PerformanceScore.Should().Be(85);
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
        analysis.OptimizedHandlerCount.Should().Be(45);
        analysis.ValueTaskCount.Should().Be(40);
        analysis.StringConcatInLoopCount.Should().Be(0);
        analysis.HasPGO.Should().BeTrue();
        analysis.PerformanceScore.Should().Be(98);
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
        analysis.OptimizedHandlerCount.Should().Be(2);
        analysis.ValueTaskCount.Should().Be(1);
        analysis.StringConcatInLoopCount.Should().Be(5);
        analysis.HasRelay.Should().BeFalse();
        analysis.PerformanceScore.Should().Be(25);
    }

    [Fact]
    public void PerformanceAnalysis_PerformanceScore_CanBeZero()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { PerformanceScore = 0 };

        // Assert
        analysis.PerformanceScore.Should().Be(0);
    }

    [Fact]
    public void PerformanceAnalysis_PerformanceScore_CanBeMaximum()
    {
        // Arrange & Act
        var analysis = new PerformanceAnalysis { PerformanceScore = 100 };

        // Assert
        analysis.PerformanceScore.Should().Be(100);
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
        analysis.ProjectCount.Should().Be(0);
        analysis.HandlerCount.Should().Be(0);
        analysis.OptimizedHandlerCount.Should().Be(0);
        analysis.CachedHandlerCount.Should().Be(0);
        analysis.AsyncMethodCount.Should().Be(0);
        analysis.ValueTaskCount.Should().Be(0);
        analysis.TaskCount.Should().Be(0);
        analysis.CancellationTokenCount.Should().Be(0);
        analysis.ConfigureAwaitCount.Should().Be(0);
        analysis.RecordCount.Should().Be(0);
        analysis.StructCount.Should().Be(0);
        analysis.LinqUsageCount.Should().Be(0);
        analysis.StringBuilderCount.Should().Be(0);
        analysis.StringConcatInLoopCount.Should().Be(0);
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
        analysis.HasRelay.Should().BeFalse();
        analysis.ModernFramework.Should().BeFalse();
        analysis.HasPGO.Should().BeFalse();
        analysis.HasOptimizations.Should().BeFalse();
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
        analysis.HasRelay.Should().BeTrue();
        analysis.ModernFramework.Should().BeTrue();
        analysis.HasPGO.Should().BeTrue();
        analysis.HasOptimizations.Should().BeTrue();
    }
}