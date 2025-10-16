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
        severity.Should().Be(ValidationSeverity.Info);
        ((int)severity).Should().Be(0);
    }

    [Fact]
    public void ValidationSeverity_ShouldHaveLowValue()
    {
        // Arrange & Act
        var severity = ValidationSeverity.Low;

        // Assert
        severity.Should().Be(ValidationSeverity.Low);
        ((int)severity).Should().Be(1);
    }

    [Fact]
    public void ValidationSeverity_ShouldHaveMediumValue()
    {
        // Arrange & Act
        var severity = ValidationSeverity.Medium;

        // Assert
        severity.Should().Be(ValidationSeverity.Medium);
        ((int)severity).Should().Be(2);
    }

    [Fact]
    public void ValidationSeverity_ShouldHaveHighValue()
    {
        // Arrange & Act
        var severity = ValidationSeverity.High;

        // Assert
        severity.Should().Be(ValidationSeverity.High);
        ((int)severity).Should().Be(3);
    }

    [Fact]
    public void ValidationSeverity_ShouldHaveCriticalValue()
    {
        // Arrange & Act
        var severity = ValidationSeverity.Critical;

        // Assert
        severity.Should().Be(ValidationSeverity.Critical);
        ((int)severity).Should().Be(4);
    }

    [Fact]
    public void ValidationSeverity_AllValues_ShouldBeDefined()
    {
        // Arrange & Act
        var values = Enum.GetValues(typeof(ValidationSeverity)).Cast<ValidationSeverity>().ToList();

        // Assert
        values.Should().HaveCount(5);
        values.Should().Contain(ValidationSeverity.Info);
        values.Should().Contain(ValidationSeverity.Low);
        values.Should().Contain(ValidationSeverity.Medium);
        values.Should().Contain(ValidationSeverity.High);
        values.Should().Contain(ValidationSeverity.Critical);
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
            ((int)ordered[i]).Should().BeLessThan((int)ordered[i + 1]);
        }
    }

    [Fact]
    public void ValidationSeverity_DefaultValue_ShouldBeInfo()
    {
        // Arrange & Act
        var defaultSeverity = default(ValidationSeverity);

        // Assert
        defaultSeverity.Should().Be(ValidationSeverity.Info);
        ((int)defaultSeverity).Should().Be(0);
    }

    [Fact]
    public void ValidationSeverity_CanBeParsedFromString()
    {
        // Act & Assert
        Enum.Parse<ValidationSeverity>("Info").Should().Be(ValidationSeverity.Info);
        Enum.Parse<ValidationSeverity>("Low").Should().Be(ValidationSeverity.Low);
        Enum.Parse<ValidationSeverity>("Medium").Should().Be(ValidationSeverity.Medium);
        Enum.Parse<ValidationSeverity>("High").Should().Be(ValidationSeverity.High);
        Enum.Parse<ValidationSeverity>("Critical").Should().Be(ValidationSeverity.Critical);
    }

    [Fact]
    public void ValidationSeverity_CanBeConvertedToString()
    {
        // Act & Assert
        ValidationSeverity.Info.ToString().Should().Be("Info");
        ValidationSeverity.Low.ToString().Should().Be("Low");
        ValidationSeverity.Medium.ToString().Should().Be("Medium");
        ValidationSeverity.High.ToString().Should().Be("High");
        ValidationSeverity.Critical.ToString().Should().Be("Critical");
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
        severities.Should().HaveCount(5);
        severities.Distinct().Should().HaveCount(5);
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
        grouped.Should().HaveCount(5);
        grouped.First(g => g.Key == ValidationSeverity.High).Should().HaveCount(2);
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
        ordered[0].Should().Be(ValidationSeverity.Info);
        ordered[1].Should().Be(ValidationSeverity.Low);
        ordered[2].Should().Be(ValidationSeverity.Medium);
        ordered[3].Should().Be(ValidationSeverity.High);
        ordered[4].Should().Be(ValidationSeverity.Critical);
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
        highSeverity.Should().HaveCount(3);
        highSeverity.Should().Contain(ValidationSeverity.Medium);
        highSeverity.Should().Contain(ValidationSeverity.High);
        highSeverity.Should().Contain(ValidationSeverity.Critical);

        lowSeverity.Should().HaveCount(2);
        lowSeverity.Should().Contain(ValidationSeverity.Info);
        lowSeverity.Should().Contain(ValidationSeverity.Low);
    }

    [Fact]
    public void ValidationSeverity_ShouldBeEnum()
    {
        // Arrange & Act
        var type = typeof(ValidationSeverity);

        // Assert
        type.IsEnum.Should().BeTrue();
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
        severityCounts[ValidationSeverity.Info].Should().Be(2);
        severityCounts[ValidationSeverity.Low].Should().Be(1);
        severityCounts[ValidationSeverity.Medium].Should().Be(2);
        severityCounts[ValidationSeverity.High].Should().Be(1);
        severityCounts[ValidationSeverity.Critical].Should().Be(1);
    }

    [Fact]
    public void ValidationSeverity_CanBeUsedWithBitwiseOperations()
    {
        // Arrange
        var severity1 = ValidationSeverity.Medium;
        var severity2 = ValidationSeverity.High;

        // Act & Assert - Basic comparison operations
        ((int)severity1).Should().BeLessThan((int)severity2);
        ((int)severity2).Should().BeGreaterThan((int)severity1);
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
        ((int)severity).Should().Be(expectedValue);
    }

    [Fact]
    public void ValidationSeverity_CanBeUsedInConditionalLogic()
    {
        // Arrange
        var severity = ValidationSeverity.Medium;

        // Act & Assert
        (severity == ValidationSeverity.Info).Should().BeFalse();
        (severity == ValidationSeverity.Medium).Should().BeTrue();
        (severity != ValidationSeverity.Critical).Should().BeTrue();
        (severity >= ValidationSeverity.Medium).Should().BeTrue();
        (severity <= ValidationSeverity.Critical).Should().BeTrue();
        (severity > ValidationSeverity.Low).Should().BeTrue();
        (severity < ValidationSeverity.Critical).Should().BeTrue();
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
        issue.Severity.Should().Be("Medium");
        ValidationSeverity.Medium.ToString().Should().Be("Medium");
    }

    [Fact]
    public void ValidationSeverity_AllValues_ShouldBeUnique()
    {
        // Arrange & Act
        var values = Enum.GetValues(typeof(ValidationSeverity)).Cast<int>().ToList();

        // Assert
        values.Distinct().Should().HaveCount(5);
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
        severityDescriptions.Should().HaveCount(5);
        severityDescriptions[ValidationSeverity.Critical].Should().Be("Critical issue requiring immediate attention");
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
        report[0].Description.Should().Be("Info");
        report[0].IsHighPriority.Should().BeFalse();
        report[3].Description.Should().Be("High");
        report[3].IsHighPriority.Should().BeTrue();
        report[4].Description.Should().Be("Critical");
        report[4].IsHighPriority.Should().BeTrue();
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
        criticalIssues.Should().HaveCount(1);
        highAndAbove.Should().HaveCount(3);
        mediumAndBelow.Should().HaveCount(3);
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
        priority.Should().Be("Medium");
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
        shouldBlockDeployment.Should().BeTrue();
        shouldWarnUser.Should().BeTrue();
        hasOnlyInfo.Should().BeFalse();
    }
}