using Relay.CLI.Commands.Models.Diagnostic;

namespace Relay.CLI.Tests.Commands;

public class DiagnosticResultTests
{
    [Fact]
    public void DiagnosticResult_ShouldHaveCategoryProperty()
    {
        // Arrange & Act
        var result = new DiagnosticResult { Category = "Project Structure" };

        // Assert
        result.Category.Should().Be("Project Structure");
    }

    [Fact]
    public void DiagnosticResult_ShouldHaveStatusProperty()
    {
        // Arrange & Act
        var result = new DiagnosticResult { Status = "Pass" };

        // Assert
        result.Status.Should().Be("Pass");
    }

    [Fact]
    public void DiagnosticResult_ShouldHaveMessageProperty()
    {
        // Arrange & Act
        var result = new DiagnosticResult { Message = "Project structure is valid" };

        // Assert
        result.Message.Should().Be("Project structure is valid");
    }

    [Fact]
    public void DiagnosticResult_DefaultValues_ShouldBeEmptyStrings()
    {
        // Arrange & Act
        var result = new DiagnosticResult();

        // Assert
        result.Category.Should().Be("");
        result.Status.Should().Be("");
        result.Message.Should().Be("");
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
        result.Category.Should().Be("Dependencies");
        result.Status.Should().Be("Warning");
        result.Message.Should().Contain("Outdated");
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
        result.Status.Should().Be(status);
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
        result.Category.Should().Be(category);
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
        result.Status.Should().Be("Pass");
        result.Message.Should().Contain("properly structured");
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
        result.Status.Should().Be("Warning");
        result.Message.Should().Contain("version mismatch");
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
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("No handlers");
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
        result.Message.Should().Be("");
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
        result.Message.Should().Contain("5 handlers");
        result.Message.Should().Contain("Handlers/");
        result.Message.Should().Contain("naming conventions");
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
            result.Category.Should().Be(category);
        }
    }

    [Fact]
    public void DiagnosticResult_ShouldBeClass()
    {
        // Arrange & Act
        var result = new DiagnosticResult();

        // Assert
        result.Should().NotBeNull();
        result.GetType().IsClass.Should().BeTrue();
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
        results.Should().HaveCount(3);
        results.Count(r => r.Status == "Pass").Should().Be(1);
        results.Count(r => r.Status == "Warning").Should().Be(1);
        results.Count(r => r.Status == "Error").Should().Be(1);
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
        errors.Should().HaveCount(1);
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
        handlerResults.Should().HaveCount(2);
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
        result.Category.Should().Be("Modified");
        result.Status.Should().Be("Error");
        result.Message.Should().Be("Modified message");
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
        result.Category.Should().Be("Project Structure");
        result.Status.Should().Be("Pass");
        result.Message.Should().Contain("directories");
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
        result.Category.Should().Be("Dependencies");
        result.Status.Should().Be("Warning");
        result.Message.Should().Contain("Relay.Core");
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
        result.Category.Should().Be("Handlers");
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("handler classes");
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
        result.Category.Should().Be("Performance");
        result.Status.Should().Be("Info");
        result.Message.Should().Contain("optimally configured");
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
        result.Category.Should().Be("Best Practices");
        result.Status.Should().Be("Pass");
        result.Message.Should().Contain("best practice");
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
        result.Message.Should().Contain("Dependency check results");
        result.Message.Should().Contain("Relay.Core: OK");
        result.Message.Should().Contain("Relay.MessageBroker: Outdated");
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
        result.Category.Should().Be(category);
        result.Status.Should().Be(status);
        result.Message.Should().Be(message);
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
        grouped.Should().HaveCount(3);
        grouped.First(g => g.Key == "Handlers").Should().HaveCount(2);
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
        ordered[0].Category.Should().Be("Dependencies");
        ordered[1].Category.Should().Be("Handlers");
        ordered[2].Category.Should().Be("Performance");
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
        result.Category.Should().Be("Best Practices Violation");
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
        result.Message.Should().Contain("'CreateUserHandler'");
        result.Message.Should().Contain("->");
        result.Message.Should().Contain("[Handle]");
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
        result.Status.Should().Be("Pass (with warnings)");
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
        result.Status.Should().Be("Pass");
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
        result.Status.Should().Be("Warning");
        result.Message.Should().Contain("updates");
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
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("missing");
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
        result.Status.Should().Be("Info");
        result.Message.Should().Contain("completed");
    }
}