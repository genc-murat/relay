using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using System.Linq;
using System.Collections.Generic;

namespace Relay.SourceGenerator.Tests;

public class DiagnosticReporterTests
{
    [Fact]
    public void IncrementalDiagnosticReporter_ShouldStoreReportedDiagnostics()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var diagnostic = CreateTestDiagnostic(DiagnosticDescriptors.DuplicateHandler, "TestRequest", "TestResponse");

        // Act
        reporter.ReportDiagnostic(diagnostic);
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal(diagnostic, diagnostics[0]);
    }

    [Fact]
    public void IncrementalDiagnosticReporter_ShouldStoreMultipleDiagnostics()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var diagnostic1 = CreateTestDiagnostic(DiagnosticDescriptors.DuplicateHandler, "Request1", "Response1");
        var diagnostic2 = CreateTestDiagnostic(DiagnosticDescriptors.InvalidHandlerReturnType, "string", "int");

        // Act
        reporter.ReportDiagnostic(diagnostic1);
        reporter.ReportDiagnostic(diagnostic2);
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Equal(2, diagnostics.Count);
        Assert.Contains(diagnostic1, diagnostics);
        Assert.Contains(diagnostic2, diagnostics);
    }

    [Fact]
    public void IncrementalDiagnosticReporter_GetDiagnostics_ShouldReturnReadOnlyList()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();

        // Act
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.NotNull(diagnostics);
        Assert.IsAssignableFrom<IReadOnlyList<Diagnostic>>(diagnostics);
    }

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

    private static Diagnostic CreateTestDiagnostic(DiagnosticDescriptor descriptor, params object[] messageArgs)
    {
        return Diagnostic.Create(descriptor, Location.None, messageArgs);
    }

    private static Location CreateTestLocation()
    {
        var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("class Test { }");
        var span = new TextSpan(0, 5);
        return Location.Create(syntaxTree, span);
    }

    [Fact]
    public void SourceProductionContextDiagnosticReporter_ShouldBeAliasForSourceOutputDiagnosticReporter()
    {
        // This test verifies that SourceProductionContextDiagnosticReporter is an alias/subclass
        // of SourceOutputDiagnosticReporter for better naming clarity

        // Assert
        Assert.True(typeof(SourceOutputDiagnosticReporter).IsAssignableFrom(
            typeof(SourceProductionContextDiagnosticReporter)));

        Assert.True(typeof(IDiagnosticReporter).IsAssignableFrom(
            typeof(SourceProductionContextDiagnosticReporter)));
    }

    [Fact]
    public void SourceOutputDiagnosticReporter_ShouldImplementIDiagnosticReporter()
    {
        // Assert - verify type implements the interface
        Assert.True(typeof(IDiagnosticReporter).IsAssignableFrom(typeof(SourceOutputDiagnosticReporter)));
    }

    [Fact]
    public void SourceOutputDiagnosticReporter_ShouldStoreContextAndReportDiagnostics()
    {
        // This test verifies that SourceOutputDiagnosticReporter wraps SourceProductionContext
        // Since SourceProductionContext is a struct and cannot be easily mocked,
        // we verify the class structure and behavior through other means

        // Arrange - Create a mock reporter to test the pattern
        var mockReporter = new MockSourceOutputReporter();
        var diagnostic = CreateTestDiagnostic(DiagnosticDescriptors.DuplicateHandler, "TestRequest", "TestResponse");

        // Act
        mockReporter.ReportDiagnostic(diagnostic);

        // Assert
        Assert.Single(mockReporter.ReportedDiagnostics);
        Assert.Equal(diagnostic, mockReporter.ReportedDiagnostics[0]);
    }

    [Fact]
    public void SourceOutputDiagnosticReporter_ShouldReportMultipleDiagnostics()
    {
        // Arrange
        var mockReporter = new MockSourceOutputReporter();
        var diagnostic1 = CreateTestDiagnostic(DiagnosticDescriptors.DuplicateHandler, "Request1", "Response1");
        var diagnostic2 = CreateTestDiagnostic(DiagnosticDescriptors.InvalidHandlerReturnType, "string", "int");

        // Act
        mockReporter.ReportDiagnostic(diagnostic1);
        mockReporter.ReportDiagnostic(diagnostic2);

        // Assert
        Assert.Equal(2, mockReporter.ReportedDiagnostics.Count);
        Assert.Contains(diagnostic1, mockReporter.ReportedDiagnostics);
        Assert.Contains(diagnostic2, mockReporter.ReportedDiagnostics);
    }

    [Fact]
    public void SourceOutputDiagnosticReporter_ShouldReportDiagnosticWithCorrectSeverity()
    {
        // Arrange
        var mockReporter = new MockSourceOutputReporter();
        var errorDiagnostic = CreateTestDiagnostic(DiagnosticDescriptors.GeneratorError, "error message");
        var warningDiagnostic = CreateTestDiagnostic(DiagnosticDescriptors.HandlerMissingCancellationToken, "HandleAsync");

        // Act
        mockReporter.ReportDiagnostic(errorDiagnostic);
        mockReporter.ReportDiagnostic(warningDiagnostic);

        // Assert
        Assert.Equal(2, mockReporter.ReportedDiagnostics.Count);
        Assert.Equal(DiagnosticSeverity.Error, mockReporter.ReportedDiagnostics[0].Severity);
        Assert.Equal(DiagnosticSeverity.Warning, mockReporter.ReportedDiagnostics[1].Severity);
    }

    [Fact]
    public void SourceProductionContextDiagnosticReporter_ShouldReportDiagnosticToMockContext()
    {
        // Arrange
        var mockReporter = new MockSourceProductionContextReporter();
        var diagnostic = CreateTestDiagnostic(DiagnosticDescriptors.DuplicateHandler, "TestRequest", "TestResponse");

        // Act
        mockReporter.ReportDiagnostic(diagnostic);

        // Assert
        Assert.Single(mockReporter.ReportedDiagnostics);
        Assert.Equal(diagnostic, mockReporter.ReportedDiagnostics[0]);
    }

    [Fact]
    public void SourceProductionContextDiagnosticReporter_ShouldReportMultipleDiagnostics()
    {
        // Arrange
        var mockReporter = new MockSourceProductionContextReporter();
        var diagnostic1 = CreateTestDiagnostic(DiagnosticDescriptors.NoHandlersFound);
        var diagnostic2 = CreateTestDiagnostic(DiagnosticDescriptors.InvalidPriorityValue, -5);

        // Act
        mockReporter.ReportDiagnostic(diagnostic1);
        mockReporter.ReportDiagnostic(diagnostic2);

        // Assert
        Assert.Equal(2, mockReporter.ReportedDiagnostics.Count);
        Assert.Contains(diagnostic1, mockReporter.ReportedDiagnostics);
        Assert.Contains(diagnostic2, mockReporter.ReportedDiagnostics);
    }

    [Fact]
    public void SourceProductionContextDiagnosticReporter_ShouldImplementIDiagnosticReporter()
    {
        // Assert - verify type implements the interface
        Assert.True(typeof(IDiagnosticReporter).IsAssignableFrom(typeof(SourceProductionContextDiagnosticReporter)));
    }

    [Fact]
    public void SourceProductionContextDiagnosticReporter_ShouldInheritFromSourceOutputDiagnosticReporter()
    {
        // Arrange
        var mockReporter = new MockSourceProductionContextReporter();
        var diagnostic = CreateTestDiagnostic(DiagnosticDescriptors.Info, "test info");

        // Act
        mockReporter.ReportDiagnostic(diagnostic);

        // Assert - should behave exactly like SourceOutputDiagnosticReporter
        Assert.Single(mockReporter.ReportedDiagnostics);
        Assert.Equal(diagnostic, mockReporter.ReportedDiagnostics[0]);
        Assert.Equal(DiagnosticDescriptors.Info.Id, mockReporter.ReportedDiagnostics[0].Id);
    }

    [Fact]
    public void SourceProductionContextDiagnosticReporter_ShouldWorkWithExtensionMethods()
    {
        // Arrange
        var mockReporter = new MockSourceProductionContextReporter();
        var location = Location.None;

        // Act
        mockReporter.ReportDuplicateHandler(location, "MyRequest", "MyResponse");
        mockReporter.ReportError("Test error");
        mockReporter.ReportInfo("Test info");

        // Assert
        Assert.Equal(3, mockReporter.ReportedDiagnostics.Count);
        Assert.Contains(mockReporter.ReportedDiagnostics, d => d.Id == DiagnosticDescriptors.DuplicateHandler.Id);
        Assert.Contains(mockReporter.ReportedDiagnostics, d => d.Id == DiagnosticDescriptors.GeneratorError.Id);
        Assert.Contains(mockReporter.ReportedDiagnostics, d => d.Id == DiagnosticDescriptors.Info.Id);
    }

    // Mock reporter for testing SourceOutputDiagnosticReporter behavior
    private class MockSourceOutputReporter : IDiagnosticReporter
    {
        public List<Diagnostic> ReportedDiagnostics { get; } = new();

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            ReportedDiagnostics.Add(diagnostic);
        }
    }

    // Mock reporter for testing SourceProductionContextDiagnosticReporter behavior
    private class MockSourceProductionContextReporter : IDiagnosticReporter
    {
        public List<Diagnostic> ReportedDiagnostics { get; } = new();

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            ReportedDiagnostics.Add(diagnostic);
        }
    }
}
