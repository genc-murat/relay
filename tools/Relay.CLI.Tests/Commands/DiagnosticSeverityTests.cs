using Relay.CLI.Commands.Models.Diagnostic;
using Xunit;

namespace Relay.CLI.Tests.Commands;

public class DiagnosticSeverityTests
{
    [Fact]
    public void DiagnosticSeverity_ShouldHaveSuccessValue()
    {
        // Arrange & Act
        var severity = DiagnosticSeverity.Success;

        // Assert
        Assert.Equal(DiagnosticSeverity.Success, severity);
        Assert.Equal(0, (int)severity);
    }

    [Fact]
    public void DiagnosticSeverity_ShouldHaveInfoValue()
    {
        // Arrange & Act
        var severity = DiagnosticSeverity.Info;

        // Assert
        Assert.Equal(DiagnosticSeverity.Info, severity);
        Assert.Equal(1, (int)severity);
    }

    [Fact]
    public void DiagnosticSeverity_ShouldHaveWarningValue()
    {
        // Arrange & Act
        var severity = DiagnosticSeverity.Warning;

        // Assert
        Assert.Equal(DiagnosticSeverity.Warning, severity);
        Assert.Equal(2, (int)severity);
    }

    [Fact]
    public void DiagnosticSeverity_ShouldHaveErrorValue()
    {
        // Arrange & Act
        var severity = DiagnosticSeverity.Error;

        // Assert
        Assert.Equal(DiagnosticSeverity.Error, severity);
        Assert.Equal(3, (int)severity);
    }

    [Fact]
    public void DiagnosticSeverity_AllValues_ShouldBeDefined()
    {
        // Arrange & Act
        var values = Enum.GetValues(typeof(DiagnosticSeverity)).Cast<DiagnosticSeverity>().ToList();

        // Assert
        Assert.Equal(4, values.Count());
        Assert.Contains(DiagnosticSeverity.Success, values);
        Assert.Contains(DiagnosticSeverity.Info, values);
        Assert.Contains(DiagnosticSeverity.Warning, values);
        Assert.Contains(DiagnosticSeverity.Error, values);
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
        Assert.True((int)ordered[0] < (int)ordered[1]);
        Assert.True((int)ordered[1] < (int)ordered[2]);
        Assert.True((int)ordered[2] < (int)ordered[3]);
    }

    [Fact]
    public void DiagnosticSeverity_DefaultValue_ShouldBeSuccess()
    {
        // Arrange & Act
        var defaultSeverity = default(DiagnosticSeverity);

        // Assert
        Assert.Equal(DiagnosticSeverity.Success, defaultSeverity);
        Assert.Equal(0, (int)defaultSeverity);
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
        Assert.Equal("Warning", result);
    }

    [Fact]
    public void DiagnosticSeverity_CanBeParsedFromString()
    {
        // Act & Assert
        Assert.Equal(DiagnosticSeverity.Success, Enum.Parse<DiagnosticSeverity>("Success"));
        Assert.Equal(DiagnosticSeverity.Info, Enum.Parse<DiagnosticSeverity>("Info"));
        Assert.Equal(DiagnosticSeverity.Warning, Enum.Parse<DiagnosticSeverity>("Warning"));
        Assert.Equal(DiagnosticSeverity.Error, Enum.Parse<DiagnosticSeverity>("Error"));
    }

    [Fact]
    public void DiagnosticSeverity_CanBeConvertedToString()
    {
        // Act & Assert
        Assert.Equal("Success", DiagnosticSeverity.Success.ToString());
        Assert.Equal("Info", DiagnosticSeverity.Info.ToString());
        Assert.Equal("Warning", DiagnosticSeverity.Warning.ToString());
        Assert.Equal("Error", DiagnosticSeverity.Error.ToString());
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
        Assert.Equal(4, severities.Count());
        Assert.Equal(4, severities.Distinct().Count());
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
        Assert.Equal(4, grouped.Count());
        Assert.Equal(2, grouped.First(g => g.Key == DiagnosticSeverity.Warning).Count());
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
        Assert.Equal(DiagnosticSeverity.Success, ordered[0]);
        Assert.Equal(DiagnosticSeverity.Info, ordered[1]);
        Assert.Equal(DiagnosticSeverity.Warning, ordered[2]);
        Assert.Equal(DiagnosticSeverity.Error, ordered[3]);
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
        Assert.Equal(2, highSeverity.Count());
        Assert.Contains(DiagnosticSeverity.Warning, highSeverity);
        Assert.Contains(DiagnosticSeverity.Error, highSeverity);
    }

    [Fact]
    public void DiagnosticSeverity_ShouldBeEnum()
    {
        // Arrange & Act
        var type = typeof(DiagnosticSeverity);

        // Assert
        Assert.True(type.IsEnum);
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
        Assert.Equal(2, severityCounts[DiagnosticSeverity.Success]);
        Assert.Equal(1, severityCounts[DiagnosticSeverity.Info]);
        Assert.Equal(2, severityCounts[DiagnosticSeverity.Warning]);
        Assert.Equal(1, severityCounts[DiagnosticSeverity.Error]);
    }

    [Fact]
    public void DiagnosticSeverity_CanBeUsedWithBitwiseOperations()
    {
        // Arrange
        var severity1 = DiagnosticSeverity.Warning;
        var severity2 = DiagnosticSeverity.Error;

        // Act & Assert - Basic comparison operations
        Assert.True((int)severity1 < (int)severity2);
        Assert.True((int)severity2 > (int)severity1);
    }

    [Theory]
    [InlineData(DiagnosticSeverity.Success, 0)]
    [InlineData(DiagnosticSeverity.Info, 1)]
    [InlineData(DiagnosticSeverity.Warning, 2)]
    [InlineData(DiagnosticSeverity.Error, 3)]
    public void DiagnosticSeverity_ShouldHaveCorrectIntegerValues(DiagnosticSeverity severity, int expectedValue)
    {
        // Act & Assert
        Assert.Equal(expectedValue, (int)severity);
    }

    [Fact]
    public void DiagnosticSeverity_CanBeUsedInConditionalLogic()
    {
        // Arrange
        var severity = DiagnosticSeverity.Warning;

        // Act & Assert
        Assert.False(severity == DiagnosticSeverity.Success);
        Assert.True(severity == DiagnosticSeverity.Warning);
        Assert.True(severity != DiagnosticSeverity.Error);
        Assert.True(severity >= DiagnosticSeverity.Info);
        Assert.True(severity <= DiagnosticSeverity.Error);
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
        Assert.Equal(DiagnosticSeverity.Warning, issue.Severity);
    }

    [Fact]
    public void DiagnosticSeverity_AllValues_ShouldBeUnique()
    {
        // Arrange & Act
        var values = Enum.GetValues(typeof(DiagnosticSeverity)).Cast<int>().ToList();

        // Assert
        Assert.Equal(4, values.Distinct().Count());
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
        Assert.Equal(4, severityDescriptions.Count());
        Assert.Equal("Error that needs to be fixed", severityDescriptions[DiagnosticSeverity.Error]);
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
        Assert.Equal("Success", report[0].Description);
        Assert.Equal("Info", report[1].Description);
        Assert.Equal("Warning", report[2].Description);
        Assert.Equal("Error", report[3].Description);
    }
}

