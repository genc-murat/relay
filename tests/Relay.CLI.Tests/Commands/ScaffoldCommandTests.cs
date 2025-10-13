using System.CommandLine;
using Relay.CLI.Commands;
using Xunit;
using System.IO;
using System.Threading.Tasks;

namespace Relay.CLI.Tests.Commands;

public class ScaffoldCommandTests
{
    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        // Arrange & Act
        var command = ScaffoldCommand.Create();

        // Assert
        Assert.Equal("scaffold", command.Name);
        Assert.Equal("Generate boilerplate code for handlers, requests, and tests", command.Description);
    }

    [Fact]
    public void Create_CommandHasRequiredOptions()
    {
        // Arrange & Act
        var command = ScaffoldCommand.Create();

        // Assert
        Assert.Contains(command.Options, o => o.Name == "handler");
        Assert.Contains(command.Options, o => o.Name == "request");
        Assert.Contains(command.Options, o => o.Name == "response");
        Assert.Contains(command.Options, o => o.Name == "namespace");
        Assert.Contains(command.Options, o => o.Name == "output");
        Assert.Contains(command.Options, o => o.Name == "template");
        Assert.Contains(command.Options, o => o.Name == "include-tests");
        Assert.Contains(command.Options, o => o.Name == "include-validation");
    }

    [Fact]
    public async Task ExecuteScaffold_WithMinimalTemplate_CreatesFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayScaffoldMinimalTest");
        var handlerName = "TestHandler";
        var requestName = "TestRequest";

        try
        {
            // Act
            await ScaffoldCommand.ExecuteScaffold(
                handlerName, requestName, null, "TestNamespace",
                tempDir, "minimal", false, false);

            // Assert
            Assert.True(File.Exists(Path.Combine(tempDir, $"{requestName}.cs")));
            Assert.True(File.Exists(Path.Combine(tempDir, $"{handlerName}.cs")));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteScaffold_WithStandardTemplate_IncludesTests()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayScaffoldStandardTest");
        var handlerName = "TestHandler";
        var requestName = "TestRequest";

        try
        {
            // Act
            await ScaffoldCommand.ExecuteScaffold(
                handlerName, requestName, null, "TestNamespace",
                tempDir, "standard", true, false);

            // Assert
            Assert.True(File.Exists(Path.Combine(tempDir, $"{requestName}.cs")));
            Assert.True(File.Exists(Path.Combine(tempDir, $"{handlerName}.cs")));
            Assert.True(File.Exists(Path.Combine(tempDir, $"{handlerName}Tests.cs")));
            Assert.True(File.Exists(Path.Combine(tempDir, $"{handlerName}IntegrationTests.cs")));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteScaffold_WithResponse_CreatesResponseFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayScaffoldResponseTest");
        var handlerName = "TestHandler";
        var requestName = "TestRequest";
        var responseName = "TestResponse";

        try
        {
            // Act
            await ScaffoldCommand.ExecuteScaffold(
                handlerName, requestName, responseName, "TestNamespace",
                tempDir, "standard", false, false);

            // Assert
            var requestFile = Path.Combine(tempDir, $"{requestName}.cs");
            Assert.True(File.Exists(requestFile));
            var requestContent = await File.ReadAllTextAsync(requestFile);
            Assert.Contains(responseName, requestContent);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteScaffold_WithValidation_IncludesValidationAttributes()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayScaffoldValidationTest");
        var handlerName = "TestHandler";
        var requestName = "TestRequest";

        try
        {
            // Act
            await ScaffoldCommand.ExecuteScaffold(
                handlerName, requestName, null, "TestNamespace",
                tempDir, "standard", false, true);

            // Assert
            var requestFile = Path.Combine(tempDir, $"{requestName}.cs");
            Assert.True(File.Exists(requestFile));
            var requestContent = await File.ReadAllTextAsync(requestFile);
            Assert.Contains("[Required", requestContent);
            Assert.Contains("[StringLength", requestContent);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteScaffold_WithEnterpriseTemplate_IncludesAdvancedFeatures()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayScaffoldEnterpriseTest");
        var handlerName = "TestHandler";
        var requestName = "TestRequest";

        try
        {
            // Act
            await ScaffoldCommand.ExecuteScaffold(
                handlerName, requestName, null, "TestNamespace",
                tempDir, "enterprise", false, false);

            // Assert
            var requestFile = Path.Combine(tempDir, $"{requestName}.cs");
            Assert.True(File.Exists(requestFile));
            var requestContent = await File.ReadAllTextAsync(requestFile);
            Assert.Contains("[Cacheable", requestContent);
            Assert.Contains("[Authorize", requestContent);
            Assert.Contains("[JsonPropertyName", requestContent);

            var handlerFile = Path.Combine(tempDir, $"{handlerName}.cs");
            var handlerContent = await File.ReadAllTextAsync(handlerFile);
            Assert.Contains("PERFORMANCE TIPS", handlerContent);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteScaffold_CreatesValidRequestCode()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayScaffoldRequestTest");
        var handlerName = "TestHandler";
        var requestName = "TestRequest";

        try
        {
            // Act
            await ScaffoldCommand.ExecuteScaffold(
                handlerName, requestName, null, "TestNamespace",
                tempDir, "standard", false, false);

            // Assert
            var requestFile = Path.Combine(tempDir, $"{requestName}.cs");
            var requestContent = await File.ReadAllTextAsync(requestFile);
            Assert.Contains("using Relay.Core;", requestContent);
            Assert.Contains($"namespace TestNamespace;", requestContent);
            Assert.Contains($"public record {requestName}", requestContent);
            Assert.Contains("IRequest", requestContent);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteScaffold_CreatesValidHandlerCode()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayScaffoldHandlerTest");
        var handlerName = "TestHandler";
        var requestName = "TestRequest";

        try
        {
            // Act
            await ScaffoldCommand.ExecuteScaffold(
                handlerName, requestName, null, "TestNamespace",
                tempDir, "standard", false, false);

            // Assert
            var handlerFile = Path.Combine(tempDir, $"{handlerName}.cs");
            var handlerContent = await File.ReadAllTextAsync(handlerFile);
            Assert.Contains("using Relay.Core;", handlerContent);
            Assert.Contains($"namespace TestNamespace;", handlerContent);
            Assert.Contains($"public class {handlerName}", handlerContent);
            Assert.Contains("[Handle]", handlerContent);
            Assert.Contains("ValueTask", handlerContent);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteScaffold_CreatesValidTestCode()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayScaffoldTestTest");
        var handlerName = "TestHandler";
        var requestName = "TestRequest";

        try
        {
            // Act
            await ScaffoldCommand.ExecuteScaffold(
                handlerName, requestName, null, "TestNamespace",
                tempDir, "standard", true, false);

            // Assert
            var testFile = Path.Combine(tempDir, $"{handlerName}Tests.cs");
            var testContent = await File.ReadAllTextAsync(testFile);
            Assert.Contains("using Xunit;", testContent);
            Assert.Contains($"namespace TestNamespace.Tests;", testContent);
            Assert.Contains($"public class {handlerName}Tests", testContent);
            Assert.Contains("[Fact]", testContent);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}