using System.CommandLine;
using System.Text;
using Spectre.Console;

namespace Relay.CLI.Commands;

public static class ScaffoldCommand
{
    public static Command Create()
    {
        var command = new Command("scaffold", "Generate boilerplate code for handlers, requests, and tests");

        var handlerOption = new Option<string>("--handler", "Handler class name") { IsRequired = true };
        var requestOption = new Option<string>("--request", "Request class name") { IsRequired = true };
        var responseOption = new Option<string>("--response", "Response class name (optional)");
        var namespaceOption = new Option<string>("--namespace", () => "YourApp", "Target namespace");
        var outputOption = new Option<string>("--output", () => ".", "Output directory");
        var templateOption = new Option<string>("--template", () => "standard", "Template type (standard, minimal, enterprise)");
        var includeTestsOption = new Option<bool>("--include-tests", () => true, "Generate test files");
        var includeValidationOption = new Option<bool>("--include-validation", () => false, "Include validation pipeline");

        command.AddOption(handlerOption);
        command.AddOption(requestOption);
        command.AddOption(responseOption);
        command.AddOption(namespaceOption);
        command.AddOption(outputOption);
        command.AddOption(templateOption);
        command.AddOption(includeTestsOption);
        command.AddOption(includeValidationOption);

        command.SetHandler(async (handler, request, response, ns, output, template, includeTests, includeValidation) =>
        {
            await ExecuteScaffold(handler, request, response, ns, output, template, includeTests, includeValidation);
        }, handlerOption, requestOption, responseOption, namespaceOption, outputOption, templateOption, includeTestsOption, includeValidationOption);

        return command;
    }

    private static async Task ExecuteScaffold(
        string handlerName, 
        string requestName, 
        string? responseName, 
        string namespaceName,
        string outputPath,
        string template,
        bool includeTests,
        bool includeValidation)
    {
        try
        {
            // Ensure output directory exists
            Directory.CreateDirectory(outputPath);
            
            // Generate the code files
            var requestCode = GenerateRequest(requestName, responseName, namespaceName, template, includeValidation);
            var requestFile = Path.Combine(outputPath, $"{requestName}.cs");
            await File.WriteAllTextAsync(requestFile, requestCode);

            var handlerCode = GenerateHandler(handlerName, requestName, responseName, namespaceName, template);
            var handlerFile = Path.Combine(outputPath, $"{handlerName}.cs");
            await File.WriteAllTextAsync(handlerFile, handlerCode);

            if (includeTests)
            {
                var testCode = GenerateTests(handlerName, requestName, responseName, namespaceName, template);
                var testFile = Path.Combine(outputPath, $"{handlerName}Tests.cs");
                await File.WriteAllTextAsync(testFile, testCode);

                var integrationTestCode = GenerateIntegrationTests(handlerName, requestName, responseName, namespaceName);
                var integrationTestFile = Path.Combine(outputPath, $"{handlerName}IntegrationTests.cs");
                await File.WriteAllTextAsync(integrationTestFile, integrationTestCode);
            }

            // Try to show progress and UI in console if possible
            try
            {
                AnsiConsole.MarkupLine($"[green]🏗️  Scaffolding Relay components...[/]");
                
                await AnsiConsole.Progress()
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask("[green]Generating files[/]", maxValue: includeTests ? 4 : 2);

                        // Simulate progress after files are already created
                        await Task.Delay(100); // Small delay to show progress
                        task.Increment(1);
                        AnsiConsole.MarkupLine($"[dim]✓ Generated {requestName}.cs[/]");

                        await Task.Delay(100); // Small delay to show progress
                        task.Increment(1);
                        AnsiConsole.MarkupLine($"[dim]✓ Generated {handlerName}.cs[/]");

                        if (includeTests)
                        {
                            await Task.Delay(100); // Small delay to show progress
                            task.Increment(1);
                            AnsiConsole.MarkupLine($"[dim]✓ Generated {handlerName}Tests.cs[/]");

                            await Task.Delay(100); // Small delay to show progress
                            task.Increment(1);
                            AnsiConsole.MarkupLine($"[dim]✓ Generated {handlerName}IntegrationTests.cs[/]");
                        }

                        task.Value = task.MaxValue;
                    });

                // Display success summary
                var panel = new Panel(BuildSuccessMessage(handlerName, requestName, responseName, includeTests))
                    .Header("[green]✅ Scaffolding Complete[/]")
                    .BorderColor(Color.Green);

                AnsiConsole.Write(panel);
            }
            catch
            {
                // If console operations fail (e.g., in test environment), just continue
                // The files were already generated successfully above
            }
        }
        catch (Exception ex)
        {
            // Handle any exception and try to display error message
            try
            {
                var errorPanel = new Panel($"[red]Error during scaffolding:[/] {ex.Message}\n\n[red]Stack Trace:[/] {ex.StackTrace}")
                    .Header("[red]❌ Scaffolding Failed[/]")
                    .BorderColor(Color.Red);

                AnsiConsole.Write(errorPanel);
            }
            catch
            {
                // If console operations fail, just re-throw the original exception
            }
            throw; // Re-throw to ensure command returns non-zero exit code
        }
    }

    private static string GenerateRequest(string requestName, string? responseName, string namespaceName, string template, bool includeValidation)
    {
        var hasResponse = !string.IsNullOrEmpty(responseName);
        var baseInterface = hasResponse ? $"IRequest<{responseName}>" : "IRequest";
        
        var validationAttributes = includeValidation ? 
            $@"    [Required(ErrorMessage = ""Parameter is required"")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = ""Parameter must be between 1 and 100 characters"")]" : "";

        var usingStatements = template switch
        {
            "enterprise" => @"using Relay.Core;
using Relay.Core.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;",
            "minimal" => "using Relay.Core;",
            _ => @"using Relay.Core;
using System.ComponentModel.DataAnnotations;"
        };

        var requestBody = template switch
        {
            "enterprise" => $@"/// <summary>
/// Request for {requestName.Replace("Request", "").Replace("Query", "").Replace("Command", "")} operation
/// </summary>
[Cacheable(Duration = 300)] // Cache for 5 minutes if applicable
[Authorize(Roles = ""User"")] // Add authorization if needed
public record {requestName}(
    /// <summary>
    /// Example parameter - replace with your actual parameters
    /// </summary>{validationAttributes}
    [JsonPropertyName(""exampleParameter"")]
    string ExampleParameter,
    
    /// <summary>
    /// Optional correlation ID for tracking
    /// </summary>
    [JsonPropertyName(""correlationId"")]
    string? CorrelationId = null
) : {baseInterface};",
            "minimal" => $@"public record {requestName}(string ExampleParameter) : {baseInterface};",
            _ => $@"/// <summary>
/// Request for {requestName.Replace("Request", "").Replace("Query", "").Replace("Command", "")} operation
/// </summary>
public record {requestName}(
    /// <summary>
    /// Example parameter - replace with your actual parameters
    /// </summary>{validationAttributes}
    string ExampleParameter
) : {baseInterface};"
        };

        var responseBody = hasResponse ? $@"

/// <summary>
/// Response for {requestName}
/// </summary>
public record {responseName}(
    /// <summary>
    /// Example result - replace with your actual response properties
    /// </summary>
    string ExampleResult,
    
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    bool IsSuccess = true,
    
    /// <summary>
    /// Optional error message
    /// </summary>
    string? ErrorMessage = null
);" : "";

        return $@"{usingStatements}

namespace {namespaceName};

{requestBody}{responseBody}
";
    }

    private static string GenerateHandler(string handlerName, string requestName, string? responseName, string namespaceName, string template)
    {
        var hasResponse = !string.IsNullOrEmpty(responseName);
        var returnType = hasResponse ? $"ValueTask<{responseName}>" : "ValueTask";
        var methodName = requestName.Replace("Request", "").Replace("Query", "").Replace("Command", "");
        
        var usingStatements = template switch
        {
            "enterprise" => @"using Relay.Core;
using Relay.Core.Attributes;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;",
            "minimal" => @"using Relay.Core;
using System.Threading;
using System.Threading.Tasks;",
            _ => @"using Relay.Core;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;"
        };

        var dependencies = template switch
        {
            "enterprise" => $@"    private readonly ILogger<{handlerName}> _logger;
    // Add your dependencies here (repositories, services, etc.)
    // private readonly IYourRepository _repository;
    // private readonly IYourService _service;
    
    public {handlerName}(ILogger<{handlerName}> logger)
    {{
        _logger = logger;
        // _repository = repository;
        // _service = service;
    }}",
            "minimal" => @"    // Add your dependencies here if needed
    // private readonly IYourService _service;",
            _ => $@"    private readonly ILogger<{handlerName}> _logger;
    // Add your dependencies here
    // private readonly IYourService _service;
    
    public {handlerName}(ILogger<{handlerName}> logger)
    {{
        _logger = logger;
        // _service = service;
    }}"
        };

        var implementation = template switch
        {
            "enterprise" => hasResponse ? $@"        _logger.LogInformation(""Processing {{RequestType}} with parameter: {{Parameter}}"", 
            nameof({requestName}), request.ExampleParameter);
        
        try
        {{
            // TODO: Implement your business logic here
            await Task.Delay(1, cancellationToken); // Remove this - just for demo
            
            var result = new {responseName}(
                ExampleResult: $""Processed: {{request.ExampleParameter}}"",
                IsSuccess: true
            );
            
            _logger.LogInformation(""Successfully processed {{RequestType}}"", nameof({requestName}));
            return result;
        }}
        catch (Exception ex)
        {{
            _logger.LogError(ex, ""Failed to process {{RequestType}}"", nameof({requestName}));
            return new {responseName}(
                ExampleResult: string.Empty,
                IsSuccess: false,
                ErrorMessage: ex.Message
            );
        }}" : $@"        _logger.LogInformation(""Processing {{RequestType}} with parameter: {{Parameter}}"", 
            nameof({requestName}), request.ExampleParameter);
        
        try
        {{
            // TODO: Implement your business logic here
            await Task.Delay(1, cancellationToken); // Remove this - just for demo
            
            _logger.LogInformation(""Successfully processed {{RequestType}}"", nameof({requestName}));
        }}
        catch (Exception ex)
        {{
            _logger.LogError(ex, ""Failed to process {{RequestType}}"", nameof({requestName}));
            throw;
        }}",
            "minimal" => hasResponse ? $@"        // TODO: Implement your business logic here
        await Task.Delay(1, cancellationToken);
        return new {responseName}(""Processed: "" + request.ExampleParameter);" : @"        // TODO: Implement your business logic here
        await Task.Delay(1, cancellationToken);",
            _ => hasResponse ? $@"        _logger?.LogInformation(""Processing {{RequestType}}"", nameof({requestName}));
        
        // TODO: Implement your business logic here
        await Task.Delay(1, cancellationToken);
        
        return new {responseName}(""Processed: "" + request.ExampleParameter);" : $@"        _logger?.LogInformation(""Processing {{RequestType}}"", nameof({requestName}));
        
        // TODO: Implement your business logic here
        await Task.Delay(1, cancellationToken);"
        };

        var performanceHints = template == "enterprise" ? @"
    
    // 🚀 PERFORMANCE TIPS:
    // 1. Use ValueTask<T> instead of Task<T> for better performance
    // 2. Consider caching results for read operations
    // 3. Use cancellation tokens properly
    // 4. Log performance metrics for monitoring
    // 5. Consider using Relay's ultra-fast implementations" : "";

        return $@"{usingStatements}

namespace {namespaceName};

/// <summary>
/// Handler for {requestName}
/// Processes {methodName} operations with high performance
/// </summary>
public class {handlerName}
{{
{dependencies}

    /// <summary>
    /// Handles the {requestName} with optimal performance
    /// </summary>
    [Handle]
    public async {returnType} Handle{methodName}(
        {requestName} request, 
        CancellationToken cancellationToken = default)
    {{
{implementation}
    }}{performanceHints}
}}
";
    }

    private static string GenerateTests(string handlerName, string requestName, string? responseName, string namespaceName, string template)
    {
        var hasResponse = !string.IsNullOrEmpty(responseName);
        var methodName = requestName.Replace("Request", "").Replace("Query", "").Replace("Command", "");

        var testAssertions = hasResponse ? $@"        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Contains(""Processed"", result.ExampleResult);" : @"        // Assert - verify side effects or state changes
        // Add specific assertions based on your business logic";

        var performanceTest = template == "enterprise" ? $@"
    [Fact]
    public async Task Handle{methodName}_Performance_ShouldBeFast()
    {{
        // Arrange
        var handler = CreateHandler();
        var request = new {requestName}(""Performance Test"");
        
        // Act - Warm up
        await handler.Handle{methodName}(request);
        
        // Act - Measure performance
        var stopwatch = Stopwatch.StartNew();
        const int iterations = 1000;
        
        for (int i = 0; i < iterations; i++)
        {{
            await handler.Handle{methodName}(new {requestName}($""Test {{i}}""));
        }}
        
        stopwatch.Stop();
        
        // Assert
        var avgTime = stopwatch.Elapsed.TotalMilliseconds / iterations;
        Assert.True(avgTime < 10, $""Average execution time {{avgTime:F2}}ms should be less than 10ms"");
    }}" : "";

        return $@"using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace {namespaceName}.Tests;

public class {handlerName}Tests
{{
    private {handlerName} CreateHandler()
    {{
        var mockLogger = new Mock<ILogger<{handlerName}>>();
        return new {handlerName}(mockLogger.Object);
    }}

    [Fact]
    public async Task Handle{methodName}_WithValidRequest_ShouldReturnExpectedResult()
    {{
        // Arrange
        var handler = CreateHandler();
        var request = new {requestName}(""Test Parameter"");

        // Act
        {(hasResponse ? $"var result = await handler.Handle{methodName}(request);" : $"await handler.Handle{methodName}(request);")}
        {testAssertions}
    }}

    [Fact]
    public async Task Handle{methodName}_WithCancellation_ShouldRespectCancellationToken()
    {{
        // Arrange
        var handler = CreateHandler();
        var request = new {requestName}(""Test Parameter"");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle{methodName}(request, cts.Token){(hasResponse ? "" : ".AsTask()")});
    }}

    [Theory]
    [InlineData("""")]
    [InlineData(""   "")]
    [InlineData(null)]
    public async Task Handle{methodName}_WithInvalidInput_ShouldHandleGracefully(string invalidInput)
    {{
        // Arrange
        var handler = CreateHandler();
        var request = new {requestName}(invalidInput ?? string.Empty);

        // Act & Assert
        {(hasResponse ? $@"var result = await handler.Handle{methodName}(request);
        
        // Verify graceful handling
        Assert.NotNull(result);" : @"// Should not throw exception
        await handler.Handle{methodName}(request);")}
    }}{performanceTest}
}}
";
    }

    private static string GenerateIntegrationTests(string handlerName, string requestName, string? responseName, string namespaceName)
    {
        var hasResponse = !string.IsNullOrEmpty(responseName);
        var methodName = requestName.Replace("Request", "").Replace("Query", "").Replace("Command", "");

        return $@"using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay.Core;
using Relay.Core.Testing;
using System.Threading.Tasks;

namespace {namespaceName}.IntegrationTests;

public class {handlerName}IntegrationTests
{{
    private IHost CreateTestHost()
    {{
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {{
                services.AddRelay();
                services.AddScoped<{handlerName}>();
                // Add other dependencies as needed
            }})
            .Build();
    }}

    [Fact]
    public async Task Handle{methodName}_ThroughRelay_ShouldWorkEndToEnd()
    {{
        // Arrange
        using var host = CreateTestHost();
        var relay = host.Services.GetRequiredService<IRelay>();
        var request = new {requestName}(""Integration Test"");

        // Act
        {(hasResponse ? $"var result = await relay.SendAsync(request);" : "await relay.SendAsync(request);")}

        // Assert
        {(hasResponse ? @"Assert.NotNull(result);
        Assert.True(result.IsSuccess);" : "// Verify the operation completed successfully")}
    }}

    [Fact]
    public async Task Handle{methodName}_WithTestHarness_ShouldProvideTestingUtilities()
    {{
        // Arrange
        var handler = new {handlerName}(Mock.Of<Microsoft.Extensions.Logging.ILogger<{handlerName}>>());
        var testRelay = RelayTestHarness.CreateTestRelay(handler);
        var request = new {requestName}(""Test Harness"");

        // Act
        {(hasResponse ? $"var result = await testRelay.SendAsync(request);" : "await testRelay.SendAsync(request);")}

        // Assert
        {(hasResponse ? "Assert.NotNull(result);" : "// Test completed successfully")}
    }}
}}
";
    }

    private static string BuildSuccessMessage(string handlerName, string requestName, string? responseName, bool includeTests)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[green]✓[/] {requestName}.cs - Request/Response definitions");
        sb.AppendLine($"[green]✓[/] {handlerName}.cs - Handler implementation");
        
        if (includeTests)
        {
            sb.AppendLine($"[green]✓[/] {handlerName}Tests.cs - Unit tests");
            sb.AppendLine($"[green]✓[/] {handlerName}IntegrationTests.cs - Integration tests");
        }
        
        sb.AppendLine();
        sb.AppendLine("[yellow]Next Steps:[/]");
        sb.AppendLine("• Implement your business logic in the handler");
        sb.AppendLine("• Update the request/response properties");
        sb.AppendLine("• Add proper validation and error handling");
        sb.AppendLine("• Run tests: [cyan]dotnet test[/]");
        sb.AppendLine("• Optimize performance: [cyan]relay optimize[/]");

        return sb.ToString();
    }
}