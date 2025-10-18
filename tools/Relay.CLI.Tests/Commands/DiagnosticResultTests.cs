using Relay.CLI.Commands.Models.Diagnostic;
using Xunit;

namespace Relay.CLI.Tests.Commands;

public class DiagnosticResultTests
{
    [Fact]
    public void DiagnosticResult_ShouldHaveCategoryProperty()
    {
        // Arrange & Act
        var result = new DiagnosticResult { Category = "Project Structure" };

        // Assert
        Assert.Equal("Project Structure", result.Category);
    }

    [Fact]
    public void DiagnosticResult_ShouldHaveStatusProperty()
    {
        // Arrange & Act
        var result = new DiagnosticResult { Status = "Pass" };

        // Assert
        Assert.Equal("Pass", result.Status);
    }

    [Fact]
    public void DiagnosticResult_ShouldHaveMessageProperty()
    {
        // Arrange & Act
        var result = new DiagnosticResult { Message = "Project structure is valid" };

        // Assert
        Assert.Equal("Project structure is valid", result.Message);
    }

    [Fact]
    public void DiagnosticResult_DefaultValues_ShouldBeEmptyStrings()
    {
        // Arrange & Act
        var result = new DiagnosticResult();

        // Assert
        Assert.Equal("", result.Category);
        Assert.Equal("", result.Status);
        Assert.Equal("", result.Message);
    }

    [Fact]
    public void DiagnosticResult_CanSetAllPropertiesViaInitializer()
    {
        // Arrange & Act
        var result = new DiagnosticResult
        {
            Category = "Dependencies",
            Status = "Warning",
            Message = "Outdated package found"
        };

        // Assert
        Assert.Equal("Dependencies", result.Category);
        Assert.Equal("Warning", result.Status);
        Assert.Contains("Outdated", result.Message);
    }

    [Theory]
    [InlineData("Pass")]
    [InlineData("Warning")]
    [InlineData("Error")]
    [InlineData("Info")]
    public void DiagnosticResult_ShouldSupportVariousStatuses(string status)
    {
        // Arrange & Act
        var result = new DiagnosticResult { Status = status };

        // Assert
        Assert.Equal(status, result.Status);
    }

    [Theory]
    [InlineData("Project Structure")]
    [InlineData("Dependencies")]
    [InlineData("Handlers")]
    [InlineData("Performance")]
    [InlineData("Best Practices")]
    public void DiagnosticResult_ShouldSupportVariousCategories(string category)
    {
        // Arrange & Act
        var result = new DiagnosticResult { Category = category };

        // Assert
        Assert.Equal(category, result.Category);
    }

    [Fact]
    public void DiagnosticResult_PassStatus_WithValidMessage()
    {
        // Arrange & Act
        var result = new DiagnosticResult
        {
            Category = "Project Structure",
            Status = "Pass",
            Message = "All project files are properly structured"
        };

        // Assert
        Assert.Equal("Pass", result.Status);
        Assert.Contains("properly structured", result.Message);
    }

    [Fact]
    public void DiagnosticResult_WarningStatus_WithSuggestion()
    {
        // Arrange & Act
        var result = new DiagnosticResult
        {
            Category = "Dependencies",
            Status = "Warning",
            Message = "Package version mismatch detected"
        };

        // Assert
        Assert.Equal("Warning", result.Status);
        Assert.Contains("version mismatch", result.Message);
    }

    [Fact]
    public void DiagnosticResult_ErrorStatus_WithCriticalMessage()
    {
        // Arrange & Act
        var result = new DiagnosticResult
        {
            Category = "Handlers",
            Status = "Error",
            Message = "No handlers found in the project"
        };

        // Assert
        Assert.Equal("Error", result.Status);
        Assert.Contains("No handlers", result.Message);
    }

    [Fact]
    public void DiagnosticResult_Message_CanBeEmpty()
    {
        // Arrange & Act
        var result = new DiagnosticResult
        {
            Category = "Configuration",
            Status = "Info"
        };

        // Assert
        Assert.Equal("", result.Message);
    }

    [Fact]
    public void DiagnosticResult_Message_CanContainDetailedInformation()
    {
        // Arrange & Act
        var result = new DiagnosticResult
        {
            Message = "Found 5 handlers in Handlers/ directory, all following proper naming conventions"
        };

        // Assert
        Assert.Contains("5 handlers", result.Message);
        Assert.Contains("Handlers/", result.Message);
        Assert.Contains("naming conventions", result.Message);
    }

    [Fact]
    public void DiagnosticResult_CategoryProperty_CanDescribeVariousAreas()
    {
        // Arrange
        var categories = new[]
        {
            "Project Structure",
            "Dependencies",
            "Handlers",
            "Performance",
            "Best Practices",
            "Configuration",
            "Code Quality"
        };

        foreach (var category in categories)
        {
            // Act
            var result = new DiagnosticResult { Category = category };

            // Assert
            Assert.Equal(category, result.Category);
        }
    }

    [Fact]
    public void DiagnosticResult_ShouldBeClass()
    {
        // Arrange & Act
        var result = new DiagnosticResult();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.GetType().IsClass);
    }

    [Fact]
    public void DiagnosticResult_CanBeUsedInList()
    {
        // Arrange & Act
        var results = new List<DiagnosticResult>
        {
            new DiagnosticResult { Category = "Structure", Status = "Pass" },
            new DiagnosticResult { Category = "Dependencies", Status = "Warning" },
            new DiagnosticResult { Category = "Handlers", Status = "Error" }
        };

        // Assert
        Assert.Equal(3, results.Count());
        Assert.Equal(1, results.Count(r => r.Status == "Pass"));
        Assert.Equal(1, results.Count(r => r.Status == "Warning"));
        Assert.Equal(1, results.Count(r => r.Status == "Error"));
    }

    [Fact]
    public void DiagnosticResult_CanBeFiltered_ByStatus()
    {
        // Arrange
        var results = new List<DiagnosticResult>
        {
            new DiagnosticResult { Status = "Pass" },
            new DiagnosticResult { Status = "Pass" },
            new DiagnosticResult { Status = "Warning" },
            new DiagnosticResult { Status = "Error" }
        };

        // Act
        var errors = results.Where(r => r.Status == "Error").ToList();

        // Assert
        Assert.Equal(1, errors.Count());
    }

    [Fact]
    public void DiagnosticResult_CanBeFiltered_ByCategory()
    {
        // Arrange
        var results = new List<DiagnosticResult>
        {
            new DiagnosticResult { Category = "Handlers" },
            new DiagnosticResult { Category = "Handlers" },
            new DiagnosticResult { Category = "Dependencies" },
            new DiagnosticResult { Category = "Performance" }
        };

        // Act
        var handlerResults = results.Where(r => r.Category == "Handlers").ToList();

        // Assert
        Assert.Equal(2, handlerResults.Count());
    }

    [Fact]
    public void DiagnosticResult_PropertiesCanBeModified()
    {
        // Arrange
        var result = new DiagnosticResult
        {
            Category = "Initial",
            Status = "Pass",
            Message = "Initial message"
        };

        // Act
        result.Category = "Modified";
        result.Status = "Error";
        result.Message = "Modified message";

        // Assert
        Assert.Equal("Modified", result.Category);
        Assert.Equal("Error", result.Status);
        Assert.Equal("Modified message", result.Message);
    }

    [Fact]
    public void DiagnosticResult_WithProjectStructureCheck_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new DiagnosticResult
        {
            Category = "Project Structure",
            Status = "Pass",
            Message = "All required directories and files are present"
        };

        // Assert
        Assert.Equal("Project Structure", result.Category);
        Assert.Equal("Pass", result.Status);
        Assert.Contains("directories", result.Message);
    }

    [Fact]
    public void DiagnosticResult_WithDependencyCheck_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new DiagnosticResult
        {
            Category = "Dependencies",
            Status = "Warning",
            Message = "Outdated Relay.Core package detected"
        };

        // Assert
        Assert.Equal("Dependencies", result.Category);
        Assert.Equal("Warning", result.Status);
        Assert.Contains("Relay.Core", result.Message);
    }

    [Fact]
    public void DiagnosticResult_WithHandlerCheck_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new DiagnosticResult
        {
            Category = "Handlers",
            Status = "Error",
            Message = "No handler classes found in the expected locations"
        };

        // Assert
        Assert.Equal("Handlers", result.Category);
        Assert.Equal("Error", result.Status);
        Assert.Contains("handler classes", result.Message);
    }

    [Fact]
    public void DiagnosticResult_WithPerformanceCheck_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new DiagnosticResult
        {
            Category = "Performance",
            Status = "Info",
            Message = "Performance settings are optimally configured"
        };

        // Assert
        Assert.Equal("Performance", result.Category);
        Assert.Equal("Info", result.Status);
        Assert.Contains("optimally configured", result.Message);
    }

    [Fact]
    public void DiagnosticResult_WithBestPracticesCheck_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new DiagnosticResult
        {
            Category = "Best Practices",
            Status = "Pass",
            Message = "All best practice checks passed"
        };

        // Assert
        Assert.Equal("Best Practices", result.Category);
        Assert.Equal("Pass", result.Status);
        Assert.Contains("best practice", result.Message);
    }

    [Fact]
    public void DiagnosticResult_MessageCanBeMultiline()
    {
        // Arrange & Act
        var result = new DiagnosticResult
        {
            Message = "Dependency check results:\n- Relay.Core: OK\n- Relay.MessageBroker: Outdated"
        };

        // Assert
        Assert.Contains("Dependency check results", result.Message);
        Assert.Contains("Relay.Core: OK", result.Message);
        Assert.Contains("Relay.MessageBroker: Outdated", result.Message);
    }

    [Theory]
    [InlineData("Project Structure", "Pass", "All files present")]
    [InlineData("Dependencies", "Warning", "Version mismatch")]
    [InlineData("Handlers", "Error", "Missing handlers")]
    public void DiagnosticResult_WithVariousScenarios_ShouldStoreCorrectValues(
        string category,
        string status,
        string message)
    {
        // Arrange & Act
        var result = new DiagnosticResult
        {
            Category = category,
            Status = status,
            Message = message
        };

        // Assert
        Assert.Equal(category, result.Category);
        Assert.Equal(status, result.Status);
        Assert.Equal(message, result.Message);
    }

    [Fact]
    public void DiagnosticResult_CanBeGroupedByCategory()
    {
        // Arrange
        var results = new List<DiagnosticResult>
        {
            new DiagnosticResult { Category = "Handlers" },
            new DiagnosticResult { Category = "Handlers" },
            new DiagnosticResult { Category = "Dependencies" },
            new DiagnosticResult { Category = "Performance" }
        };

        // Act
        var grouped = results.GroupBy(r => r.Category);

        // Assert
        Assert.Equal(3, grouped.Count());
        Assert.Equal(2, grouped.First(g => g.Key == "Handlers").Count());
    }

    [Fact]
    public void DiagnosticResult_CanBeOrdered_ByCategory()
    {
        // Arrange
        var results = new List<DiagnosticResult>
        {
            new DiagnosticResult { Category = "Performance" },
            new DiagnosticResult { Category = "Dependencies" },
            new DiagnosticResult { Category = "Handlers" }
        };

        // Act
        var ordered = results.OrderBy(r => r.Category).ToList();

        // Assert
        Assert.Equal("Dependencies", ordered[0].Category);
        Assert.Equal("Handlers", ordered[1].Category);
        Assert.Equal("Performance", ordered[2].Category);
    }

    [Fact]
    public void DiagnosticResult_CategoryCanContainSpaces()
    {
        // Arrange & Act
        var result = new DiagnosticResult
        {
            Category = "Best Practices Violation"
        };

        // Assert
        Assert.Equal("Best Practices Violation", result.Category);
    }

    [Fact]
    public void DiagnosticResult_MessageCanContainSpecialCharacters()
    {
        // Arrange & Act
        var result = new DiagnosticResult
        {
            Message = "Handler 'CreateUserHandler' -> missing [Handle] attribute"
        };

        // Assert
        Assert.Contains("'CreateUserHandler'", result.Message);
        Assert.Contains("->", result.Message);
        Assert.Contains("[Handle]", result.Message);
    }

    [Fact]
    public void DiagnosticResult_StatusCanContainSpecialCharacters()
    {
        // Arrange & Act
        var result = new DiagnosticResult
        {
            Status = "Pass (with warnings)"
        };

        // Assert
        Assert.Equal("Pass (with warnings)", result.Status);
    }

    [Fact]
    public void DiagnosticResult_CanRepresentSuccess()
    {
        // Arrange & Act
        var result = new DiagnosticResult
        {
            Category = "Configuration",
            Status = "Pass",
            Message = "All configuration files are valid"
        };

        // Assert
        Assert.Equal("Pass", result.Status);
    }

    [Fact]
    public void DiagnosticResult_CanRepresentWarning()
    {
        // Arrange & Act
        var result = new DiagnosticResult
        {
            Category = "Dependencies",
            Status = "Warning",
            Message = "Some packages have available updates"
        };

        // Assert
        Assert.Equal("Warning", result.Status);
        Assert.Contains("updates", result.Message);
    }

    [Fact]
    public void DiagnosticResult_CanRepresentError()
    {
        // Arrange & Act
        var result = new DiagnosticResult
        {
            Category = "Project Structure",
            Status = "Error",
            Message = "Critical project files are missing"
        };

        // Assert
        Assert.Equal("Error", result.Status);
        Assert.Contains("missing", result.Message);
    }

    [Fact]
    public void DiagnosticResult_CanRepresentInfo()
    {
        // Arrange & Act
        var result = new DiagnosticResult
        {
            Category = "Performance",
            Status = "Info",
            Message = "Performance analysis completed"
        };

        // Assert
        Assert.Equal("Info", result.Status);
        Assert.Contains("completed", result.Message);
    }
}

