using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Relay.SourceGenerator.Diagnostics;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for new diagnostic reporter extension methods added in Task 2.
/// </summary>
public class DiagnosticReporterExtensionsNewTests
{
    #region ReportInvalidConfigurationValue Tests

    [Fact]
    public void ReportInvalidConfigurationValue_ShouldCreateCorrectDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportInvalidConfigurationValue(location, "MaxRetries", "invalid", "Value must be a positive integer");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal("RELAY_GEN_213", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("MaxRetries", diagnostic.GetMessage());
        Assert.Contains("invalid", diagnostic.GetMessage());
        Assert.Contains("Value must be a positive integer", diagnostic.GetMessage());
    }

    [Fact]
    public void ReportInvalidConfigurationValue_WithEmptyPropertyName_ShouldStillCreateDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportInvalidConfigurationValue(location, "", "value", "reason");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal("RELAY_GEN_213", diagnostics[0].Id);
    }

    [Fact]
    public void ReportInvalidConfigurationValue_WithNullValue_ShouldHandleGracefully()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportInvalidConfigurationValue(location, "Property", null!, "reason");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal("RELAY_GEN_213", diagnostics[0].Id);
    }

    [Fact]
    public void ReportInvalidConfigurationValue_WithLongReason_ShouldIncludeFullReason()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;
        var longReason = new string('A', 500);

        // Act
        reporter.ReportInvalidConfigurationValue(location, "Property", "value", longReason);
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Contains(longReason, diagnostics[0].GetMessage());
    }

    [Fact]
    public void ReportInvalidConfigurationValue_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportInvalidConfigurationValue(location, "Config<T>", "value\"with'quotes", "Reason: <>&");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var message = diagnostics[0].GetMessage();
        Assert.Contains("Config<T>", message);
        Assert.Contains("value\"with'quotes", message);
        Assert.Contains("Reason: <>&", message);
    }

    #endregion

    #region ReportMissingRequiredAttribute Tests

    [Fact]
    public void ReportMissingRequiredAttribute_ShouldCreateCorrectDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportMissingRequiredAttribute(location, "HandleAsync", "Handle");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal("RELAY_GEN_214", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("HandleAsync", diagnostic.GetMessage());
        Assert.Contains("Handle", diagnostic.GetMessage());
    }

    [Fact]
    public void ReportMissingRequiredAttribute_WithMultipleAttributes_ShouldIncludeAttributeName()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportMissingRequiredAttribute(location, "ProcessRequest", "HandleAttribute");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Contains("HandleAttribute", diagnostics[0].GetMessage());
    }

    [Fact]
    public void ReportMissingRequiredAttribute_WithEmptyMethodName_ShouldStillCreateDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportMissingRequiredAttribute(location, "", "Handle");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal("RELAY_GEN_214", diagnostics[0].Id);
    }

    [Fact]
    public void ReportMissingRequiredAttribute_WithNullAttributeName_ShouldHandleGracefully()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportMissingRequiredAttribute(location, "Method", null!);
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal("RELAY_GEN_214", diagnostics[0].Id);
    }

    [Fact]
    public void ReportMissingRequiredAttribute_WithGenericMethodName_ShouldHandleCorrectly()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportMissingRequiredAttribute(location, "HandleAsync<TRequest, TResponse>", "Handle");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Contains("HandleAsync<TRequest, TResponse>", diagnostics[0].GetMessage());
    }

    #endregion

    #region ReportObsoleteHandlerPattern Tests

    [Fact]
    public void ReportObsoleteHandlerPattern_ShouldCreateCorrectDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportObsoleteHandlerPattern(
            location,
            "MyHandler",
            "Synchronous handler methods",
            "Use async Task-based handlers");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal("RELAY_GEN_215", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("MyHandler", diagnostic.GetMessage());
        Assert.Contains("Synchronous handler methods", diagnostic.GetMessage());
        Assert.Contains("Use async Task-based handlers", diagnostic.GetMessage());
    }

    [Fact]
    public void ReportObsoleteHandlerPattern_WithDetailedRecommendation_ShouldIncludeAllDetails()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportObsoleteHandlerPattern(
            location,
            "LegacyHandler",
            "Using IRequest<T> interface",
            "Migrate to IRequest<TRequest, TResponse> for better type safety");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var message = diagnostics[0].GetMessage();
        Assert.Contains("LegacyHandler", message);
        Assert.Contains("IRequest<T>", message);
        Assert.Contains("IRequest<TRequest, TResponse>", message);
    }

    [Fact]
    public void ReportObsoleteHandlerPattern_WithEmptyHandlerName_ShouldStillCreateDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportObsoleteHandlerPattern(location, "", "pattern", "recommendation");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal("RELAY_GEN_215", diagnostics[0].Id);
    }

    [Fact]
    public void ReportObsoleteHandlerPattern_WithNullPattern_ShouldHandleGracefully()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportObsoleteHandlerPattern(location, "Handler", null!, "recommendation");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal("RELAY_GEN_215", diagnostics[0].Id);
    }

    [Fact]
    public void ReportObsoleteHandlerPattern_WithMultilineRecommendation_ShouldHandleCorrectly()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;
        var multilineRecommendation = "Step 1: Update interface\nStep 2: Modify implementation\nStep 3: Test changes";

        // Act
        reporter.ReportObsoleteHandlerPattern(location, "Handler", "old pattern", multilineRecommendation);
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Contains(multilineRecommendation, diagnostics[0].GetMessage());
    }

    #endregion

    #region ReportPerformanceBottleneck Tests

    [Fact]
    public void ReportPerformanceBottleneck_ShouldCreateCorrectDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportPerformanceBottleneck(
            location,
            "SlowHandler",
            "Synchronous database calls in loop",
            "Use async batch operations");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal("RELAY_GEN_216", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("SlowHandler", diagnostic.GetMessage());
        Assert.Contains("Synchronous database calls in loop", diagnostic.GetMessage());
        Assert.Contains("Use async batch operations", diagnostic.GetMessage());
    }

    [Fact]
    public void ReportPerformanceBottleneck_WithDetailedSuggestion_ShouldIncludeAllDetails()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportPerformanceBottleneck(
            location,
            "DataHandler",
            "N+1 query problem detected",
            "Use Include() or eager loading to fetch related entities in a single query");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var message = diagnostics[0].GetMessage();
        Assert.Contains("DataHandler", message);
        Assert.Contains("N+1 query problem", message);
        Assert.Contains("Include()", message);
    }

    [Fact]
    public void ReportPerformanceBottleneck_WithEmptyHandlerName_ShouldStillCreateDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportPerformanceBottleneck(location, "", "issue", "suggestion");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal("RELAY_GEN_216", diagnostics[0].Id);
    }

    [Fact]
    public void ReportPerformanceBottleneck_WithNullIssue_ShouldHandleGracefully()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportPerformanceBottleneck(location, "Handler", null!, "suggestion");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal("RELAY_GEN_216", diagnostics[0].Id);
    }

    [Fact]
    public void ReportPerformanceBottleneck_WithCodeSnippetInSuggestion_ShouldHandleCorrectly()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;
        var suggestionWithCode = "Replace: for(var i=0; i<items.Count; i++) with: await Task.WhenAll(items.Select(x => ProcessAsync(x)))";

        // Act
        reporter.ReportPerformanceBottleneck(location, "Handler", "Sequential processing", suggestionWithCode);
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Contains(suggestionWithCode, diagnostics[0].GetMessage());
    }

    #endregion

    #region ReportMissingReference Tests

    [Fact]
    public void ReportMissingReference_ShouldCreateCorrectDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();

        // Act
        reporter.ReportMissingReference();
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal("RELAY_GEN_004", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(Location.None, diagnostic.Location);
    }

    [Fact]
    public void ReportMissingReference_WithLocation_ShouldUseProvidedLocation()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = CreateTestLocation();

        // Act
        reporter.ReportMissingReference(location);
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal(location, diagnostics[0].Location);
    }

    [Fact]
    public void ReportMissingReference_MultipleCallsShouldCreateMultipleDiagnostics()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();

        // Act
        reporter.ReportMissingReference();
        reporter.ReportMissingReference();
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Equal(2, diagnostics.Count);
        Assert.All(diagnostics, d => Assert.Equal("RELAY_GEN_004", d.Id));
    }

    #endregion

    #region ReportInvalidSignature Tests

    [Fact]
    public void ReportInvalidSignature_ShouldCreateCorrectDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportInvalidSignature(location, "handler", "HandleAsync", "Missing CancellationToken parameter");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal("RELAY_GEN_002", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("handler", diagnostic.GetMessage());
        Assert.Contains("HandleAsync", diagnostic.GetMessage());
        Assert.Contains("Missing CancellationToken parameter", diagnostic.GetMessage());
    }

    [Fact]
    public void ReportInvalidSignature_WithPipelineType_ShouldIncludeType()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportInvalidSignature(location, "pipeline", "ProcessAsync", "Invalid delegate parameter");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Contains("pipeline", diagnostics[0].GetMessage());
    }

    [Fact]
    public void ReportInvalidSignature_WithEmptyIssue_ShouldStillCreateDiagnostic()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportInvalidSignature(location, "handler", "Method", "");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal("RELAY_GEN_002", diagnostics[0].Id);
    }

    #endregion

    #region ReportConfiguredDiagnostic Tests

    [Fact]
    public void ReportConfiguredDiagnostic_WithoutConfiguration_ShouldReportNormally()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;
        var descriptor = DiagnosticDescriptors.InvalidHandlerSignature;

        // Act
        reporter.ReportConfiguredDiagnostic(descriptor, location, null, "handler", "signature");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal(descriptor.Id, diagnostics[0].Id);
        Assert.Equal(descriptor.DefaultSeverity, diagnostics[0].Severity);
    }

    [Fact]
    public void ReportConfiguredDiagnostic_WithSuppressedDiagnostic_ShouldNotReport()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;
        var descriptor = DiagnosticDescriptors.PrivateHandler;
        var configuration = CreateConfigurationWithSuppression("RELAY_GEN_106");

        // Act
        reporter.ReportConfiguredDiagnostic(descriptor, location, configuration, "MyHandler");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void ReportConfiguredDiagnostic_WithSeverityOverride_ShouldApplyOverride()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;
        var descriptor = DiagnosticDescriptors.HandlerMissingCancellationToken; // Default: Warning
        var configuration = CreateConfigurationWithSeverityOverride("RELAY_GEN_207", DiagnosticSeverity.Error);

        // Act
        reporter.ReportConfiguredDiagnostic(descriptor, location, configuration, "HandleAsync");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticSeverity.Error, diagnostics[0].Severity);
    }

    [Fact]
    public void ReportConfiguredDiagnostic_WithMultipleArguments_ShouldFormatCorrectly()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;
        var descriptor = DiagnosticDescriptors.InvalidConfigurationValue;

        // Act
        reporter.ReportConfiguredDiagnostic(descriptor, location, null, "Property", "Value", "Reason");
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        var message = diagnostics[0].GetMessage();
        Assert.Contains("Property", message);
        Assert.Contains("Value", message);
        Assert.Contains("Reason", message);
    }

    [Fact]
    public void ReportConfiguredDiagnostic_WithNoArguments_ShouldHandleGracefully()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;
        var descriptor = DiagnosticDescriptors.NoHandlersFound;

        // Act
        reporter.ReportConfiguredDiagnostic(descriptor, location, null);
        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal(descriptor.Id, diagnostics[0].Id);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void NewExtensionMethods_ShouldWorkWithExistingMethods()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act - Mix old and new methods
        reporter.ReportError("Old method");
        reporter.ReportInvalidConfigurationValue(location, "Prop", "Val", "Reason");
        reporter.ReportDuplicateHandler(location, "Req", "Res");
        reporter.ReportMissingRequiredAttribute(location, "Method", "Attr");
        reporter.ReportInfo("Info");
        reporter.ReportObsoleteHandlerPattern(location, "Handler", "old", "new");

        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Equal(6, diagnostics.Count);
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_001");
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_213");
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_003");
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_214");
        Assert.Contains(diagnostics, d => d.Id == "RELAY_INFO");
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_215");
    }

    [Fact]
    public void AllNewDiagnostics_ShouldHaveCorrectSeverity()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act
        reporter.ReportInvalidConfigurationValue(location, "P", "V", "R");
        reporter.ReportMissingRequiredAttribute(location, "M", "A");
        reporter.ReportObsoleteHandlerPattern(location, "H", "O", "N");
        reporter.ReportPerformanceBottleneck(location, "H", "I", "S");

        var diagnostics = reporter.GetDiagnostics();

        // Assert
        Assert.Equal(4, diagnostics.Count);
        Assert.Equal(DiagnosticSeverity.Error, diagnostics.First(d => d.Id == "RELAY_GEN_213").Severity);
        Assert.Equal(DiagnosticSeverity.Error, diagnostics.First(d => d.Id == "RELAY_GEN_214").Severity);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostics.First(d => d.Id == "RELAY_GEN_215").Severity);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostics.First(d => d.Id == "RELAY_GEN_216").Severity);
    }

    [Fact]
    public void NewExtensionMethods_ShouldHandleNullReporter_Gracefully()
    {
        // This test verifies that extension methods don't throw on null reporter
        // In practice, this shouldn't happen, but it's good defensive programming

        // Arrange
        IDiagnosticReporter? reporter = null;
        var location = Location.None;

        // Act & Assert - Should throw ArgumentNullException
        Assert.Throws<System.NullReferenceException>(() =>
            reporter!.ReportInvalidConfigurationValue(location, "P", "V", "R"));
    }

    [Fact]
    public void AllNewDiagnostics_ShouldBeReportedCorrectly()
    {
        // Arrange
        var reporter = new IncrementalDiagnosticReporter();
        var location = Location.None;

        // Act - Report all new diagnostics
        reporter.ReportInvalidConfigurationValue(location, "P1", "V1", "R1");
        reporter.ReportMissingRequiredAttribute(location, "M1", "A1");
        reporter.ReportObsoleteHandlerPattern(location, "H1", "O1", "N1");
        reporter.ReportPerformanceBottleneck(location, "H2", "I2", "S2");

        var diagnostics = reporter.GetDiagnostics();

        // Assert - All diagnostics should be present
        Assert.Equal(4, diagnostics.Count);
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_213");
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_214");
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_215");
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_216");
    }

    #endregion

    #region Helper Methods

    private static Location CreateTestLocation()
    {
        var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("class Test { }");
        var span = new TextSpan(0, 5);
        return Location.Create(syntaxTree, span);
    }

    private static DiagnosticSeverityConfiguration CreateConfigurationWithSuppression(string diagnosticId)
    {
        var config = new DiagnosticSeverityConfiguration();
        var suppressedField = typeof(DiagnosticSeverityConfiguration)
            .GetField("_suppressedDiagnostics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var suppressedSet = (System.Collections.Generic.HashSet<string>)suppressedField!.GetValue(config)!;
        suppressedSet.Add(diagnosticId);
        return config;
    }

    private static DiagnosticSeverityConfiguration CreateConfigurationWithSeverityOverride(string diagnosticId, DiagnosticSeverity severity)
    {
        var config = new DiagnosticSeverityConfiguration();
        var overridesField = typeof(DiagnosticSeverityConfiguration)
            .GetField("_severityOverrides", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var overridesDict = (System.Collections.Generic.Dictionary<string, DiagnosticSeverity>)overridesField!.GetValue(config)!;
        overridesDict[diagnosticId] = severity;
        return config;
    }

    #endregion
}
