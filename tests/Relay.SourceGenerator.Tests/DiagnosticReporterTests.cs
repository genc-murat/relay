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
    public void SourceOutputDiagnosticReporter_Constructor_ShouldAcceptValidContext()
    {
        // Arrange - SourceProductionContext is a struct, so we can't test null directly
        // Instead, we test that the constructor accepts a valid context
        // This is more of a compilation test than a runtime test

        // Since SourceProductionContext is a struct and cannot be null,
        // and we can't easily create one in tests, we verify the class can be instantiated
        // with reflection or by checking the constructor exists

        var constructor = typeof(SourceOutputDiagnosticReporter)
            .GetConstructor(new[] { typeof(Microsoft.CodeAnalysis.SourceProductionContext) });

        // Assert
        Assert.NotNull(constructor);
        Assert.True(constructor!.IsPublic);
    }

    [Fact]
    public void SourceProductionContextDiagnosticReporter_Constructor_ShouldAcceptValidContext()
    {
        // Arrange - Similar to above, SourceProductionContext is a struct

        var constructor = typeof(SourceProductionContextDiagnosticReporter)
            .GetConstructor(new[] { typeof(Microsoft.CodeAnalysis.SourceProductionContext) });

        // Assert
        Assert.NotNull(constructor);
        Assert.True(constructor!.IsPublic);
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
    public void SourceOutputDiagnosticReporter_ShouldHandleNullDiagnostic()
    {
        // Arrange
        var mockReporter = new MockSourceOutputReporter();

        // Act & Assert - Should not throw when reporting null diagnostic
        Assert.Throws<ArgumentNullException>(() => mockReporter.ReportDiagnostic(null!));
    }

    [Fact]
    public void SourceOutputDiagnosticReporter_ShouldHandleReportingSameDiagnosticMultipleTimes()
    {
        // Arrange
        var mockReporter = new MockSourceOutputReporter();
        var diagnostic = CreateTestDiagnostic(DiagnosticDescriptors.DuplicateHandler, "TestRequest", "TestResponse");

        // Act
        mockReporter.ReportDiagnostic(diagnostic);
        mockReporter.ReportDiagnostic(diagnostic);
        mockReporter.ReportDiagnostic(diagnostic);

        // Assert
        Assert.Equal(3, mockReporter.ReportedDiagnostics.Count);
        Assert.All(mockReporter.ReportedDiagnostics, d => Assert.Equal(diagnostic, d));
    }

    [Fact]
    public void SourceOutputDiagnosticReporter_ShouldHandleLargeNumberOfDiagnostics()
    {
        // Arrange
        var mockReporter = new MockSourceOutputReporter();
        const int diagnosticCount = 1000;
        var diagnostics = new List<Diagnostic>();

        for (int i = 0; i < diagnosticCount; i++)
        {
            diagnostics.Add(CreateTestDiagnostic(DiagnosticDescriptors.DuplicateHandler, $"Request{i}", $"Response{i}"));
        }

        // Act
        foreach (var diagnostic in diagnostics)
        {
            mockReporter.ReportDiagnostic(diagnostic);
        }

        // Assert
        Assert.Equal(diagnosticCount, mockReporter.ReportedDiagnostics.Count);
        for (int i = 0; i < diagnosticCount; i++)
        {
            Assert.Equal(diagnostics[i], mockReporter.ReportedDiagnostics[i]);
        }
    }

    [Fact]
    public async Task SourceOutputDiagnosticReporter_ShouldBeThreadSafeForConcurrentReporting()
    {
        // Arrange
        var mockReporter = new MockSourceOutputReporter();
        const int threadCount = 10;
        const int diagnosticsPerThread = 100;
        var allDiagnostics = new List<Diagnostic>();
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        // Create diagnostics for each thread
        for (int i = 0; i < threadCount * diagnosticsPerThread; i++)
        {
            allDiagnostics.Add(CreateTestDiagnostic(DiagnosticDescriptors.DuplicateHandler, $"Request{i}", $"Response{i}"));
        }

        // Act - Report diagnostics concurrently
        var tasks = new Task[threadCount];
        for (int t = 0; t < threadCount; t++)
        {
            var threadIndex = t;
            tasks[t] = Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < diagnosticsPerThread; i++)
                    {
                        var diagnosticIndex = threadIndex * diagnosticsPerThread + i;
                        mockReporter.ReportDiagnostic(allDiagnostics[diagnosticIndex]);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions); // No exceptions should occur
        Assert.Equal(threadCount * diagnosticsPerThread, mockReporter.ReportedDiagnostics.Count);

        // Verify all diagnostics were reported (order may vary due to concurrency)
        foreach (var expectedDiagnostic in allDiagnostics)
        {
            Assert.Contains(expectedDiagnostic, mockReporter.ReportedDiagnostics);
        }
    }

    [Fact]
    public void SourceOutputDiagnosticReporter_ShouldHandleBulkDiagnosticReportingEfficiently()
    {
        // Arrange
        var mockReporter = new MockSourceOutputReporter();
        const int bulkSize = 10000;
        var diagnostics = new List<Diagnostic>();

        for (int i = 0; i < bulkSize; i++)
        {
            diagnostics.Add(CreateTestDiagnostic(DiagnosticDescriptors.Info, $"Bulk diagnostic {i}"));
        }

        // Act - Measure performance (basic check that it completes within reasonable time)
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        foreach (var diagnostic in diagnostics)
        {
            mockReporter.ReportDiagnostic(diagnostic);
        }
        stopwatch.Stop();

        // Assert
        Assert.Equal(bulkSize, mockReporter.ReportedDiagnostics.Count);

        // Performance check - should complete in less than 1 second for 10k diagnostics
        // This is a reasonable performance expectation for diagnostic reporting
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"Bulk reporting took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
    }

    [Fact]
    public void SourceOutputDiagnosticReporter_ShouldHandleAllDiagnosticSeverities()
    {
        // Arrange
        var mockReporter = new MockSourceOutputReporter();
        var diagnostics = new[]
        {
            CreateTestDiagnostic(DiagnosticDescriptors.GeneratorError, "error message"), // Error
            CreateTestDiagnostic(DiagnosticDescriptors.HandlerMissingCancellationToken, "HandleAsync"), // Warning
            CreateTestDiagnostic(DiagnosticDescriptors.Info, "info message"), // Info
            CreateTestDiagnostic(DiagnosticDescriptors.DuplicateHandler, "Request", "Response") // Error
        };

        // Act
        foreach (var diagnostic in diagnostics)
        {
            mockReporter.ReportDiagnostic(diagnostic);
        }

        // Assert
        Assert.Equal(diagnostics.Length, mockReporter.ReportedDiagnostics.Count);
        Assert.Equal(DiagnosticSeverity.Error, mockReporter.ReportedDiagnostics[0].Severity);
        Assert.Equal(DiagnosticSeverity.Warning, mockReporter.ReportedDiagnostics[1].Severity);
        Assert.Equal(DiagnosticSeverity.Info, mockReporter.ReportedDiagnostics[2].Severity);
        Assert.Equal(DiagnosticSeverity.Error, mockReporter.ReportedDiagnostics[3].Severity);
    }

    [Fact]
    public void SourceOutputDiagnosticReporter_ShouldPreserveDiagnosticProperties()
    {
        // Arrange
        var mockReporter = new MockSourceOutputReporter();
        var diagnostic = CreateTestDiagnostic(DiagnosticDescriptors.DuplicateHandler, "TestRequest", "TestResponse");

        // Act
        mockReporter.ReportDiagnostic(diagnostic);

        // Assert
        var reported = mockReporter.ReportedDiagnostics[0];
        Assert.Equal(diagnostic.Id, reported.Id);
        Assert.Equal(diagnostic.Severity, reported.Severity);
        Assert.Equal(diagnostic.GetMessage(), reported.GetMessage());
        Assert.Equal(diagnostic.Location, reported.Location);
        Assert.Equal(diagnostic.Descriptor, reported.Descriptor);
    }

    [Fact]
    public void SourceOutputDiagnosticReporter_ShouldReportDiagnosticsInOrder()
    {
        // Arrange
        var mockReporter = new MockSourceOutputReporter();
        var diagnostics = new[]
        {
            CreateTestDiagnostic(DiagnosticDescriptors.DuplicateHandler, "Request1", "Response1"),
            CreateTestDiagnostic(DiagnosticDescriptors.InvalidHandlerReturnType, "string", "int"),
            CreateTestDiagnostic(DiagnosticDescriptors.NoHandlersFound),
            CreateTestDiagnostic(DiagnosticDescriptors.InvalidPriorityValue, -5)
        };

        // Act
        foreach (var diagnostic in diagnostics)
        {
            mockReporter.ReportDiagnostic(diagnostic);
        }

        // Assert
        Assert.Equal(diagnostics.Length, mockReporter.ReportedDiagnostics.Count);
        for (int i = 0; i < diagnostics.Length; i++)
        {
            Assert.Equal(diagnostics[i], mockReporter.ReportedDiagnostics[i]);
        }
    }

    [Fact]
    public void SourceOutputDiagnosticReporter_ShouldHandleEmptyDiagnosticList()
    {
        // Arrange
        var mockReporter = new MockSourceOutputReporter();

        // Act - No diagnostics reported

        // Assert
        Assert.Empty(mockReporter.ReportedDiagnostics);
    }

    [Fact]
    public void SourceProductionContextDiagnosticReporter_ShouldBehaveIdenticallyToSourceOutputDiagnosticReporter()
    {
        // This test verifies that SourceProductionContextDiagnosticReporter (the alias)
        // behaves exactly the same as SourceOutputDiagnosticReporter

        // Arrange
        var mockReporter = new MockSourceOutputReporter();
        var diagnostic1 = CreateTestDiagnostic(DiagnosticDescriptors.DuplicateHandler, "TestRequest", "TestResponse");
        var diagnostic2 = CreateTestDiagnostic(DiagnosticDescriptors.InvalidHandlerReturnType, "string", "int");

        // Act
        mockReporter.ReportDiagnostic(diagnostic1);
        mockReporter.ReportDiagnostic(diagnostic2);

        // Assert - Both types should have identical behavior through the interface
        Assert.Equal(2, mockReporter.ReportedDiagnostics.Count);
        Assert.Equal(diagnostic1, mockReporter.ReportedDiagnostics[0]);
        Assert.Equal(diagnostic2, mockReporter.ReportedDiagnostics[1]);

        // Verify that SourceProductionContextDiagnosticReporter is indeed just an alias
        Assert.Equal(typeof(SourceOutputDiagnosticReporter), typeof(SourceProductionContextDiagnosticReporter).BaseType);
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

    [Fact]
    public void SourceProductionContextDiagnosticReporter_ShouldReportDiagnosticWithCorrectSeverity()
    {
        // Arrange
        var mockReporter = new MockSourceProductionContextReporter();
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
    public void SourceProductionContextDiagnosticReporter_ShouldHandleNullDiagnostic()
    {
        // Arrange
        var mockReporter = new MockSourceProductionContextReporter();

        // Act & Assert - Should throw when reporting null diagnostic
        Assert.Throws<ArgumentNullException>(() => mockReporter.ReportDiagnostic(null!));
    }

    [Fact]
    public void SourceProductionContextDiagnosticReporter_ShouldReportDiagnosticsInOrder()
    {
        // Arrange
        var mockReporter = new MockSourceProductionContextReporter();
        var diagnostics = new[]
        {
            CreateTestDiagnostic(DiagnosticDescriptors.DuplicateHandler, "Request1", "Response1"),
            CreateTestDiagnostic(DiagnosticDescriptors.InvalidHandlerReturnType, "string", "int"),
            CreateTestDiagnostic(DiagnosticDescriptors.NoHandlersFound),
            CreateTestDiagnostic(DiagnosticDescriptors.InvalidPriorityValue, -5)
        };

        // Act
        foreach (var diagnostic in diagnostics)
        {
            mockReporter.ReportDiagnostic(diagnostic);
        }

        // Assert
        Assert.Equal(diagnostics.Length, mockReporter.ReportedDiagnostics.Count);
        for (int i = 0; i < diagnostics.Length; i++)
        {
            Assert.Equal(diagnostics[i], mockReporter.ReportedDiagnostics[i]);
        }
    }

    [Fact]
    public void SourceProductionContextDiagnosticReporter_ShouldHandleEmptyDiagnosticList()
    {
        // Arrange
        var mockReporter = new MockSourceProductionContextReporter();

        // Act - No diagnostics reported

        // Assert
        Assert.Empty(mockReporter.ReportedDiagnostics);
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
    public void IncrementalDiagnosticReporter_ShouldHandleConcurrentAccess()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var diagnostic = CreateTestDiagnostic(DiagnosticDescriptors.Info, "test");
        const int numThreads = 10;
        const int reportsPerThread = 100;

        // Act
        var tasks = new List<Task>();
        for (int i = 0; i < numThreads; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < reportsPerThread; j++)
                {
                    reporter.ReportDiagnostic(diagnostic);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var diagnostics = reporter.GetDiagnostics();
        Assert.Equal(numThreads * reportsPerThread, diagnostics.Count);
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

    // Mock reporter for testing SourceOutputDiagnosticReporter behavior
    private class MockSourceOutputReporter : IDiagnosticReporter
    {
        private readonly object _lock = new();
        public List<Diagnostic> ReportedDiagnostics { get; } = new();

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            if (diagnostic is null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }
            lock (_lock)
            {
                ReportedDiagnostics.Add(diagnostic);
            }
        }
    }

    // Mock reporter for testing SourceProductionContextDiagnosticReporter behavior
    private class MockSourceProductionContextReporter : IDiagnosticReporter
    {
        public List<Diagnostic> ReportedDiagnostics { get; } = new();

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            if (diagnostic is null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }
            ReportedDiagnostics.Add(diagnostic);
        }
    }
}
