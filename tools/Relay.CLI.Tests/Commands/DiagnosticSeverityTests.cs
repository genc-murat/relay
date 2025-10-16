using Relay.CLI.Commands.Models.Diagnostic;

namespace Relay.CLI.Tests.Commands;

public class DiagnosticSeverityTests
{
    [Fact]
    public void DiagnosticSeverity_ShouldHaveSuccessValue()
    {
        // Arrange & Act
        var severity = DiagnosticSeverity.Success;

        // Assert
        severity.Should().Be(DiagnosticSeverity.Success);
        ((int)severity).Should().Be(0);
    }

    [Fact]
    public void DiagnosticSeverity_ShouldHaveInfoValue()
    {
        // Arrange & Act
        var severity = DiagnosticSeverity.Info;

        // Assert
        severity.Should().Be(DiagnosticSeverity.Info);
        ((int)severity).Should().Be(1);
    }

    [Fact]
    public void DiagnosticSeverity_ShouldHaveWarningValue()
    {
        // Arrange & Act
        var severity = DiagnosticSeverity.Warning;

        // Assert
        severity.Should().Be(DiagnosticSeverity.Warning);
        ((int)severity).Should().Be(2);
    }

    [Fact]
    public void DiagnosticSeverity_ShouldHaveErrorValue()
    {
        // Arrange & Act
        var severity = DiagnosticSeverity.Error;

        // Assert
        severity.Should().Be(DiagnosticSeverity.Error);
        ((int)severity).Should().Be(3);
    }

    [Fact]
    public void DiagnosticSeverity_AllValues_ShouldBeDefined()
    {
        // Arrange & Act
        var values = Enum.GetValues(typeof(DiagnosticSeverity)).Cast<DiagnosticSeverity>().ToList();

        // Assert
        values.Should().HaveCount(4);
        values.Should().Contain(DiagnosticSeverity.Success);
        values.Should().Contain(DiagnosticSeverity.Info);
        values.Should().Contain(DiagnosticSeverity.Warning);
        values.Should().Contain(DiagnosticSeverity.Error);
    }

    [Fact]
    public void DiagnosticSeverity_ShouldHaveCorrectOrder()
    {
        // Arrange & Act
        var ordered = new[]
        {
            DiagnosticSeverity.Success,
            DiagnosticSeverity.Info,
            DiagnosticSeverity.Warning,
            DiagnosticSeverity.Error
        };

        // Assert
        ((int)ordered[0]).Should().BeLessThan((int)ordered[1]);
        ((int)ordered[1]).Should().BeLessThan((int)ordered[2]);
        ((int)ordered[2]).Should().BeLessThan((int)ordered[3]);
    }

    [Fact]
    public void DiagnosticSeverity_DefaultValue_ShouldBeSuccess()
    {
        // Arrange & Act
        var defaultSeverity = default(DiagnosticSeverity);

        // Assert
        defaultSeverity.Should().Be(DiagnosticSeverity.Success);
        ((int)defaultSeverity).Should().Be(0);
    }

    [Fact]
    public void DiagnosticSeverity_CanBeUsedInSwitchStatement()
    {
        // Arrange
        var severity = DiagnosticSeverity.Warning;
        string result = "";

        // Act
        switch (severity)
        {
            case DiagnosticSeverity.Success:
                result = "Success";
                break;
            case DiagnosticSeverity.Info:
                result = "Info";
                break;
            case DiagnosticSeverity.Warning:
                result = "Warning";
                break;
            case DiagnosticSeverity.Error:
                result = "Error";
                break;
        }

        // Assert
        result.Should().Be("Warning");
    }

    [Fact]
    public void DiagnosticSeverity_CanBeParsedFromString()
    {
        // Act & Assert
        Enum.Parse<DiagnosticSeverity>("Success").Should().Be(DiagnosticSeverity.Success);
        Enum.Parse<DiagnosticSeverity>("Info").Should().Be(DiagnosticSeverity.Info);
        Enum.Parse<DiagnosticSeverity>("Warning").Should().Be(DiagnosticSeverity.Warning);
        Enum.Parse<DiagnosticSeverity>("Error").Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public void DiagnosticSeverity_CanBeConvertedToString()
    {
        // Act & Assert
        DiagnosticSeverity.Success.ToString().Should().Be("Success");
        DiagnosticSeverity.Info.ToString().Should().Be("Info");
        DiagnosticSeverity.Warning.ToString().Should().Be("Warning");
        DiagnosticSeverity.Error.ToString().Should().Be("Error");
    }

    [Fact]
    public void DiagnosticSeverity_CanBeUsedInCollections()
    {
        // Arrange & Act
        var severities = new List<DiagnosticSeverity>
        {
            DiagnosticSeverity.Success,
            DiagnosticSeverity.Info,
            DiagnosticSeverity.Warning,
            DiagnosticSeverity.Error
        };

        // Assert
        severities.Should().HaveCount(4);
        severities.Distinct().Should().HaveCount(4);
    }

    [Fact]
    public void DiagnosticSeverity_CanBeGrouped()
    {
        // Arrange
        var items = new List<(string Name, DiagnosticSeverity Severity)>
        {
            ("Item1", DiagnosticSeverity.Success),
            ("Item2", DiagnosticSeverity.Info),
            ("Item3", DiagnosticSeverity.Warning),
            ("Item4", DiagnosticSeverity.Error),
            ("Item5", DiagnosticSeverity.Warning)
        };

        // Act
        var grouped = items.GroupBy(i => i.Severity);

        // Assert
        grouped.Should().HaveCount(4);
        grouped.First(g => g.Key == DiagnosticSeverity.Warning).Should().HaveCount(2);
    }

    [Fact]
    public void DiagnosticSeverity_CanBeOrdered()
    {
        // Arrange
        var severities = new List<DiagnosticSeverity>
        {
            DiagnosticSeverity.Error,
            DiagnosticSeverity.Success,
            DiagnosticSeverity.Warning,
            DiagnosticSeverity.Info
        };

        // Act
        var ordered = severities.OrderBy(s => s).ToList();

        // Assert
        ordered[0].Should().Be(DiagnosticSeverity.Success);
        ordered[1].Should().Be(DiagnosticSeverity.Info);
        ordered[2].Should().Be(DiagnosticSeverity.Warning);
        ordered[3].Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public void DiagnosticSeverity_CanBeFiltered()
    {
        // Arrange
        var severities = Enum.GetValues(typeof(DiagnosticSeverity))
            .Cast<DiagnosticSeverity>()
            .ToList();

        // Act
        var highSeverity = severities.Where(s => (int)s >= (int)DiagnosticSeverity.Warning).ToList();

        // Assert
        highSeverity.Should().HaveCount(2);
        highSeverity.Should().Contain(DiagnosticSeverity.Warning);
        highSeverity.Should().Contain(DiagnosticSeverity.Error);
    }

    [Fact]
    public void DiagnosticSeverity_ShouldBeEnum()
    {
        // Arrange & Act
        var type = typeof(DiagnosticSeverity);

        // Assert
        type.IsEnum.Should().BeTrue();
    }

    [Fact]
    public void DiagnosticSeverity_CanBeUsedInLinqQueries()
    {
        // Arrange
        var severities = new List<DiagnosticSeverity>
        {
            DiagnosticSeverity.Success,
            DiagnosticSeverity.Info,
            DiagnosticSeverity.Warning,
            DiagnosticSeverity.Error,
            DiagnosticSeverity.Success,
            DiagnosticSeverity.Warning
        };

        // Act
        var severityCounts = severities.GroupBy(s => s)
            .ToDictionary(g => g.Key, g => g.Count());

        // Assert
        severityCounts[DiagnosticSeverity.Success].Should().Be(2);
        severityCounts[DiagnosticSeverity.Info].Should().Be(1);
        severityCounts[DiagnosticSeverity.Warning].Should().Be(2);
        severityCounts[DiagnosticSeverity.Error].Should().Be(1);
    }

    [Fact]
    public void DiagnosticSeverity_CanBeUsedWithBitwiseOperations()
    {
        // Arrange
        var severity1 = DiagnosticSeverity.Warning;
        var severity2 = DiagnosticSeverity.Error;

        // Act & Assert - Basic comparison operations
        ((int)severity1).Should().BeLessThan((int)severity2);
        ((int)severity2).Should().BeGreaterThan((int)severity1);
    }

    [Theory]
    [InlineData(DiagnosticSeverity.Success, 0)]
    [InlineData(DiagnosticSeverity.Info, 1)]
    [InlineData(DiagnosticSeverity.Warning, 2)]
    [InlineData(DiagnosticSeverity.Error, 3)]
    public void DiagnosticSeverity_ShouldHaveCorrectIntegerValues(DiagnosticSeverity severity, int expectedValue)
    {
        // Act & Assert
        ((int)severity).Should().Be(expectedValue);
    }

    [Fact]
    public void DiagnosticSeverity_CanBeUsedInConditionalLogic()
    {
        // Arrange
        var severity = DiagnosticSeverity.Warning;

        // Act & Assert
        (severity == DiagnosticSeverity.Success).Should().BeFalse();
        (severity == DiagnosticSeverity.Warning).Should().BeTrue();
        (severity != DiagnosticSeverity.Error).Should().BeTrue();
        (severity >= DiagnosticSeverity.Info).Should().BeTrue();
        (severity <= DiagnosticSeverity.Error).Should().BeTrue();
    }

    [Fact]
    public void DiagnosticSeverity_CanBeUsedInDiagnosticIssueCreation()
    {
        // Arrange & Act
        var issue = new Relay.CLI.Commands.Models.Diagnostic.DiagnosticIssue
        {
            Message = "Test issue",
            Severity = DiagnosticSeverity.Warning,
            Code = "TEST001"
        };

        // Assert
        issue.Severity.Should().Be(DiagnosticSeverity.Warning);
    }

    [Fact]
    public void DiagnosticSeverity_AllValues_ShouldBeUnique()
    {
        // Arrange & Act
        var values = Enum.GetValues(typeof(DiagnosticSeverity)).Cast<int>().ToList();

        // Assert
        values.Distinct().Should().HaveCount(4);
    }

    [Fact]
    public void DiagnosticSeverity_CanBeUsedInDictionary()
    {
        // Arrange & Act
        var severityDescriptions = new Dictionary<DiagnosticSeverity, string>
        {
            { DiagnosticSeverity.Success, "Operation completed successfully" },
            { DiagnosticSeverity.Info, "Informational message" },
            { DiagnosticSeverity.Warning, "Warning that may require attention" },
            { DiagnosticSeverity.Error, "Error that needs to be fixed" }
        };

        // Assert
        severityDescriptions.Should().HaveCount(4);
        severityDescriptions[DiagnosticSeverity.Error].Should().Be("Error that needs to be fixed");
    }

    [Fact]
    public void DiagnosticSeverity_CanBeUsedInReporting()
    {
        // Arrange
        var severities = new List<DiagnosticSeverity>
        {
            DiagnosticSeverity.Success,
            DiagnosticSeverity.Info,
            DiagnosticSeverity.Warning,
            DiagnosticSeverity.Error
        };

        // Act
        var report = severities.Select(s => new
        {
            Severity = s,
            Level = (int)s,
            Description = s.ToString()
        }).ToList();

        // Assert
        report[0].Description.Should().Be("Success");
        report[1].Description.Should().Be("Info");
        report[2].Description.Should().Be("Warning");
        report[3].Description.Should().Be("Error");
    }
}