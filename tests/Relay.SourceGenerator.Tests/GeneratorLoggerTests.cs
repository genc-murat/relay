using Microsoft.CodeAnalysis;
using Xunit;
using System.Linq;
using Relay.SourceGenerator.Diagnostics;

namespace Relay.SourceGenerator.Tests;

public class GeneratorLoggerTests
{
    [Fact]
    public void LogDebug_ShouldReportDebugDiagnostic()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var message = "Debug message";

        // Act
        GeneratorLogger.LogDebug(reporter, message);

        // Assert
        Assert.Single(reporter.Diagnostics);
        var diagnostic = reporter.Diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.Debug.Id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Contains(message, diagnostic.GetMessage());
        Assert.Equal(Location.None, diagnostic.Location);
    }

    [Fact]
    public void LogInfo_ShouldReportInfoDiagnostic()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var message = "Informational message";

        // Act
        GeneratorLogger.LogInfo(reporter, message);

        // Assert
        Assert.Single(reporter.Diagnostics);
        var diagnostic = reporter.Diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.Info.Id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Contains(message, diagnostic.GetMessage());
        Assert.Equal(Location.None, diagnostic.Location);
    }

    [Fact]
    public void LogWarning_ShouldReportWarningDiagnostic()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var message = "Warning message";

        // Act
        GeneratorLogger.LogWarning(reporter, message);

        // Assert
        Assert.Single(reporter.Diagnostics);
        var diagnostic = reporter.Diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.Info.Id, diagnostic.Id); // Currently uses Info
        Assert.Contains(message, diagnostic.GetMessage());
        Assert.Equal(Location.None, diagnostic.Location);
    }

    [Fact]
    public void LogError_ShouldReportErrorDiagnostic()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var message = "Error message";

        // Act
        GeneratorLogger.LogError(reporter, message);

        // Assert
        Assert.Single(reporter.Diagnostics);
        var diagnostic = reporter.Diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.GeneratorError.Id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains(message, diagnostic.GetMessage());
        Assert.Equal(Location.None, diagnostic.Location);
    }

    [Fact]
    public void LogPerformance_ShouldReportPerformanceMetric()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var operation = "Handler Discovery";
        var elapsedMs = 150L;

        // Act
        GeneratorLogger.LogPerformance(reporter, operation, elapsedMs);

        // Assert
        Assert.Single(reporter.Diagnostics);
        var diagnostic = reporter.Diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.Debug.Id, diagnostic.Id);
        Assert.Contains("Generator performance", diagnostic.GetMessage());
        Assert.Contains(operation, diagnostic.GetMessage());
        Assert.Contains($"{elapsedMs}ms", diagnostic.GetMessage());
        Assert.Equal(Location.None, diagnostic.Location);
    }

    [Fact]
    public void ReportDiagnostic_ShouldReportWithSpecificDescriptor()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var descriptor = DiagnosticDescriptors.DuplicateHandler;
        var messageArgs = new object[] { "MyRequest", "MyResponse" };

        // Act
        GeneratorLogger.ReportDiagnostic(reporter, descriptor, null, messageArgs);

        // Assert
        Assert.Single(reporter.Diagnostics);
        var diagnostic = reporter.Diagnostics[0];
        Assert.Equal(descriptor.Id, diagnostic.Id);
        Assert.Contains("MyRequest", diagnostic.GetMessage());
        Assert.Contains("MyResponse", diagnostic.GetMessage());
        Assert.Equal(Location.None, diagnostic.Location);
    }

    [Fact]
    public void ReportDiagnostic_ShouldUseProvidedLocation()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var descriptor = DiagnosticDescriptors.Info;
        var location = CreateTestLocation();

        // Act
        GeneratorLogger.ReportDiagnostic(reporter, descriptor, location, "Test message");

        // Assert
        Assert.Single(reporter.Diagnostics);
        var diagnostic = reporter.Diagnostics[0];
        Assert.Equal(location, diagnostic.Location);
        Assert.NotEqual(Location.None, diagnostic.Location);
    }

    [Fact]
    public void LogDebug_ShouldHandleEmptyMessage()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();

        // Act
        GeneratorLogger.LogDebug(reporter, string.Empty);

        // Assert
        Assert.Single(reporter.Diagnostics);
        Assert.NotNull(reporter.Diagnostics[0].GetMessage());
    }

    [Fact]
    public void LogInfo_ShouldHandleEmptyMessage()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();

        // Act
        GeneratorLogger.LogInfo(reporter, string.Empty);

        // Assert
        Assert.Single(reporter.Diagnostics);
        Assert.NotNull(reporter.Diagnostics[0].GetMessage());
    }

    [Fact]
    public void LogError_ShouldHandleEmptyMessage()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();

        // Act
        GeneratorLogger.LogError(reporter, string.Empty);

        // Assert
        Assert.Single(reporter.Diagnostics);
        Assert.NotNull(reporter.Diagnostics[0].GetMessage());
    }

    [Fact]
    public void LogPerformance_ShouldHandleZeroElapsedTime()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();

        // Act
        GeneratorLogger.LogPerformance(reporter, "Quick Operation", 0L);

        // Assert
        Assert.Single(reporter.Diagnostics);
        Assert.Contains("0ms", reporter.Diagnostics[0].GetMessage());
    }

    [Fact]
    public void LogPerformance_ShouldHandleLargeElapsedTime()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var largeTime = 999999L;

        // Act
        GeneratorLogger.LogPerformance(reporter, "Slow Operation", largeTime);

        // Assert
        Assert.Single(reporter.Diagnostics);
        Assert.Contains($"{largeTime}ms", reporter.Diagnostics[0].GetMessage());
    }

    [Fact]
    public void ReportDiagnostic_ShouldHandleNoMessageArgs()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var descriptor = DiagnosticDescriptors.NoHandlersFound;

        // Act
        GeneratorLogger.ReportDiagnostic(reporter, descriptor, null);

        // Assert
        Assert.Single(reporter.Diagnostics);
        Assert.Equal(descriptor.Id, reporter.Diagnostics[0].Id);
    }

    [Fact]
    public void ReportDiagnostic_ShouldHandleMultipleMessageArgs()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var descriptor = DiagnosticDescriptors.DuplicateHandler;
        var args = new object[] { "Request1", "Response1" };

        // Act
        GeneratorLogger.ReportDiagnostic(reporter, descriptor, null, args);

        // Assert
        Assert.Single(reporter.Diagnostics);
        var message = reporter.Diagnostics[0].GetMessage();
        Assert.Contains("Request1", message);
        Assert.Contains("Response1", message);
    }

    [Fact]
    public void LogDebug_ShouldAllowMultipleCallsToSameReporter()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();

        // Act
        GeneratorLogger.LogDebug(reporter, "Message 1");
        GeneratorLogger.LogDebug(reporter, "Message 2");
        GeneratorLogger.LogDebug(reporter, "Message 3");

        // Assert
        Assert.Equal(3, reporter.Diagnostics.Count);
        Assert.Contains("Message 1", reporter.Diagnostics[0].GetMessage());
        Assert.Contains("Message 2", reporter.Diagnostics[1].GetMessage());
        Assert.Contains("Message 3", reporter.Diagnostics[2].GetMessage());
    }

    [Fact]
    public void MixedLogCalls_ShouldReportAllDiagnostics()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();

        // Act
        GeneratorLogger.LogDebug(reporter, "Debug");
        GeneratorLogger.LogInfo(reporter, "Info");
        GeneratorLogger.LogWarning(reporter, "Warning");
        GeneratorLogger.LogError(reporter, "Error");
        GeneratorLogger.LogPerformance(reporter, "Operation", 100L);

        // Assert
        Assert.Equal(5, reporter.Diagnostics.Count);
        Assert.Contains(reporter.Diagnostics, d => d.Id == DiagnosticDescriptors.Debug.Id);
        Assert.Contains(reporter.Diagnostics, d => d.Id == DiagnosticDescriptors.Info.Id);
        Assert.Contains(reporter.Diagnostics, d => d.Id == DiagnosticDescriptors.GeneratorError.Id);
    }

    [Fact]
    public void LogDebug_ShouldHandleSpecialCharactersInMessage()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var message = "Message with special chars: <>&\"'";

        // Act
        GeneratorLogger.LogDebug(reporter, message);

        // Assert
        Assert.Single(reporter.Diagnostics);
        Assert.Contains(message, reporter.Diagnostics[0].GetMessage());
    }

    [Fact]
    public void LogInfo_ShouldHandleMultilineMessage()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var message = "Line 1\nLine 2\nLine 3";

        // Act
        GeneratorLogger.LogInfo(reporter, message);

        // Assert
        Assert.Single(reporter.Diagnostics);
        Assert.Contains(message, reporter.Diagnostics[0].GetMessage());
    }

    [Fact]
    public void LogError_ShouldHandleLongMessage()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var message = new string('A', 1000);

        // Act
        GeneratorLogger.LogError(reporter, message);

        // Assert
        Assert.Single(reporter.Diagnostics);
        Assert.Contains(message, reporter.Diagnostics[0].GetMessage());
    }

    [Fact]
    public void LogPerformance_ShouldFormatMessageCorrectly()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var operation = "Code Generation";
        var elapsed = 250L;

        // Act
        GeneratorLogger.LogPerformance(reporter, operation, elapsed);

        // Assert
        Assert.Single(reporter.Diagnostics);
        var message = reporter.Diagnostics[0].GetMessage();
        Assert.StartsWith("Generator performance:", message);
        Assert.Contains(operation, message);
        Assert.Contains("took", message);
        Assert.EndsWith("ms", message);
    }

    [Fact]
    public void ReportDiagnostic_ShouldUseLocationNoneWhenLocationIsNull()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var descriptor = DiagnosticDescriptors.Info;

        // Act
        GeneratorLogger.ReportDiagnostic(reporter, descriptor, null, "Message");

        // Assert
        Assert.Single(reporter.Diagnostics);
        Assert.Equal(Location.None, reporter.Diagnostics[0].Location);
    }

    private Location CreateTestLocation()
    {
        var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("class Test { }");
        var span = new Microsoft.CodeAnalysis.Text.TextSpan(0, 5);
        return Location.Create(syntaxTree, span);
    }

    // Test helper class
    private class TestDiagnosticReporter : IDiagnosticReporter
    {
        public System.Collections.Generic.List<Diagnostic> Diagnostics { get; } = new();

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            Diagnostics.Add(diagnostic);
        }
    }
}
