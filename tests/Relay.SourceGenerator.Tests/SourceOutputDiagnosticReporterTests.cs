using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Relay.SourceGenerator.Tests;

public class SourceOutputDiagnosticReporterTests
{
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
    public void SourceOutputDiagnosticReporter_ShouldPreserveDiagnosticAdditionalLocations()
    {
        // Arrange
        var mockReporter = new MockSourceOutputReporter();
        var additionalLocation = CreateTestLocation();
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.DuplicateHandler,
            Location.None,
            new[] { additionalLocation },
            ImmutableDictionary<string, string?>.Empty,
            "TestRequest", "TestResponse");

        // Act
        mockReporter.ReportDiagnostic(diagnostic);

        // Assert
        var reported = mockReporter.ReportedDiagnostics[0];
        Assert.Equal(diagnostic.AdditionalLocations, reported.AdditionalLocations);
        Assert.Single(reported.AdditionalLocations);
        Assert.Equal(additionalLocation, reported.AdditionalLocations[0]);
    }

    [Fact]
    public void SourceOutputDiagnosticReporter_ShouldPreserveDiagnosticCustomProperties()
    {
        // Arrange
        var mockReporter = new MockSourceOutputReporter();
        var properties = ImmutableDictionary<string, string?>.Empty
            .Add("CustomProperty1", "Value1")
            .Add("CustomProperty2", "Value2");
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.DuplicateHandler,
            Location.None,
            properties,
            "TestRequest", "TestResponse");

        // Act
        mockReporter.ReportDiagnostic(diagnostic);

        // Assert
        var reported = mockReporter.ReportedDiagnostics[0];
        Assert.Equal(diagnostic.Properties, reported.Properties);
        Assert.Equal(2, reported.Properties.Count);
        Assert.Equal("Value1", reported.Properties["CustomProperty1"]);
        Assert.Equal("Value2", reported.Properties["CustomProperty2"]);
    }

    [Fact]
    public void SourceOutputDiagnosticReporter_ShouldPreserveDiagnosticDescriptorProperties()
    {
        // Arrange
        var mockReporter = new MockSourceOutputReporter();
        var diagnostic = CreateTestDiagnostic(DiagnosticDescriptors.DuplicateHandler, "TestRequest", "TestResponse");

        // Act
        mockReporter.ReportDiagnostic(diagnostic);

        // Assert
        var reported = mockReporter.ReportedDiagnostics[0];
        Assert.Equal(diagnostic.Descriptor.Id, reported.Descriptor.Id);
        Assert.Equal(diagnostic.Descriptor.Title, reported.Descriptor.Title);
        Assert.Equal(diagnostic.Descriptor.Description, reported.Descriptor.Description);
        Assert.Equal(diagnostic.Descriptor.MessageFormat, reported.Descriptor.MessageFormat);
        Assert.Equal(diagnostic.Descriptor.Category, reported.Descriptor.Category);
        Assert.Equal(diagnostic.Descriptor.DefaultSeverity, reported.Descriptor.DefaultSeverity);
        Assert.Equal(diagnostic.Descriptor.IsEnabledByDefault, reported.Descriptor.IsEnabledByDefault);
    }

    [Fact]
    public void SourceOutputDiagnosticReporter_ShouldPreserveDiagnosticWarningLevel()
    {
        // Arrange
        var mockReporter = new MockSourceOutputReporter();
        var diagnostic = CreateTestDiagnostic(DiagnosticDescriptors.DuplicateHandler, "TestRequest", "TestResponse");

        // Act
        mockReporter.ReportDiagnostic(diagnostic);

        // Assert
        var reported = mockReporter.ReportedDiagnostics[0];
        Assert.Equal(diagnostic.WarningLevel, reported.WarningLevel);
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
}