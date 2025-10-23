using Relay.CLI.Commands.Models.Validation;

namespace Relay.CLI.Tests.Commands;

public class ValidationSeverityTests
{
    [Fact]
    public void ValidationSeverity_ShouldHaveInfoValue()
    {
        // Arrange & Act
        var severity = ValidationSeverity.Info;

        // Assert
        Assert.Equal(ValidationSeverity.Info, severity);
        Assert.Equal(0, (int)severity);
    }

    [Fact]
    public void ValidationSeverity_ShouldHaveLowValue()
    {
        // Arrange & Act
        var severity = ValidationSeverity.Low;

        // Assert
        Assert.Equal(ValidationSeverity.Low, severity);
        Assert.Equal(1, (int)severity);
    }

    [Fact]
    public void ValidationSeverity_ShouldHaveMediumValue()
    {
        // Arrange & Act
        var severity = ValidationSeverity.Medium;

        // Assert
        Assert.Equal(ValidationSeverity.Medium, severity);
        Assert.Equal(2, (int)severity);
    }

    [Fact]
    public void ValidationSeverity_ShouldHaveHighValue()
    {
        // Arrange & Act
        var severity = ValidationSeverity.High;

        // Assert
        Assert.Equal(ValidationSeverity.High, severity);
        Assert.Equal(3, (int)severity);
    }

    [Fact]
    public void ValidationSeverity_ShouldHaveCriticalValue()
    {
        // Arrange & Act
        var severity = ValidationSeverity.Critical;

        // Assert
        Assert.Equal(ValidationSeverity.Critical, severity);
        Assert.Equal(4, (int)severity);
    }

    [Fact]
    public void ValidationSeverity_AllValues_ShouldBeDefined()
    {
        // Arrange & Act
        var values = Enum.GetValues(typeof(ValidationSeverity)).Cast<ValidationSeverity>().ToList();

        // Assert
        Assert.Equal(5, values.Count);
        Assert.Contains(ValidationSeverity.Info, values);
        Assert.Contains(ValidationSeverity.Low, values);
        Assert.Contains(ValidationSeverity.Medium, values);
        Assert.Contains(ValidationSeverity.High, values);
        Assert.Contains(ValidationSeverity.Critical, values);
    }

    [Fact]
    public void ValidationSeverity_ShouldHaveCorrectOrder()
    {
        // Arrange & Act
        var ordered = new[]
        {
            ValidationSeverity.Info,
            ValidationSeverity.Low,
            ValidationSeverity.Medium,
            ValidationSeverity.High,
            ValidationSeverity.Critical
        };

        // Assert
        for (int i = 0; i < ordered.Length - 1; i++)
        {
            Assert.True((int)ordered[i] < (int)ordered[i + 1]);
        }
    }

    [Fact]
    public void ValidationSeverity_DefaultValue_ShouldBeInfo()
    {
        // Arrange & Act
        var defaultSeverity = default(ValidationSeverity);

        // Assert
        Assert.Equal(ValidationSeverity.Info, defaultSeverity);
        Assert.Equal(0, (int)defaultSeverity);
    }

    [Fact]
    public void ValidationSeverity_CanBeParsedFromString()
    {
        // Act & Assert
        Assert.Equal(ValidationSeverity.Info, Enum.Parse<ValidationSeverity>("Info"));
        Assert.Equal(ValidationSeverity.Low, Enum.Parse<ValidationSeverity>("Low"));
        Assert.Equal(ValidationSeverity.Medium, Enum.Parse<ValidationSeverity>("Medium"));
        Assert.Equal(ValidationSeverity.High, Enum.Parse<ValidationSeverity>("High"));
        Assert.Equal(ValidationSeverity.Critical, Enum.Parse<ValidationSeverity>("Critical"));
    }

    [Fact]
    public void ValidationSeverity_CanBeConvertedToString()
    {
        // Act & Assert
        Assert.Equal("Info", ValidationSeverity.Info.ToString());
        Assert.Equal("Low", ValidationSeverity.Low.ToString());
        Assert.Equal("Medium", ValidationSeverity.Medium.ToString());
        Assert.Equal("High", ValidationSeverity.High.ToString());
        Assert.Equal("Critical", ValidationSeverity.Critical.ToString());
    }

    [Fact]
    public void ValidationSeverity_CanBeUsedInCollections()
    {
        // Arrange & Act
        var severities = new List<ValidationSeverity>
        {
            ValidationSeverity.Info,
            ValidationSeverity.Low,
            ValidationSeverity.Medium,
            ValidationSeverity.High,
            ValidationSeverity.Critical
        };

        // Assert
        Assert.Equal(5, severities.Count);
        Assert.Equal(5, severities.Distinct().Count());
    }

    [Fact]
    public void ValidationSeverity_CanBeGrouped()
    {
        // Arrange
        var items = new List<(string Name, ValidationSeverity Severity)>
        {
            ("Item1", ValidationSeverity.Info),
            ("Item2", ValidationSeverity.Low),
            ("Item3", ValidationSeverity.Medium),
            ("Item4", ValidationSeverity.High),
            ("Item5", ValidationSeverity.Critical),
            ("Item6", ValidationSeverity.High)
        };

        // Act
        var grouped = items.GroupBy(i => i.Severity);

        // Assert
        Assert.Equal(5, grouped.Count());
        Assert.Equal(2, grouped.First(g => g.Key == ValidationSeverity.High).Count());
    }

    [Fact]
    public void ValidationSeverity_CanBeOrdered()
    {
        // Arrange
        var severities = new List<ValidationSeverity>
        {
            ValidationSeverity.Critical,
            ValidationSeverity.Info,
            ValidationSeverity.High,
            ValidationSeverity.Low,
            ValidationSeverity.Medium
        };

        // Act
        var ordered = severities.OrderBy(s => s).ToList();

        // Assert
        Assert.Equal(ValidationSeverity.Info, ordered[0]);
        Assert.Equal(ValidationSeverity.Low, ordered[1]);
        Assert.Equal(ValidationSeverity.Medium, ordered[2]);
        Assert.Equal(ValidationSeverity.High, ordered[3]);
        Assert.Equal(ValidationSeverity.Critical, ordered[4]);
    }

    [Fact]
    public void ValidationSeverity_CanBeFiltered()
    {
        // Arrange
        var severities = Enum.GetValues(typeof(ValidationSeverity))
            .Cast<ValidationSeverity>()
            .ToList();

        // Act
        var highSeverity = severities.Where(s => (int)s >= (int)ValidationSeverity.Medium).ToList();
        var lowSeverity = severities.Where(s => (int)s <= (int)ValidationSeverity.Low).ToList();

        // Assert
        Assert.Equal(3, highSeverity.Count);
        Assert.Contains(ValidationSeverity.Medium, highSeverity);
        Assert.Contains(ValidationSeverity.High, highSeverity);
        Assert.Contains(ValidationSeverity.Critical, highSeverity);

        Assert.Equal(2, lowSeverity.Count);
        Assert.Contains(ValidationSeverity.Info, lowSeverity);
        Assert.Contains(ValidationSeverity.Low, lowSeverity);
    }

    [Fact]
    public void ValidationSeverity_ShouldBeEnum()
    {
        // Arrange & Act
        var type = typeof(ValidationSeverity);

        // Assert
        Assert.True(type.IsEnum);
    }

    [Fact]
    public void ValidationSeverity_CanBeUsedInLinqQueries()
    {
        // Arrange
        var severities = new List<ValidationSeverity>
        {
            ValidationSeverity.Info,
            ValidationSeverity.Low,
            ValidationSeverity.Medium,
            ValidationSeverity.High,
            ValidationSeverity.Critical,
            ValidationSeverity.Info,
            ValidationSeverity.Medium
        };

        // Act
        var severityCounts = severities.GroupBy(s => s)
            .ToDictionary(g => g.Key, g => g.Count());

        // Assert
        Assert.Equal(2, severityCounts[ValidationSeverity.Info]);
        Assert.Equal(1, severityCounts[ValidationSeverity.Low]);
        Assert.Equal(2, severityCounts[ValidationSeverity.Medium]);
        Assert.Equal(1, severityCounts[ValidationSeverity.High]);
        Assert.Equal(1, severityCounts[ValidationSeverity.Critical]);
    }

    [Fact]
    public void ValidationSeverity_CanBeUsedWithBitwiseOperations()
    {
        // Arrange
        var severity1 = ValidationSeverity.Medium;
        var severity2 = ValidationSeverity.High;

        // Act & Assert - Basic comparison operations
        Assert.True((int)severity1 < (int)severity2);
        Assert.True((int)severity2 > (int)severity1);
    }

    [Theory]
    [InlineData(ValidationSeverity.Info, 0)]
    [InlineData(ValidationSeverity.Low, 1)]
    [InlineData(ValidationSeverity.Medium, 2)]
    [InlineData(ValidationSeverity.High, 3)]
    [InlineData(ValidationSeverity.Critical, 4)]
    public void ValidationSeverity_ShouldHaveCorrectIntegerValues(ValidationSeverity severity, int expectedValue)
    {
        // Act & Assert
        Assert.Equal(expectedValue, (int)severity);
    }

    [Fact]
    public void ValidationSeverity_CanBeUsedInConditionalLogic()
    {
        // Arrange
        var severity = ValidationSeverity.Medium;

        // Act & Assert
        Assert.NotEqual(ValidationSeverity.Info, severity);
        Assert.Equal(ValidationSeverity.Medium, severity);
        Assert.NotEqual(ValidationSeverity.Critical, severity);
        Assert.True(severity >= ValidationSeverity.Medium);
        Assert.True(severity <= ValidationSeverity.Critical);
        Assert.True(severity > ValidationSeverity.Low);
        Assert.True(severity < ValidationSeverity.Critical);
    }

    [Fact]
    public void ValidationSeverity_CanBeUsedInValidationIssueCreation()
    {
        // Arrange & Act
        var issue = new Relay.CLI.Commands.Models.Validation.ValidationIssue
        {
            Message = "Test validation issue",
            Severity = ValidationSeverity.Medium.ToString(), // Convert enum to string for ValidationIssue
        };

        // Assert
        Assert.Equal("Medium", issue.Severity);
        Assert.Equal("Medium", ValidationSeverity.Medium.ToString());
    }

    [Fact]
    public void ValidationSeverity_AllValues_ShouldBeUnique()
    {
        // Arrange & Act
        var values = Enum.GetValues(typeof(ValidationSeverity)).Cast<int>().ToList();

        // Assert
        Assert.Equal(5, values.Distinct().Count());
    }

    [Fact]
    public void ValidationSeverity_CanBeUsedInDictionary()
    {
        // Arrange & Act
        var severityDescriptions = new Dictionary<ValidationSeverity, string>
        {
            { ValidationSeverity.Info, "Informational message" },
            { ValidationSeverity.Low, "Low priority issue" },
            { ValidationSeverity.Medium, "Medium priority issue" },
            { ValidationSeverity.High, "High priority issue" },
            { ValidationSeverity.Critical, "Critical issue requiring immediate attention" }
        };

        // Assert
        Assert.Equal(5, severityDescriptions.Count);
        Assert.Equal("Critical issue requiring immediate attention", severityDescriptions[ValidationSeverity.Critical]);
    }

    [Fact]
    public void ValidationSeverity_CanBeUsedInReporting()
    {
        // Arrange
        var severities = new List<ValidationSeverity>
        {
            ValidationSeverity.Info,
            ValidationSeverity.Low,
            ValidationSeverity.Medium,
            ValidationSeverity.High,
            ValidationSeverity.Critical
        };

        // Act
        var report = severities.Select(s => new
        {
            Severity = s,
            Level = (int)s,
            Description = s.ToString(),
            IsHighPriority = (int)s >= (int)ValidationSeverity.High
        }).ToList();

        // Assert
        Assert.Equal("Info", report[0].Description);
        Assert.False(report[0].IsHighPriority);
        Assert.Equal("High", report[3].Description);
        Assert.True(report[3].IsHighPriority);
        Assert.Equal("Critical", report[4].Description);
        Assert.True(report[4].IsHighPriority);
    }

    [Fact]
    public void ValidationSeverity_CanBeUsedForSeverityBasedFiltering()
    {
        // Arrange
        var issues = new List<(string Message, ValidationSeverity Severity)>
        {
            ("Minor issue", ValidationSeverity.Info),
            ("Low priority", ValidationSeverity.Low),
            ("Medium concern", ValidationSeverity.Medium),
            ("High priority", ValidationSeverity.High),
            ("Critical problem", ValidationSeverity.Critical),
            ("Another high", ValidationSeverity.High)
        };

        // Act
        var criticalIssues = issues.Where(i => i.Severity == ValidationSeverity.Critical).ToList();
        var highAndAbove = issues.Where(i => i.Severity >= ValidationSeverity.High).ToList();
        var mediumAndBelow = issues.Where(i => i.Severity <= ValidationSeverity.Medium).ToList();

        // Assert
        Assert.Single(criticalIssues);
        Assert.Equal(3, highAndAbove.Count);
        Assert.Equal(3, mediumAndBelow.Count);
    }

    [Fact]
    public void ValidationSeverity_CanBeUsedInSwitchStatement()
    {
        // Arrange
        var severity = ValidationSeverity.Medium;
        string priority = "";

        // Act
        switch (severity)
        {
            case ValidationSeverity.Info:
                priority = "Informational";
                break;
            case ValidationSeverity.Low:
                priority = "Low";
                break;
            case ValidationSeverity.Medium:
                priority = "Medium";
                break;
            case ValidationSeverity.High:
                priority = "High";
                break;
            case ValidationSeverity.Critical:
                priority = "Critical";
                break;
        }

        // Assert
        Assert.Equal("Medium", priority);
    }

    [Fact]
    public void ValidationSeverity_CanBeUsedInValidationWorkflow()
    {
        // Arrange
        var validationResults = new List<ValidationSeverity>
        {
            ValidationSeverity.Info,
            ValidationSeverity.Low,
            ValidationSeverity.Medium,
            ValidationSeverity.High,
            ValidationSeverity.Critical
        };

        // Act - Simulate validation workflow
        var shouldBlockDeployment = validationResults.Any(s => s >= ValidationSeverity.High);
        var shouldWarnUser = validationResults.Any(s => s >= ValidationSeverity.Medium);
        var hasOnlyInfo = validationResults.All(s => s == ValidationSeverity.Info);

        // Assert
        Assert.True(shouldBlockDeployment);
        Assert.True(shouldWarnUser);
        Assert.False(hasOnlyInfo);
    }
}
