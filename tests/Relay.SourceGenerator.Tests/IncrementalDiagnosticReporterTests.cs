using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Relay.SourceGenerator.Tests;

public class IncrementalDiagnosticReporterTests
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
    public async Task IncrementalDiagnosticReporter_ShouldHandleConcurrentAccess()
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

        await Task.WhenAll(tasks);

        // Assert
        var diagnostics = reporter.GetDiagnostics();
        Assert.Equal(numThreads * reportsPerThread, diagnostics.Count);
    }

    private static Diagnostic CreateTestDiagnostic(DiagnosticDescriptor descriptor, params object[] messageArgs)
    {
        return Diagnostic.Create(descriptor, Location.None, messageArgs);
    }
}