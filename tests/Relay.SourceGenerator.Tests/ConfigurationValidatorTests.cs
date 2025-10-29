using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Diagnostics;
using Relay.SourceGenerator.Generators;
using Relay.SourceGenerator.Validation;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for ConfigurationValidator to verify configuration validation logic.
/// </summary>
public class ConfigurationValidatorTests
{
    private class TestDiagnosticReporter : IDiagnosticReporter
    {
        public List<Diagnostic> Diagnostics { get; } = new();

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            Diagnostics.Add(diagnostic);
        }
    }

    [Fact]
    public void ValidateGenerationOptions_ValidOptions_NoDiagnostics()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var validator = new ConfigurationValidator(reporter);
        var options = new GenerationOptions
        {
            MaxDegreeOfParallelism = 4,
            CustomNamespace = "Valid.Namespace",
            EnableDIGeneration = true
        };

        // Act
        validator.ValidateGenerationOptions(options);

        // Assert
        Assert.Empty(reporter.Diagnostics);
    }

    [Fact]
    public void ValidateGenerationOptions_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var validator = new ConfigurationValidator(reporter);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => validator.ValidateGenerationOptions(null!));
    }

    [Fact]
    public void ValidateGenerationOptions_MaxDegreeOfParallelismTooLow_ReportsDiagnostic()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var validator = new ConfigurationValidator(reporter);
        var options = new GenerationOptions
        {
            MaxDegreeOfParallelism = 0
        };

        // Act
        validator.ValidateGenerationOptions(options);

        // Assert
        Assert.Single(reporter.Diagnostics);
        Assert.Contains("RelayMaxDegreeOfParallelism", reporter.Diagnostics[0].GetMessage());
    }

    [Fact]
    public void ValidateGenerationOptions_MaxDegreeOfParallelismTooHigh_ReportsDiagnostic()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var validator = new ConfigurationValidator(reporter);
        var options = new GenerationOptions
        {
            MaxDegreeOfParallelism = 100
        };

        // Act
        validator.ValidateGenerationOptions(options);

        // Assert
        Assert.Single(reporter.Diagnostics);
        Assert.Contains("RelayMaxDegreeOfParallelism", reporter.Diagnostics[0].GetMessage());
    }

    [Fact]
    public void ValidateGenerationOptions_MaxDegreeOfParallelismValid_NoDiagnostics()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var validator = new ConfigurationValidator(reporter);
        var options = new GenerationOptions
        {
            MaxDegreeOfParallelism = 8,
            EnableDIGeneration = true
        };

        // Act
        validator.ValidateGenerationOptions(options);

        // Assert
        Assert.Empty(reporter.Diagnostics);
    }

    [Fact]
    public void ValidateGenerationOptions_InvalidNamespace_ReportsDiagnostic()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var validator = new ConfigurationValidator(reporter);
        var options = new GenerationOptions
        {
            CustomNamespace = "123Invalid",
            EnableDIGeneration = true
        };

        // Act
        validator.ValidateGenerationOptions(options);

        // Assert
        Assert.Single(reporter.Diagnostics);
        Assert.Contains("RelayCustomNamespace", reporter.Diagnostics[0].GetMessage());
    }

    [Fact]
    public void ValidateGenerationOptions_NamespaceWithInvalidCharacters_ReportsDiagnostic()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var validator = new ConfigurationValidator(reporter);
        var options = new GenerationOptions
        {
            CustomNamespace = "Invalid-Namespace",
            EnableDIGeneration = true
        };

        // Act
        validator.ValidateGenerationOptions(options);

        // Assert
        Assert.Single(reporter.Diagnostics);
        Assert.Contains("RelayCustomNamespace", reporter.Diagnostics[0].GetMessage());
    }

    [Fact]
    public void ValidateGenerationOptions_ValidNamespace_NoDiagnostics()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var validator = new ConfigurationValidator(reporter);
        var options = new GenerationOptions
        {
            CustomNamespace = "MyCompany.MyApp.Generated",
            EnableDIGeneration = true
        };

        // Act
        validator.ValidateGenerationOptions(options);

        // Assert
        Assert.Empty(reporter.Diagnostics);
    }

    [Fact]
    public void ValidateGenerationOptions_NamespaceWithUnderscore_NoDiagnostics()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var validator = new ConfigurationValidator(reporter);
        var options = new GenerationOptions
        {
            CustomNamespace = "My_Company.My_App",
            EnableDIGeneration = true
        };

        // Act
        validator.ValidateGenerationOptions(options);

        // Assert
        Assert.Empty(reporter.Diagnostics);
    }

    [Fact]
    public void ValidateGenerationOptions_EmptyNamespace_NoDiagnostics()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var validator = new ConfigurationValidator(reporter);
        var options = new GenerationOptions
        {
            CustomNamespace = null,
            EnableDIGeneration = true
        };

        // Act
        validator.ValidateGenerationOptions(options);

        // Assert
        Assert.Empty(reporter.Diagnostics);
    }

    [Fact]
    public void ValidateGenerationOptions_AllGeneratorsDisabled_ReportsDiagnostic()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var validator = new ConfigurationValidator(reporter);
        var options = new GenerationOptions
        {
            EnableDIGeneration = false,
            EnableHandlerRegistry = false,
            EnableOptimizedDispatcher = false,
            EnableNotificationDispatcher = false,
            EnablePipelineRegistry = false,
            EnableEndpointMetadata = false
        };

        // Act
        validator.ValidateGenerationOptions(options);

        // Assert
        Assert.Single(reporter.Diagnostics);
        Assert.Contains("All Generators", reporter.Diagnostics[0].GetMessage());
    }

    [Fact]
    public void ValidateGenerationOptions_AtLeastOneGeneratorEnabled_NoDiagnostics()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var validator = new ConfigurationValidator(reporter);
        var options = new GenerationOptions
        {
            EnableDIGeneration = true,
            EnableHandlerRegistry = false,
            EnableOptimizedDispatcher = false,
            EnableNotificationDispatcher = false,
            EnablePipelineRegistry = false,
            EnableEndpointMetadata = false
        };

        // Act
        validator.ValidateGenerationOptions(options);

        // Assert
        Assert.Empty(reporter.Diagnostics);
    }

    [Fact]
    public void ValidateGenerationOptions_MultipleInvalidValues_ReportsMultipleDiagnostics()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();
        var validator = new ConfigurationValidator(reporter);
        var options = new GenerationOptions
        {
            MaxDegreeOfParallelism = 0,
            CustomNamespace = "123Invalid",
            EnableDIGeneration = false,
            EnableHandlerRegistry = false,
            EnableOptimizedDispatcher = false,
            EnableNotificationDispatcher = false,
            EnablePipelineRegistry = false,
            EnableEndpointMetadata = false
        };

        // Act
        validator.ValidateGenerationOptions(options);

        // Assert
        Assert.True(reporter.Diagnostics.Count >= 3); // At least 3 errors
    }

    [Fact]
    public void Constructor_NullReporter_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ConfigurationValidator(null!));
    }
}
