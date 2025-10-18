using Relay.CLI.Commands.Models.Performance;

namespace Relay.CLI.Tests.Commands;

public class PerformanceRecommendationTests
{
    [Fact]
    public void PerformanceRecommendation_ShouldHaveCategoryProperty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Category = "Async" };

        // Assert
        Assert.Equal("Async", recommendation.Category);
    }

    [Fact]
    public void PerformanceRecommendation_ShouldHavePriorityProperty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Priority = "High" };

        // Assert
        Assert.Equal("High", recommendation.Priority);
    }

    [Fact]
    public void PerformanceRecommendation_ShouldHaveTitleProperty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Title = "Use ValueTask" };

        // Assert
        Assert.Equal("Use ValueTask", recommendation.Title);
    }

    [Fact]
    public void PerformanceRecommendation_ShouldHaveDescriptionProperty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Description = "Replace Task with ValueTask for better performance" };

        // Assert
        Assert.Equal("Replace Task with ValueTask for better performance", recommendation.Description);
    }

    [Fact]
    public void PerformanceRecommendation_ShouldHaveImpactProperty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Impact = "Medium" };

        // Assert
        Assert.Equal("Medium", recommendation.Impact);
    }

    [Fact]
    public void PerformanceRecommendation_DefaultValues_ShouldBeEmpty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation();

        // Assert
        Assert.Equal("", recommendation.Category);
        Assert.Equal("", recommendation.Priority);
        Assert.Equal("", recommendation.Title);
        Assert.Equal("", recommendation.Description);
        Assert.Equal("", recommendation.Impact);
    }

    [Fact]
    public void PerformanceRecommendation_CanSetAllPropertiesViaInitializer()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation
        {
            Category = "Memory",
            Priority = "High",
            Title = "Use StringBuilder",
            Description = "Replace string concatenation with StringBuilder in loops",
            Impact = "High"
        };

        // Assert
        Assert.Equal("Memory", recommendation.Category);
        Assert.Equal("High", recommendation.Priority);
        Assert.Equal("Use StringBuilder", recommendation.Title);
        Assert.Equal("Replace string concatenation with StringBuilder in loops", recommendation.Description);
        Assert.Equal("High", recommendation.Impact);
    }

    [Theory]
    [InlineData("Async")]
    [InlineData("Memory")]
    [InlineData("Caching")]
    [InlineData("LINQ")]
    public void PerformanceRecommendation_ShouldSupportVariousCategories(string category)
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Category = category };

        // Assert
        Assert.Equal(category, recommendation.Category);
    }

    [Theory]
    [InlineData("Low")]
    [InlineData("Medium")]
    [InlineData("High")]
    [InlineData("Critical")]
    public void PerformanceRecommendation_ShouldSupportVariousPriorities(string priority)
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Priority = priority };

        // Assert
        Assert.Equal(priority, recommendation.Priority);
    }

    [Theory]
    [InlineData("Low")]
    [InlineData("Medium")]
    [InlineData("High")]
    public void PerformanceRecommendation_ShouldSupportVariousImpacts(string impact)
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Impact = impact };

        // Assert
        Assert.Equal(impact, recommendation.Impact);
    }

    [Fact]
    public void PerformanceRecommendation_WithAsyncRecommendation_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation
        {
            Category = "Async",
            Priority = "High",
            Title = "Use ValueTask instead of Task",
            Description = "Replace Task<T> with ValueTask<T> for methods that complete synchronously most of the time",
            Impact = "Medium"
        };

        // Assert
        Assert.Equal("Async", recommendation.Category);
        Assert.Equal("High", recommendation.Priority);
        Assert.Contains("ValueTask", recommendation.Title);
        Assert.Contains("synchronously", recommendation.Description);
        Assert.Equal("Medium", recommendation.Impact);
    }

    [Fact]
    public void PerformanceRecommendation_WithMemoryRecommendation_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation
        {
            Category = "Memory",
            Priority = "Medium",
            Title = "Use StringBuilder for string concatenation",
            Description = "Replace string concatenation in loops with StringBuilder to avoid memory allocations",
            Impact = "High"
        };

        // Assert
        Assert.Equal("Memory", recommendation.Category);
        Assert.Equal("Medium", recommendation.Priority);
        Assert.Contains("StringBuilder", recommendation.Title);
        Assert.Contains("memory allocations", recommendation.Description);
        Assert.Equal("High", recommendation.Impact);
    }

    [Fact]
    public void PerformanceRecommendation_WithCachingRecommendation_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation
        {
            Category = "Caching",
            Priority = "High",
            Title = "Implement result caching",
            Description = "Add caching to expensive operations to improve response times",
            Impact = "High"
        };

        // Assert
        Assert.Equal("Caching", recommendation.Category);
        Assert.Equal("High", recommendation.Priority);
        Assert.Contains("caching", recommendation.Title);
        Assert.Contains("response times", recommendation.Description);
        Assert.Equal("High", recommendation.Impact);
    }

    [Fact]
    public void PerformanceRecommendation_WithLINQRecommendation_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation
        {
            Category = "LINQ",
            Priority = "Low",
            Title = "Use ToList() or ToArray() appropriately",
            Description = "Consider using ToList() for multiple enumerations or ToArray() for fixed-size collections",
            Impact = "Low"
        };

        // Assert
        Assert.Equal("LINQ", recommendation.Category);
        Assert.Equal("Low", recommendation.Priority);
        Assert.Contains("ToList", recommendation.Title);
        Assert.Contains("enumerations", recommendation.Description);
        Assert.Equal("Low", recommendation.Impact);
    }

    [Fact]
    public void PerformanceRecommendation_Title_CanContainSpecialCharacters()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation
        {
            Title = "Use async/await instead of Task.Wait()"
        };

        // Assert
        Assert.Contains("async/await", recommendation.Title);
        Assert.Contains("Task.Wait()", recommendation.Title);
    }

    [Fact]
    public void PerformanceRecommendation_Description_CanBeMultiline()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation
        {
            Description = "This is a multiline description:\n- Point 1\n- Point 2\n- Point 3"
        };

        // Assert
        Assert.Contains("multiline", recommendation.Description);
        Assert.Contains("Point 1", recommendation.Description);
        Assert.Contains("Point 2", recommendation.Description);
        Assert.Contains("Point 3", recommendation.Description);
    }

    [Fact]
    public void PerformanceRecommendation_ShouldBeClass()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation();

        // Assert
        Assert.NotNull(recommendation);
        Assert.True(recommendation.GetType().IsClass);
    }

    [Fact]
    public void PerformanceRecommendation_CanBeUsedInList()
    {
        // Arrange & Act
        var recommendations = new List<PerformanceRecommendation>
        {
            new PerformanceRecommendation { Category = "Async", Title = "Use ValueTask" },
            new PerformanceRecommendation { Category = "Memory", Title = "Use StringBuilder" },
            new PerformanceRecommendation { Category = "Caching", Title = "Add caching" }
        };

        // Assert
        Assert.Equal(3, recommendations.Count());
        Assert.Equal(1, recommendations.Count(r => r.Category == "Async"));
        Assert.Equal(1, recommendations.Count(r => r.Category == "Memory"));
        Assert.Equal(1, recommendations.Count(r => r.Category == "Caching"));
    }

    [Fact]
    public void PerformanceRecommendation_CanBeFiltered_ByCategory()
    {
        // Arrange
        var recommendations = new List<PerformanceRecommendation>
        {
            new PerformanceRecommendation { Category = "Async", Priority = "High" },
            new PerformanceRecommendation { Category = "Memory", Priority = "Medium" },
            new PerformanceRecommendation { Category = "Async", Priority = "Low" },
            new PerformanceRecommendation { Category = "Caching", Priority = "High" }
        };

        // Act
        var asyncRecommendations = recommendations.Where(r => r.Category == "Async").ToList();

        // Assert
        Assert.Equal(2, asyncRecommendations.Count());
        Assert.True(asyncRecommendations.All(r => r.Category == "Async"));
    }

    [Fact]
    public void PerformanceRecommendation_CanBeFiltered_ByPriority()
    {
        // Arrange
        var recommendations = new List<PerformanceRecommendation>
        {
            new PerformanceRecommendation { Priority = "High", Category = "Async" },
            new PerformanceRecommendation { Priority = "Medium", Category = "Memory" },
            new PerformanceRecommendation { Priority = "High", Category = "Caching" },
            new PerformanceRecommendation { Priority = "Low", Category = "LINQ" }
        };

        // Act
        var highPriority = recommendations.Where(r => r.Priority == "High").ToList();

        // Assert
        Assert.Equal(2, highPriority.Count());
        Assert.True(highPriority.All(r => r.Priority == "High"));
    }

    [Fact]
    public void PerformanceRecommendation_CanBeOrdered_ByPriority()
    {
        // Arrange
        var recommendations = new List<PerformanceRecommendation>
        {
            new PerformanceRecommendation { Priority = "Low", Title = "Minor optimization" },
            new PerformanceRecommendation { Priority = "High", Title = "Critical fix" },
            new PerformanceRecommendation { Priority = "Medium", Title = "Important improvement" }
        };

        // Act - Order by string length as a simple ordering (High=4, Medium=6, Low=3)
        var ordered = recommendations.OrderByDescending(r => r.Priority.Length).ToList();

        // Assert
        Assert.Equal("Medium", ordered[0].Priority);
        Assert.Equal("High", ordered[1].Priority);
        Assert.Equal("Low", ordered[2].Priority);
    }

    [Fact]
    public void PerformanceRecommendation_CanBeGrouped_ByCategory()
    {
        // Arrange
        var recommendations = new List<PerformanceRecommendation>
        {
            new PerformanceRecommendation { Category = "Async", Title = "Use ValueTask" },
            new PerformanceRecommendation { Category = "Memory", Title = "Use StringBuilder" },
            new PerformanceRecommendation { Category = "Async", Title = "Avoid Task.Wait" },
            new PerformanceRecommendation { Category = "Memory", Title = "Use object pooling" },
            new PerformanceRecommendation { Category = "Caching", Title = "Add caching" }
        };

        // Act
        var grouped = recommendations.GroupBy(r => r.Category);

        // Assert
        Assert.Equal(3, grouped.Count());
        Assert.Equal(2, grouped.First(g => g.Key == "Async").Count());
        Assert.Equal(2, grouped.First(g => g.Key == "Memory").Count());
        Assert.Equal(1, grouped.First(g => g.Key == "Caching").Count());
    }

    [Fact]
    public void PerformanceRecommendation_PropertiesCanBeModified()
    {
        // Arrange
        var recommendation = new PerformanceRecommendation
        {
            Category = "Initial",
            Priority = "Low",
            Title = "Initial title"
        };

        // Act
        recommendation.Category = "Modified";
        recommendation.Priority = "High";
        recommendation.Title = "Modified title";

        // Assert
        Assert.Equal("Modified", recommendation.Category);
        Assert.Equal("High", recommendation.Priority);
        Assert.Equal("Modified title", recommendation.Title);
    }

    [Fact]
    public void PerformanceRecommendation_Category_CanBeEmpty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Category = "" };

        // Assert
        Assert.Empty(recommendation.Category);
    }

    [Fact]
    public void PerformanceRecommendation_Priority_CanBeEmpty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Priority = "" };

        // Assert
        Assert.Empty(recommendation.Priority);
    }

    [Fact]
    public void PerformanceRecommendation_Title_CanBeEmpty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Title = "" };

        // Assert
        Assert.Empty(recommendation.Title);
    }

    [Fact]
    public void PerformanceRecommendation_Description_CanBeEmpty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Description = "" };

        // Assert
        Assert.Empty(recommendation.Description);
    }

    [Fact]
    public void PerformanceRecommendation_Impact_CanBeEmpty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Impact = "" };

        // Assert
        Assert.Empty(recommendation.Impact);
    }

    [Fact]
    public void PerformanceRecommendation_WithComprehensiveRecommendation_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation
        {
            Category = "Performance",
            Priority = "Critical",
            Title = "Optimize database queries",
            Description = "Database queries are causing performance bottlenecks. Consider adding indexes, optimizing joins, and implementing query result caching.",
            Impact = "Critical"
        };

        // Assert
        Assert.Equal("Performance", recommendation.Category);
        Assert.Equal("Critical", recommendation.Priority);
        Assert.Contains("database queries", recommendation.Title);
        Assert.Contains("bottlenecks", recommendation.Description);
        Assert.Contains("indexes", recommendation.Description);
        Assert.Equal("Critical", recommendation.Impact);
    }

    [Fact]
    public void PerformanceRecommendation_WithSimpleRecommendation_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation
        {
            Category = "Code Quality",
            Priority = "Low",
            Title = "Remove unused variables",
            Description = "Clean up unused variables to improve code readability",
            Impact = "Low"
        };

        // Assert
        Assert.Equal("Code Quality", recommendation.Category);
        Assert.Equal("Low", recommendation.Priority);
        Assert.Contains("unused variables", recommendation.Title);
        Assert.Contains("readability", recommendation.Description);
        Assert.Equal("Low", recommendation.Impact);
    }

    [Fact]
    public void PerformanceRecommendation_Category_CanContainSpaces()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation
        {
            Category = "Code Quality"
        };

        // Assert
        Assert.Equal("Code Quality", recommendation.Category);
    }

    [Fact]
    public void PerformanceRecommendation_Title_CanContainNumbers()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation
        {
            Title = "Use .NET 8 features"
        };

        // Assert
        Assert.Equal("Use .NET 8 features", recommendation.Title);
    }

    [Fact]
    public void PerformanceRecommendation_Description_CanContainCodeSnippets()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation
        {
            Description = "Use 'await using' instead of 'using' with async disposables"
        };

        // Assert
        Assert.Contains("'await using'", recommendation.Description);
        Assert.Contains("'using'", recommendation.Description);
    }
}

