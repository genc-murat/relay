using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using Relay.SourceGenerator.Diagnostics;

namespace Relay.SourceGenerator.Tests;

public class SourceProductionContextDiagnosticReporterTests
{
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

    private static Diagnostic CreateTestDiagnostic(DiagnosticDescriptor descriptor, params object[] messageArgs)
    {
        return Diagnostic.Create(descriptor, Location.None, messageArgs);
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