using Microsoft.CodeAnalysis;
using Xunit;
using FluentAssertions;
using System.Linq;

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
        reporter.Diagnostics.Should().ContainSingle();
        var diagnostic = reporter.Diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.Debug.Id);
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Info);
        diagnostic.GetMessage().Should().Contain(message);
        diagnostic.Location.Should().Be(Location.None);
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
        reporter.Diagnostics.Should().ContainSingle();
        var diagnostic = reporter.Diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.Info.Id);
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Info);
        diagnostic.GetMessage().Should().Contain(message);
        diagnostic.Location.Should().Be(Location.None);
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
        reporter.Diagnostics.Should().ContainSingle();
        var diagnostic = reporter.Diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.Info.Id); // Currently uses Info
        diagnostic.GetMessage().Should().Contain(message);
        diagnostic.Location.Should().Be(Location.None);
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
        reporter.Diagnostics.Should().ContainSingle();
        var diagnostic = reporter.Diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.GeneratorError.Id);
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
        diagnostic.GetMessage().Should().Contain(message);
        diagnostic.Location.Should().Be(Location.None);
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
        reporter.Diagnostics.Should().ContainSingle();
        var diagnostic = reporter.Diagnostics[0];
        diagnostic.Id.Should().Be(DiagnosticDescriptors.Debug.Id);
        diagnostic.GetMessage().Should().Contain("Generator performance");
        diagnostic.GetMessage().Should().Contain(operation);
        diagnostic.GetMessage().Should().Contain($"{elapsedMs}ms");
        diagnostic.Location.Should().Be(Location.None);
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
        reporter.Diagnostics.Should().ContainSingle();
        var diagnostic = reporter.Diagnostics[0];
        diagnostic.Id.Should().Be(descriptor.Id);
        diagnostic.GetMessage().Should().Contain("MyRequest");
        diagnostic.GetMessage().Should().Contain("MyResponse");
        diagnostic.Location.Should().Be(Location.None);
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
        reporter.Diagnostics.Should().ContainSingle();
        var diagnostic = reporter.Diagnostics[0];
        diagnostic.Location.Should().Be(location);
        diagnostic.Location.Should().NotBe(Location.None);
    }

    [Fact]
    public void LogDebug_ShouldHandleEmptyMessage()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();

        // Act
        GeneratorLogger.LogDebug(reporter, string.Empty);

        // Assert
        reporter.Diagnostics.Should().ContainSingle();
        reporter.Diagnostics[0].GetMessage().Should().NotBeNull();
    }

    [Fact]
    public void LogInfo_ShouldHandleEmptyMessage()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();

        // Act
        GeneratorLogger.LogInfo(reporter, string.Empty);

        // Assert
        reporter.Diagnostics.Should().ContainSingle();
        reporter.Diagnostics[0].GetMessage().Should().NotBeNull();
    }

    [Fact]
    public void LogError_ShouldHandleEmptyMessage()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();

        // Act
        GeneratorLogger.LogError(reporter, string.Empty);

        // Assert
        reporter.Diagnostics.Should().ContainSingle();
        reporter.Diagnostics[0].GetMessage().Should().NotBeNull();
    }

    [Fact]
    public void LogPerformance_ShouldHandleZeroElapsedTime()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();

        // Act
        GeneratorLogger.LogPerformance(reporter, "Quick Operation", 0L);

        // Assert
        reporter.Diagnostics.Should().ContainSingle();
        reporter.Diagnostics[0].GetMessage().Should().Contain("0ms");
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
        reporter.Diagnostics.Should().ContainSingle();
        reporter.Diagnostics[0].GetMessage().Should().Contain($"{largeTime}ms");
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
        reporter.Diagnostics.Should().ContainSingle();
        reporter.Diagnostics[0].Id.Should().Be(descriptor.Id);
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
        reporter.Diagnostics.Should().ContainSingle();
        var message = reporter.Diagnostics[0].GetMessage();
        message.Should().Contain("Request1");
        message.Should().Contain("Response1");
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
        reporter.Diagnostics.Should().HaveCount(3);
        reporter.Diagnostics[0].GetMessage().Should().Contain("Message 1");
        reporter.Diagnostics[1].GetMessage().Should().Contain("Message 2");
        reporter.Diagnostics[2].GetMessage().Should().Contain("Message 3");
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
        reporter.Diagnostics.Should().HaveCount(5);
        reporter.Diagnostics.Should().Contain(d => d.Id == DiagnosticDescriptors.Debug.Id);
        reporter.Diagnostics.Should().Contain(d => d.Id == DiagnosticDescriptors.Info.Id);
        reporter.Diagnostics.Should().Contain(d => d.Id == DiagnosticDescriptors.GeneratorError.Id);
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
        reporter.Diagnostics.Should().ContainSingle();
        reporter.Diagnostics[0].GetMessage().Should().Contain(message);
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
        reporter.Diagnostics.Should().ContainSingle();
        reporter.Diagnostics[0].GetMessage().Should().Contain(message);
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
        reporter.Diagnostics.Should().ContainSingle();
        reporter.Diagnostics[0].GetMessage().Should().Contain(message);
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
        reporter.Diagnostics.Should().ContainSingle();
        var message = reporter.Diagnostics[0].GetMessage();
        message.Should().StartWith("Generator performance:");
        message.Should().Contain(operation);
        message.Should().Contain("took");
        message.Should().EndWith("ms");
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
        reporter.Diagnostics.Should().ContainSingle();
        reporter.Diagnostics[0].Location.Should().Be(Location.None);
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
