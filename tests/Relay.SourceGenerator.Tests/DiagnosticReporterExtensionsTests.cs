using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Relay.SourceGenerator.Tests;

public class DiagnosticReporterExtensionsTests
{
    [Fact]
    public void DiagnosticReporterExtensions_ReportDuplicateHandler_ShouldCreateCorrectDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportDuplicateHandler(location, "MyRequest", "MyResponse");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.DuplicateHandler.Id, diagnostic.Id);
        Assert.Contains("MyRequest", diagnostic.GetMessage());
        Assert.Contains("MyResponse", diagnostic.GetMessage());
    }

    [Fact]
    public void DiagnosticReporterExtensions_ReportDuplicateHandler_WithVoidResponse_ShouldUseVoid()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportDuplicateHandler(location, "MyRequest", null);
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Contains("void", diagnostics[0].GetMessage());
    }

    [Fact]
    public void DiagnosticReporterExtensions_ReportDuplicateNamedHandler_ShouldCreateCorrectDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportDuplicateNamedHandler(location, "MyRequest", "MyHandler");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.NamedHandlerConflict.Id, diagnostic.Id);
        Assert.Contains("MyHandler", diagnostic.GetMessage());
        Assert.Contains("MyRequest", diagnostic.GetMessage());
    }

    [Fact]
    public void DiagnosticReporterExtensions_ReportDuplicatePipelineOrder_ShouldCreateCorrectDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportDuplicatePipelineOrder(location, 10, "Global");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.DuplicatePipelineOrder.Id, diagnostic.Id);
        Assert.Contains("10", diagnostic.GetMessage());
        Assert.Contains("Global", diagnostic.GetMessage());
    }

    [Fact]
    public void DiagnosticReporterExtensions_ReportInvalidHandlerReturnType_ShouldCreateCorrectDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportInvalidHandlerReturnType(location, "string", "int");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.InvalidHandlerReturnType.Id, diagnostic.Id);
        Assert.Contains("string", diagnostic.GetMessage());
        Assert.Contains("int", diagnostic.GetMessage());
    }

    [Fact]
    public void DiagnosticReporterExtensions_ReportInvalidStreamHandlerReturnType_ShouldCreateCorrectDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportInvalidStreamHandlerReturnType(location, "Task<string>", "IAsyncEnumerable<string>");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.InvalidStreamHandlerReturnType.Id, diagnostic.Id);
    }

    [Fact]
    public void DiagnosticReporterExtensions_ReportInvalidNotificationHandlerReturnType_ShouldCreateCorrectDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportInvalidNotificationHandlerReturnType(location, "string");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.InvalidNotificationHandlerReturnType.Id, diagnostic.Id);
        Assert.Contains("string", diagnostic.GetMessage());
    }

    [Fact]
    public void DiagnosticReporterExtensions_ReportHandlerMissingRequestParameter_ShouldCreateCorrectDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportHandlerMissingRequestParameter(location, "HandleAsync");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.HandlerMissingRequestParameter.Id, diagnostic.Id);
        Assert.Contains("HandleAsync", diagnostic.GetMessage());
    }

    [Fact]
    public void DiagnosticReporterExtensions_ReportHandlerInvalidRequestParameter_ShouldCreateCorrectDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportHandlerInvalidRequestParameter(location, "string", "MyRequest");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.HandlerInvalidRequestParameter.Id, diagnostic.Id);
    }

    [Fact]
    public void DiagnosticReporterExtensions_ReportHandlerMissingCancellationToken_ShouldCreateWarning()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportHandlerMissingCancellationToken(location, "HandleAsync");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.HandlerMissingCancellationToken.Id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public void DiagnosticReporterExtensions_ReportNotificationHandlerMissingParameter_ShouldCreateCorrectDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportNotificationHandlerMissingParameter(location, "HandleAsync");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.NotificationHandlerMissingParameter.Id, diagnostic.Id);
    }

    [Fact]
    public void DiagnosticReporterExtensions_ReportInvalidPriorityValue_ShouldCreateCorrectDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportInvalidPriorityValue(location, -1);
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.InvalidPriorityValue.Id, diagnostic.Id);
        Assert.Contains("-1", diagnostic.GetMessage());
    }

    [Fact]
    public void DiagnosticReporterExtensions_ReportNoHandlersFound_ShouldCreateWarning()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();

        // Act
        reporter.ReportNoHandlersFound();
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.NoHandlersFound.Id, diagnostic.Id);
        Assert.Equal(Location.None, diagnostic.Location);
    }

    [Fact]
    public void DiagnosticReporterExtensions_ReportConfigurationConflict_ShouldCreateCorrectDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportConfigurationConflict(location, "Conflicting settings found");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.ConfigurationConflict.Id, diagnostic.Id);
        Assert.Contains("Conflicting settings found", diagnostic.GetMessage());
    }

    [Fact]
    public void DiagnosticReporterExtensions_ReportInvalidPipelineScope_ShouldCreateCorrectDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportInvalidPipelineScope(location, "Request", "MyMethod", "Global");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.InvalidPipelineScope.Id, diagnostic.Id);
        Assert.Contains("Request", diagnostic.GetMessage());
        Assert.Contains("MyMethod", diagnostic.GetMessage());
        Assert.Contains("Global", diagnostic.GetMessage());
    }

    [Fact]
    public void DiagnosticReporterExtensions_ReportInvalidHandlerSignature_ShouldCreateCorrectDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportInvalidHandlerSignature(location, "HandleAsync", "Task<Response> HandleAsync(Request, CancellationToken)");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.InvalidHandlerSignature.Id, diagnostic.Id);
    }

    [Fact]
    public void DiagnosticReporterExtensions_ReportError_ShouldCreateCorrectDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();

        // Act
        reporter.ReportError("Something went wrong");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.GeneratorError.Id, diagnostic.Id);
        Assert.Contains("Something went wrong", diagnostic.GetMessage());
        Assert.Equal(Location.None, diagnostic.Location);
    }

    [Fact]
    public void DiagnosticReporterExtensions_ReportError_WithLocation_ShouldUseProvidedLocation()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = CreateTestLocation();

        // Act
        reporter.ReportError("Error message", location);
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal(location, diagnostics[0].Location);
    }

    [Fact]
    public void DiagnosticReporterExtensions_ReportDebug_ShouldCreateInfoDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();

        // Act
        reporter.ReportDebug("Debug information");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.Debug.Id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Contains("Debug information", diagnostic.GetMessage());
    }

    [Fact]
    public void DiagnosticReporterExtensions_ReportInfo_ShouldCreateInfoDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();

        // Act
        reporter.ReportInfo("Informational message");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.Info.Id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Contains("Informational message", diagnostic.GetMessage());
    }

    [Fact]
    public void DiagnosticReporterExtensions_MultipleReports_ShouldAccumulateAllDiagnostics()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportError("Error 1");
        reporter.ReportDuplicateHandler(location, "Request1", "Response1");
        reporter.ReportInvalidHandlerReturnType(location, "string", "int");
        reporter.ReportInfo("Info message");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Equal(4, diagnostics.Count);
        Assert.Contains(diagnostics, d => d.Id == DiagnosticDescriptors.GeneratorError.Id);
        Assert.Contains(diagnostics, d => d.Id == DiagnosticDescriptors.DuplicateHandler.Id);
        Assert.Contains(diagnostics, d => d.Id == DiagnosticDescriptors.InvalidHandlerReturnType.Id);
        Assert.Contains(diagnostics, d => d.Id == DiagnosticDescriptors.Info.Id);
    }

    [Fact]
    public void DiagnosticReporterExtensions_ShouldHandleNullOrEmptyMessages()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();

        // Act & Assert - Empty message should still create diagnostic
        reporter.ReportError("");
        var diagnostics = reporter.GetDiagnostics();
        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticDescriptors.GeneratorError.Id, diagnostics[0].Id);

        // Reset
        reporter = new IncrementalDiagnosticReporter();

        // Act & Assert - Null message should handle gracefully
        reporter.ReportError(null!);
        diagnostics = reporter.GetDiagnostics();
        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticDescriptors.GeneratorError.Id, diagnostics[0].Id);
    }

    [Fact]
    public void DiagnosticReporterExtensions_ShouldHandleNullLocations()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();

        // Act
        reporter.ReportDuplicateHandler(null!, "Request", "Response");

        // Assert
        var diagnostics = reporter.GetDiagnostics();
        Assert.Single(diagnostics);
        Assert.Equal(Location.None, diagnostics[0].Location);
    }

    [Fact]
    public void DiagnosticReporterExtensions_ShouldHandleNullResponseTypeInDuplicateHandler()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportDuplicateHandler(location, "MyRequest", null);

        // Assert
        var diagnostics = reporter.GetDiagnostics();
        Assert.Single(diagnostics);
        Assert.Contains("void", diagnostics[0].GetMessage());
    }

    [Fact]
    public void DiagnosticReporterExtensions_ShouldHandleNegativeOrderInDuplicatePipelineOrder()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportDuplicatePipelineOrder(location, -5, "Global");

        // Assert
        var diagnostics = reporter.GetDiagnostics();
        Assert.Single(diagnostics);
        Assert.Contains("-5", diagnostics[0].GetMessage());
    }

    [Fact]
    public void DiagnosticReporterExtensions_ShouldHandleEmptyScopeInInvalidPipelineScope()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportInvalidPipelineScope(location, "Request", "MyMethod", "");

        // Assert
        var diagnostics = reporter.GetDiagnostics();
        Assert.Single(diagnostics);
        Assert.Contains("Request", diagnostics[0].GetMessage());
        Assert.Contains("MyMethod", diagnostics[0].GetMessage());
    }

    [Fact]
    public void DiagnosticReporterExtensions_ShouldHandleNullSignatureInInvalidHandlerSignature()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportInvalidHandlerSignature(location, "HandleAsync", null!);

        // Assert
        var diagnostics = reporter.GetDiagnostics();
        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticDescriptors.InvalidHandlerSignature.Id, diagnostics[0].Id);
    }

    [Fact]
    public void DiagnosticReporterExtensions_ShouldHandleZeroPriorityValue()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportInvalidPriorityValue(location, 0);

        // Assert
        var diagnostics = reporter.GetDiagnostics();
        Assert.Single(diagnostics);
        Assert.Contains("0", diagnostics[0].GetMessage());
    }

    [Fact]
    public void DiagnosticReporterExtensions_ShouldHandleVeryLongMessages()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var longMessage = new string('A', 10000);

        // Act
        reporter.ReportError(longMessage);

        // Assert
        var diagnostics = reporter.GetDiagnostics();
        Assert.Single(diagnostics);
        Assert.Contains(longMessage, diagnostics[0].GetMessage());
    }

    [Fact]
    public void DiagnosticReporterExtensions_ShouldHandleSpecialCharactersInMessages()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var specialMessage = "Message with <>&\"'\n\r\t special chars";

        // Act
        reporter.ReportError(specialMessage);

        // Assert
        var diagnostics = reporter.GetDiagnostics();
        Assert.Single(diagnostics);
        Assert.Contains(specialMessage, diagnostics[0].GetMessage());
    }

    [Fact]
    public void DiagnosticReporterExtensions_ShouldHandleAllDiagnosticTypes()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act - Report one of each diagnostic type
        reporter.ReportDuplicateHandler(location, "Req", "Res");
        reporter.ReportDuplicateNamedHandler(location, "Req", "Handler");
        reporter.ReportDuplicatePipelineOrder(location, 1, "Global");
        reporter.ReportInvalidHandlerReturnType(location, "string", "int");
        reporter.ReportInvalidStreamHandlerReturnType(location, "Task", "IAsyncEnumerable");
        reporter.ReportInvalidNotificationHandlerReturnType(location, "string");
        reporter.ReportHandlerMissingRequestParameter(location, "Method");
        reporter.ReportHandlerInvalidRequestParameter(location, "string", "Request");
        reporter.ReportHandlerMissingCancellationToken(location, "Method");
        reporter.ReportNotificationHandlerMissingParameter(location, "Method");
        reporter.ReportInvalidPriorityValue(location, -1);
        reporter.ReportNoHandlersFound();
        reporter.ReportConfigurationConflict(location, "conflict");
        reporter.ReportInvalidPipelineScope(location, "Request", "Method", "Global");
        reporter.ReportInvalidHandlerSignature(location, "Method", "signature");
        reporter.ReportError("error");
        reporter.ReportDebug("debug");
        reporter.ReportInfo("info");

        // Assert
        var diagnostics = reporter.GetDiagnostics();
        Assert.Equal(18, diagnostics.Count);

        // Verify we have all expected diagnostic IDs
        var expectedIds = new[]
        {
            DiagnosticDescriptors.DuplicateHandler.Id,
            DiagnosticDescriptors.NamedHandlerConflict.Id,
            DiagnosticDescriptors.DuplicatePipelineOrder.Id,
            DiagnosticDescriptors.InvalidHandlerReturnType.Id,
            DiagnosticDescriptors.InvalidStreamHandlerReturnType.Id,
            DiagnosticDescriptors.InvalidNotificationHandlerReturnType.Id,
            DiagnosticDescriptors.HandlerMissingRequestParameter.Id,
            DiagnosticDescriptors.HandlerInvalidRequestParameter.Id,
            DiagnosticDescriptors.HandlerMissingCancellationToken.Id,
            DiagnosticDescriptors.NotificationHandlerMissingParameter.Id,
            DiagnosticDescriptors.InvalidPriorityValue.Id,
            DiagnosticDescriptors.NoHandlersFound.Id,
            DiagnosticDescriptors.ConfigurationConflict.Id,
            DiagnosticDescriptors.InvalidPipelineScope.Id,
            DiagnosticDescriptors.InvalidHandlerSignature.Id,
            DiagnosticDescriptors.GeneratorError.Id,
            DiagnosticDescriptors.Debug.Id,
            DiagnosticDescriptors.Info.Id
        };

        foreach (var expectedId in expectedIds)
        {
            Assert.Contains(diagnostics, d => d.Id == expectedId);
        }
    }

    private static Location CreateTestLocation()
    {
        var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("class Test { }");
        var span = new TextSpan(0, 5);
        return Location.Create(syntaxTree, span);
    }
}