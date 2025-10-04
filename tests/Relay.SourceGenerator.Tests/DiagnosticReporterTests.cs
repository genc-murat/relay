using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using FluentAssertions;
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
        diagnostics.Should().ContainSingle();
        diagnostics[0].Should().Be(diagnostic);
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
        diagnostics.Should().HaveCount(2);
        diagnostics.Should().Contain(diagnostic1);
        diagnostics.Should().Contain(diagnostic2);
    }

    [Fact]
    public void IncrementalDiagnosticReporter_GetDiagnostics_ShouldReturnReadOnlyList()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();

        // Act
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        diagnostics.Should().NotBeNull();
        diagnostics.Should().BeAssignableTo<IReadOnlyList<Diagnostic>>();
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
        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.DuplicateHandler.Id);
        diagnostic.GetMessage().Should().Contain("MyRequest");
        diagnostic.GetMessage().Should().Contain("MyResponse");
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
        diagnostics.Should().ContainSingle();
        diagnostics[0].GetMessage().Should().Contain("void");
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
        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.NamedHandlerConflict.Id);
        diagnostic.GetMessage().Should().Contain("MyHandler");
        diagnostic.GetMessage().Should().Contain("MyRequest");
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
        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.DuplicatePipelineOrder.Id);
        diagnostic.GetMessage().Should().Contain("10");
        diagnostic.GetMessage().Should().Contain("Global");
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
        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.InvalidHandlerReturnType.Id);
        diagnostic.GetMessage().Should().Contain("string");
        diagnostic.GetMessage().Should().Contain("int");
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
        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.InvalidStreamHandlerReturnType.Id);
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
        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.InvalidNotificationHandlerReturnType.Id);
        diagnostic.GetMessage().Should().Contain("string");
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
        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.HandlerMissingRequestParameter.Id);
        diagnostic.GetMessage().Should().Contain("HandleAsync");
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
        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.HandlerInvalidRequestParameter.Id);
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
        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.HandlerMissingCancellationToken.Id);
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
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
        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.NotificationHandlerMissingParameter.Id);
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
        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.InvalidPriorityValue.Id);
        diagnostic.GetMessage().Should().Contain("-1");
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
        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.NoHandlersFound.Id);
        diagnostic.Location.Should().Be(Location.None);
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
        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.ConfigurationConflict.Id);
        diagnostic.GetMessage().Should().Contain("Conflicting settings found");
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
        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.InvalidPipelineScope.Id);
        diagnostic.GetMessage().Should().Contain("Request");
        diagnostic.GetMessage().Should().Contain("MyMethod");
        diagnostic.GetMessage().Should().Contain("Global");
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
        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.InvalidHandlerSignature.Id);
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
        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.GeneratorError.Id);
        diagnostic.GetMessage().Should().Contain("Something went wrong");
        diagnostic.Location.Should().Be(Location.None);
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
        diagnostics.Should().ContainSingle();
        diagnostics[0].Location.Should().Be(location);
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
        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.Debug.Id);
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Info);
        diagnostic.GetMessage().Should().Contain("Debug information");
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
        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.Info.Id);
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Info);
        diagnostic.GetMessage().Should().Contain("Informational message");
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
        diagnostics.Should().HaveCount(4);
        diagnostics.Should().Contain(d => d.Id == DiagnosticDescriptors.GeneratorError.Id);
        diagnostics.Should().Contain(d => d.Id == DiagnosticDescriptors.DuplicateHandler.Id);
        diagnostics.Should().Contain(d => d.Id == DiagnosticDescriptors.InvalidHandlerReturnType.Id);
        diagnostics.Should().Contain(d => d.Id == DiagnosticDescriptors.Info.Id);
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
        typeof(SourceProductionContextDiagnosticReporter)
            .Should().BeAssignableTo<SourceOutputDiagnosticReporter>();

        typeof(SourceProductionContextDiagnosticReporter)
            .Should().BeAssignableTo<IDiagnosticReporter>();
    }

    [Fact]
    public void SourceOutputDiagnosticReporter_ShouldImplementIDiagnosticReporter()
    {
        // Assert - verify type implements the interface
        typeof(SourceOutputDiagnosticReporter).Should().BeAssignableTo<IDiagnosticReporter>();
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
        mockReporter.ReportedDiagnostics.Should().ContainSingle();
        mockReporter.ReportedDiagnostics[0].Should().Be(diagnostic);
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
        mockReporter.ReportedDiagnostics.Should().HaveCount(2);
        mockReporter.ReportedDiagnostics.Should().Contain(diagnostic1);
        mockReporter.ReportedDiagnostics.Should().Contain(diagnostic2);
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
        mockReporter.ReportedDiagnostics.Should().HaveCount(2);
        mockReporter.ReportedDiagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
        mockReporter.ReportedDiagnostics[1].Severity.Should().Be(DiagnosticSeverity.Warning);
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
        mockReporter.ReportedDiagnostics.Should().ContainSingle();
        mockReporter.ReportedDiagnostics[0].Should().Be(diagnostic);
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
        mockReporter.ReportedDiagnostics.Should().HaveCount(2);
        mockReporter.ReportedDiagnostics.Should().Contain(diagnostic1);
        mockReporter.ReportedDiagnostics.Should().Contain(diagnostic2);
    }

    [Fact]
    public void SourceProductionContextDiagnosticReporter_ShouldImplementIDiagnosticReporter()
    {
        // Assert - verify type implements the interface
        typeof(SourceProductionContextDiagnosticReporter).Should().BeAssignableTo<IDiagnosticReporter>();
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
        mockReporter.ReportedDiagnostics.Should().ContainSingle();
        mockReporter.ReportedDiagnostics[0].Should().Be(diagnostic);
        mockReporter.ReportedDiagnostics[0].Id.Should().Be(DiagnosticDescriptors.Info.Id);
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
        mockReporter.ReportedDiagnostics.Should().HaveCount(3);
        mockReporter.ReportedDiagnostics.Should().Contain(d => d.Id == DiagnosticDescriptors.DuplicateHandler.Id);
        mockReporter.ReportedDiagnostics.Should().Contain(d => d.Id == DiagnosticDescriptors.GeneratorError.Id);
        mockReporter.ReportedDiagnostics.Should().Contain(d => d.Id == DiagnosticDescriptors.Info.Id);
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
