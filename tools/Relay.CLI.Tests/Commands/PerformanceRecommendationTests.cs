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
        recommendation.Category.Should().Be("Async");
    }

    [Fact]
    public void PerformanceRecommendation_ShouldHavePriorityProperty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Priority = "High" };

        // Assert
        recommendation.Priority.Should().Be("High");
    }

    [Fact]
    public void PerformanceRecommendation_ShouldHaveTitleProperty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Title = "Use ValueTask" };

        // Assert
        recommendation.Title.Should().Be("Use ValueTask");
    }

    [Fact]
    public void PerformanceRecommendation_ShouldHaveDescriptionProperty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Description = "Replace Task with ValueTask for better performance" };

        // Assert
        recommendation.Description.Should().Be("Replace Task with ValueTask for better performance");
    }

    [Fact]
    public void PerformanceRecommendation_ShouldHaveImpactProperty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Impact = "Medium" };

        // Assert
        recommendation.Impact.Should().Be("Medium");
    }

    [Fact]
    public void PerformanceRecommendation_DefaultValues_ShouldBeEmpty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation();

        // Assert
        recommendation.Category.Should().Be("");
        recommendation.Priority.Should().Be("");
        recommendation.Title.Should().Be("");
        recommendation.Description.Should().Be("");
        recommendation.Impact.Should().Be("");
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
        recommendation.Category.Should().Be("Memory");
        recommendation.Priority.Should().Be("High");
        recommendation.Title.Should().Be("Use StringBuilder");
        recommendation.Description.Should().Be("Replace string concatenation with StringBuilder in loops");
        recommendation.Impact.Should().Be("High");
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
        recommendation.Category.Should().Be(category);
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
        recommendation.Priority.Should().Be(priority);
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
        recommendation.Impact.Should().Be(impact);
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
        recommendation.Category.Should().Be("Async");
        recommendation.Priority.Should().Be("High");
        recommendation.Title.Should().Contain("ValueTask");
        recommendation.Description.Should().Contain("synchronously");
        recommendation.Impact.Should().Be("Medium");
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
        recommendation.Category.Should().Be("Memory");
        recommendation.Priority.Should().Be("Medium");
        recommendation.Title.Should().Contain("StringBuilder");
        recommendation.Description.Should().Contain("memory allocations");
        recommendation.Impact.Should().Be("High");
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
        recommendation.Category.Should().Be("Caching");
        recommendation.Priority.Should().Be("High");
        recommendation.Title.Should().Contain("caching");
        recommendation.Description.Should().Contain("response times");
        recommendation.Impact.Should().Be("High");
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
        recommendation.Category.Should().Be("LINQ");
        recommendation.Priority.Should().Be("Low");
        recommendation.Title.Should().Contain("ToList");
        recommendation.Description.Should().Contain("enumerations");
        recommendation.Impact.Should().Be("Low");
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
        recommendation.Title.Should().Contain("async/await");
        recommendation.Title.Should().Contain("Task.Wait()");
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
        recommendation.Description.Should().Contain("multiline");
        recommendation.Description.Should().Contain("Point 1");
        recommendation.Description.Should().Contain("Point 2");
        recommendation.Description.Should().Contain("Point 3");
    }

    [Fact]
    public void PerformanceRecommendation_ShouldBeClass()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation();

        // Assert
        recommendation.Should().NotBeNull();
        recommendation.GetType().IsClass.Should().BeTrue();
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
        recommendations.Should().HaveCount(3);
        recommendations.Count(r => r.Category == "Async").Should().Be(1);
        recommendations.Count(r => r.Category == "Memory").Should().Be(1);
        recommendations.Count(r => r.Category == "Caching").Should().Be(1);
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
        asyncRecommendations.Should().HaveCount(2);
        asyncRecommendations.All(r => r.Category == "Async").Should().BeTrue();
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
        highPriority.Should().HaveCount(2);
        highPriority.All(r => r.Priority == "High").Should().BeTrue();
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
        ordered[0].Priority.Should().Be("Medium");
        ordered[1].Priority.Should().Be("High");
        ordered[2].Priority.Should().Be("Low");
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
        grouped.Should().HaveCount(3);
        grouped.First(g => g.Key == "Async").Should().HaveCount(2);
        grouped.First(g => g.Key == "Memory").Should().HaveCount(2);
        grouped.First(g => g.Key == "Caching").Should().HaveCount(1);
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
        recommendation.Category.Should().Be("Modified");
        recommendation.Priority.Should().Be("High");
        recommendation.Title.Should().Be("Modified title");
    }

    [Fact]
    public void PerformanceRecommendation_Category_CanBeEmpty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Category = "" };

        // Assert
        recommendation.Category.Should().BeEmpty();
    }

    [Fact]
    public void PerformanceRecommendation_Priority_CanBeEmpty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Priority = "" };

        // Assert
        recommendation.Priority.Should().BeEmpty();
    }

    [Fact]
    public void PerformanceRecommendation_Title_CanBeEmpty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Title = "" };

        // Assert
        recommendation.Title.Should().BeEmpty();
    }

    [Fact]
    public void PerformanceRecommendation_Description_CanBeEmpty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Description = "" };

        // Assert
        recommendation.Description.Should().BeEmpty();
    }

    [Fact]
    public void PerformanceRecommendation_Impact_CanBeEmpty()
    {
        // Arrange & Act
        var recommendation = new PerformanceRecommendation { Impact = "" };

        // Assert
        recommendation.Impact.Should().BeEmpty();
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
        recommendation.Category.Should().Be("Performance");
        recommendation.Priority.Should().Be("Critical");
        recommendation.Title.Should().Contain("database queries");
        recommendation.Description.Should().Contain("bottlenecks");
        recommendation.Description.Should().Contain("indexes");
        recommendation.Impact.Should().Be("Critical");
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
        recommendation.Category.Should().Be("Code Quality");
        recommendation.Priority.Should().Be("Low");
        recommendation.Title.Should().Contain("unused variables");
        recommendation.Description.Should().Contain("readability");
        recommendation.Impact.Should().Be("Low");
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
        recommendation.Category.Should().Be("Code Quality");
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
        recommendation.Title.Should().Be("Use .NET 8 features");
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
        recommendation.Description.Should().Contain("'await using'");
        recommendation.Description.Should().Contain("'using'");
    }
}