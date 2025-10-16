using Relay.CLI.Commands.Models.Validation;

namespace Relay.CLI.Tests.Commands;

public class ValidationStatusTests
{
    [Fact]
    public void ValidationStatus_ShouldHavePassValue()
    {
        // Arrange & Act
        var status = ValidationStatus.Pass;

        // Assert
        status.Should().Be(ValidationStatus.Pass);
        ((int)status).Should().Be(0);
    }

    [Fact]
    public void ValidationStatus_ShouldHaveWarningValue()
    {
        // Arrange & Act
        var status = ValidationStatus.Warning;

        // Assert
        status.Should().Be(ValidationStatus.Warning);
        ((int)status).Should().Be(1);
    }

    [Fact]
    public void ValidationStatus_ShouldHaveFailValue()
    {
        // Arrange & Act
        var status = ValidationStatus.Fail;

        // Assert
        status.Should().Be(ValidationStatus.Fail);
        ((int)status).Should().Be(2);
    }

    [Fact]
    public void ValidationStatus_AllValues_ShouldBeDefined()
    {
        // Arrange & Act
        var values = Enum.GetValues(typeof(ValidationStatus)).Cast<ValidationStatus>().ToList();

        // Assert
        values.Should().HaveCount(3);
        values.Should().Contain(ValidationStatus.Pass);
        values.Should().Contain(ValidationStatus.Warning);
        values.Should().Contain(ValidationStatus.Fail);
    }

    [Fact]
    public void ValidationStatus_ShouldHaveCorrectOrder()
    {
        // Arrange & Act
        var ordered = new[]
        {
            ValidationStatus.Pass,
            ValidationStatus.Warning,
            ValidationStatus.Fail
        };

        // Assert
        ((int)ordered[0]).Should().BeLessThan((int)ordered[1]);
        ((int)ordered[1]).Should().BeLessThan((int)ordered[2]);
    }

    [Fact]
    public void ValidationStatus_DefaultValue_ShouldBePass()
    {
        // Arrange & Act
        var defaultStatus = default(ValidationStatus);

        // Assert
        defaultStatus.Should().Be(ValidationStatus.Pass);
        ((int)defaultStatus).Should().Be(0);
    }

    [Fact]
    public void ValidationStatus_CanBeParsedFromString()
    {
        // Act & Assert
        Enum.Parse<ValidationStatus>("Pass").Should().Be(ValidationStatus.Pass);
        Enum.Parse<ValidationStatus>("Warning").Should().Be(ValidationStatus.Warning);
        Enum.Parse<ValidationStatus>("Fail").Should().Be(ValidationStatus.Fail);
    }

    [Fact]
    public void ValidationStatus_CanBeConvertedToString()
    {
        // Act & Assert
        ValidationStatus.Pass.ToString().Should().Be("Pass");
        ValidationStatus.Warning.ToString().Should().Be("Warning");
        ValidationStatus.Fail.ToString().Should().Be("Fail");
    }

    [Fact]
    public void ValidationStatus_CanBeUsedInCollections()
    {
        // Arrange & Act
        var statuses = new List<ValidationStatus>
        {
            ValidationStatus.Pass,
            ValidationStatus.Warning,
            ValidationStatus.Fail
        };

        // Assert
        statuses.Should().HaveCount(3);
        statuses.Distinct().Should().HaveCount(3);
    }

    [Fact]
    public void ValidationStatus_CanBeGrouped()
    {
        // Arrange
        var items = new List<(string Name, ValidationStatus Status)>
        {
            ("Item1", ValidationStatus.Pass),
            ("Item2", ValidationStatus.Warning),
            ("Item3", ValidationStatus.Fail),
            ("Item4", ValidationStatus.Warning),
            ("Item5", ValidationStatus.Pass)
        };

        // Act
        var grouped = items.GroupBy(i => i.Status);

        // Assert
        grouped.Should().HaveCount(3);
        grouped.First(g => g.Key == ValidationStatus.Warning).Should().HaveCount(2);
        grouped.First(g => g.Key == ValidationStatus.Pass).Should().HaveCount(2);
    }

    [Fact]
    public void ValidationStatus_CanBeOrdered()
    {
        // Arrange
        var statuses = new List<ValidationStatus>
        {
            ValidationStatus.Fail,
            ValidationStatus.Pass,
            ValidationStatus.Warning
        };

        // Act
        var ordered = statuses.OrderBy(s => s).ToList();

        // Assert
        ordered[0].Should().Be(ValidationStatus.Pass);
        ordered[1].Should().Be(ValidationStatus.Warning);
        ordered[2].Should().Be(ValidationStatus.Fail);
    }

    [Fact]
    public void ValidationStatus_CanBeFiltered()
    {
        // Arrange
        var statuses = Enum.GetValues(typeof(ValidationStatus))
            .Cast<ValidationStatus>()
            .ToList();

        // Act
        var passingStatuses = statuses.Where(s => s == ValidationStatus.Pass).ToList();
        var nonPassingStatuses = statuses.Where(s => s != ValidationStatus.Pass).ToList();

        // Assert
        passingStatuses.Should().HaveCount(1);
        passingStatuses[0].Should().Be(ValidationStatus.Pass);

        nonPassingStatuses.Should().HaveCount(2);
        nonPassingStatuses.Should().Contain(ValidationStatus.Warning);
        nonPassingStatuses.Should().Contain(ValidationStatus.Fail);
    }

    [Fact]
    public void ValidationStatus_ShouldBeEnum()
    {
        // Arrange & Act
        var type = typeof(ValidationStatus);

        // Assert
        type.IsEnum.Should().BeTrue();
    }

    [Fact]
    public void ValidationStatus_CanBeUsedInLinqQueries()
    {
        // Arrange
        var statuses = new List<ValidationStatus>
        {
            ValidationStatus.Pass,
            ValidationStatus.Warning,
            ValidationStatus.Fail,
            ValidationStatus.Pass,
            ValidationStatus.Warning
        };

        // Act
        var statusCounts = statuses.GroupBy(s => s)
            .ToDictionary(g => g.Key, g => g.Count());

        // Assert
        statusCounts[ValidationStatus.Pass].Should().Be(2);
        statusCounts[ValidationStatus.Warning].Should().Be(2);
        statusCounts[ValidationStatus.Fail].Should().Be(1);
    }

    [Fact]
    public void ValidationStatus_CanBeUsedWithComparisonOperations()
    {
        // Arrange
        var status1 = ValidationStatus.Warning;
        var status2 = ValidationStatus.Fail;

        // Act & Assert - Basic comparison operations
        ((int)status1).Should().BeLessThan((int)status2);
        ((int)status2).Should().BeGreaterThan((int)status1);
    }

    [Theory]
    [InlineData(ValidationStatus.Pass, 0)]
    [InlineData(ValidationStatus.Warning, 1)]
    [InlineData(ValidationStatus.Fail, 2)]
    public void ValidationStatus_ShouldHaveCorrectIntegerValues(ValidationStatus status, int expectedValue)
    {
        // Act & Assert
        ((int)status).Should().Be(expectedValue);
    }

    [Fact]
    public void ValidationStatus_CanBeUsedInConditionalLogic()
    {
        // Arrange
        var status = ValidationStatus.Warning;

        // Act & Assert
        (status == ValidationStatus.Pass).Should().BeFalse();
        (status == ValidationStatus.Warning).Should().BeTrue();
        (status != ValidationStatus.Fail).Should().BeTrue();
        (status >= ValidationStatus.Warning).Should().BeTrue();
        (status <= ValidationStatus.Fail).Should().BeTrue();
        (status > ValidationStatus.Pass).Should().BeTrue();
        (status < ValidationStatus.Fail).Should().BeTrue();
    }

    [Fact]
    public void ValidationStatus_CanBeUsedInValidationResultCreation()
    {
        // Arrange & Act
        var result = new Relay.CLI.Commands.Models.Validation.ValidationResult
        {
            Status = ValidationStatus.Warning,
            Message = "Validation warning"
        };

        // Assert
        result.Status.Should().Be(ValidationStatus.Warning);
    }

    [Fact]
    public void ValidationStatus_AllValues_ShouldBeUnique()
    {
        // Arrange & Act
        var values = Enum.GetValues(typeof(ValidationStatus)).Cast<int>().ToList();

        // Assert
        values.Distinct().Should().HaveCount(3);
    }

    [Fact]
    public void ValidationStatus_CanBeUsedInDictionary()
    {
        // Arrange & Act
        var statusDescriptions = new Dictionary<ValidationStatus, string>
        {
            { ValidationStatus.Pass, "Validation passed successfully" },
            { ValidationStatus.Warning, "Validation passed with warnings" },
            { ValidationStatus.Fail, "Validation failed" }
        };

        // Assert
        statusDescriptions.Should().HaveCount(3);
        statusDescriptions[ValidationStatus.Fail].Should().Be("Validation failed");
    }

    [Fact]
    public void ValidationStatus_CanBeUsedInReporting()
    {
        // Arrange
        var statuses = new List<ValidationStatus>
        {
            ValidationStatus.Pass,
            ValidationStatus.Warning,
            ValidationStatus.Fail
        };

        // Act
        var report = statuses.Select(s => new
        {
            Status = s,
            Level = (int)s,
            Description = s.ToString(),
            IsSuccessful = s != ValidationStatus.Fail,
            RequiresAttention = s == ValidationStatus.Warning || s == ValidationStatus.Fail
        }).ToList();

        // Assert
        report[0].Description.Should().Be("Pass");
        report[0].IsSuccessful.Should().BeTrue();
        report[0].RequiresAttention.Should().BeFalse();

        report[1].Description.Should().Be("Warning");
        report[1].IsSuccessful.Should().BeTrue();
        report[1].RequiresAttention.Should().BeTrue();

        report[2].Description.Should().Be("Fail");
        report[2].IsSuccessful.Should().BeFalse();
        report[2].RequiresAttention.Should().BeTrue();
    }

    [Fact]
    public void ValidationStatus_CanBeUsedForStatusBasedFiltering()
    {
        // Arrange
        var results = new List<(string TestName, ValidationStatus Status)>
        {
            ("Test1", ValidationStatus.Pass),
            ("Test2", ValidationStatus.Warning),
            ("Test3", ValidationStatus.Fail),
            ("Test4", ValidationStatus.Pass),
            ("Test5", ValidationStatus.Warning)
        };

        // Act
        var passedTests = results.Where(r => r.Status == ValidationStatus.Pass).ToList();
        var failedTests = results.Where(r => r.Status == ValidationStatus.Fail).ToList();
        var testsRequiringAttention = results.Where(r => r.Status != ValidationStatus.Pass).ToList();

        // Assert
        passedTests.Should().HaveCount(2);
        failedTests.Should().HaveCount(1);
        testsRequiringAttention.Should().HaveCount(3);
    }

    [Fact]
    public void ValidationStatus_CanBeUsedInSwitchStatement()
    {
        // Arrange
        var status = ValidationStatus.Warning;
        string action = "";

        // Act
        switch (status)
        {
            case ValidationStatus.Pass:
                action = "Continue";
                break;
            case ValidationStatus.Warning:
                action = "Review";
                break;
            case ValidationStatus.Fail:
                action = "Stop";
                break;
        }

        // Assert
        action.Should().Be("Review");
    }

    [Fact]
    public void ValidationStatus_CanBeUsedInValidationWorkflow()
    {
        // Arrange
        var validationResults = new List<ValidationStatus>
        {
            ValidationStatus.Pass,
            ValidationStatus.Warning,
            ValidationStatus.Pass,
            ValidationStatus.Fail
        };

        // Act - Simulate validation workflow
        var overallStatus = validationResults.Contains(ValidationStatus.Fail) ? ValidationStatus.Fail :
                           validationResults.Contains(ValidationStatus.Warning) ? ValidationStatus.Warning :
                           ValidationStatus.Pass;

        var canProceed = overallStatus != ValidationStatus.Fail;
        var hasWarnings = validationResults.Contains(ValidationStatus.Warning);

        // Assert
        overallStatus.Should().Be(ValidationStatus.Fail);
        canProceed.Should().BeFalse();
        hasWarnings.Should().BeTrue();
    }

    [Fact]
    public void ValidationStatus_CanDetermineBuildSuccess()
    {
        // Test cases for build success determination
        var testCases = new List<(List<ValidationStatus> Results, ValidationStatus ExpectedOverall, bool CanBuild)>
        {
            (new List<ValidationStatus> { ValidationStatus.Pass, ValidationStatus.Pass }, ValidationStatus.Pass, true),
            (new List<ValidationStatus> { ValidationStatus.Pass, ValidationStatus.Warning }, ValidationStatus.Warning, true),
            (new List<ValidationStatus> { ValidationStatus.Warning, ValidationStatus.Warning }, ValidationStatus.Warning, true),
            (new List<ValidationStatus> { ValidationStatus.Pass, ValidationStatus.Fail }, ValidationStatus.Fail, false),
            (new List<ValidationStatus> { ValidationStatus.Warning, ValidationStatus.Fail }, ValidationStatus.Fail, false),
            (new List<ValidationStatus> { ValidationStatus.Fail, ValidationStatus.Fail }, ValidationStatus.Fail, false)
        };

        foreach (var testCase in testCases)
        {
            // Act
            var overallStatus = testCase.Results.Contains(ValidationStatus.Fail) ? ValidationStatus.Fail :
                               testCase.Results.Contains(ValidationStatus.Warning) ? ValidationStatus.Warning :
                               ValidationStatus.Pass;

            var canBuild = overallStatus != ValidationStatus.Fail;

            // Assert
            overallStatus.Should().Be(testCase.ExpectedOverall);
            canBuild.Should().Be(testCase.CanBuild);
        }
    }

    [Fact]
    public void ValidationStatus_CanBeUsedInProgressReporting()
    {
        // Arrange
        var validationSteps = new List<(string StepName, ValidationStatus Status)>
        {
            ("Code Analysis", ValidationStatus.Pass),
            ("Security Scan", ValidationStatus.Warning),
            ("Performance Test", ValidationStatus.Pass),
            ("Integration Test", ValidationStatus.Fail)
        };

        // Act
        var progressReport = validationSteps.Select(step => new
        {
            Step = step.StepName,
            Status = step.Status,
            Icon = step.Status switch
            {
                ValidationStatus.Pass => "✅",
                ValidationStatus.Warning => "⚠️",
                ValidationStatus.Fail => "❌",
                _ => "❓"
            },
            BlocksProgress = step.Status == ValidationStatus.Fail
        }).ToList();

        // Assert
        progressReport[0].Icon.Should().Be("✅");
        progressReport[0].BlocksProgress.Should().BeFalse();

        progressReport[1].Icon.Should().Be("⚠️");
        progressReport[1].BlocksProgress.Should().BeFalse();

        progressReport[3].Icon.Should().Be("❌");
        progressReport[3].BlocksProgress.Should().BeTrue();
    }
}